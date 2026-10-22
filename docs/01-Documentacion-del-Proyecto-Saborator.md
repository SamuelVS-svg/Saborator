# SABORATOR
## Sistema de Gestión Integral para Restaurantes

**Documentación del Proyecto**

| Campo | Información |
|---|---|
| Proyecto | Saborator — Software de gestión para restaurantes |
| Programa de formación | Análisis y Desarrollo de Software (ADSO) — SENA |
| Ficha | [NÚMERO DE FICHA] |
| Aprendices | [NOMBRES Y APELLIDOS DE LOS APRENDICES] |
| Instructor / Líder técnico | [NOMBRE DEL INSTRUCTOR] |
| Centro de formación | [CENTRO DE FORMACIÓN] |
| Ciudad y fecha | Barranquilla, [MES] de [AÑO] |

> Los campos entre corchetes deben ser diligenciados por el equipo antes de la entrega.

---

## Tabla de contenido

1. Introducción y Contexto
2. Planteamiento del Problema
3. Objetivo General
4. Tecnologías Utilizadas
5. Conclusiones

---

## 1. Introducción y Contexto

### 1.1 ¿De qué trata Saborator?

**Saborator** es una aplicación web de gestión para restaurantes que integra en una sola plataforma todo el ciclo de atención al comensal: la consulta de la carta digital, la reserva de mesas, la toma y el seguimiento de pedidos (en salón, para llevar y a domicilio), la gestión de comandas en cocina, el cobro y la facturación en caja, el inventario, el control de proveedores y los informes administrativos.

El sistema está pensado para un restaurante de alta cocina con servicio en salón y domicilios, y atiende de manera diferenciada a **seis perfiles de usuario**: Cliente, Mesero, Cocinero, Cajero, Domiciliario y Administrador. Cada perfil ve únicamente las opciones que le corresponden, lo que reduce errores operativos y protege la información sensible del negocio.

### 1.2 Contexto

Los restaurantes pequeños y medianos de la región Caribe colombiana suelen operar con procesos manuales: comandas escritas en papel, pedidos tomados por teléfono o WhatsApp, cuentas calculadas a mano y cierres de caja que dependen de la memoria del cajero. Al mismo tiempo, el cliente actual espera reservar en línea, pedir a domicilio, pagar con medios digitales (Nequi, PSE, tarjeta) y conocer el estado de su pedido en tiempo real.

Saborator nace para cerrar esa brecha entre la forma tradicional de operar y las expectativas del comensal digital, ofreciendo un sistema que se puede usar desde un computador, una tableta de salón o un teléfono móvil.

### 1.3 Alcance

El sistema cubre los siguientes módulos:

| Módulo | Descripción |
|---|---|
| Carta digital y página de inicio | Presentación del menú por categorías, ingredientes, alérgenos y precios; reseñas de clientes y ubicación de la sede. |
| Reservas | Selección de fecha, turno y número de personas; plano interactivo de mesas; confirmación y consulta de reservas; gestión administrativa. |
| Pedidos | Toma de pedidos por el mesero, pedidos en línea del cliente (domicilio o para llevar), cupones de descuento y seguimiento por código. |
| Cocina | Tablero de comandas en tiempo real con estados de preparación y recetario. |
| Domicilios | Tarifas por barrio, asignación de repartidor, ruta y confirmación de entrega. |
| Caja y facturación | Apertura y cierre de turno con arqueo, emisión y anulación de facturas, propinas e informes exportables. |
| Pagos en línea | Pasarela Wompi (tarjeta, Nequi, PSE) con modo de pruebas y modo de demostración simulado. |
| Inventario y proveedores | Productos, existencias, categorías y proveedores. |
| Administración | Panel de indicadores, usuarios del personal, clientes, cupones, reseñas y bitácora de auditoría. |
| Seguridad y cuentas | Registro con verificación por código (OTP) y control de acceso por rol. |

**Fuera del alcance** de esta versión: facturación electrónica ante la DIAN, nómina y contabilidad, y aplicación móvil nativa (la plataforma funciona como aplicación web adaptable a pantallas pequeñas).

### 1.4 Impacto esperado

- **Operativo:** menos errores de transcripción entre salón, cocina y caja, porque la comanda viaja digitalmente de un rol a otro.
- **Económico:** control del inventario y de las ventas diarias, con información disponible para decidir (ventas del día frente al promedio de la semana, productos con stock crítico, ocupación del salón).
- **Servicio al cliente:** reserva, pedido y pago en línea, y seguimiento del pedido sin necesidad de llamar al restaurante.
- **Seguridad y trazabilidad:** cada acción relevante queda registrada en una bitácora de auditoría y cada rol accede solo a lo necesario.
- **Social y formativo:** el proyecto aplica buenas prácticas de ingeniería de software (arquitectura por capas, pruebas automatizadas y seguridad por diseño) a un problema real del sector gastronómico.

---

## 2. Planteamiento del Problema

La gestión tradicional de un restaurante presenta falencias que afectan la rentabilidad, la calidad del servicio y la satisfacción del cliente. Las más relevantes se describen a continuación.

### 2.1 Gestión de comandas

- **Comandas en papel:** se extravían, son ilegibles o se escriben de forma incompleta, lo que genera platos equivocados, devoluciones y desperdicio de insumos.
- **Comunicación lenta entre salón y cocina:** el mesero debe caminar hasta la cocina para entregar la nota y regresar para saber si el plato está listo, lo que alarga los tiempos de espera.
- **Sin priorización ni trazabilidad:** la cocina no sabe con certeza cuánto lleva esperando cada pedido ni quién modificó una comanda.

### 2.2 Gestión de pedidos y reservas

- **Pedidos por teléfono o mensajería:** dependen de que alguien atienda, se toman datos a mano y no existe un registro único del estado del pedido ni de su domicilio.
- **Reservas sin control:** los cuadernos de reservas generan cruces de mesa, doble asignación de turnos y ausencias sin garantía.
- **El cliente no tiene visibilidad:** no puede saber si su pedido está en preparación, en camino o entregado, y debe llamar para averiguarlo.

### 2.3 Gestión de caja

- **Cuentas calculadas manualmente:** aumentan los errores en sumas, propinas y descuentos.
- **Cierres de caja poco confiables:** sin un turno de caja registrado, las diferencias entre lo vendido y lo recibido son difíciles de explicar.
- **Medios de pago dispersos:** efectivo, transferencias y pagos por billeteras digitales se concilian de forma manual, con riesgo de comprobantes falsos o duplicados.
- **Facturas sin control:** numeración irregular, anulaciones sin responsable y ausencia de historial exportable.

### 2.4 Inventario y administración

- **Existencias desactualizadas:** el stock se descuenta a mano o no se descuenta, lo que provoca ventas de productos agotados y compras tardías.
- **Información dispersa:** las ventas, los proveedores y los clientes viven en hojas de cálculo o cuadernos distintos, sin informes que apoyen la toma de decisiones.
- **Poca seguridad de la información:** cuentas compartidas, contraseñas débiles y ausencia de registro sobre quién hizo qué.

### 2.5 Formulación del problema

> **¿Cómo integrar en una sola plataforma web la gestión de reservas, comandas, pedidos, domicilios, cobro en caja e inventario de un restaurante, de modo que se reduzcan los errores operativos, se agilice la atención y la administración cuente con información confiable en tiempo real?**

---

## 3. Objetivo General

Desarrollar e implementar **Saborator**, un sistema web de gestión para restaurantes que integre reservas, toma de pedidos, comandas de cocina, domicilios, cobro y facturación en caja, inventario e informes administrativos, con acceso diferenciado para **seis roles** (Cliente, Mesero, Cocinero, Cajero, Domiciliario y Administrador), y que permita registrar el ciclo completo de un pedido —desde su creación hasta su pago y facturación— de forma digital, trazable y segura.

**Criterios de cumplimiento (medibles):**

| Criterio | Meta |
|---|---|
| Cobertura de roles | 6 de 6 roles con acceso diferenciado y funciones propias. |
| Ciclo del pedido | El 100 % de los pedidos registrados con estado verificable (recibido, en preparación, listo, en camino o entregado, facturado o cancelado). |
| Cierre de caja | Cada turno de caja con arqueo registrado y diferencia calculada automáticamente. |
| Pagos en línea | Pago aprobado o rechazado reflejado en el pedido y con comprobante digital, sin intervención manual. |
| Calidad del software | Pruebas automatizadas de las reglas de negocio y de seguridad ejecutadas sin fallos antes de cada entrega. |
| Seguridad | Control de acceso por rol en todas las rutas del personal, contraseñas cifradas y bitácora de auditoría de las acciones críticas. |

---

## 4. Tecnologías Utilizadas

### 4.1 Arquitectura general

Saborator es una aplicación web **cliente–servidor** con arquitectura por capas basada en el patrón **MVC** (Modelo–Vista–Controlador):

```
┌────────────────────────────────────────────────────────────┐
│  CLIENTE (navegador web, tableta o teléfono)              │
│  Vistas Razor + HTML/CSS/JavaScript · Módulo React (Vite)  │
└──────────────┬─────────────────────────────┬───────────────┘
               │ HTTPS                       │ WebSockets (SignalR)
┌──────────────▼─────────────────────────────▼───────────────┐
│  SERVIDOR — ASP.NET Core (.NET 10)                          │
│  Controladores → Servicios (reglas de negocio) → Repositorios│
│  Autenticación por cookies · Roles · Limitación de peticiones│
└───────┬───────────────────┬───────────────────┬────────────┘
        │ Entity Framework  │ API REST          │ SMTP / API
┌───────▼───────┐   ┌───────▼───────┐   ┌───────▼────────────┐
│  SQL Server   │   │  Wompi        │   │  Correo (OTP),     │
│  (SQLite de   │   │  (pagos)      │   │  SMS / WhatsApp    │
│  respaldo)    │   │               │   │  (Twilio)          │
└───────────────┘   └───────────────┘   └────────────────────┘
```

### 4.2 Backend

| Tecnología | Uso en el proyecto |
|---|---|
| **.NET 10 / ASP.NET Core MVC (C#)** | Lógica del servidor, controladores por módulo, servicios de negocio y repositorios. |
| **Entity Framework Core 10** | Acceso a datos y modelo de entidades (pedidos, facturas, productos, reservas, usuarios, turnos de caja, transacciones de pago, auditoría). |
| **SignalR** | Comunicación en tiempo real: tablero de cocina, avisos a domiciliarios y seguimiento del pedido por el cliente. |
| **BCrypt.Net** | Cifrado (hash) de contraseñas. |
| **Autenticación por cookies y autorización por roles** | Acceso diferenciado para Cliente, Mesero, Cocinero, Cajero, Domiciliario y Administrador; inicio de sesión también con cuenta de Google. |
| **Limitación de peticiones (Rate Limiting)** | Protección contra abuso en rutas públicas y en el envío de códigos OTP. |
| **xUnit** | Pruebas automatizadas (38 métodos de prueba) de reglas de negocio, inventario, reparto de domicilios, verificación de correo y seguridad de pagos. |

### 4.3 Frontend

| Tecnología | Uso en el proyecto |
|---|---|
| **Vistas Razor (Razor Pages/MVC), HTML5, CSS3 y JavaScript** | Interfaz principal para todos los roles: carta digital, checkout, reservas, panel administrativo, caja, cocina y repartos. Diseño adaptable (responsive), modo claro y oscuro. |
| **React 19 + Vite** | Módulo visual integrado ("Smash Lab"): experiencia interactiva de presentación del producto, compilado y servido dentro de la misma aplicación. |
| **Tailwind CSS, Three.js, Framer Motion y GSAP** | Estilos, gráficos 3D y animaciones del módulo en React. |
| **PWA (Service Worker y manifiesto)** | Instalación en tabletas de salón y recursos estáticos en caché. |

### 4.4 Base de datos

- **Microsoft SQL Server** como motor de base de datos principal (relacional, transaccional).
- **SQLite** como base de respaldo automática cuando SQL Server no está disponible (por ejemplo, en una demostración en la nube).
- Mecanismo de actualización de esquema al iniciar la aplicación y reintentos automáticos ante fallos transitorios de conexión.

### 4.5 Servicios externos

| Servicio | Función |
|---|---|
| **Wompi (Bancolombia)** | Pasarela de pagos en línea: tarjeta de crédito/débito, Nequi y PSE. El servidor define el monto y firma la sesión de pago, verifica el resultado contra la API de Wompi y recibe notificaciones (webhook) de transacciones aprobadas. Incluye un **modo de pruebas (sandbox)** y un **checkout de demostración simulado** para presentaciones sin conexión ni cobro real. |
| **Correo electrónico (SMTP) — OTP** | Envío de códigos de verificación de seis dígitos para validar cuentas nuevas y de comprobantes de pago y confirmaciones de reserva. |
| **SMS y WhatsApp (Twilio)** | Canal alterno para el envío del código OTP y avisos al cliente. |
| **Google Sign-In** | Registro e inicio de sesión con cuenta de Google. |

### 4.6 Seguridad y calidad

- Control de acceso por rol en cada controlador y en el canal de tiempo real.
- Protección contra falsificación de peticiones (tokens anti-CSRF) y contra acceso a datos de otros usuarios (verificación del propietario del pedido o la reserva).
- Montos de pago calculados siempre en el servidor, nunca tomados del navegador.
- Bitácora de auditoría de acciones críticas, exportable a CSV.
- Pruebas automatizadas de reglas de negocio y de seguridad.
- Despliegue mediante contenedor (Docker) y publicación en un servidor propio con acceso seguro por internet.

---

## 5. Conclusiones

1. Saborator integra en una sola plataforma los procesos que en un restaurante tradicional se manejan por separado (reservas, comandas, caja e inventario), lo que reduce la dependencia del papel y de la memoria del personal.
2. La separación en seis roles permite que cada persona vea solo lo que necesita, mejorando la operación y la seguridad de la información.
3. El diseño por capas, las pruebas automatizadas y la verificación de pagos en el servidor hacen el sistema mantenible y confiable.
4. La integración con Wompi y con códigos OTP acerca el restaurante a los hábitos de pago y de verificación digital del cliente actual; el modo de demostración garantiza que el sistema pueda presentarse incluso sin conexión con la pasarela.

**Trabajo futuro:** facturación electrónica ante la DIAN, informes avanzados y exportación a hojas de cálculo, aplicación móvil nativa para meseros y domiciliarios, impresión de comandas en cocina, módulo de recetas con costeo por plato, y programa de fidelización de clientes.

---

## Referencias

- Microsoft. *Documentación de ASP.NET Core y Entity Framework Core.* learn.microsoft.com
- Wompi. *Documentación para desarrolladores — Colombia.* docs.wompi.co
- Facebook/Meta. *React — Documentación oficial.* react.dev
- SENA. *Guía para la presentación de proyectos del programa Análisis y Desarrollo de Software.* [Completar con la guía vigente del centro de formación]
