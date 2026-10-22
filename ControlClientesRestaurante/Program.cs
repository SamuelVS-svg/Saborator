using Microsoft.AspNetCore.DataProtection;
using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;
using ControlClientesRestaurante.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuración local opcional (llaves de pruebas, etc.). Está en .gitignore: nunca se sube al repositorio.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

// ── Dónde escucha la app ──────────────────────────────────────────────────────
// · SABORATOR_SOLO_LOCAL=true: SOLO 127.0.0.1 (puerto PORT, o 5200 si no existe). Para publicar detrás de
//   un túnel como Tailscale Funnel.
// · Contenedor (Railway/Docker): existe PORT → 0.0.0.0:$PORT y 8080, como antes.
// · Equipo propio en producción, sin PORT ni SOLO_LOCAL: también SOLO 127.0.0.1, para que nadie de la
//   red local entre directamente. El puerto se cambia con SABORATOR_PUERTO (por defecto 5106).
var port = Environment.GetEnvironmentVariable("PORT");
var soloLocal = string.Equals(Environment.GetEnvironmentVariable("SABORATOR_SOLO_LOCAL"), "true", StringComparison.OrdinalIgnoreCase);
var enContenedor = !string.IsNullOrWhiteSpace(port) && !soloLocal;
if (soloLocal)
{
    builder.WebHost.UseUrls($"http://127.0.0.1:{(string.IsNullOrWhiteSpace(port) ? "5200" : port.Trim())}");
}
else if (enContenedor)
{
    var urls = new HashSet<string> { "http://0.0.0.0:8080", $"http://0.0.0.0:{port!.Trim()}" };
    builder.WebHost.UseUrls(string.Join(";", urls));
}
else if (!builder.Environment.IsDevelopment())
{
    var puertoLocal = Environment.GetEnvironmentVariable("SABORATOR_PUERTO");
    if (string.IsNullOrWhiteSpace(puertoLocal) || !int.TryParse(puertoLocal, out _)) puertoLocal = "5106";
    builder.WebHost.UseUrls($"http://127.0.0.1:{puertoLocal}");
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    if (enContenedor)
    {
        // El proxy de Railway no tiene una IP fija conocida
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
    // En el equipo propio se conservan los valores por defecto: solo se confía en las cabeceras
    // X-Forwarded-* que llegan desde 127.0.0.1 / ::1 (Tailscale), no desde cualquier origen.
});

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();

// Detectar si SQL Server está disponible o usar SQLite embebido automáticamente en la nube
var rawConnStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
bool useSqlite = false;
// SABORATOR_FORZAR_SQLITE=true usa SQLite aunque haya SQL Server (hosting gratuito o pruebas en Windows)
bool forzarSqlite = string.Equals(Environment.GetEnvironmentVariable("SABORATOR_FORZAR_SQLITE"), "true", StringComparison.OrdinalIgnoreCase);

if (forzarSqlite ||
    (!OperatingSystem.IsWindows() &&
    (string.IsNullOrWhiteSpace(rawConnStr) ||
     rawConnStr.Contains("SQLEXPRESS", StringComparison.OrdinalIgnoreCase) ||
     rawConnStr.Contains("Trusted_Connection=True", StringComparison.OrdinalIgnoreCase))))
{
    useSqlite = true;
    Console.WriteLine("[Saborator] Entorno Linux detectado sin cadena SQL Server remota; activando base de datos SQLite integrada.");
}
else if (!OperatingSystem.IsWindows())
{
    try
    {
        var csb = new SqlConnectionStringBuilder(rawConnStr) { ConnectTimeout = 5 };
        using var testConn = new SqlConnection(csb.ConnectionString);
        testConn.Open();
        Console.WriteLine("[Saborator] Conexión a SQL Server verificada exitosamente.");
    }
    catch (Exception ex)
    {
        useSqlite = true;
        Console.WriteLine($"[Saborator] No se pudo conectar a SQL Server ({ex.Message}). Activando SQLite de respaldo automático.");
    }
}

// SABORATOR_DB_PATH permite guardar la base SQLite en un volumen persistente (por ejemplo /data/saborator.db)
var sqliteDbPath = Environment.GetEnvironmentVariable("SABORATOR_DB_PATH");
if (string.IsNullOrWhiteSpace(sqliteDbPath))
{
    var carpetaBd = Environment.GetEnvironmentVariable("SABORATOR_DATA_DIR");
    sqliteDbPath = Path.Combine(string.IsNullOrWhiteSpace(carpetaBd) ? AppContext.BaseDirectory : carpetaBd, "saborator.db");
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(sqliteDbPath))!);
}
else
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(sqliteDbPath))!);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useSqlite)
    {
        options.UseSqlite($"Data Source={sqliteDbPath}");
    }
    else
    {
        options.UseSqlServer(
            rawConnStr,
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null));
    }
});

// Autenticación por Cookies (OWASP & Claims)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.LogoutPath = "/Login/Logout";
        options.AccessDeniedPath = "/Login/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Solo por HTTPS en producción (en local se permite HTTP para desarrollar)
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.Name = ".Saborator.Auth";

        // Expulsar de inmediato a cuentas desactivadas o eliminadas (no esperar a que caduque la cookie)
        options.Events.OnValidatePrincipal = async context =>
        {
            var nombre = context.Principal?.Identity?.Name;
            if (string.IsNullOrEmpty(nombre)) return;
            var repo = context.HttpContext.RequestServices.GetRequiredService<IUsuarioRepository>();
            var usuario = repo.ObtenerPorNombre(nombre);
            if (usuario == null || !usuario.Estado)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

// En producción todas las cookies viajan solo por HTTPS (Railway y Tailscale Funnel sirven HTTPS)
var cookiePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = cookiePolicy);
builder.Services.Configure<Microsoft.AspNetCore.Mvc.CookieTempDataProviderOptions>(options => options.Cookie.SecurePolicy = cookiePolicy);

// SABORATOR_DATA_DIR: carpeta persistente (volumen) donde se guardan las claves de cifrado de cookies y tokens.
// Sin esto, en un contenedor las claves se regeneran en cada reinicio y todos los usuarios pierden la sesión.
var carpetaDatos = Environment.GetEnvironmentVariable("SABORATOR_DATA_DIR");
if (!string.IsNullOrWhiteSpace(carpetaDatos))
{
    var carpetaClaves = Path.Combine(carpetaDatos, "keys");
    Directory.CreateDirectory(carpetaClaves);
    builder.Services.AddDataProtection()
        .SetApplicationName("Saborator")
        .PersistKeysToFileSystem(new DirectoryInfo(carpetaClaves));
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = cookiePolicy;
});

// Límite de intentos para inicio de sesión, registro y códigos OTP (fuerza bruta)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Formularios y APIs públicas (cupones, reseñas, reservas, pedidos, chat): 30 por minuto por IP
    options.AddPolicy("publico", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    // Reseñas públicas: 5 cada 10 minutos por IP (evita spam)
    options.AddPolicy("resenas", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueLimit = 0
            }));
    options.AddPolicy("autenticacion", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "{\"ok\":false,\"success\":false,\"mensaje\":\"Demasiados intentos. Espera un minuto e inténtalo de nuevo.\",\"error\":\"Demasiados intentos. Espera un minuto e inténtalo de nuevo.\"}", token);
    };
});

// Repositorios
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IFacturaRepository, FacturaRepository>();
builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<IMesaRepository, MesaRepository>();
builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IResenaRepository, ResenaRepository>();

// Servicios
builder.Services.AddScoped<ClienteService>();
builder.Services.AddSingleton<LimitadorIntentos>();
builder.Services.AddScoped<AutenticacionService>();
builder.Services.AddScoped<ProductoService>();
builder.Services.AddScoped<FacturaService>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<ProveedorService>();
builder.Services.AddScoped<MesaService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.AddScoped<ResenaService>();
builder.Services.AddScoped<CajaService>();
builder.Services.AddScoped<CuponService>();
builder.Services.AddScoped<AuditoriaService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient("TwilioClient");
builder.Services.AddScoped<ITwilioService, TwilioService>();
builder.Services.AddHttpClient("WompiClient");
builder.Services.Configure<WompiSettings>(builder.Configuration.GetSection("Wompi"));
builder.Services.AddScoped<IWompiService, WompiService>();

var app = builder.Build();

app.UseForwardedHeaders();

// Detrás de Tailscale Funnel todo el tráfico público llega por HTTPS. Si el proxy local no envía
// X-Forwarded-Proto, SABORATOR_PROXY_HTTPS=true marca como HTTPS las peticiones que llegan desde
// el propio equipo (127.0.0.1), para que las cookies Secure y las URLs se generen bien.
if (string.Equals(Environment.GetEnvironmentVariable("SABORATOR_PROXY_HTTPS"), "true", StringComparison.OrdinalIgnoreCase))
{
    app.Use((context, next) =>
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip != null && System.Net.IPAddress.IsLoopback(ip))
            context.Request.Scheme = "https";
        return next();
    });
}

// Comprobación de salud para el script vigilante y para verificar HTTPS detrás del proxy
app.MapGet("/salud", (HttpContext ctx) => Results.Json(new { ok = true, https = ctx.Request.IsHttps, host = ctx.Request.Host.Host }));

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ── Cabeceras de Seguridad HTTP (OWASP / Producción) ─────────────────────────
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    // CSP: las vistas usan scripts y estilos en línea, por eso 'unsafe-inline'; el resto limita de dónde
    // se puede cargar contenido, quién puede incrustar el sitio y a dónde pueden enviarse formularios.
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://accounts.google.com https://checkout.wompi.co https://www.googletagmanager.com https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://accounts.google.com; " +
        "font-src 'self' data: https://fonts.gstatic.com; " +
        "img-src 'self' data: blob: https:; " +
        "media-src 'self' data: blob:; " +
        "frame-src 'self' https://www.google.com https://maps.google.com https://accounts.google.com https://checkout.wompi.co; " +
        "connect-src 'self' https://*.wompi.co https://accounts.google.com https://*.googletagmanager.com https://*.google-analytics.com; " +
        "object-src 'none'; base-uri 'self'; frame-ancestors 'self'; form-action 'self' https://checkout.wompi.co");
    await next();
});

// Manejo elegante de errores 404 y códigos de estado
app.UseStatusCodePagesWithReExecute("/Home/NotFound", "?statusCode={0}");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapStaticAssets();

app.MapHub<ControlClientesRestaurante.Hubs.CocinaHub>("/hubs/cocina");

// Ruta raíz → página principal pública
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ── Seed Resiliente (SQL Server + SQLite) ─────────────────────────────────────
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (useSqlite)
    {
        db.Database.EnsureCreated();

        // EnsureCreated no altera bases SQLite ya existentes: añadir columnas nuevas si faltan
        var columnasPedidos = new List<string>();
        var conn = db.Database.GetDbConnection();
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info('Pedidos');";
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) columnasPedidos.Add(reader.GetString(1));
        }
        conn.Close();
        if (!columnasPedidos.Contains("Repartidor"))
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE Pedidos ADD COLUMN Repartidor TEXT NULL;");
            db.Database.ExecuteSqlRaw("ALTER TABLE Pedidos ADD COLUMN FechaDespacho TEXT NULL;");
            db.Database.ExecuteSqlRaw("ALTER TABLE Pedidos ADD COLUMN FechaEntrega TEXT NULL;");
        }
    }
    else
    {
        db.Database.Migrate();
        db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Reservas')
        BEGIN
            CREATE TABLE [Reservas] (
                [Id] int IDENTITY(1,1) NOT NULL,
                [Codigo] nvarchar(50) NOT NULL,
                [ClienteId] int NULL,
                [UsuarioId] int NULL,
                [NombreCliente] nvarchar(150) NOT NULL,
                [Telefono] nvarchar(50) NOT NULL,
                [Correo] nvarchar(150) NULL,
                [NumeroMesa] int NOT NULL,
                [Fecha] datetime2 NOT NULL,
                [Hora] nvarchar(20) NOT NULL,
                [NumeroPersonas] int NOT NULL,
                [Notas] nvarchar(500) NULL,
                [Estado] nvarchar(50) NOT NULL,
                [FechaCreacion] datetime2 NOT NULL,
                CONSTRAINT [PK_Reservas] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_Reservas_Codigo] ON [Reservas] ([Codigo]);
        END

        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Mesas' AND COLUMN_NAME = 'Estado')
        BEGIN
            ALTER TABLE [Mesas] ALTER COLUMN [Estado] NVARCHAR(50) NOT NULL;
        END

        IF NOT EXISTS (SELECT 1 FROM [Mesas])
        BEGIN
            DECLARE @i INT = 1;
            WHILE @i <= 30
            BEGIN
                INSERT INTO [Mesas] ([Numero],[Estado],[FechaCreacion])
                VALUES (@i, 'Libre', GETDATE());
                SET @i = @i + 1;
            END
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Pedidos' AND COLUMN_NAME = 'TipoEntrega')
        BEGIN
            ALTER TABLE [Pedidos] ADD [TipoEntrega] NVARCHAR(50) NOT NULL DEFAULT 'EnMesa';
            ALTER TABLE [Pedidos] ADD [Direccion] NVARCHAR(250) NULL;
            ALTER TABLE [Pedidos] ADD [Barrio] NVARCHAR(100) NULL;
            ALTER TABLE [Pedidos] ADD [Telefono] NVARCHAR(50) NULL;
            ALTER TABLE [Pedidos] ADD [MetodoPago] NVARCHAR(50) NOT NULL DEFAULT 'Efectivo';
            ALTER TABLE [Pedidos] ADD [CostoEnvio] DECIMAL(18,2) NOT NULL DEFAULT 0;
            ALTER TABLE [Pedidos] ADD [PagaCon] DECIMAL(18,2) NULL;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Facturas' AND COLUMN_NAME = 'CostoEnvio')
        BEGIN
            ALTER TABLE [Facturas] ADD [CostoEnvio] DECIMAL(18,2) NOT NULL DEFAULT 0;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'DebeCambiarContraseña')
        BEGIN
            ALTER TABLE [Usuarios] ADD [DebeCambiarContraseña] BIT NOT NULL DEFAULT 0;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'FotoPerfil')
        BEGIN
            ALTER TABLE [Usuarios] ADD [FotoPerfil] NVARCHAR(500) NULL;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'Tema')
        BEGIN
            ALTER TABLE [Usuarios] ADD [Tema] NVARCHAR(50) NOT NULL DEFAULT 'clasico';
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'Correo')
        BEGIN
            ALTER TABLE [Usuarios] ADD [Correo] NVARCHAR(150) NULL;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'EmailVerificado')
        BEGIN
            ALTER TABLE [Usuarios] ADD [EmailVerificado] BIT NOT NULL DEFAULT 1;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'TokenVerificacionEmail')
        BEGIN
            ALTER TABLE [Usuarios] ADD [TokenVerificacionEmail] NVARCHAR(100) NULL;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'FechaExpiracionToken')
        BEGIN
            ALTER TABLE [Usuarios] ADD [FechaExpiracionToken] DATETIME2 NULL;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Productos' AND COLUMN_NAME = 'Categoria')
        BEGIN
            ALTER TABLE [Productos] ADD [Categoria] NVARCHAR(50) NOT NULL DEFAULT 'Hamburguesas & Carnes';
            ALTER TABLE [Productos] ADD [Alergenos] NVARCHAR(250) NULL;
            ALTER TABLE [Productos] ADD [Ingredientes] NVARCHAR(500) NULL;
            ALTER TABLE [Productos] ADD [EsVegetariano] BIT NOT NULL DEFAULT 0;
            ALTER TABLE [Productos] ADD [EsLibreGluten] BIT NOT NULL DEFAULT 0;
            ALTER TABLE [Productos] ADD [TiempoCoccionMinutos] INT NOT NULL DEFAULT 15;

            EXEC(N'
            UPDATE [Productos] SET 
                [Categoria] = ''Hamburguesas & Carnes'',
                [Alergenos] = ''Gluten, Lácteos, Huevos'',
                [Ingredientes] = ''Carne 100% res angus 200g, queso cheddar fundido, tocineta ahumada, cebolla caramelizada, pan brioche artesanal'',
                [TiempoCoccionMinutos] = 15
            WHERE [Nombre] LIKE ''%Hamburguesa%'';

            UPDATE [Productos] SET 
                [Categoria] = ''Salchipapas Gourmet'',
                [Alergenos] = ''Lácteos'',
                [Ingredientes] = ''Papas rústicas doradas, butifarra tradicional, lomo de cerdo, queso costeño rallado, salsa de la casa'',
                [EsLibreGluten] = 1,
                [TiempoCoccionMinutos] = 12
            WHERE [Nombre] LIKE ''%Salchipapa%'';

            UPDATE [Productos] SET 
                [Categoria] = ''Perros & Especiales'',
                [Alergenos] = ''Gluten, Lácteos'',
                [Ingredientes] = ''Salchicha suiza premium, queso mozzarella fundido, tocineta crocante, ripio de papa, pan artesanal'',
                [TiempoCoccionMinutos] = 10
            WHERE [Nombre] LIKE ''%Perro%'';

            UPDATE [Productos] SET 
                [Categoria] = ''Acompañamientos'',
                [Alergenos] = ''Sin alérgenos comunes'',
                [Ingredientes] = ''Papas corte fino crocantes, sal marina, paprika ahumada'',
                [EsVegetariano] = 1,
                [EsLibreGluten] = 1,
                [TiempoCoccionMinutos] = 8
            WHERE [Nombre] LIKE ''%Papas%'';

            UPDATE [Productos] SET 
                [Categoria] = ''Bebidas'',
                [Alergenos] = ''Sin alérgenos'',
                [EsVegetariano] = 1,
                [EsLibreGluten] = 1,
                [TiempoCoccionMinutos] = 1
            WHERE [Nombre] LIKE ''%Coca%'' OR [Nombre] LIKE ''%Pepsi%'' OR [Nombre] LIKE ''%Agua%'';
            ');
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Resenas')
        BEGIN
            CREATE TABLE [Resenas] (
                [Id] int IDENTITY(1,1) NOT NULL,
                [NombreCliente] nvarchar(150) NOT NULL,
                [Correo] nvarchar(150) NULL,
                [Calificacion] int NOT NULL DEFAULT 5,
                [Comentario] nvarchar(1000) NOT NULL,
                [PlatoRecomendado] nvarchar(150) NULL,
                [TipoExperiencia] nvarchar(50) NOT NULL DEFAULT 'En Restaurante',
                [Aprobada] bit NOT NULL DEFAULT 1,
                [Verificado] bit NOT NULL DEFAULT 1,
                [FechaCreacion] datetime2 NOT NULL DEFAULT GETDATE(),
                CONSTRAINT [PK_Resenas] PRIMARY KEY ([Id])
            );

            INSERT INTO [Resenas] ([NombreCliente], [Correo], [Calificacion], [Comentario], [PlatoRecomendado], [TipoExperiencia], [Aprobada], [Verificado], [FechaCreacion])
            VALUES 
            ('Valeria Sarmiento', 'valeria.s@gmail.com', 5, 'El desgranado especial es simplemente una obra de arte. La salsa de la casa y el queso gratinado tienen un sabor insuperable. El domicilio llegó a Miramar en 28 minutos hirviendo y con empaque térmico impecable.', 'Desgranado Especial', 'Domicilio Express', 1, 1, DATEADD(hour, -5, GETDATE())),
            ('Carlos Andrés Meza', 'carlos.meza@hotmail.com', 5, 'Excelente ambiente en el salón de la Cra 4 Sur. Pedimos la hamburguesa artesanal y la carne tiene un término perfecto con pan brioche fresco. La atención de los meseros fue de 10 estrellas.', 'Hamburguesa Clásica Artesanal', 'En Restaurante', 1, 1, DATEADD(day, -1, GETDATE())),
            ('Daniela Char', 'daniela.char@outlook.com', 5, '¡El mejor salchipapa gourmet de Barranquilla! La butifarra y el lomo de cerdo son de altísima calidad. Además el sistema para pedir en línea y elegir la tarifa de domicilio exacta es rapidísimo.', 'Salchipapa Gourmet Suprema', 'Domicilio Express', 1, 1, DATEADD(day, -2, GETDATE())),
            ('Andrés Felipe Rojas', 'rojas.a@gmail.com', 4, 'Muy buen sabor en las alitas y costillas BBQ. La porción es generosa y la limonada de coco natural es exquisita. Definitivamente volveremos a reservar mesa.', 'Costillas BBQ en Leña', 'En Restaurante', 1, 1, DATEADD(day, -4, GETDATE())),
            ('Mariana Osorio', 'mariana.o@yahoo.com', 5, 'Pudimos pedir para recoger sin filas con el Pick-up. Todo estaba empacado como si fuera de un restaurante 5 estrellas. 100% recomendado.', 'Perro Caliente Suizo', 'Para Llevar', 1, 1, DATEADD(day, -6, GETDATE()));
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TransaccionesPago')
        BEGIN
            CREATE TABLE [TransaccionesPago] (
                [Id] int IDENTITY(1,1) NOT NULL,
                [ReferenciaTransaccion] nvarchar(100) NOT NULL,
                [CodigoAprobacion] nvarchar(50) NOT NULL,
                [TipoOrigen] nvarchar(50) NOT NULL DEFAULT 'Pedido',
                [OrigenId] int NULL,
                [Monto] decimal(18,2) NOT NULL,
                [MetodoPago] nvarchar(50) NOT NULL DEFAULT 'Tarjeta',
                [DetalleMetodo] nvarchar(250) NOT NULL,
                [NombreCliente] nvarchar(150) NOT NULL,
                [Correo] nvarchar(150) NULL,
                [Telefono] nvarchar(50) NULL,
                [Concepto] nvarchar(250) NULL,
                [NumeroPedido] nvarchar(50) NULL,
                [NumeroFactura] nvarchar(50) NULL,
                [CodigoReserva] nvarchar(50) NULL,
                [Mesa] nvarchar(50) NULL,
                [Estado] nvarchar(50) NOT NULL DEFAULT 'Aprobado',
                [UsuarioRegistro] nvarchar(100) NOT NULL DEFAULT 'Sistema',
                [FechaCreacion] datetime2 NOT NULL DEFAULT GETDATE(),
                CONSTRAINT [PK_TransaccionesPago] PRIMARY KEY ([Id])
            );
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TurnosCaja')
        BEGIN
            CREATE TABLE [TurnosCaja] (
                [Id] int IDENTITY(1,1) NOT NULL,
                [CodigoTurno] nvarchar(50) NOT NULL,
                [UsuarioCajero] nvarchar(100) NOT NULL,
                [FechaApertura] datetime2 NOT NULL DEFAULT GETDATE(),
                [FechaCierre] datetime2 NULL,
                [MontoInicial] decimal(18,2) NOT NULL DEFAULT 0,
                [VentasEfectivo] decimal(18,2) NOT NULL DEFAULT 0,
                [VentasTransferencia] decimal(18,2) NOT NULL DEFAULT 0,
                [VentasTarjeta] decimal(18,2) NOT NULL DEFAULT 0,
                [EfectivoRealDeclarado] decimal(18,2) NULL,
                [Estado] nvarchar(30) NOT NULL DEFAULT 'Abierto',
                [Observaciones] nvarchar(500) NULL,
                [FechaCreacion] datetime2 NOT NULL DEFAULT GETDATE(),
                CONSTRAINT [PK_TurnosCaja] PRIMARY KEY ([Id])
            );
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Pedidos' AND COLUMN_NAME = 'CodigoCupon')
        BEGIN
            ALTER TABLE [Pedidos] ADD [CodigoCupon] NVARCHAR(50) NULL;
            ALTER TABLE [Pedidos] ADD [Descuento] DECIMAL(18,2) NOT NULL DEFAULT 0;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Pedidos' AND COLUMN_NAME = 'Repartidor')
        BEGIN
            ALTER TABLE [Pedidos] ADD [Repartidor] NVARCHAR(100) NULL;
            ALTER TABLE [Pedidos] ADD [FechaDespacho] DATETIME2 NULL;
            ALTER TABLE [Pedidos] ADD [FechaEntrega] DATETIME2 NULL;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Facturas' AND COLUMN_NAME = 'CodigoCupon')
        BEGIN
            ALTER TABLE [Facturas] ADD [CodigoCupon] NVARCHAR(50) NULL;
            ALTER TABLE [Facturas] ADD [Descuento] DECIMAL(18,2) NOT NULL DEFAULT 0;
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Cupones')
        BEGIN
            CREATE TABLE [Cupones] (
                [Id] int IDENTITY(1,1) NOT NULL,
                [Codigo] nvarchar(50) NOT NULL,
                [Descripcion] nvarchar(200) NOT NULL,
                [TipoDescuento] nvarchar(20) NOT NULL DEFAULT 'Porcentaje',
                [Valor] decimal(18,2) NOT NULL,
                [MontoMinimo] decimal(18,2) NOT NULL DEFAULT 0,
                [TopeMaximoDescuento] decimal(18,2) NULL,
                [FechaVencimiento] datetime2 NULL,
                [UsosMaximos] int NOT NULL DEFAULT 100,
                [UsosActuales] int NOT NULL DEFAULT 0,
                [Activo] bit NOT NULL DEFAULT 1,
                [FechaCreacion] datetime2 NOT NULL DEFAULT GETDATE(),
                CONSTRAINT [PK_Cupones] PRIMARY KEY ([Id])
            );
            CREATE UNIQUE INDEX [IX_Cupones_Codigo] ON [Cupones] ([Codigo]);

            INSERT INTO [Cupones] ([Codigo], [Descripcion], [TipoDescuento], [Valor], [MontoMinimo], [TopeMaximoDescuento], [FechaVencimiento], [UsosMaximos], [UsosActuales], [Activo], [FechaCreacion])
            VALUES 
            ('BURGER15', '15% de descuento en pedidos desde $30.000 COP', 'Porcentaje', 15, 30000, 12000, DATEADD(month, 3, GETDATE()), 200, 0, 1, GETDATE()),
            ('BIENVENIDA', 'Bono de $10.000 COP en compras superiores a $45.000 COP', 'Fijo', 10000, 45000, NULL, DATEADD(month, 6, GETDATE()), 150, 0, 1, GETDATE()),
            ('ENVIOGRATIS', 'Descuento de $5.000 COP equivalente al envío a domicilio', 'Fijo', 5000, 25000, NULL, DATEADD(month, 3, GETDATE()), 300, 0, 1, GETDATE());
        END

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditoriaLogs')
        BEGIN
            CREATE TABLE [AuditoriaLogs] (
                [Id] int IDENTITY(1,1) NOT NULL,
                [Usuario] nvarchar(100) NOT NULL,
                [Rol] nvarchar(50) NULL,
                [Accion] nvarchar(50) NOT NULL,
                [Modulo] nvarchar(50) NOT NULL,
                [Detalle] nvarchar(1000) NOT NULL,
                [EntidadAfectada] nvarchar(50) NULL,
                [EntidadId] int NULL,
                [IpAddress] nvarchar(100) NULL,
                [FechaCreacion] datetime2 NOT NULL DEFAULT GETDATE(),
                CONSTRAINT [PK_AuditoriaLogs] PRIMARY KEY ([Id])
            );
            CREATE INDEX [IX_AuditoriaLogs_FechaCreacion] ON [AuditoriaLogs] ([FechaCreacion] DESC);
            CREATE INDEX [IX_AuditoriaLogs_Modulo] ON [AuditoriaLogs] ([Modulo]);

            INSERT INTO [AuditoriaLogs] ([Usuario], [Rol], [Accion], [Modulo], [Detalle], [EntidadAfectada], [IpAddress], [FechaCreacion])
            VALUES
            ('admin', 'Administrador', 'INICIALIZACION_SISTEMA', 'Seguridad', 'Sistema Saborator iniciado y verificado con módulos de seguridad, SignalR y facturación.', 'Sistema', '127.0.0.1', GETDATE());
        END
    ");
    }

    if (!db.Mesas.Any())
    {
        for (int i = 1; i <= 30; i++)
        {
            db.Mesas.Add(new Mesa { Numero = i, Estado = "Libre", FechaCreacion = DateTime.Now });
        }
        db.SaveChanges();
    }

    if (!db.Resenas.Any())
    {
        db.Resenas.AddRange(
            new Resena { NombreCliente = "Valeria Sarmiento", Correo = "valeria.s@gmail.com", Calificacion = 5, Comentario = "El desgranado especial es simplemente una obra de arte. La salsa de la casa y el queso gratinado tienen un sabor insuperable. El domicilio llegó a Miramar en 28 minutos hirviendo y con empaque térmico impecable.", PlatoRecomendado = "Desgranado Especial", TipoExperiencia = "Domicilio Express", Aprobada = true, Verificado = true, FechaCreacion = DateTime.Now.AddHours(-5) },
            new Resena { NombreCliente = "Carlos Andrés Meza", Correo = "carlos.meza@hotmail.com", Calificacion = 5, Comentario = "Excelente ambiente en el salón de la Cra 4 Sur. Pedimos la hamburguesa artesanal y la carne tiene un término perfecto con pan brioche fresco. La atención de los meseros fue de 10 estrellas.", PlatoRecomendado = "Hamburguesa Clásica Artesanal", TipoExperiencia = "En Restaurante", Aprobada = true, Verificado = true, FechaCreacion = DateTime.Now.AddDays(-1) },
            new Resena { NombreCliente = "Daniela Char", Correo = "daniela.char@outlook.com", Calificacion = 5, Comentario = "¡El mejor salchipapa gourmet de Barranquilla! La butifarra y el lomo de cerdo son de altísima calidad. Además el sistema para pedir en línea y elegir la tarifa de domicilio exacta es rapidísimo.", PlatoRecomendado = "Salchipapa Gourmet Suprema", TipoExperiencia = "Domicilio Express", Aprobada = true, Verificado = true, FechaCreacion = DateTime.Now.AddDays(-2) },
            new Resena { NombreCliente = "Andrés Felipe Rojas", Correo = "rojas.a@gmail.com", Calificacion = 4, Comentario = "Muy buen sabor en las alitas y costillas BBQ. La porción es generosa y la limonada de coco natural es exquisita. Definitivamente volveremos a reservar mesa.", PlatoRecomendado = "Costillas BBQ en Leña", TipoExperiencia = "En Restaurante", Aprobada = true, Verificado = true, FechaCreacion = DateTime.Now.AddDays(-4) },
            new Resena { NombreCliente = "Mariana Osorio", Correo = "mariana.o@yahoo.com", Calificacion = 5, Comentario = "Pudimos pedir para recoger sin filas con el Pick-up. Todo estaba empacado como si fuera de un restaurante 5 estrellas. 100% recomendado.", PlatoRecomendado = "Perro Caliente Suizo", TipoExperiencia = "Para Llevar", Aprobada = true, Verificado = true, FechaCreacion = DateTime.Now.AddDays(-6) }
        );
        db.SaveChanges();
    }

    if (!db.Cupones.Any())
    {
        db.Cupones.AddRange(
            new Cupon { Codigo = "BURGER15", Descripcion = "15% de descuento en pedidos desde $30.000 COP", TipoDescuento = "Porcentaje", Valor = 15m, MontoMinimo = 30000m, TopeMaximoDescuento = 12000m, FechaVencimiento = DateTime.Now.AddMonths(3), UsosMaximos = 200, UsosActuales = 0, Activo = true, FechaCreacion = DateTime.Now },
            new Cupon { Codigo = "BIENVENIDA", Descripcion = "Bono de $10.000 COP en compras superiores a $45.000 COP", TipoDescuento = "Fijo", Valor = 10000m, MontoMinimo = 45000m, TopeMaximoDescuento = null, FechaVencimiento = DateTime.Now.AddMonths(6), UsosMaximos = 150, UsosActuales = 0, Activo = true, FechaCreacion = DateTime.Now },
            new Cupon { Codigo = "ENVIOGRATIS", Descripcion = "Descuento de $5.000 COP equivalente al envío a domicilio", TipoDescuento = "Fijo", Valor = 5000m, MontoMinimo = 25000m, TopeMaximoDescuento = null, FechaVencimiento = DateTime.Now.AddMonths(3), UsosMaximos = 300, UsosActuales = 0, Activo = true, FechaCreacion = DateTime.Now }
        );
        db.SaveChanges();
    }

    if (!db.AuditoriaLogs.Any())
    {
        db.AuditoriaLogs.Add(new AuditoriaLog
        {
            Usuario = "admin",
            Rol = "Administrador",
            Accion = "INICIALIZACION_SISTEMA",
            Modulo = "Seguridad",
            Detalle = "Sistema Saborator iniciado y verificado con módulos de seguridad, SignalR y facturación.",
            EntidadAfectada = "Sistema",
            IpAddress = "127.0.0.1",
            FechaCreacion = DateTime.Now
        });
        db.SaveChanges();
    }

    // Asegurar que existan todos los roles del sistema
    foreach (var nombreRol in ControlClientesRestaurante.Models.Roles.Personal.Append(ControlClientesRestaurante.Models.Roles.Cliente))
    {
        if (!db.Roles.Any(r => r.NombreRol == nombreRol))
            db.Roles.Add(new Rol { NombreRol = nombreRol });
    }
    db.SaveChanges();

    // Cuentas de demostración (solo desarrollo). Nunca se sobrescribe la contraseña de una cuenta existente.
    // En producción NO se crean cuentas con claves conocidas: ver el bloque "Producción" más abajo.
    var cuentasSemilla = new[]
    {
        (Usuario: "admin",        Clave: "Admin#Saborator2026!",        Rol: "Administrador"),
        (Usuario: "cajero",       Clave: "Cajero#Saborator2026!",       Rol: "Cajero"),
        (Usuario: "mesero",       Clave: "Mesero#Saborator2026!",       Rol: "Mesero"),
        (Usuario: "cocinero",     Clave: "Cocinero#Saborator2026!",     Rol: "Cocinero"),
        (Usuario: "domiciliario", Clave: "Domiciliario#Saborator2026!", Rol: "Domiciliario"),
        (Usuario: "cliente",      Clave: "Cliente#Saborator2026!",      Rol: "Cliente"),
    };
    foreach (var cuenta in cuentasSemilla)
    {
        if (!app.Environment.IsDevelopment()) break;
        if (db.Usuarios.Any(u => u.NombreUsuario == cuenta.Usuario)) continue;

        var rolId = db.Roles.First(r => r.NombreRol == cuenta.Rol).Id;
        db.Usuarios.Add(new Usuario
        {
            NombreUsuario         = cuenta.Usuario,
            ContraseñaHash        = BCrypt.Net.BCrypt.HashPassword(cuenta.Clave),
            RolId                 = rolId,
            DebeCambiarContraseña = false,
            EmailVerificado       = true,
            Correo                = cuenta.Rol == "Cliente" ? "cliente@saborator.com" : null
        });
    }
    db.SaveChanges();

    // ── Producción: cerrar la puerta a las claves de demostración ───────────────
    // Las claves de las cuentas semilla están en el código fuente (público). Si una cuenta existente
    // todavía usa esa clave, se reemplaza por una aleatoria y se obliga a cambiarla en el primer ingreso.
    // Las nuevas claves se escriben en credenciales-iniciales.txt (junto a la aplicación).
    if (!app.Environment.IsDevelopment())
    {
        var nuevasClaves = new List<string>();

        foreach (var cuenta in cuentasSemilla)
        {
            var existente = db.Usuarios.FirstOrDefault(u => u.NombreUsuario == cuenta.Usuario);
            if (existente == null) continue;

            bool claveConocida;
            try { claveConocida = BCrypt.Net.BCrypt.Verify(cuenta.Clave, existente.ContraseñaHash); }
            catch { claveConocida = false; }
            if (!claveConocida) continue;

            var clave = GenerarClaveTemporal();
            existente.ContraseñaHash = BCrypt.Net.BCrypt.HashPassword(clave);
            existente.DebeCambiarContraseña = true;
            nuevasClaves.Add($"{cuenta.Usuario}\t{clave}");
        }

        // Garantizar que exista al menos un administrador activo
        var rolAdminId = db.Roles.First(r => r.NombreRol == "Administrador").Id;
        if (!db.Usuarios.Any(u => u.RolId == rolAdminId && u.Estado))
        {
            var clave = GenerarClaveTemporal();
            db.Usuarios.Add(new Usuario
            {
                NombreUsuario = "admin",
                ContraseñaHash = BCrypt.Net.BCrypt.HashPassword(clave),
                RolId = rolAdminId,
                DebeCambiarContraseña = true,
                EmailVerificado = true
            });
            nuevasClaves.Add($"admin\t{clave}");
        }

        if (nuevasClaves.Count > 0)
        {
            db.SaveChanges();
            var ruta = Path.Combine(AppContext.BaseDirectory, "credenciales-iniciales.txt");
            File.AppendAllText(ruta,
                $"# {DateTime.Now:yyyy-MM-dd HH:mm} - claves temporales (usuario<TAB>clave). Se piden cambiar al primer ingreso. Borra este archivo despues de usarlas.{Environment.NewLine}" +
                string.Join(Environment.NewLine, nuevasClaves) + Environment.NewLine);
            Console.WriteLine($"[Saborator] Se reemplazaron claves de demostracion por claves temporales. Revisa: {ruta}");
        }
    }

    var cCliente = db.Clientes.FirstOrDefault(c => c.Correo == "cliente@saborator.com" || c.Nombre == "Cliente Saborator");
    if (cCliente == null)
    {
        db.Clientes.Add(new Cliente {
            Nombre = "Cliente Saborator",
            Documento = "CC-100200300",
            Telefono = "3001234567",
            Correo = "cliente@saborator.com",
            Direccion = "Cra 4 Sur # 50D-107",
            Activo = true
        });
    }

    db.SaveChanges();

    var menu = new[]
    {
        new { 
            Nombre = "Salchipapa",          
            Descripcion = "Salchicha clásica, pechuga, lomo de cerdo, chorizo, butifarra, suiza, lechuga, chongo, queso costeño, salsas de la casa, queso gratinado y tocineta.", 
            Precio = 7000m,  
            Stock = 60,
            Categoria = "Salchipapas Gourmet"
        },
        new { 
            Nombre = "Perro Caliente",     
            Descripcion = "Salchicha clásica, pechuga, butifarra, lechuga, chongo, queso costeño, tocineta, queso gratinado, salsa piña y salsa de la casa.", 
            Precio = 8500m,  
            Stock = 50,
            Categoria = "Perros & Especiales"
        },
        new { 
            Nombre = "Hamburguesa",         
            Descripcion = "Pan brioche, carne de 125 g, queso mozzarella, vegetales frescos, salsa de la casa y papas a la francesa.",      
            Precio = 12000m, 
            Stock = 40,
            Categoria = "Hamburguesas & Carnes"
        },
        new { 
            Nombre = "Papas a la Francesa", 
            Descripcion = "Porción de papas a la francesa doradas y crujientes, sazonadas al punto de sal y acompañadas con salsa de la casa.",             
            Precio = 4500m,  
            Stock = 80,
            Categoria = "Acompañamientos"
        },
        new { 
            Nombre = "Coca-Cola",           
            Descripcion = "Refrescante gaseosa Coca-Cola Original (600 ml), servida bien fría con el inconfundible sabor clásico.",                             
            Precio = 3000m,  
            Stock = 80,
            Categoria = "Bebidas"
        },
        new { 
            Nombre = "Pepsi",               
            Descripcion = "Gaseosa Pepsi helada (600 ml), con su balance perfecto de notas refrescantes, burbujas vivas y cuerpo cítrico.",                                 
            Precio = 2800m,  
            Stock = 80,
            Categoria = "Bebidas"
        },
        new { 
            Nombre = "Agua",                
            Descripcion = "Agua mineral Cristal pura (600 ml), hidratación cristalina y natural servida fría de cava.",                                    
            Precio = 2000m,  
            Stock = 100,
            Categoria = "Bebidas"
        },
    };
    foreach (var item in menu)
    {
        var existing = db.Productos.FirstOrDefault(p => p.Nombre == item.Nombre);
        if (existing == null)
            db.Productos.Add(new Producto { Nombre = item.Nombre, Descripcion = item.Descripcion, Precio = item.Precio, Stock = item.Stock, Categoria = item.Categoria, Activo = true });
        else
            existing.Descripcion = item.Descripcion;
    }
    db.SaveChanges();
}
catch (Exception ex)
{
    Console.WriteLine($"[Saborator] Advertencia durante la inicialización de la base de datos: {ex.Message}");
}

app.Run();

// Clave temporal aleatoria (sin caracteres ambiguos) con mayúscula, minúscula, número y símbolo
static string GenerarClaveTemporal()
{
    const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    var chars = new char[14];
    for (int i = 0; i < chars.Length; i++)
        chars[i] = alfabeto[System.Security.Cryptography.RandomNumberGenerator.GetInt32(alfabeto.Length)];
    return new string(chars) + "#" + System.Security.Cryptography.RandomNumberGenerator.GetInt32(10);
}
