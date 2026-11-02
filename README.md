# CS SAS

App en español dentro del teléfono de **Schedule I** para consultar ventas, inventario y efectivo de camellos en cooperativo. Antes llamada Cravel.

**0.1.8 experimental** · Windows x64 · Schedule I **0.4.7f12 IL2CPP** · MelonLoader **0.7.3** · S1API **3.2.2**. No usar la DLL en la rama Mono.

## Descargar e instalar

1. Descargar **CS-SAS-v0.1.8-IL2CPP.zip** en [Releases](https://github.com/brunobianchi95/ScheduleIStats/releases/tag/v0.1.8). El ZIP de código fuente no es el paquete instalable.
2. Cerrar el juego. Tener MelonLoader 0.7.3 instalado.
3. En Steam → Schedule I → Propiedades → Archivos instalados → Explorar.
4. Copiar las carpetas **Mods** y **Plugins** del ZIP dentro de esa carpeta. Si ya hay otra versión de S1API, conservar una copia antes de reemplazarla y evitar DLL duplicadas.
5. Abrir desde Steam, entrar a la partida y elegir **CS SAS** en el teléfono.

El archivo principal mantiene el nombre **Mods/ScheduleIStats.dll** para reemplazar la versión anterior. No renombrarlo ni dejar dos copias. El anfitrión y los invitados deben actualizar a 0.1.8 para disponer de todas las funciones nuevas. El historial anterior se conserva en UserData/ScheduleIStats.

## Qué incluye

- Ícono original de hoja verde, interfaz oscura, tarjetas y cantidades destacadas.
- **Scroll vertical** por rueda y arrastre en las listas, con barra lateral. El historial carga más ventas automáticamente al desplazarse.
- **Resumen:** ingresos, ventas y unidades; inventario por propiedad, jugador y camello. Si no hay productos, muestra **Sin inventario**.
- **Productos:** unidades y precio promedio, con filtro Hoy/Todo.
- **Historial:** cada venta registrada, con cliente, productos, unidades, importe y fecha del juego.
- **Stock:** cada lugar o persona como tarjeta, con todos sus productos debajo. Suma contenedores de la misma propiedad.
- **Camellos reclutados:** productos en sus slots y overflow, más **Dinero por cobrar**, leído del saldo Cash del juego. El efectivo no se suma a las unidades de inventario. El mod no cobra ni modifica esos valores.
- **Engranaje de ajustes:** estado de conexión, captura y contadores de sincronización.
- Español y dólares del juego sin centavos; sin conversión de moneda.

## Inventario y sincronización

El anfitrión consulta el stock cada cinco segundos y comparte las instantáneas con los invitados mediante datos de la sala de Steam. No requiere servidor dedicado ni servicios web. El transporte base conserva el protocolo 4 y agrega opcionalmente los saldos de camellos; un anfitrión antiguo no consulta esos nuevos datos.

El stock incluye productos terminados en contenedores colocados en propiedades propias, jugadores conectados y cargados, y camellos reclutados. El anfitrión usa la copia de inventario de invitados que mantiene el juego. Si esa copia aún no está disponible, se omite hasta una consulta posterior. No incluye vehículos, producción en curso ni otros tipos de contenedores.

Las actualizaciones conservan la posición del scroll; cambiar de sección o filtro vuelve al principio. El filtro Hoy/Todo afecta a ventas, no al stock actual. La hora de última lectura figura en Stock.

## Límites y validación

**Versión experimental:** compila sin errores ni advertencias y pasan 43 comprobaciones automáticas de datos. La nueva interfaz y la lectura real de inventarios remotos/camellos necesitan comprobarse dentro del juego y entre dos PCs. Ver VALIDACION.md.

- No importa ventas anteriores a la instalación: registra las entregas que detecta desde entonces.
- La captura observa el procesamiento de entregas del anfitrión. Rutas especiales que no pasen por ese método pueden quedar fuera.
- Las ventas de jugadores todavía no identifican al vendedor individual.
- En ventas de varios productos con un único pago, el precio por producto se estima repartiendo el importe por unidades y se indica como estimado.
- Ingresos no equivale a ganancia: no descuenta costos ni calcula comisión neta.
- Si se detecta que la partida volvió a un momento anterior a la última venta, se archiva la rama previa. No es una integración exacta con cada guardado; reemplazar una partida en la misma ruta o restaurar al mismo minuto puede requerir separar el historial manualmente.
- Un registro individual demasiado grande para el transporte (7000 bytes) muestra un aviso y no se transfiere en esta prueba.
- El efectivo es el saldo actual del camello. La app no garantiza que se pueda interactuar con él en ese instante.

## Prueba entre dos PCs

1. Ambos instalan 0.1.8 con el juego cerrado.
2. El anfitrión carga la partida y el invitado entra como siempre.
3. Abrir el engranaje y comprobar sincronización, incluso con historial vacío.
4. Realizar una venta nueva y comparar ambos historiales.
5. Comparar cantidades conocidas en estante, inventario de cada jugador y camello.
6. Cobrar al camello por el diálogo normal y comprobar que cambie Dinero por cobrar en la siguiente lectura.
7. Verificar rueda/arrastre, listas largas, filtros y que las actualizaciones no salten al principio.

Si falla, comunicar los mensajes y contadores del engranaje. Conservar MelonLoader/Latest.log para diagnóstico; revisar datos personales antes de publicarlo.

## Archivos y desinstalación

El registro principal queda en la PC del anfitrión; las copias de invitados quedan localmente. No se envían partidas ni historiales a GitHub o a una nube.

```text
UserData/ScheduleIStats/partidas/<identificador>/historial.json
UserData/ScheduleIStats/partidas/<identificador>/historial.json.bak
UserData/ScheduleIStats/cache/<historial>.json
```

Para desactivar, cerrar el juego y retirar Mods/ScheduleIStats.dll. Mantener S1API si otros mods lo necesitan. Para actualizar, reemplazar esa DLL y conservar UserData/ScheduleIStats.

## Compilar

Requiere SDK .NET 6 y una instalación IL2CPP local iniciada una vez con MelonLoader para generar referencias.

```powershell
./build.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Schedule I'
dotnet run --project tests -c Release
```

El script descarga S1API 3.2.2 y verifica SHA-256. Las DLL propietarias del juego son referencias locales y no se redistribuyen.

Dependencias: [MelonLoader](https://github.com/LavaGang/MelonLoader), instalado por el usuario, y [S1API 3.2.2](https://github.com/ifBars/S1API/releases/tag/v3.2.2), licencia MIT incluida. Mod comunitario no oficial, sin afiliación con TVGS ni Valve.
