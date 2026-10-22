# SABORATOR
## Manual de Usuario

| Campo | Información |
|---|---|
| Sistema | Saborator — Software de gestión para restaurantes |
| Versión del manual | 1.0 |
| Programa | Análisis y Desarrollo de Software (ADSO) — SENA |
| Ficha | [NÚMERO DE FICHA] |
| Aprendices | [NOMBRES Y APELLIDOS DE LOS APRENDICES] |

> **Nota para la entrega:** en cada sección aparece la marca **[Insertar captura]**. Reemplácela por una captura de pantalla de la vista descrita, tomada con el sistema en funcionamiento.

---

## Tabla de contenido

1. Introducción al manual
2. Requisitos y acceso al sistema
3. Rol Cliente
4. Rol Mesero
5. Rol Cocinero (Cocina)
6. Rol Cajero
7. Rol Administrador
8. Rol Domiciliario (complemento)
9. Preguntas frecuentes y solución de problemas

---

## 1. Introducción al manual

Este manual explica, paso a paso, cómo utiliza cada tipo de usuario el sistema **Saborator**. El sistema muestra a cada persona únicamente las opciones que corresponden a su rol; por eso, después de iniciar sesión, cada usuario llega a una página de inicio distinta:

| Rol | Página a la que llega al iniciar sesión | Función principal |
|---|---|---|
| Cliente | Carta digital (página de inicio) | Consultar el menú, reservar, pedir y pagar. |
| Mesero | Menú de toma de pedidos | Registrar pedidos de las mesas y ver su estado. |
| Cocinero | Tablero de cocina | Preparar y marcar las comandas. |
| Cajero | Caja | Abrir y cerrar turno, cobrar y facturar. |
| Administrador | Panel de indicadores | Supervisar el negocio y administrar la información. |
| Domiciliario | Panel de repartos | Tomar y entregar los domicilios. |

### Convenciones del manual
- **Negrita:** nombre de un botón, opción de menú o campo.
- ➜ indica el camino de menú, por ejemplo: **Mi Perfil ➜ Apariencia**.
- Los estados de un pedido son: **Pendiente**, **En preparación**, **Listo**, **En camino** (domicilios), **Entregado**, **Facturado** (pagado) y **Cancelado**.

---

## 2. Requisitos y acceso al sistema

### 2.1 Requisitos
- Un dispositivo con navegador actualizado (Chrome, Edge, Firefox o Safari): computador, tableta o teléfono.
- Conexión a internet o a la red local del restaurante.
- Para el personal: un usuario creado por el administrador.

### 2.2 Iniciar sesión
1. Abra la dirección del sistema en el navegador.
2. Pulse **Ingresar** (arriba a la derecha).
3. Escriba su **usuario** y su **contraseña**, o use **Continuar con Google** si su cuenta está vinculada.
4. Pulse **Ingresar al sistema**.
5. Si es su primer ingreso como personal, el sistema le pedirá **cambiar la contraseña** antes de continuar.

**[Insertar captura: pantalla de inicio de sesión]**

### 2.3 Cerrar sesión
Pulse el icono de salida (o el botón **Salir**) junto a su nombre en la barra superior. En equipos compartidos, cierre siempre la sesión al terminar.

### 2.4 Modo claro y oscuro
El botón de luna/sol de la barra superior cambia la apariencia. Su elección se recuerda en el dispositivo.

---

## 3. Rol Cliente

El cliente puede consultar la carta y reservar sin tener cuenta; para hacer pedidos con historial y ver **Mis Reservas** y **Mis Pedidos** necesita registrarse.

### 3.1 Crear una cuenta (registro con verificación por código)
1. Pulse **Ingresar ➜ Crear cuenta**.
2. Complete nombre, teléfono, correo electrónico, usuario y contraseña (mínimo 8 caracteres).
3. Elija cómo recibirá el **código de verificación de 6 dígitos** (correo electrónico o mensaje de texto/WhatsApp).
4. Escriba el código recibido y pulse **Verificar**. El código vence en pocos minutos; si no llega, solicite uno nuevo.
5. Quedará con la sesión iniciada.

> El sistema limita a tres los códigos que se pueden solicitar cada diez minutos, para proteger al usuario de envíos repetidos.

**[Insertar captura: registro y verificación]**

### 3.2 Menú digital (La Carta)
1. En la página de inicio, desplácese hasta **La Carta** o pulse **La Carta** en la barra superior.
2. Explore los platos por categoría (hamburguesas, salchipapas gourmet, perros, acompañamientos, bebidas).
3. Cada plato muestra foto, descripción, precio, ingredientes y alérgenos.
4. Pulse **Agregar** para sumar el plato al **carrito** (icono de bolsa en la barra superior).
5. Dentro del carrito puede cambiar cantidades o quitar platos.

**[Insertar captura: carta digital y carrito]**

### 3.3 Hacer un pedido (domicilio o para llevar)
1. Con productos en el carrito, pulse **Hacer Pedido**.
2. Elija la **modalidad**: **Domicilio** o **Para llevar**.
3. Si es domicilio, seleccione su **barrio/sector** (el costo del envío se calcula automáticamente), escriba la dirección y las indicaciones. Tenga en cuenta que existe un **valor mínimo** de pedido para domicilios.
4. Escriba su nombre y teléfono (y correo si desea recibir el comprobante).
5. Si tiene un **cupón**, escríbalo y pulse **Aplicar**; el descuento aparece en el resumen.
6. Pulse **Confirmar y Enviar Pedido**.
7. El sistema genera un código como **PED-00012** y lo lleva a la pantalla de **Seguimiento**.

**[Insertar captura: checkout del pedido]**

### 3.4 Seguir el pedido
- Desde **Rastrear Pedido** escriba el código (por ejemplo, PED-00012) o su número de celular.
- La línea de tiempo muestra el avance: **Recibido ➜ En cocina ➜ Listo ➜ Entregado**. Para domicilios se informa también cuándo va en camino.
- La pantalla se actualiza sola cuando cocina o el repartidor cambian el estado.

**[Insertar captura: seguimiento del pedido]**

### 3.5 Pagar
1. En **Mis Pedidos** o en la pantalla de seguimiento pulse **Pagar con Wompi**.
2. En la pasarela, elija el medio de pago: **Tarjeta**, **Nequi** o **PSE**, y complete los datos.
3. Si el pago es aprobado, el sistema muestra el **comprobante de pago** con el número de factura. Si es rechazado, puede intentarlo con otro medio.
4. Puede imprimir el comprobante con **Imprimir recibo**.

> En una demostración, la pasarela puede funcionar en **modo de simulación**: el pago se aprueba o rechaza según los datos de prueba mostrados en pantalla y no se realiza ningún cobro.

**[Insertar captura: pasarela y comprobante]**

### 3.6 Reservar una mesa
1. Pulse **Reservar Mesa**.
2. **Paso 1 — Horario:** elija fecha, turno y número de personas; pulse **Siguiente: Seleccionar mesa**.
3. **Paso 2 — Mesa:** en el **plano interactivo** elija una mesa disponible.
4. **Paso 3 — Confirmación:** verifique sus datos y confirme.
5. Recibirá un **código de reserva**. Puede consultarlo en **Mis Reservas** y cancelarlo si no podrá asistir.

**[Insertar captura: plano de mesas]**

### 3.7 Mi perfil
En **Mi Perfil** puede cambiar su foto, su tema (claro u oscuro), sus datos de contacto y su contraseña.

### 3.8 Reseñas
Puede dejar una calificación y un comentario sobre su experiencia; el administrador las aprueba antes de publicarlas.

---

## 4. Rol Mesero

**Inicio:** **Menú** (toma de pedidos). Opciones disponibles: **Menú**, **Mesas** y **Mis Pedidos**.

### 4.1 Ver el estado de las mesas
1. Entre a **Mesas**.
2. Cada mesa se muestra con su estado (**Libre**, **Ocupada**, **Reservada**).
3. Para cambiar el estado, abra la mesa y elija el nuevo estado (por ejemplo, de **Libre** a **Ocupada** cuando se sientan los comensales).

**[Insertar captura: tablero de mesas]**

### 4.2 Tomar un pedido
1. Entre a **Menú** y seleccione la **mesa** (si viene desde una mesa, ya aparece seleccionada).
2. Busque los platos por categoría y pulse **Agregar** para cada uno; ajuste las cantidades.
3. Use el campo de **notas** para indicaciones especiales (por ejemplo, "sin cebolla").
4. Revise el resumen y pulse **Enviar pedido**.
5. El pedido queda en estado **Pendiente** y aparece de inmediato en el tablero de cocina.

**[Insertar captura: toma de pedido por mesa]**

### 4.3 Seguir mis pedidos
En **Mis Pedidos** verá los pedidos que ha tomado y su estado. Cuando un pedido pasa a **Listo**, recójalo en cocina y llévelo a la mesa.

### 4.4 Solicitar la cuenta
Cuando la mesa pida la cuenta, avise a caja: el cajero factura el pedido de esa mesa (ver sección 6).

---

## 5. Rol Cocinero (Cocina)

**Inicio:** **Tablero de cocina**.

### 5.1 Gestión de comandas
1. Al iniciar sesión se abre el **tablero de comandas** con una tarjeta por pedido, que muestra mesa o tipo de entrega, platos, cantidades y notas.
2. Los pedidos nuevos entran automáticamente en estado **Pendiente** (el tablero se actualiza en tiempo real y no es necesario recargar).
3. Cuando empiece a preparar un pedido, pulse **Iniciar Preparación**; pasa a **En preparación**.
4. Cuando el pedido esté terminado, pulse **¡Comanda Lista! (Despachar)**; pasa a **Listo** y se avisa al mesero o al domiciliario.
5. Las tarjetas de pedidos con más espera se destacan para priorizar su preparación.

**[Insertar captura: tablero de cocina]**

### 5.2 Recetario
La opción **Recetario** permite consultar los ingredientes y la preparación de cada plato, útil para estandarizar la calidad.

> Si un pedido debe cancelarse, solo el cajero o el administrador pueden hacerlo; avíseles.

---

## 6. Rol Cajero

**Inicio:** **Caja**. Opciones: **Caja (turnos)**, **Facturas** y **Pedidos**.

### 6.1 Abrir el turno de caja
1. Entre a **Caja**. Si no hay un turno abierto, el sistema lo indica.
2. Escriba la **base inicial en efectivo** (monto con que arranca la caja) y, si lo desea, una observación.
3. Pulse **Abrir turno**. Se genera un **código de turno** que identifica su jornada.

**[Insertar captura: apertura de turno]**

### 6.2 Cobrar y facturar un pedido
1. Entre a **Pedidos** y localice el pedido de la mesa o el domicilio.
2. Abra el detalle y verifique los platos y el total.
3. Pulse **Cobrar en Caja**, elija el **medio de pago** (efectivo, tarjeta, Nequi, Daviplata o PSE) y, si el cliente lo autoriza, incluya la **propina sugerida del 10 %**.
4. El sistema emite la **factura** con su número consecutivo, marca el pedido como **Facturado** y libera la mesa.
5. Si el cliente pagó en línea con Wompi, el pedido ya aparece como **Facturado**; no debe cobrarse de nuevo.

> Los pagos reportados por un cliente mediante transferencia manual quedan **pendientes de verificación**: el cajero comprueba el comprobante antes de aprobarlos.

**[Insertar captura: cobro en caja]**

### 6.3 Factura de venta directa
En **Facturas ➜ Nueva** puede registrar una venta sin pedido previo: seleccione los productos y cantidades, el cliente (o nombre libre) y el medio de pago.

### 6.4 Anular una factura
Abra la factura en **Facturas** y pulse **Anular**. La anulación queda registrada con el usuario que la realizó.

### 6.5 Cerrar el turno (arqueo)
1. Entre a **Caja**; el sistema muestra el **resumen en vivo** del turno (ventas por medio de pago).
2. Cuente el efectivo y escríbalo en **Efectivo declarado**.
3. Pulse **Realizar Arqueo & Cerrar Turno**. El sistema calcula la **diferencia** entre el efectivo esperado y el declarado.
4. El turno queda en el **historial de turnos y arqueos**.

**[Insertar captura: arqueo y cierre de turno]**

---

## 7. Rol Administrador

**Inicio:** **Panel de indicadores**. El administrador accede a todos los módulos.

### 7.1 Panel de indicadores
Al ingresar se muestra la **Operación de hoy**:
- **Ventas del día** y su comparación con el promedio diario de los siete días anteriores.
- **Pedidos activos** y domicilios en ruta.
- **Ocupación del salón** (mesas ocupadas frente al total).
- **Reservas del día** y próximas reservas.
- **Alertas de inventario** (productos con stock crítico).
- **Curva de facturación y demanda** y tiempo medio de domicilio.

**[Insertar captura: panel de indicadores]**

### 7.2 Informes y exportaciones
- **Facturas:** filtre por fechas, cliente, cajero o estado y exporte el listado en **CSV** (resumido o detallado).
- **Historial de turnos de caja:** exportable a CSV.
- **Reservas:** filtre por día, estado o búsqueda y exporte a CSV.
- **Auditoría:** consulte quién hizo cada acción importante (fecha, usuario, módulo y detalle) y expórtela a CSV.

**[Insertar captura: listado de facturas con filtros]**

### 7.3 Productos e inventario
1. Entre a **Productos**.
2. Pulse **Nuevo producto** para registrar nombre, descripción, precio, existencias, categoría, ingredientes, alérgenos y si es vegetariano o libre de gluten.
3. Use **Editar** para modificar datos o reponer el stock y **Desactivar** para retirar un plato de la carta sin borrar su historial.

### 7.4 Proveedores
En **Proveedores** registre nombre, NIT, contacto, teléfono, correo, dirección y categoría; edite o desactive proveedores.

### 7.5 Clientes
En **Clientes** consulte y edite los datos de los comensales registrados (documento, teléfono, correo, dirección, fecha de nacimiento).

### 7.6 Cupones de descuento
1. Entre a **Cupones ➜ Nuevo**.
2. Defina el **código**, el tipo de descuento (porcentaje o valor fijo), el valor, el monto mínimo de compra, el tope máximo, la fecha de vencimiento y el número máximo de usos.
3. Active o desactive un cupón con **Activar/Desactivar**.

### 7.7 Personal (usuarios del sistema)
1. Entre a **Personal ➜ Crear**.
2. Escriba el usuario, el correo y elija el **rol** (Mesero, Cocinero, Cajero, Domiciliario o Administrador).
3. El sistema genera una contraseña temporal; el empleado deberá cambiarla en su primer ingreso.
4. Desde la misma pantalla puede **cambiar el rol**, **activar o desactivar** una cuenta y **restablecer la contraseña**.

### 7.8 Reservas
En **Reservas (administración)** vea las reservas por día, cambie su estado (confirmada, completada, cancelada) y consulte los avisos recientes.

### 7.9 Domicilios
En **Domicilios** el administrador ajusta el valor del envío de un pedido, asigna el repartidor y actualiza el estado del domicilio.

### 7.10 Reseñas
En **Reseñas** apruebe, oculte o elimine los comentarios de los clientes antes de que se publiquen en la página de inicio.

---

## 8. Rol Domiciliario (complemento)

**Inicio:** **Repartos**.

1. En **Repartos** aparecen los domicilios **listos para recoger** o asignados a usted; el panel avisa en tiempo real cuando llega uno nuevo.
2. Pulse **Tomar y salir a entregar** para asignárselo; el pedido pasa a **En camino** y el cliente puede ver el avance.
3. Entregue el pedido y pulse **Confirmar entrega**; pasa a **Entregado**.
4. Consulte el **reporte de repartos** por rango de fechas para ver sus entregas.

**[Insertar captura: panel de repartos]**

---

## 9. Preguntas frecuentes y solución de problemas

| Situación | Qué hacer |
|---|---|
| No recibo el código de verificación | Revise la carpeta de correo no deseado, confirme que el correo o teléfono esté bien escrito y solicite un código nuevo (máximo tres cada diez minutos). |
| Olvidé mi contraseña | Use **¿Olvidaste tu contraseña?** en la pantalla de ingreso (junto al campo de contraseña) o pida al administrador que la restablezca. |
| El pedido no aparece en cocina | Verifique que el mesero lo haya enviado y que la pantalla de cocina tenga conexión; recargue la página. |
| El pago con Wompi se queda cargando | Revise la conexión a internet o use otro medio de pago; si el problema continúa, avise a caja para cobrar el pedido en el establecimiento. |
| El pago fue rechazado | Intente con otro medio (Tarjeta, Nequi o PSE) o con otros datos. |
| No puedo cerrar el turno de caja | Confirme que escribió el efectivo declarado y que usted sea el cajero que abrió el turno. |
| Veo "No tienes permiso" | Su rol no tiene acceso a esa opción; consulte al administrador. |

**Soporte:** [CORREO / TELÉFONO DEL RESPONSABLE DEL PROYECTO]
