# Validación de Cravel 0.1.4

- Nombre visible de la app y el ícono: Cravel. El identificador interno, el archivo ScheduleIStats.dll y la ruta de historial se mantienen para conservar las ventas anteriores.
- Nueva pestaña Stock: productos terminados en contenedores colocados dentro de propiedades propias, consultados por el anfitrión cada cinco segundos. Cuenta unidades del contenido, no paquetes. Agrupa producto y ubicación.
- No incluye bolsillos de jugadores, distribuidores, vehículos ni producción en curso. Solo las entidades PlaceableStorageEntity de propiedades propias que están cargadas.
- Stock sincronizado por páginas mediante datos de la sala, protocolo 3. Los invitados publican una nueva instantánea solo después de recibir todas sus páginas. La revisión depende del contenido, de forma que las consultas periódicas no invalidan las transferencias que aún están en curso.
- Una instantánea vacía elimina el stock anterior; el panel muestra la hora de última lectura.
- Compilación Release x64 sin errores ni advertencias.
- 31 comprobaciones automáticas correctas, incluidas agregación por producto/ubicación, revisión estable, páginas parciales/completas, rechazo de páginas repetidas, actualización a stock vacío y cantidades negativas.

Pendiente de validación en el juego: disposición de cinco pestañas, alcance del lector sobre las propiedades de la partida, unidades de bolsas/frascos/ladrillos y llegada del stock a la otra PC. La compilación y las comprobaciones de datos no reemplazan esa prueba.

Prueba sugerida: ambos instalan 0.1.4; el anfitrión coloca una cantidad conocida en un estante de una propiedad propia; comparan Stock en ambas PCs, retiran parte y luego vacían el contenedor, esperando la siguiente consulta y sincronización. El filtro Hoy/Todo afecta a ventas; Stock siempre representa la consulta actual.
