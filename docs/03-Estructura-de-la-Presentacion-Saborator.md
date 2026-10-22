# SABORATOR — Estructura de la Presentación (Plantilla SENA)

**Duración sugerida:** 12 a 15 minutos (11 diapositivas).
**Cada diapositiva incluye:** título, contenido para proyectar y **guion del expositor** (lo que se dice en voz alta).
**Consejo de diseño:** fondo blanco o crema, un solo color de acento (burdeos del sistema), máximo 5 líneas por diapositiva y letra mínima de 24 pt. Las capturas de pantalla deben tomarse del sistema en funcionamiento.

---

## Diapositiva 1 — Portada

**Contenido para proyectar**

> # SABORATOR
> ### Sistema de gestión integral para restaurantes
>
> **Aprendices:** [NOMBRES Y APELLIDOS]
> **Ficha:** [NÚMERO DE FICHA]
> **Programa:** Análisis y Desarrollo de Software (ADSO)
> **Centro de formación:** [CENTRO] — SENA
> **Instructor:** [NOMBRE DEL INSTRUCTOR]
> Barranquilla, [AÑO]

**Elementos visuales:** logo de Saborator (hamburguesa dorada), logo del SENA, captura de la página de inicio como imagen de fondo difuminada.

**Guion:** "Buenos días. Somos [nombres], aprendices del programa Análisis y Desarrollo de Software, ficha [número]. Hoy presentamos Saborator, un sistema web para gestionar un restaurante de principio a fin."

---

## Diapositiva 2 — De qué trata Saborator (definición y justificación)

**Contenido para proyectar**

**¿Qué es?**
- Aplicación web que integra reservas, pedidos, cocina, domicilios, caja e inventario.
- Seis perfiles de usuario: Cliente, Mesero, Cocinero, Cajero, Domiciliario y Administrador.

**¿Por qué se justifica?**
- Los restaurantes pequeños aún operan con papel, llamadas y cuentas manuales.
- El cliente de hoy espera reservar, pedir y pagar en línea y ver el estado de su pedido.
- Una plataforma única reduce errores, agiliza la atención y da información al administrador.

**Elemento visual:** esquema simple del ciclo "Reserva → Pedido → Cocina → Entrega → Pago → Factura" con los iconos de cada rol.

**Guion:** "Saborator conecta a todas las personas que participan en la atención de un restaurante. Se justifica porque la gestión tradicional genera errores y demoras, y porque el comensal ya espera servicios digitales."

---

## Diapositiva 3 — Planteamiento del problema

**Contenido para proyectar**

| Área | Falencia en la gestión tradicional |
|---|---|
| Comandas | Notas en papel que se pierden o no se entienden; cocina y salón sin comunicación inmediata. |
| Pedidos y reservas | Atención por teléfono, cruces de mesas, el cliente no sabe en qué estado va su pedido. |
| Caja | Cuentas a mano, cierres sin respaldo, comprobantes de pago sin verificar. |
| Inventario | Existencias desactualizadas, ventas de productos agotados. |
| Administración | Información dispersa y sin registro de quién hizo qué. |

**Pregunta de investigación (en recuadro):** *¿Cómo integrar en una sola plataforma la gestión de reservas, comandas, pedidos, cobro en caja e inventario de un restaurante para reducir errores y contar con información confiable en tiempo real?*

**Guion:** "El problema no es una sola falla, sino la suma de procesos manuales desconectados. Cada uno genera errores que se acumulan: platos equivocados, esperas largas y cajas que no cuadran."

---

## Diapositiva 4 — Objetivo general

**Contenido para proyectar**

> **Desarrollar e implementar Saborator, un sistema web de gestión para restaurantes que integre reservas, pedidos, cocina, domicilios, caja, inventario e informes, con acceso diferenciado para seis roles, registrando el ciclo completo del pedido de forma digital, trazable y segura.**

**Cómo se mide (3 indicadores):**
- 6 de 6 roles con funciones propias.
- 100 % de los pedidos con estado verificable.
- Cada turno de caja con arqueo y diferencia calculada automáticamente.

**Guion:** "El objetivo es medible: sabemos que lo cumplimos cuando los seis roles operan con sus funciones, cada pedido tiene un estado y cada turno de caja se cierra con arqueo."

---

## Diapositiva 5 — Tecnologías y arquitectura del sistema

**Contenido para proyectar**

**Diagrama de capas (de arriba hacia abajo):**
1. **Cliente:** navegador (computador, tableta, teléfono) — vistas Razor, HTML/CSS/JavaScript y módulo en React.
2. **Servidor:** ASP.NET Core (.NET 10), patrón MVC, servicios de negocio, SignalR (tiempo real).
3. **Datos:** SQL Server con Entity Framework Core (SQLite de respaldo).
4. **Servicios externos:** Wompi (pagos), correo electrónico y SMS/WhatsApp para códigos OTP, Google Sign-In.

**Calidad y seguridad (3 viñetas):**
- Acceso por rol, contraseñas cifradas (BCrypt), protección anti-CSRF.
- Pagos con monto definido y verificado en el servidor.
- 38 métodos de prueba automatizados (xUnit).

**Guion:** "Usamos una arquitectura por capas. La interfaz principal está en vistas Razor de .NET y un módulo visual en React. El servidor aplica las reglas de negocio y usa SignalR para actualizar cocina en tiempo real. Los datos están en SQL Server y los pagos pasan por Wompi."

---

## Diapositiva 6 — Demostración 1: Cliente (carta, reserva y pedido)

**Qué mostrar en vivo (≈ 2 min)**
1. Página de inicio y **carta digital** con ingredientes y alérgenos.
2. **Reservar Mesa**: fecha, turno, plano de mesas y confirmación con código.
3. **Hacer Pedido** (para llevar o domicilio): carrito, cupón y confirmación.
4. **Rastrear Pedido** con el código PED-xxxxx.

**Capturas de respaldo:** carta, plano de mesas, seguimiento del pedido.

**Guion:** "El cliente consulta la carta, reserva su mesa en un plano interactivo, hace su pedido y puede seguirlo con un código sin llamar al restaurante."

---

## Diapositiva 7 — Demostración 2: Mesero y Cocina

**Qué mostrar en vivo (≈ 2 min)**
1. Sesión de **Mesero**: tablero de mesas y **toma de pedido** por mesa (con notas).
2. En otra pantalla, sesión de **Cocinero**: la comanda aparece **sin recargar** (tiempo real).
3. Pulsar **Iniciar Preparación** y luego **¡Comanda Lista! (Despachar)**.
4. El mesero ve el cambio de estado.

**Capturas de respaldo:** tablero de mesas y tablero de cocina.

**Guion:** "Aquí se resuelve el problema de las comandas en papel: el pedido viaja digitalmente del salón a la cocina y vuelve el aviso cuando está listo."

---

## Diapositiva 8 — Demostración 3: Pago en línea

**Qué mostrar en vivo (≈ 2 min)**
1. Desde el pedido, pulsar **Pagar con Wompi**.
2. Elegir **Tarjeta, Nequi o PSE** y completar el pago (en modo de prueba o de demostración).
3. Mostrar el **comprobante** con número de factura y el pedido ya en estado **Facturado**.
4. Mostrar también un pago **rechazado** y cómo el sistema lo informa.

**Idea clave (en recuadro):** el monto lo calcula y verifica el servidor; el navegador no puede alterarlo.

**Guion:** "El pago se hace con la pasarela Wompi. Si es aprobado, el sistema factura automáticamente y genera el comprobante. En esta demostración no se realiza ningún cobro real."

---

## Diapositiva 9 — Demostración 4: Cajero (cobro y turno de caja)

**Qué mostrar en vivo (≈ 2 min)**
1. **Abrir turno** con la base inicial en efectivo.
2. **Cobrar en Caja** un pedido de mesa con propina del 10 % y medio de pago.
3. Consultar la **factura** emitida (y la opción de anular).
4. **Realizar Arqueo & Cerrar Turno**: efectivo declarado y diferencia calculada.

**Capturas de respaldo:** apertura de turno, factura y arqueo.

**Guion:** "En caja cada jornada queda registrada en un turno. Al cierre el sistema compara el efectivo esperado con el declarado y deja el historial de arqueos."

---

## Diapositiva 10 — Demostración 5: Administrador (informes y control)

**Qué mostrar en vivo (≈ 2 min)**
1. **Panel de indicadores**: ventas del día vs. promedio semanal, pedidos activos, ocupación del salón, alertas de stock.
2. **Facturas** con filtros y **exportación a CSV**.
3. **Productos** (inventario), **Cupones** y **Personal** (crear usuario con rol).
4. **Auditoría**: quién hizo cada acción.

**Capturas de respaldo:** panel de indicadores, listado de facturas, bitácora de auditoría.

**Guion:** "El administrador ve el estado del negocio en una sola pantalla, gestiona productos, personal y cupones, y puede auditar cualquier acción importante."

---

## Diapositiva 11 — Conclusiones y trabajo futuro

**Conclusiones**
1. Saborator integra procesos que antes estaban separados y reduce la dependencia del papel.
2. Los seis roles permiten que cada persona vea solo lo que necesita.
3. La arquitectura por capas, las pruebas automatizadas y la verificación de pagos en el servidor hacen el sistema confiable y mantenible.
4. Cumple el objetivo: el ciclo completo del pedido queda digital, trazable y seguro.

**Trabajo futuro**
- Facturación electrónica ante la DIAN.
- Aplicación móvil para meseros y domiciliarios.
- Impresión de comandas y costeo de recetas por plato.
- Informes avanzados y programa de fidelización de clientes.

**Cierre:** "Gracias. Quedamos atentos a sus preguntas." — Datos de contacto: [CORREO] · Repositorio: [ENLACE]

---

## Anexo A — Plan de la demostración en vivo

| Orden | Rol | Cuenta a usar | Qué debe estar listo antes |
|---|---|---|---|
| 1 | Cliente | Sin sesión o cuenta de prueba | Carta con productos y mesas libres. |
| 2 | Mesero | Usuario con rol Mesero | Al menos una mesa libre. |
| 3 | Cocinero | Usuario con rol Cocinero (segunda ventana) | Pantalla visible junto a la del mesero. |
| 4 | Cajero | Usuario con rol Cajero | Sin turno abierto, para mostrar la apertura. |
| 5 | Administrador | Usuario con rol Administrador | Facturas del día para ver el panel con datos. |

## Anexo B — Lista de verificación antes de presentar

- [ ] Servidor iniciado y probado con la red del lugar de la exposición.
- [ ] Variable de modo de demostración de pagos activada (checkout simulado de Wompi).
- [ ] Una cuenta de prueba por cada rol y sus contraseñas anotadas (fuera de las diapositivas).
- [ ] Datos de ejemplo: productos, mesas libres y un pedido listo para cobrar.
- [ ] Capturas de respaldo en las diapositivas por si falla la conexión.
- [ ] Navegador en pantalla completa, zoom al 100 % y notificaciones silenciadas.
- [ ] Ensayo cronometrado (12 a 15 minutos) con reparto de intervenciones entre aprendices.
