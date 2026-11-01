# Validación de la versión 0.1.2

Corrección de la app que no aparecía en la versión 0.1.0:

El registro real del juego mostró que S1API descubría StatsApp pero no podía crearla: CultureInfo.GetCultureInfo("es-AR") lanzaba CultureNotFoundException porque MelonLoader ejecuta .NET en modo de globalización invariable. La inicialización estática fallida impedía crear la pantalla y luego su ícono.

- Se reemplazó esa dependencia regional por un NumberFormatInfo explícito: $1,234.50.
- Se limita el registro de errores repetidos y se incluye el detalle completo de la excepción.
- Compilación Release x64: correcta, cero errores y cero advertencias.
- 14 comprobaciones automáticas correctas ejecutadas con DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1, incluido el formato de dólares sin culturas instaladas.
- La carga de S1API y la activación de la captura de ventas fueron comprobadas en el arranque de la versión anterior.
- Pendiente en esta versión: reiniciar el juego y confirmar visualmente el ícono y la pantalla. Las ventas reales y la sincronización entre dos PCs siguen pendientes de validación.


Los montos se muestran en dólares del juego, sin conversión de moneda. La interfaz permanece en español.
