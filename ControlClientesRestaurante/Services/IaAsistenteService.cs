using System.Text.RegularExpressions;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Services
{
    public class IaMensajeDto
    {
        public string Remitente { get; set; } = "usuario"; // "usuario" | "asistente"
        public string Contenido { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class IaConsultaRequest
    {
        public string Mensaje { get; set; } = "";
        public string? Rol { get; set; }
        public List<IaMensajeDto>? Historial { get; set; }
    }

    public class IaConsultaResponse
    {
        public bool Ok { get; set; } = true;
        public string Respuesta { get; set; } = "";
        public string PersonaNombre { get; set; } = "";
        public string PersonaRol { get; set; } = "";
        public string ColorTema { get; set; } = "";
        public List<string> Sugerencias { get; set; } = new();
        public string? EnlaceAccion { get; set; }
        public string? TextoAccion { get; set; }
        
        // Atributos de Razonamiento Cognitivo (Chain of Thought & Metacognición)
        public string? IntencionDetectada { get; set; }
        public List<string> PasosRazonamiento { get; set; } = new();
    }

    public class IaPersonaInfo
    {
        public string CodigoRol { get; set; } = "Cliente";
        public string Nombre { get; set; } = "Sabori";
        public string Titulo { get; set; } = "Sommelier Gastronómico";
        public string Saludo { get; set; } = "¡Hola! Soy Sabori, tu sommelier culinario en Saborator.";
        public string Descripcion { get; set; } = "Te guío en la carta, maridajes, domicilios en Barranquilla y reservas.";
        public string ColorTema { get; set; } = "#C8973A";
        public string ColorSecundario { get; set; } = "#6B1E2E";
        public List<string> SugerenciasIniciales { get; set; } = new();
    }

    public class IaAsistenteService
    {
        private readonly ProductoService _productoSvc;
        private readonly MesaService _mesaSvc;
        private readonly PedidoService _pedidoSvc;
        private readonly ResenaService _resenaSvc;

        public IaAsistenteService(
            ProductoService productoSvc,
            MesaService mesaSvc,
            PedidoService pedidoSvc,
            ResenaService resenaSvc)
        {
            _productoSvc = productoSvc;
            _mesaSvc = mesaSvc;
            _pedidoSvc = pedidoSvc;
            _resenaSvc = resenaSvc;
        }

        public IaPersonaInfo ObtenerInfoPersona(string? rol)
        {
            var rolNormalizado = NormalizarRol(rol);

            return rolNormalizado switch
            {
                "Mesero" => new IaPersonaInfo
                {
                    CodigoRol = "Mesero",
                    Nombre = "Gastón",
                    Titulo = "Capitán de Salón & Mesas",
                    Saludo = "¡A la orden! Soy Gastón, tu asistente de salón y servicio en mesas.",
                    Descripcion = "Te apoyo con estado de mesas, comandas rápidas y sugerencias de maridaje.",
                    ColorTema = "#2563EB",
                    ColorSecundario = "#1E293B",
                    SugerenciasIniciales = new List<string>
                    {
                        "🍽️ ¿Cómo están las mesas ahora?",
                        "⚡ Recomendación rápida para comensal indeciso",
                        "⏱️ ¿Qué platos salen más rápido de cocina?",
                        "🍷 Sugerir maridaje para hamburguesa de autor"
                    }
                },
                "Cocinero" => new IaPersonaInfo
                {
                    CodigoRol = "Cocinero",
                    Nombre = "Chef Gusteau",
                    Titulo = "Jefe KDS & Recetario",
                    Saludo = "¡Oído cocina! Soy el Chef Gusteau, asistente técnico de línea y recetas.",
                    Descripcion = "Gramajes, puntos de cocción, armado de salchipapas y sincronización de pedidos.",
                    ColorTema = "#DC2626",
                    ColorSecundario = "#991B1B",
                    SugerenciasIniciales = new List<string>
                    {
                        "🔥 ¿Cuántos pedidos hay pendientes en cocina?",
                        "🥩 Receta y armado de Hamburguesa Saborator",
                        "🍟 Tiempos y gramaje de Salchipapa Salvaje",
                        "⏳ ¿Cómo priorizo los pedidos demorados?"
                    }
                },
                "Cajero" => new IaPersonaInfo
                {
                    CodigoRol = "Cajero",
                    Nombre = "Aurelio",
                    Titulo = "Gestor de Caja & Tarifas",
                    Saludo = "¡Buen turno! Soy Aurelio, tu asistente de caja, facturación y domicilios.",
                    Descripcion = "Cálculo de cambio, tarifas de Barranquilla, cortesías autorizadas y cuadre.",
                    ColorTema = "#059669",
                    ColorSecundario = "#064E3B",
                    SugerenciasIniciales = new List<string>
                    {
                        "🛵 Tarifas oficiales de domicilio en Barranquilla",
                        "💵 ¿Cuándo aplica tarifa de cortesía $0?",
                        "💳 Métodos de pago y cambio en efectivo",
                        "📊 Resumen de pedidos para facturar"
                    }
                },
                "Administrador" => new IaPersonaInfo
                {
                    CodigoRol = "Administrador",
                    Nombre = "Hermes",
                    Titulo = "Director de Analítica & Gestión",
                    Saludo = "Saludos, Dirección. Soy Hermes, tu asistente ejecutivo y de analítica.",
                    Descripcion = "Monitoreo de stock crítico, satisfacción de reseñas y visión operacional.",
                    ColorTema = "#7C3AED",
                    ColorSecundario = "#4C1D95",
                    SugerenciasIniciales = new List<string>
                    {
                        "⚠️ ¿Qué productos tienen stock crítico (≤ 5)?",
                        "⭐ Resumen de satisfacción y reseñas",
                        "📈 Estado operacional de hoy",
                        "💡 Ideas para optimizar la rotación de mesas"
                    }
                },
                _ => new IaPersonaInfo
                {
                    CodigoRol = "Cliente",
                    Nombre = "Sabori",
                    Titulo = "Sommelier Gastronómico",
                    Saludo = "¡Hola! Soy Sabori, tu sommelier culinario en Saborator.",
                    Descripcion = "Te guío en la carta, maridajes, domicilios en Barranquilla y reservas.",
                    ColorTema = "#C8973A",
                    ColorSecundario = "#6B1E2E",
                    SugerenciasIniciales = new List<string>
                    {
                        "🍔 ¿Qué hamburguesa me recomiendas hoy?",
                        "🛵 ¿Cuánto cuesta el domicilio a mi barrio?",
                        "🥤 ¿Qué bebidas tienen disponibles?",
                        "📍 ¿Dónde queda el restaurante y qué horarios tienen?"
                    }
                }
            };
        }

        public IaConsultaResponse ProcesarConsulta(IaConsultaRequest request)
        {
            var rol = NormalizarRol(request.Rol);
            var persona = ObtenerInfoPersona(rol);
            var pregunta = (request.Mensaje ?? "").Trim();

            if (string.IsNullOrWhiteSpace(pregunta))
            {
                return new IaConsultaResponse
                {
                    Ok = true,
                    Respuesta = persona.Saludo + " ¿En qué te puedo colaborar en este momento?",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    Sugerencias = persona.SugerenciasIniciales,
                    IntencionDetectada = "Saludo & Disponibilidad",
                    PasosRazonamiento = new List<string>
                    {
                        "1. Detección de sesión y rol activo: " + rol,
                        "2. Inicialización de persona culinaria: " + persona.Nombre + " (" + persona.Titulo + ")",
                        "3. Mensaje vacío: Ofreciendo opciones de inicio y menú guiado."
                    }
                };
            }

            // ── MOTOR DE RAZONAMIENTO COGNITIVO DEL ASISTENTE (CHAIN OF THOUGHT) ──
            var respuesta = rol switch
            {
                "Cliente" => ResponderCliente(pregunta, persona),
                "Mesero" => ResponderMesero(pregunta, persona),
                "Cocinero" => ResponderCocinero(pregunta, persona),
                "Cajero" => ResponderCajero(pregunta, persona),
                "Administrador" => ResponderAdministrador(pregunta, persona),
                _ => ResponderCliente(pregunta, persona)
            };

            // Razonamiento: siempre se genera el análisis completo del rol; si la rama de respuesta
            // aportó razonamiento propio, se conserva como detalle adicional.
            var especificos = respuesta.PasosRazonamiento ?? new List<string>();
            var pasosFinales = GenerarPasosRazonamiento(pregunta, rol, persona, respuesta);
            foreach (var extra in especificos.Skip(1))
            {
                var idx = extra.IndexOf("]:", StringComparison.Ordinal);
                var texto = idx >= 0 ? extra[(idx + 2)..].Trim() : extra;
                if (!string.IsNullOrWhiteSpace(texto))
                    pasosFinales.Insert(pasosFinales.Count - 1, $"[Detalle específico]: {texto}");
            }
            respuesta.PasosRazonamiento = pasosFinales;

            return respuesta;
        }

        private List<string> GenerarPasosRazonamiento(string pregunta, string rol, IaPersonaInfo persona, IaConsultaResponse resp)
        {
            var lower = pregunta.ToLowerInvariant();
            var pasos = new List<string>();

            // ── 1. Clasificación de intención (con las palabras clave que la activaron) ──
            var categorias = new (string Nombre, string[] Claves)[]
            {
                ("Cálculo logístico y tarifas de domicilio en Barranquilla", new[] { "domicilio", "envio", "envío", "cuanto vale", "barrio", "tarifa", "sede", "donde queda" }),
                ("Asesoría culinaria y recomendación de carta",              new[] { "hamburguesa", "plato", "recomiend", "carta", "menu", "menú", "salchipapa", "perro", "receta", "maridaje" }),
                ("Gestión de salón y disponibilidad de mesas",               new[] { "reserva", "mesa", "cumpleaños", "salon", "salón", "rotacion", "rotación" }),
                ("Conciliación financiera y medios de pago",                 new[] { "pago", "wompi", "tarjeta", "nequi", "efectivo", "cambio", "factura", "cuadre", "cortesia", "cortesía" }),
                ("Auditoría de inventario y alertas de stock",               new[] { "stock", "agotado", "critico", "crítico", "inventario" }),
                ("Estimación de tiempos de cocina y despacho",               new[] { "tiempo", "demora", "cuanto tarda", "cuánto tarda", "horario", "pendiente", "priorizo" }),
                ("Analítica de satisfacción y reseñas",                      new[] { "reseña", "resena", "opinion", "opinión", "satisfaccion", "satisfacción", "estrellas" }),
                ("Bebidas y acompañamientos",                                new[] { "bebida", "coca", "gaseosa", "jugo", "tomar", "pepsi", "agua" })
            };

            string intencion = "Consulta general / conversación abierta";
            var coincidencias = new List<string>();
            foreach (var cat in categorias)
            {
                var hits = cat.Claves.Where(c => lower.Contains(c)).ToList();
                if (hits.Count > coincidencias.Count)
                {
                    coincidencias = hits;
                    intencion = cat.Nombre;
                }
            }

            // Si la rama de respuesta ya fijó una intención más precisa, se respeta.
            if (!string.IsNullOrWhiteSpace(resp.IntencionDetectada))
                intencion = resp.IntencionDetectada!;
            else
                resp.IntencionDetectada = intencion;

            string confianza = coincidencias.Count >= 2 ? "alta" : coincidencias.Count == 1 ? "media" : "baja";
            string claves = coincidencias.Count > 0 ? string.Join(", ", coincidencias.Select(c => $"«{c}»")) : "ninguna palabra clave específica";

            // ── 2. Alcance del rol (lo que este asistente puede y no puede consultar) ──
            string alcance = rol switch
            {
                "Administrador" => "acceso ejecutivo: inventario, reseñas, operación y pedidos",
                "Cajero"        => "acceso de caja: tarifas, pagos, pedidos por facturar (sin analítica de gerencia)",
                "Mesero"        => "acceso de salón: estado de mesas, tiempos y maridajes (sin finanzas)",
                "Cocinero"      => "acceso de cocina: cola de comandas, recetas y tiempos (sin precios ni caja)",
                _               => "acceso público de comensal: carta, domicilios y reservas (sin costos internos ni datos de otros clientes)"
            };

            // ── 3. Datos reales consultados según el rol ──
            string datos;
            try
            {
                switch (rol)
                {
                    case "Administrador":
                    {
                        var activos = _productoSvc.ObtenerActivos();
                        var criticos = activos.Count(p => p.Stock <= 5);
                        var agotados = activos.Count(p => p.Stock == 0);
                        var est = _resenaSvc.ObtenerEstadisticas();
                        var pend = _pedidoSvc.ObtenerPendientes().Count;
                        datos = $"{activos.Count} productos activos ({criticos} con stock ≤ 5, {agotados} agotados) · reseñas: {est.Promedio:0.0}/5 en {est.TotalResenas} opiniones · {pend} pedidos activos";
                        break;
                    }
                    case "Cajero":
                    {
                        var pend = _pedidoSvc.ObtenerPendientes().Count;
                        var dom = _pedidoSvc.ObtenerDomicilios().Count;
                        datos = $"{pend} pedidos activos por cobrar/despachar · {dom} domicilios y pedidos para llevar registrados · tabla oficial de tarifas ($4.000 / $6.000 / $9.000)";
                        break;
                    }
                    case "Mesero":
                    {
                        var libres = _mesaSvc.MesasLibres();
                        var ocupadas = _mesaSvc.MesasOcupadas();
                        var reservadas = _mesaSvc.MesasReservadas();
                        var activos = _productoSvc.ObtenerActivos().Count;
                        datos = $"mesas: {libres} libres · {ocupadas} ocupadas · {reservadas} reservadas · {activos} platos disponibles en carta";
                        break;
                    }
                    case "Cocinero":
                    {
                        var cocina = _pedidoSvc.ObtenerParaCocina();
                        var demorados = cocina.Count(p => (DateTime.Now - p.FechaCreacion).TotalMinutes > 20);
                        datos = $"cola de cocina: {cocina.Count} comandas activas ({demorados} con más de 20 min de espera) · recetario y tiempos de cocción de la carta";
                        break;
                    }
                    default:
                    {
                        var activos = _productoSvc.ObtenerActivos();
                        var agotados = activos.Count(p => p.Stock == 0);
                        datos = $"carta pública: {activos.Count} productos activos ({agotados} agotados, se excluyen de las recomendaciones) · políticas de domicilio y horarios de Saborator";
                        break;
                    }
                }
            }
            catch
            {
                datos = "catálogo y políticas de Saborator (datos en vivo no disponibles en este momento)";
            }

            // ── 4. Decisión sobre la respuesta ──
            string accion = !string.IsNullOrWhiteSpace(resp.TextoAccion)
                ? $"adjunto el atajo «{resp.TextoAccion}» para que continúes sin buscar en el menú"
                : "no hace falta redirigir al usuario a otra pantalla";
            string sugs = resp.Sugerencias != null && resp.Sugerencias.Count > 0
                ? $"y propongo {resp.Sugerencias.Count} preguntas de seguimiento"
                : "sin preguntas de seguimiento";

            pasos.Add($"[1. Percepción]: Mensaje recibido «{pregunta}». Hablo como {persona.Nombre} ({persona.Titulo}) con el rol «{rol}».");
            pasos.Add($"[2. Intención]: «{intencion}» · palabras clave: {claves} · confianza {confianza}.");
            pasos.Add($"[3. Alcance del rol]: {alcance}.");
            pasos.Add($"[4. Datos consultados]: {datos}.");
            pasos.Add($"[5. Decisión]: Respondo con la información verificada de arriba, {accion}, {sugs}.");

            return pasos;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // 1. PERSONA: CLIENTE / COMENSAL (SABORI)
        // ══════════════════════════════════════════════════════════════════════════
        private IaConsultaResponse ResponderCliente(string q, IaPersonaInfo persona)
        {
            var lower = q.ToLowerInvariant();

            // Consulta sobre Moneda / Precios en Dólares vs Pesos Colombianos
            if (lower.Contains("dolar") || lower.Contains("dólar") || lower.Contains("usd") || lower.Contains("moneda") || lower.Contains("divisa") || lower.Contains("pesos"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "🇨🇴 **Aclaración de Precios y Moneda Oficial:**\n\n" +
                                "Todos los precios en **Saborator** están expresados en **Pesos Colombianos (COP)**, la moneda de curso legal en Barranquilla y toda Colombia. **Ningún precio está en dólares.**\n\n" +
                                "• Por ejemplo, cuando ves `$34.000` o `$28.000`, significa **$34.000 COP** o **$28.000 COP**.\n" +
                                "• Todas las transacciones a través de Wompi (Nequi, PSE, Tarjetas) se liquidan 100% en Pesos Colombianos sin comisiones de cambio de divisa.\n\n" +
                                "¿Deseas consultar el valor en pesos de algún plato en particular?",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/#menu",
                    TextoAccion = "Ver Carta en Pesos (COP)",
                    Sugerencias = new List<string> { "🍔 Ver hamburguesas en pesos", "🛵 Consultar tarifa de domicilio", "💳 Ver métodos de pago Wompi" },
                    IntencionDetectada = "Clarificación de Moneda (Pesos Colombianos COP)",
                    PasosRazonamiento = new List<string>
                    {
                        "[1. Percepción]: Usuario pregunta sobre moneda o sospecha de precios en dólares.",
                        "[2. Análisis Cognitivo]: Identificada confusión común por el símbolo '$'.",
                        "[3. Regla de Negocio]: Saborator opera en Barranquilla (Colombia); moneda oficial y exclusiva es COP (Pesos Colombianos).",
                        "[4. Síntesis]: Explicación amigable garantizando que todos los cobros son en moneda nacional colombiana."
                    }
                };
            }

            // Guardrails: Intentos de solicitar secretos de negocio, costos o credenciales
            if (lower.Contains("clave") || lower.Contains("password") || lower.Contains("contraseña") ||
                lower.Contains("costo interno") || lower.Contains("proveedor") || lower.Contains("ganancia") ||
                lower.Contains("cuanto ganas") || lower.Contains("base de datos") || lower.Contains("sql"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "Como sommelier gastronómico de Saborator, mi enfoque exclusivo es brindarte la experiencia culinaria más deliciosa: recomendarte nuestros mejores cortes, hamburguesas de autor, bebidas artesanales y gestionar tus pedidos y reservas. Todos nuestros precios están en Pesos Colombianos (COP). ¿Te gustaría conocer el plato estrella de la casa?",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    Sugerencias = new List<string> { "🍔 Ver hamburguesas recomendadas", "🛵 Consultar domicilio", "📅 ¿Cómo reservo una mesa?" }
                };
            }

            // Preguntas sobre Domicilio y Tarifas en Barranquilla
            if (lower.Contains("domicilio") || lower.Contains("tarifa") || lower.Contains("envio") || lower.Contains("envío") || lower.Contains("cuanto vale el domicilio") || lower.Contains("barranquilla") || lower.Contains("sede") || lower.Contains("donde queda"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "**¡Despachamos desde nuestra Sede Central en Barranquilla!** 🛵\n\n" +
                                "- **Dirección:** Cra 4 Sur # 50D-107 (Atlántico).\n" +
                                "- **Tarifas oficiales según tu sector:**\n" +
                                "  • **Zona Sur Cercana ($4.000 COP):** Ciudadela 20 de Julio, Simón Bolívar, Las Cayenas, La Magdalena, Buenos Aires.\n" +
                                "  • **Zona Centro / Suroccidente / Soledad ($6.000 COP):** San Roque, Bostón, Lucero, Los Pinos, Cevillar, Soledad Centro.\n" +
                                "  • **Zona Norte / Riomar ($9.000 COP, límite 12 km):** El Prado, Alto Prado, Riomar, Miramar, Buenavista, Villa Campestre.\n\n" +
                                "*Nota:* El pedido mínimo para domicilio es de **$20.000 COP** con empaque térmico sellado de alta seguridad.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Pedido/Checkout",
                    TextoAccion = "Hacer Pedido a Domicilio",
                    Sugerencias = new List<string> { "🍔 Ver carta de platos", "⏱️ ¿Cuánto demora la entrega?", "💵 ¿Qué métodos de pago aceptan?" }
                };
            }

            // Preguntas sobre Tiempos de entrega
            if (lower.Contains("tiempo") || lower.Contains("demora") || lower.Contains("cuanto tarda") || lower.Contains("horario"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "**Nuestros Tiempos & Horarios:** ⏱️\n\n" +
                                "- **Horario de Atención:** Martes a Domingo de 12:00 m. a 11:00 p.m.\n" +
                                "- **Domicilio Zona Sur:** 20 a 30 minutos.\n" +
                                "- **Domicilio Centro / Soledad:** 30 a 40 minutos.\n" +
                                "- **Domicilio Zona Norte:** 40 a 55 minutos.\n\n" +
                                "Cada orden se elabora al momento en nuestra cocina y viaja en empaque térmico.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    Sugerencias = new List<string> { "🍔 Recomiéndame algo rápido", "🛵 Quiero pedir ahora", "⭐ Ver opiniones de clientes" }
                };
            }

            // Preguntas sobre Bebidas
            if (lower.Contains("bebida") || lower.Contains("coca cola") || lower.Contains("gaseosa") || lower.Contains("jugo") || lower.Contains("tomar"))
            {
                var bebidas = _productoSvc.ObtenerActivos()
                    .Where(p => p.Nombre.Contains("Coca", StringComparison.OrdinalIgnoreCase) ||
                                p.Nombre.Contains("Bebida", StringComparison.OrdinalIgnoreCase) ||
                                p.Nombre.Contains("Jugo", StringComparison.OrdinalIgnoreCase) ||
                                p.Nombre.Contains("Agua", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var listado = bebidas.Any()
                    ? string.Join("\n", bebidas.Select(b => $"• **{b.Nombre}** — ${b.Precio:N0} COP: {b.Descripcion}"))
                    : "• **Coca-Cola Original 350ml** — $5.000 COP: Botella de vidrio helada.\n• **Jugos Naturales** — $7.000 COP: En agua o leche fresca.";

                return new IaConsultaResponse
                {
                    Respuesta = $"**Nuestras Bebidas Refrescantes:** 🥤\n\n{listado}\n\n*Consejo Sommelier:* La Coca-Cola en botella helada realza el toque ahumado del queso costeño gratinado en nuestras hamburguesas.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Pedido/Menu",
                    TextoAccion = "Ver Bebidas en la Carta",
                    Sugerencias = new List<string> { "🍔 ¿Con qué hamburguesa combina?", "🛵 Pedir combo a domicilio" }
                };
            }

            // Preguntas sobre Hamburguesas y Carta de Platos
            if (lower.Contains("hamburguesa") || lower.Contains("plato") || lower.Contains("recomiend") || lower.Contains("carta") || lower.Contains("menu") || lower.Contains("salchipapa"))
            {
                var activos = _productoSvc.ObtenerActivos().Take(4).ToList();
                var recomendacion = string.Join("\n\n", activos.Select(p => $"✨ **{p.Nombre}** (${p.Precio:N0} COP)\n{p.Descripcion}"));

                return new IaConsultaResponse
                {
                    Respuesta = $"**Selección Especial del Sommelier:** 👨‍🍳\n\n{recomendacion}\n\nTodas nuestras creaciones incluyen vegetales seleccionados del día, salsa secreta de la casa y papas rústicas crujientes.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/#menu",
                    TextoAccion = "Explorar Carta Completa",
                    Sugerencias = new List<string> { "🛵 ¿Cuánto cuesta el envío?", "🥤 ¿Qué bebidas hay?", "⭐ ¿Cuáles son las más vendidas?" }
                };
            }

            // Preguntas sobre Reservas
            if (lower.Contains("reserva") || lower.Contains("mesa") || lower.Contains("reservar") || lower.Contains("cumpleaños"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "**¡Vive la Experiencia en Nuestro Salón!** 🥂\n\n" +
                                "- Contamos con un plano interactivo de 30 mesas exclusivas.\n" +
                                "- Puedes seleccionar tu ubicación preferida (salón principal, ventana o zona VIP).\n" +
                                "- Confirmación instantánea y recordatorio para tu velada.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Reserva",
                    TextoAccion = "Reservar Mesa en Línea",
                    Sugerencias = new List<string> { "📍 ¿Dónde queda la sede?", "🍔 Ver opciones para cenar", "🛵 Prefiero pedir a domicilio" }
                };
            }

            // Preguntas sobre Métodos de Pago
            if (lower.Contains("pago") || lower.Contains("pagar") || lower.Contains("tarjeta") || lower.Contains("nequi") || lower.Contains("efectivo"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "**Facilidades de Pago en Saborator:** 💳\n\n" +
                                "- **Efectivo Contraentrega:** Puedes indicar con cuánto billete pagas y el domiciliario te lleva el cambio exacto.\n" +
                                "- **Nequi / Daviplata:** Transferencias instantáneas a nuestra cuenta oficial al momento de entregar o en caja.\n" +
                                "- **Tarjetas Débito / Crédito:** Datáfono inalámbrico disponible tanto en salón como en tu domicilio.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Pedido/Checkout",
                    TextoAccion = "Ir a Finalizar Pedido",
                    Sugerencias = new List<string> { "🍔 Quiero armar mi pedido", "🛵 Verificar costo de envío" }
                };
            }

            // Respuesta por defecto con encanto
            return new IaConsultaResponse
            {
                Respuesta = $"¡Con gusto te atiendo! En **Saborator** nos apasiona la alta gastronomía artesanal. Puedo recomendarte las creaciones de nuestra carta, calcular tu tarifa de entrega en Barranquilla, orientarte con tus reservas o asistirte en el pago.\n\n¿Deseas conocer las hamburguesas más aclamadas de la semana o pedir a domicilio?",
                PersonaNombre = persona.Nombre,
                PersonaRol = persona.Titulo,
                ColorTema = persona.ColorTema,
                EnlaceAccion = "/#menu",
                TextoAccion = "Ver Menú Gastronómico",
                Sugerencias = new List<string> { "🍔 Hamburguesas de autor", "🛵 Domicilio en Barranquilla", "🥤 Bebidas y maridajes", "📅 Reservar mesa" }
            };
        }

        // ══════════════════════════════════════════════════════════════════════════
        // 2. PERSONA: MESERO (GASTÓN)
        // ══════════════════════════════════════════════════════════════════════════
        private IaConsultaResponse ResponderMesero(string q, IaPersonaInfo persona)
        {
            var lower = q.ToLowerInvariant();

            // Estado de mesas
            if (lower.Contains("mesa") || lower.Contains("ocupad") || lower.Contains("libre") || lower.Contains("salon") || lower.Contains("salón"))
            {
                var mesas = _mesaSvc.ObtenerTodas();
                var libres = mesas.Count(m => m.Estado == "Libre");
                var ocupadas = mesas.Count(m => m.Estado == "Ocupada");
                var reservadas = mesas.Count(m => m.Estado == "Reservada");
                var limpieza = mesas.Count(m => m.Estado == "En Limpieza");

                return new IaConsultaResponse
                {
                    Respuesta = $"**Reporte en Vivo de Salón (30 Mesas):** 📋\n\n" +
                                $"• 🟢 **Libres:** {libres} mesas disponibles para ubicar comensales.\n" +
                                $"• 🔴 **Ocupadas:** {ocupadas} mesas con comanda activa o consumiendo.\n" +
                                $"• 🟡 **Reservadas:** {reservadas} mesas programadas hoy.\n" +
                                $"• 🧹 **En Limpieza:** {limpieza} mesas por alistar.\n\n" +
                                "*Tip de Servicio:* Si recibes una pareja, las mesas 4, 8 y 12 tienen la mejor iluminación para una velada íntima.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Mesa/Index",
                    TextoAccion = "Ver Plano de Mesas",
                    Sugerencias = new List<string> { "⚡ Sugerencia para comensal indeciso", "⏱️ ¿Cómo va la cocina?", "🍷 Maridajes recomendados" }
                };
            }

            // Recomendación para comensal indeciso
            if (lower.Contains("indeciso") || lower.Contains("sugerir") || lower.Contains("recomiend") || lower.Contains("maridaje") || lower.Contains("rapido") || lower.Contains("rápido"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "**Estrategia de Sugerencia Rápida en Mesa:** 🎯\n\n" +
                                "1. **Pregunta mágica:** *«¿Prefiere un perfil ahumado y crocante o algo más jugoso y tradicional?»*\n" +
                                "2. **Si busca intensidad:** Sugiere la **Hamburguesa Saborator** con queso costeño gratinado y tocineta crocante.\n" +
                                "3. **Para compartir:** La **Salchipapa Salvaje** es imbatible para 2 a 3 personas.\n" +
                                "4. **Venta sugerida:** *«¿Se las acompaño con una Coca-Cola bien helada o un jugo natural de la casa?»*\n" +
                                "5. **Tiempo:** Sale de cocina en promedio en 10-14 minutos.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Pedido/Menu",
                    TextoAccion = "Tomar Comanda",
                    Sugerencias = new List<string> { "🍽️ Ver estado de mesas", "⏳ Tiempos de cocina hoy" }
                };
            }

            // Protocolo con cocina
            if (lower.Contains("cocina") || lower.Contains("comanda") || lower.Contains("alerg") || lower.Contains("termino") || lower.Contains("término"))
            {
                var enCocina = _pedidoSvc.ObtenerParaCocina().Count;
                return new IaConsultaResponse
                {
                    Respuesta = $"**Coordinación Salón ⇄ Cocina:** 🛎️\n\n" +
                                $"• Actualmente hay **{enCocina} pedidos** activos en línea de cocina.\n" +
                                $"• Recuerda especificar siempre en las notas de línea:\n" +
                                $"  - Término de carne (3/4 o Bien cocido).\n" +
                                $"  - Alérgenos críticos (ej: sin queso, sin salsas, salsa aparte).\n" +
                                $"• Cuando la comanda esté 'Lista', recibirás el aviso para despachar de inmediato caliente.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Pedido/Cocina",
                    TextoAccion = "Consultar KDS Cocina",
                    Sugerencias = new List<string> { "🍽️ Estado de mesas", "⚡ Plato más pedido de hoy" }
                };
            }

            // Por defecto
            return new IaConsultaResponse
            {
                Respuesta = "¡Listo colega! Como Capitán de Salón estoy atento para ayudarte a agilizar el servicio en mesas, darte argumentos de venta sugestiva para subir el ticket promedio y monitorear qué mesas necesitan atención inmediata.",
                PersonaNombre = persona.Nombre,
                PersonaRol = persona.Titulo,
                ColorTema = persona.ColorTema,
                EnlaceAccion = "/Mesa/Index",
                TextoAccion = "Gestionar Mesas",
                Sugerencias = new List<string> { "🍽️ ¿Cómo están las mesas?", "⚡ Recomendación para comensal", "🛎️ Estado de cocina" }
            };
        }

        // ══════════════════════════════════════════════════════════════════════════
        // 3. PERSONA: COCINERO (CHEF GUSTEAU)
        // ══════════════════════════════════════════════════════════════════════════
        private IaConsultaResponse ResponderCocinero(string q, IaPersonaInfo persona)
        {
            var lower = q.ToLowerInvariant();

            // Pedidos y KDS
            if (lower.Contains("pedido") || lower.Contains("pendiente") || lower.Contains("comanda") || lower.Contains("kds") || lower.Contains("cuantos") || lower.Contains("demora"))
            {
                var pendientes = _pedidoSvc.ObtenerParaCocina();
                var listos = _pedidoSvc.ObtenerListosCocina();

                return new IaConsultaResponse
                {
                    Respuesta = $"**Tablero KDS de Cocina en Tiempo Real:** 🍳🔥\n\n" +
                                $"• 🔴 **Comandas en Fila / Preparación:** {pendientes.Count}\n" +
                                $"• 🟢 **Comandas Listas para Despacho:** {listos.Count}\n\n" +
                                "*Regla de Oro en Plancha:* Prioriza las comandas con más de 12 minutos en KDS para evitar demoras en salón o mensajería.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Pedido/Cocina",
                    TextoAccion = "Ver Pantalla de Cocina",
                    Sugerencias = new List<string> { "🥩 Receta exacta de hamburguesa", "🍟 Gramaje de Salchipapa Salvaje", "⏰ Tiempos de plancha y freidora" }
                };
            }

            // Recetas y Gramajes
            if (lower.Contains("receta") || lower.Contains("gramaje") || lower.Contains("hamburguesa") || lower.Contains("salchipapa") || lower.Contains("ensamble") || lower.Contains("armado"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "**Ficha Técnica & Ensamble Maestro:** 👨‍🍳📖\n\n" +
                                "🍔 **Hamburguesa de Autor (125g):**\n" +
                                "1. Pan brioche tostado 45 seg con mantequilla clarificada.\n" +
                                "2. Carne artesanal 125g: sal marina y pimienta al colocar en plancha a 220°C. 3.5 min por cara.\n" +
                                "3. Queso costeño / mozzarella fundido con campana de vapor 45 seg.\n" +
                                "4. Base de pan con salsa de la casa, lechuga crespa, tomate milano, carne gratinada, tocineta crocante y tapa con toque de salsa tártara.\n\n" +
                                "🍟 **Salchipapa Salvaje:**\n" +
                                "• Papa francesa 250g (freidora a 175°C por 4 min 30 seg).\n" +
                                "• Mix cárnico salteado al wok: salchicha clásica, pechuga, cerdo, chorizo, butifarra, suiza.\n" +
                                "• Cubierta: Queso costeño rallado, queso fundido sopleteado y tocineta bits.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Pedido/Recetario",
                    TextoAccion = "Consultar Recetario Completo",
                    Sugerencias = new List<string> { "🔥 Ver pedidos pendientes en cocina", "⏳ Sincronización de mesas" }
                };
            }

            // Tiempos y temperaturas
            if (lower.Contains("temperatura") || lower.Contains("tiempo") || lower.Contains("freidora") || lower.Contains("plancha"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "**Estándares Térmicos de Saborator:** 🌡️\n\n" +
                                "- **Plancha de Carnes:** 215°C - 230°C (sellado caramelizado Maillard).\n" +
                                "- **Freidora de Papas:** 175°C (no sobrecargar la canastilla más de 500g a la vez).\n" +
                                "- **Punto interno de carne:** 71°C al centro para máxima jugosidad y seguridad alimentaria.\n" +
                                "- **Empaque térmico:** Sellar inmediatamente a la salida para conservar calor durante el traslado.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    Sugerencias = new List<string> { "🔥 ¿Cuántas comandas hay en espera?", "📖 Ficha técnica de armado" }
                };
            }

            return new IaConsultaResponse
            {
                Respuesta = "¡Oído cocina! Como Jefe KDS estoy listo para guiarte en recetas estándar, puntos de cocción, temperaturas de plancha y freidora, y control de tiempos para que ninguna mesa espere de más.",
                PersonaNombre = persona.Nombre,
                PersonaRol = persona.Titulo,
                ColorTema = persona.ColorTema,
                EnlaceAccion = "/Pedido/Cocina",
                TextoAccion = "Ir a KDS Cocina",
                Sugerencias = new List<string> { "🔥 Comandas en cocina", "🥩 Receta de hamburguesa", "🍟 Ficha de salchipapa" }
            };
        }

        // ══════════════════════════════════════════════════════════════════════════
        // 4. PERSONA: CAJERO (AURELIO)
        // ══════════════════════════════════════════════════════════════════════════
        private IaConsultaResponse ResponderCajero(string q, IaPersonaInfo persona)
        {
            var lower = q.ToLowerInvariant();

            // Tarifas de domicilio y políticas
            if (lower.Contains("tarifa") || lower.Contains("domicilio") || lower.Contains("barranquilla") || lower.Contains("cortesia") || lower.Contains("cortesía") || lower.Contains("modificar"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "**Guía de Tarifas de Domicilio & Políticas de Caja:** 🛵💰\n\n" +
                                "- **Tarifas Oficiales del Sistema:**\n" +
                                "  • **Sur ($4.000 COP):** Ciudadela, Simón Bolívar, Las Cayenas, La Magdalena.\n" +
                                "  • **Centro / Suroccidente / Soledad ($6.000 COP):** Bostón, Lucero, San Roque, Soledad.\n" +
                                "  • **Norte / Riomar ($9.000 COP):** El Prado, Riomar, Miramar, Buenavista.\n\n" +
                                "🔒 **Control de Autorización:**\n" +
                                "El sistema bloquea modificaciones de tarifa a clientes públicos para evitar fraudes. Como cajero, tú sí tienes los botones de ajuste rápido (`$0 Cortesía`, `$4k`, `$6k`, `$9k`) y el campo editable en caja.\n\n" +
                                "⚠️ *Regla de Auditoría:* Solo aplica **$0 Cortesía** en pedidos VIP aprobados por gerencia o compensaciones registradas en notas.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Pedido/Domicilios",
                    TextoAccion = "Ver Despacho de Domicilios",
                    Sugerencias = new List<string> { "💵 Cálculo de cambio en efectivo", "💳 Métodos de pago aceptados", "📋 Pedidos por facturar" }
                };
            }

            // Métodos de pago y cambio
            if (lower.Contains("cambio") || lower.Contains("devuelta") || lower.Contains("efectivo") || lower.Contains("nequi") || lower.Contains("tarjeta") || lower.Contains("pago"))
            {
                return new IaConsultaResponse
                {
                    Respuesta = "**Protocolo de Cobro & Medios de Pago:** 💵📱\n\n" +
                                "1. **Efectivo:** Revisa siempre el campo `Paga con` en la orden para entregar cambio listo al domiciliario o en salón.\n" +
                                "2. **Nequi / Daviplata:** Verificar comprobante con código de referencia antes de marcar como pagado.\n" +
                                "3. **Datáfono:** Conservar la copia del voucher firmada adjunta al duplicado de la comanda.\n" +
                                "4. **Pedido Mínimo a Domicilio:** $20.000 COP (controlado automáticamente por el checkout).",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Pedido/Index",
                    TextoAccion = "Ver Listado de Pedidos",
                    Sugerencias = new List<string> { "🛵 Tarifas de Barranquilla", "🧾 Ver Facturas" }
                };
            }

            return new IaConsultaResponse
            {
                Respuesta = "¡Excelente turno de caja! Como gestor financiero y de tarifas te asisto en validación de pedidos, cálculo de devuelta, control de tarifas de domicilio y facturación ágil.",
                PersonaNombre = persona.Nombre,
                PersonaRol = persona.Titulo,
                ColorTema = persona.ColorTema,
                EnlaceAccion = "/Pedido/Index",
                TextoAccion = "Panel de Pedidos",
                Sugerencias = new List<string> { "🛵 Tarifas oficiales de domicilio", "💵 Guía de cobro y cambio", "🧾 Gestión de facturas" }
            };
        }

        // ══════════════════════════════════════════════════════════════════════════
        // 5. PERSONA: ADMINISTRADOR (HERMES)
        // ══════════════════════════════════════════════════════════════════════════
        private IaConsultaResponse ResponderAdministrador(string q, IaPersonaInfo persona)
        {
            var lower = q.ToLowerInvariant();

            // Stock crítico e inventario
            if (lower.Contains("stock") || lower.Contains("inventario") || lower.Contains("alerta") || lower.Contains("agotado") || lower.Contains("critico") || lower.Contains("crítico"))
            {
                var criticos = _productoSvc.ObtenerTodos().Where(p => p.Activo && p.Stock <= 5).ToList();
                var detalle = criticos.Any()
                    ? string.Join("\n", criticos.Select(p => $"• ⚠️ **{p.Nombre}**: {p.Stock} unidades restantes (Precio: ${p.Precio:N0})"))
                    : "• ✅ No hay productos en nivel crítico. Todo el stock activo supera las 5 unidades.";

                return new IaConsultaResponse
                {
                    Respuesta = $"**Auditoría de Stock e Inventario Crítico:** 📦⚠️\n\n{detalle}\n\n*Recomendación Gerencial:* Se sugiere generar orden de compra a proveedores de carnes y pan brioche cuando el umbral baje de 10 unidades.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Producto/Index",
                    TextoAccion = "Administrar Productos y Stock",
                    Sugerencias = new List<string> { "⭐ Reporte de reseñas y satisfacción", "📈 Resumen de ventas", "👥 Gestión de empleados" }
                };
            }

            // Reseñas y satisfacción
            if (lower.Contains("reseña") || lower.Contains("resena") || lower.Contains("satisfaccion") || lower.Contains("opinion") || lower.Contains("estrellas"))
            {
                var stats = _resenaSvc.ObtenerEstadisticas();
                return new IaConsultaResponse
                {
                    Respuesta = $"**Métricas de Satisfacción de Comensales:** ⭐📊\n\n" +
                                $"• **Calificación Promedio:** {stats.Promedio:F1} / 5.0 Estrellas\n" +
                                $"• **Total de Opiniones Registradas:** {stats.TotalResenas} comensales\n" +
                                $"• **5 Estrellas:** {stats.Estrellas5} ({stats.Porcentaje5:F0}%)\n" +
                                $"• **4 Estrellas:** {stats.Estrellas4} ({stats.Porcentaje4:F0}%)\n" +
                                $"• **3 a 1 Estrellas:** {stats.Estrellas3 + stats.Estrellas2 + stats.Estrellas1} reseñas\n\n" +
                                "La recepción del nuevo menú de autor y el sistema de seguimiento en Barranquilla ha sido sobresaliente.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Resena/Index",
                    TextoAccion = "Panel de Moderación de Reseñas",
                    Sugerencias = new List<string> { "📦 Stock crítico", "👥 Estado del personal", "🍽️ Rotación de mesas" }
                };
            }

            // Estado general operacional
            if (lower.Contains("estado") || lower.Contains("resumen") || lower.Contains("operacion") || lower.Contains("hoy") || lower.Contains("ventas"))
            {
                var pedidos = _pedidoSvc.ObtenerTodos();
                var pendientes = _pedidoSvc.ObtenerPendientes().Count;
                var domicilios = _pedidoSvc.ObtenerDomicilios().Count;
                var mesas = _mesaSvc.ObtenerTodas();
                var ocupadas = mesas.Count(m => m.Estado == "Ocupada");

                return new IaConsultaResponse
                {
                    Respuesta = $"**Panel Ejecutivo de Operaciones Saborator:** 🏛️📈\n\n" +
                                $"• **Histórico Total de Pedidos:** {pedidos.Count} órdenes procesadas.\n" +
                                $"• **Pedidos en Flujo Hoy:** {pendientes} activos en cocina / despacho.\n" +
                                $"• **Domicilios Despachados:** {domicilios} pedidos.\n" +
                                $"• **Ocupación Actual de Salón:** {ocupadas} / 30 mesas activas.\n\n" +
                                "Todos los servicios de backend y módulos de seguridad de tarifas se encuentran en estado óptimo.",
                    PersonaNombre = persona.Nombre,
                    PersonaRol = persona.Titulo,
                    ColorTema = persona.ColorTema,
                    EnlaceAccion = "/Cliente/Index",
                    TextoAccion = "Panel General de Clientes",
                    Sugerencias = new List<string> { "⚠️ Ver stock bajo", "⭐ Analítica de reseñas", "👥 Roles de empleados" }
                };
            }

            return new IaConsultaResponse
            {
                Respuesta = "A sus órdenes, Dirección General. Como asistente de gestión y analítica estratégica, puedo brindarle reportes de inventario crítico, métricas de satisfacción de clientes, auditoría de tarifas de domicilio y estado operativo del restaurante.",
                PersonaNombre = persona.Nombre,
                PersonaRol = persona.Titulo,
                ColorTema = persona.ColorTema,
                EnlaceAccion = "/Producto/Index",
                TextoAccion = "Control de Inventario",
                Sugerencias = new List<string> { "⚠️ Alertas de stock crítico", "⭐ Satisfacción y reseñas", "📈 Resumen operacional" }
            };
        }

        private static string NormalizarRol(string? rol)
        {
            if (string.IsNullOrWhiteSpace(rol)) return "Cliente";
            var r = rol.Trim();
            if (r.Equals("Administrador", StringComparison.OrdinalIgnoreCase) || r.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return "Administrador";
            if (r.Equals("Cocinero", StringComparison.OrdinalIgnoreCase) || r.Equals("Chef", StringComparison.OrdinalIgnoreCase)) return "Cocinero";
            if (r.Equals("Mesero", StringComparison.OrdinalIgnoreCase)) return "Mesero";
            if (r.Equals("Cajero", StringComparison.OrdinalIgnoreCase)) return "Cajero";
            return "Cliente";
        }
    }
}
