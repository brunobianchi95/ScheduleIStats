# Validación de la versión 0.1.3

Cambios a partir del informe de que la app aparece pero no recibe el historial:

- Resuelve la sala desde SteamLobbyService, con compatibilidad con LobbyID anterior.
- Usa el estado de anfitrión de la sala además del estado de FishNet.
- Reintenta abrir el historial cuando la autoridad del anfitrión termina de iniciar después de IsGameLoaded.
- Sustituye el transporte P2P de la v0.1.2 por solicitudes y respuestas mediante datos de la propia sala de Steam, protocolo 2. Ambas PCs deben actualizar.
- Se sincroniza también un historial vacío: no es necesario que existan ventas para reconocer la conexión.
- Muestra diagnósticos aun cuando no recibió datos: detección de sala/anfitrión, versión esperada y contadores enviados/recibidos.
- Observa la entrada pública y la lógica RPC de entregas del anfitrión, con exclusión de llamadas anidadas y deduplicación de contratos. Cuenta entregas observadas para distinguir captura y sincronización.
- Conserva la corrección del formato numérico, español y dólares sin centavos.

Validado: compilación x64 sin errores ni advertencias; 24 comprobaciones automáticas en modo de globalización invariable. Las nuevas comprobaciones cubren historial vacío, lotes incrementales, duplicados, secuencias faltantes, protocolo anterior, cambio de partida y ausencia de mutaciones ante paquetes rechazados.

Pendiente: prueba real en dos PCs para confirmar el transporte de sala y las ventas. El registro local v0.1.2 confirma que la app se creó, pero no contiene una respuesta de sincronización ni una venta; no permite asegurar por sí solo la causa exacta en la PC del anfitrión. Esta versión corrige puntos de bloqueo identificados en el código y aporta diagnóstico para la siguiente prueba.

## Prueba

1. Ambos cierran el juego e instalan 0.1.3.
2. El anfitrión carga la partida; el invitado se une como siempre.
3. Abrir Estadísticas → Sincronización. Debe indicar el anfitrión detectado y luego sincronizado, incluso con cero ventas.
4. Hacer una venta nueva de un único producto y comparar ambas pantallas.
5. Si no registra, mirar Entregas observadas en el anfitrión. Si no sincroniza, comunicar el mensaje de red y los contadores de ambas PCs.

No importa datos previos a la instalación. Una operación cuyo registro individual supere 7000 bytes no puede transferirse en esta prueba y muestra un aviso en el anfitrión. No hay conversión de moneda ni modificación de los importes del juego.
