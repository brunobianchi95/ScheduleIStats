# Schedule I Estadísticas

Versión **0.1.2 experimental**. App en español dentro del teléfono para registrar ventas y consultar el historial compartido de una partida cooperativa.

Compilada contra **Schedule I 0.4.7f12, Windows x64, IL2CPP**, con **MelonLoader 0.7.3** y **S1API 3.2.2**. No usar esta DLL en la rama Mono. El anfitrión y los invitados que quieran consultar estadísticas deben instalar el mismo paquete.

## Descargar e instalar

1. En [Releases](https://github.com/brunobianchi95/ScheduleIStats/releases), descargar **ScheduleIStats-v0.1.2-IL2CPP.zip**. El ZIP de código fuente de GitHub no es el instalador.
2. Cerrar Schedule I. Tener MelonLoader 0.7.3 instalado en ambas PCs.
3. En Steam → Schedule I → Propiedades → Archivos instalados → Explorar.
4. Extraer el ZIP y copiar **Mods** y **Plugins** dentro de esa carpeta. Si ya hay una versión de S1API, usar una sola instalación compatible: no dejar DLL antiguas duplicadas. Conservar una copia de las dependencias anteriores antes de reemplazarlas.
5. Abrir el juego desde Steam. El anfitrión carga la partida y el invitado se une como siempre.
6. Abrir el teléfono y elegir **Estadísticas**.

El ZIP incluye el mod y el paquete oficial S1API 3.2.2 (MIT). El loader de S1API selecciona la variante adecuada; nuestro mod solo soporta IL2CPP. No incluye archivos propietarios del juego. No requiere servidor dedicado, cuenta adicional, puertos manuales ni servicios web.

## Prueba entre dos jugadores

1. Abrir la app en ambos equipos. En el anfitrión debe aparecer “historial global activo”; el invitado debe pasar a “Sincronizado con el anfitrión”.
2. Anotar cantidad de ventas e ingresos. El anfitrión hace una venta simple de un único producto.
3. Verificar que aparezca una sola vez en los dos historiales, con producto, unidades y pago correcto, en aproximadamente 3 segundos.
4. Repetir con una venta del invitado. Confirmar que también figura en el anfitrión.
5. Probar una venta de un distribuidor: su cobertura debe comprobarse en juego.
6. El invitado sale; el anfitrión realiza otra venta. Al reconectarse, el invitado debe recuperar esa venta.
7. Guardar la partida, cerrar normalmente y volver a abrir: comparar los totales.

Si falla, conservar el archivo `MelonLoader/Latest.log` y describir cuál de estos pasos falló. No publicar ese registro sin revisar los datos personales que pueda contener.

## Qué incluye

- App con ícono propio y pestañas **Resumen, Productos, Historial y Sincronización**.
- Ingresos, unidades, número de operaciones y precio promedio por producto; filtro Hoy/Todo y paginación.
- Captura en el anfitrión del procesamiento de entregas del juego. Los invitados consultan la copia sincronizada; no envían ventas inventadas al registro.
- Sincronización incremental por Steam P2P, canal reservado 4741. Solo se aceptan paquetes de miembros de la sala, y el historial del cliente solo acepta al anfitrión. Reintentos y recuperación por secuencia al reconectar.
- Historial separado por ruta de partida; escritura mediante archivo temporal y copia `.bak` del estado anterior.

## Datos y límites de esta prueba

- **No se importa historial previo**: comienza con las ventas detectadas tras instalar el mod.
- Compilación y pruebas automáticas no equivalen a una prueba real entre dos PCs. La captura nativa, la interfaz y el transporte se consideran experimentales hasta completar el procedimiento anterior.
- La entrega se registra cuando termina el método de procesamiento del anfitrión. Las rutas especiales que no pasen por ese método pueden quedar fuera. No se modifican precios, dinero, inventario ni las reglas del juego.
- Las ventas de jugadores se etiquetan “Jugador (sin identificar)”: todavía no se distingue qué jugador vendió. Los distribuidores se identifican cuando el evento proporciona su objeto.
- Si una operación contiene varios productos con un único pago, se conserva el total real. Para el promedio por producto se reparte por unidades y se marca **estimado**; el juego no proporciona un precio separado en ese evento.
- Ingresos no es ganancia: no se descuentan costos ni se calcula todavía la comisión neta de distribuidores.
- No se requiere que todos los jugadores instalen el mod para que el anfitrión observe sus entregas; todos los que quieran abrir la app y sincronizar deben instalarlo. Esa cobertura también debe validarse.
- Si al cargar el reloj del juego está antes de la última venta registrada, se archiva la rama anterior y comienza otro historial. Es una protección básica, no una integración exacta con cada guardado: una restauración al mismo minuto o un reemplazo de partida en la misma ruta puede requerir separar manualmente el historial.
- El anfitrión debe conservar su carpeta de estadísticas. El historial no se publica en GitHub ni se sube a una nube.

Archivos creados en `Schedule I/UserData/ScheduleIStats/`:

```text
partidas/<identificador-local>/historial.json   Registro principal del anfitrión
partidas/<identificador-local>/historial.json.bak
partidas/<identificador-local>/rama-*.json      Ramas anteriores si se detecta retroceso
cache/<identificador-del-historial>.json        Copia recibida por el invitado
```

Para desactivar: cerrar el juego y retirar `Mods/ScheduleIStats.dll`. Mantener S1API si otros mods lo usan. Para actualizar: cerrar el juego y reemplazar la DLL; conservar `UserData/ScheduleIStats`.

## Compilar desde el código

Requiere el SDK .NET 6 y una instalación local del juego IL2CPP iniciada al menos una vez con MelonLoader, para generar sus referencias.

```powershell
./build.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Schedule I'
dotnet run --project tests -c Release
```

El script descarga el paquete oficial fijado de S1API y verifica SHA-256. Produce la DLL en `bin/Release/net6.0/`. Las DLL del juego son referencias locales y **no deben subirse al repositorio**.

## Permisos del repositorio

Este repositorio público permite leer y descargar. Solo el propietario y colaboradores con permiso de escritura pueden modificarlo. Otros pueden crear sus propias copias o proponer cambios; no pueden reemplazar directamente los archivos publicados aquí.

## Dependencias

- [MelonLoader](https://github.com/LavaGang/MelonLoader), instalado por el usuario.
- [S1API](https://github.com/ifBars/S1API/releases/tag/v3.2.2), licencia MIT incluida en el paquete.
- Steamworks y Unity provistos por el juego; no se redistribuyen.

Mod comunitario no oficial, sin afiliación con TVGS ni Valve.


