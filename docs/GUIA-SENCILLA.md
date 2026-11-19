# Saborator explicado sencillo

## ¿Qué es?

Saborator es una página web para un restaurante de hamburguesas, salchipapas y perros calientes. Con ella los clientes piden comida y el personal del restaurante organiza todo: cocina, caja, mesas, domicilios e inventario.

Piénsalo como **dos páginas en una**: una pública para los clientes y otra interna para los trabajadores.

## ¿Quién la usa y para qué?

| Persona | Qué hace en la página |
|---|---|
| **Cliente** | Mira la carta, arma su pedido en el carrito, paga y sigue su pedido. |
| **Mesero** | Anota lo que piden en cada mesa y lo manda a cocina. |
| **Cocinero** | Ve en una pantalla los pedidos que debe preparar y los marca como listos. |
| **Cajero** | Cobra, genera la factura y abre y cierra la caja. |
| **Domiciliario** | Lleva los pedidos a las casas y marca cuándo los entrega. |
| **Administrador** | Controla todo: ventas, productos, inventario, personal y reportes. |

Cada persona entra con su usuario y solo ve lo que le corresponde. Un cocinero no puede ver la caja, por ejemplo.

## El recorrido de un pedido

1. El **cliente** elige productos y los pone en el carrito.
2. Escoge cómo lo quiere: a domicilio, para recoger o en la mesa.
3. Paga y el pedido llega **al instante a la pantalla de cocina**, sin recargar la página.
4. El **cocinero** lo prepara y lo marca como listo.
5. Si es a domicilio, el **domiciliario** lo recoge y sale a repartir. El cliente ve en el mapa que va *en camino*.
6. Al entregarlo, el **cajero** deja registrada la venta y la factura.

## Las partes de la página

- **Inicio:** carrusel con los combos, la carta y las ofertas.
- **Carrito:** funciona en todas las páginas y guarda lo que elegiste aunque cierres la ventana.
- **Ingreso:** usuario y contraseña, o con tu cuenta de Google. También se puede crear una cuenta nueva.
- **Seguimiento:** muestra en qué estado va el pedido.
- **Cocina:** pantalla de pedidos en vivo.
- **Caja y facturas:** cobros y comprobantes.
- **Panel del administrador:** gráficos e indicadores del negocio.
- **Perfil:** datos del cliente, pedidos anteriores, reseñas y cupones.

## ¿Cómo está hecha por dentro?

Imagina un restaurante:

- **La vista (lo que se ve):** páginas con HTML, CSS y JavaScript. Es el "comedor".
- **El servidor (el cerebro):** programado en C# con ASP.NET Core. Recibe los pedidos y decide qué hacer. Es la "cocina".
- **La base de datos (la memoria):** guarda usuarios, productos, pedidos y facturas. Usa SQL Server o, si no lo encuentra, SQLite.
- **Tiempo real (SignalR):** es el "timbre" que avisa a la cocina en cuanto entra un pedido, sin recargar.
- **Seguridad:** las contraseñas se guardan cifradas (BCrypt), cada rol tiene sus permisos y se limitan los intentos de ingreso.
- **Google:** permite entrar con la cuenta de Google sin crear otra contraseña.
- **Pagos:** está preparada para Wompi, pero falta poner las llaves reales.

## ¿Por qué se usan datos de ejemplo?

El sistema simula la gestión de un restaurante: los productos, usuarios y pedidos son de demostración. Sirve para mostrar cómo funcionaría un restaurante real sin usar datos de clientes verdaderos.

## Lo que aún no está conectado

- Pagos reales con Wompi (faltan las llaves).
- Mensajes de WhatsApp o SMS (falta Twilio).
- Facturación electrónica oficial ante la DIAN: hay facturas dentro del sistema, pero no se envían a la DIAN.

## Cómo probarla

1. Abre PowerShell en la carpeta del proyecto y ejecuta `dotnet run`.
2. Entra a `http://localhost:5106`.
3. Pulsa **Ingresar** y prueba con las cuentas de demostración (`admin`, `cajero`, `mesero`, `cocinero`, `domiciliario`, `cliente`).

Para ver los pasos de cada rol, mira la **Guía de uso**.
