# Validación de Cravel 0.1.6

Stock ahora incluye cada jugador conectado y cargado como un lugar: Jugador: nombre. Se cuentan únicamente sus productos terminados y las unidades del contenido de paquetes, junto al stock de propiedades. El anfitrión consulta los inventarios; para su propio jugador usa PlayerInventory.GetAllInventorySlots y para invitados la copia de Player._inventory que mantiene el juego en el servidor. Si un inventario todavía no está disponible, se omite hasta una consulta posterior.

Un jugador sin productos aparece como un lugar vacío con Sin productos. Los nombres duplicados reciben un sufijo para no combinar inventarios. Al desconectarse, se elimina de la siguiente instantánea al desaparecer de PlayerList.

- Compilación Release x64: correcta sin advertencias ni errores.
- 38 comprobaciones automáticas correctas: separación jugador/propiedad, jugador vacío, preservación del nombre y transferencia del marcador de inventario vacío, además de las comprobaciones anteriores.
- Protocolo 4: ambas PCs deben instalar 0.1.6.
- Conserva historial de ventas, carpeta UserData/ScheduleIStats, nombre visible Cravel, español y dólares sin centavos.

Pendiente de validación en el juego: actualización efectiva de la copia de inventario de invitados, movimientos entre jugadores y estantes, desconexión/reconexión y visualización del stock recibido. Los inventarios remotos dependen de la copia que sincroniza el propio juego. No se comprobó esta versión en una partida cooperativa real.

Prueba: ambos actualizan con el juego cerrado; comparan una cantidad conocida en el inventario de cada jugador con su entrada Jugador: nombre. Mover productos entre un jugador y un estante, esperar la consulta (5 segundos) y la sincronización. Vaciar el inventario debe dejar Sin productos; desconectarse debe retirar su entrada. No incluye inventario de distribuidores, vehículos ni producción en curso.
