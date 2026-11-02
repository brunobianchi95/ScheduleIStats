# Validación de CS SAS 0.1.8

Compilación x64 sin advertencias ni errores. 43 comprobaciones automáticas correctas en modo de globalización invariable. Incluyen historial, secuencias, stock por lugar/jugador, inventarios vacíos, separación efectivo/unidades, cambio de revisión de efectivo al cobrar, transporte del saldo y rechazo de saldos negativos o duplicados.

Dealer.AllPlayerDealers, IsRecruited, ItemSlots, overflowSlots y Cash se comprobaron contra los ensamblados locales del juego. La API de referencia documenta Cash como efectivo que mantiene el dealer. El mod solo lee esos miembros.

El ZIP de esta publicación contiene la misma DLL de la prueba local 0.1.8 instalada previamente. Algunas etiquetas de diagnóstico conservan la mención Vista local; no indican una versión distinta.

Pendiente: confirmación visual del diseño con scroll y engranaje, comparación en juego de productos y efectivo de un camello, actualización de inventarios de invitados y sincronización real entre dos PCs. Los tests no ejecutan el motor Unity ni una sesión cooperativa.

Los datos opcionales de efectivo se agregan al protocolo base 4 sin modificar el cálculo de revisión de las líneas de stock. El anfitrión necesita 0.1.8 para consultar camellos y saldos; los invitados necesitan 0.1.8 para mostrar el saldo en su interfaz.
