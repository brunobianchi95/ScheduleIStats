# Validación de la versión 0.1.0

- Compilación Release x64, .NET 6: correcta, cero errores y cero advertencias.
- 12 comprobaciones automáticas del historial: correctas (duplicados, validación de cantidades/importes, secuencias, persistencia, copia de respaldo y separación de identidades).
- Arranque local con Schedule I 0.4.7f12 IL2CPP, MelonLoader 0.7.3 y S1API 3.2.2: S1API y ScheduleIStats cargados.
- El registro de MelonLoader confirmó que Harmony activó la captura en RpcLogic___ProcessHandoverServerSide_3315874220.
- Pendiente de comprobar en partida: aspecto/controles de la app, ventas del anfitrión e invitado, ventas de distribuidores, sincronización real por Steam entre dos PCs y reconexión.

La versión se entrega como experimental para ejecutar esa prueba. No se generaron ventas ficticias ni se modificó una partida para aparentar validación.
