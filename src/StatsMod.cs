using HarmonyLib;
using MelonLoader;
using MelonLoader.Utils;
using Il2CppScheduleOne.Economy;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.GameTime;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.ItemFramework;
using Il2CppFishNet.Object;
using Il2CppFishNet;


[assembly: MelonInfo(typeof(ScheduleIStats.StatsMod), "Cravel", "0.1.6", "brunobianchi95")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ScheduleIStats;

public sealed class StatsMod : MelonMod
{
    public static StatsMod? Instance { get; private set; }
    public LedgerData? Data { get; private set; }
    public string Status { get; set; } = "Esperando una partida…";
    public string CaptureStatus { get; private set; } = "Sin comprobar";
    public string NetworkStatus { get; set; } = "Sin identificar la sala";
    public int SyncSent { get; set; }
    public int SyncReceived { get; set; }
    public int DeliveriesObserved { get; set; }
    public StockSnapshot? Stock { get; internal set; }
    public string StockStatus { get; private set; } = "Esperando el stock del anfitrión";
    private float nextStockScan;
    public bool Loaded => LoadManager.InstanceExists && LoadManager.Instance.IsGameLoaded;
    public bool Host => Loaded && (InstanceFinder.IsServer ||
        (Il2CppScheduleOne.Networking.Lobby.InstanceExists && Il2CppScheduleOne.Networking.Lobby.Instance.IsHost));
    public string Root { get; private set; } = "";
    private string? path;
    private bool wasLoaded;
    private float nextTick;
    private float nextErrorLog;
    private Sync? sync;
    private bool diskFailed;

    public override void OnInitializeMelon()
    {
        Instance = this;
        Root = Path.Combine(MelonEnvironment.UserDataDirectory, "ScheduleIStats");
        Directory.CreateDirectory(Root);
        sync = new Sync(this);
        try
        {
            var methods = AccessTools.GetDeclaredMethods(typeof(Customer))
                .Where(m => (m.Name == "ProcessHandoverServerSide" || m.Name.StartsWith("RpcLogic___ProcessHandoverServerSide_")) && m.GetParameters().Length == 6).ToArray();
            if (methods.Length == 0) throw new MissingMethodException("No se encontró el procesamiento de entregas.");
            foreach (var method in methods)
            {
                HarmonyInstance.Patch(method,
                    new HarmonyMethod(typeof(SalePatch), nameof(SalePatch.Prefix)),
                    finalizer: new HarmonyMethod(typeof(SalePatch), nameof(SalePatch.Finalizer)));
                LoggerInstance.Msg("Captura preparada: " + method.Name);
            }
            CaptureStatus = "Captura disponible (pendiente de venta)";
        }
        catch (Exception e)
        {
            CaptureStatus = "Captura incompatible con esta versión";
            LoggerInstance.Error("No se pudo activar la captura: " + e);
        }
        LoggerInstance.Msg("App Cravel v0.1.6 cargada. Abrí el teléfono dentro de una partida.");
    }

    public override void OnUpdate()
    {
        if (UnityEngine.Time.unscaledTime < nextTick) return;
        nextTick = UnityEngine.Time.unscaledTime + 0.25f;
        try
        {
            if (!Loaded)
            {
                if (wasLoaded) { Data = null; Stock = null; path = null; diskFailed = false; sync?.Reset(); }
                wasLoaded = false;
                Status = "Esperando una partida…";
                StatsApp.Current?.Refresh();
                return;
            }
            if (!wasLoaded)
            {
                wasLoaded = true;
                LoggerInstance.Msg("Partida cargada · rol: " + (Host ? "anfitrión" : "invitado"));
                Status = "Buscando el historial de la partida…";
            }
            // Server authority may become ready after IsGameLoaded. Retry instead
            // of permanently remaining a client with no ledger.
            if (Host && Data == null && !diskFailed) OpenHostLedger();
            if (Host && UnityEngine.Time.unscaledTime >= nextStockScan)
            {
                nextStockScan = UnityEngine.Time.unscaledTime + 5;
                try { Stock = StockReader.Read(); StockStatus = "Stock consultado en el anfitrión"; }
                catch (Exception e) { StockStatus = "No se pudo leer el stock; revisá el registro"; LoggerInstance.Warning("Stock: " + e); }
            }
            sync?.Tick();
            StatsApp.Current?.Refresh();
        }
        catch (Exception e)
        {
            Status = "No se pudo actualizar. Revisá MelonLoader/Latest.log";
            if (UnityEngine.Time.unscaledTime >= nextErrorLog)
            {
                nextErrorLog = UnityEngine.Time.unscaledTime + 10;
                LoggerInstance.Warning("Actualización: " + e);
            }
        }
    }

    private void OpenHostLedger()
    {
        string savePath = LoadManager.Instance.LoadedGameFolderPath;
        if (string.IsNullOrEmpty(savePath)) throw new InvalidOperationException("No se identificó la partida del anfitrión.");
        string folder = Path.Combine(Root, "partidas", Ledger.Key(Path.GetFullPath(savePath).ToLowerInvariant()));
        path = Path.Combine(folder, "historial.json");
        try { Data = File.Exists(path) ? Ledger.Load(path) : new LedgerData(); }
        catch (Exception e) { diskFailed = true; Status = "Historial dañado: se conserva el archivo original"; LoggerInstance.Error(e); return; }
        var tm = TimeManager.Instance;
        if (tm.GetTotalMinSum() < Data.LastWorldMinute)
        {
            // Preserve the old timeline, start a separate ledger on rollback.
            string archived = Path.Combine(folder, "rama-" + Data.Id + ".json");
            Ledger.Save(archived, Data);
            Data = new LedgerData();
            LoggerInstance.Warning("Se cargó un momento anterior: se inició otra rama de estadísticas. La anterior se conserva.");
        }
        Data.Name = LoadManager.Instance.ActiveSaveInfo?.OrganisationName ?? "Partida compartida";
        Persist();
        Status = "Anfitrión · historial global activo";
        LoggerInstance.Msg("Historial global abierto. Ventas guardadas: " + Data.Sales.Count);
    }

    private void Persist()
    {
        if (diskFailed || path == null || Data == null) return;
        try { Ledger.Save(path, Data); }
        catch (Exception e) { diskFailed = true; Status = "Error al guardar estadísticas: revisá el registro"; LoggerInstance.Error(e); }
    }

    internal void Record(Sale sale)
    {
        if (!Host || diskFailed || Data == null) return;
        if (!Ledger.Append(Data, sale)) return;
        Persist();
        CaptureStatus = "Captura activa · última venta registrada";
        LoggerInstance.Msg($"Venta #{sale.Sequence}: {sale.Total:0.00}, {sale.Lines.Sum(l => l.Quantity)} unidades.");
    }

    internal void Receive(LedgerData data)
    {
        if (Host) return;
        Data = data;
        try { Ledger.Save(Path.Combine(Root, "cache", data.Id + ".json"), data); }
        catch (Exception e) { LoggerInstance.Warning("No se pudo guardar la copia local: " + e.Message); }
    }
}

internal static class SalePatch
{
    [ThreadStatic] private static int depth;
    public sealed class CaptureState { public Sale? Sale; }
    public static void Prefix(Customer __instance, Il2CppSystem.Collections.Generic.List<ItemInstance> __0, bool __1, float __2,
        ProductList __3, float __4, NetworkObject __5, out CaptureState? __state)
    {
        __state = null;
        var mod = StatsMod.Instance;
        if (mod == null || !mod.Host) return;
        __state = new CaptureState();
        depth++;
        if (depth > 1) return;
        mod.DeliveriesObserved++;
        MelonLogger.Msg("[Estadísticas/captura] Entrega observada en el anfitrión.");
        if (!float.IsFinite(__2) || __2 < 0) return;
        try
        {
            var tm = TimeManager.Instance;
            var sale = new Sale {
                Id = __instance.CurrentContract != null ? "contrato-" + __instance.CurrentContract.GUID.ToString() : Guid.NewGuid().ToString("N"),
                Day = tm.ElapsedDays, Time = tm.CurrentTime, WorldMinute = tm.GetTotalMinSum(), Customer = __instance.NPC.FullName,
                Seller = __1 ? "Jugador (sin identificar)" : (__5 != null ? __5.GetComponent<Dealer>()?.FullName ?? "Distribuidor" : "Distribuidor"),
                Total = Math.Round((decimal)__2, 2)
            };
            for (int i = 0; i < __0.Count; i++)
            {
                var product = __0[i].TryCast<ProductItemInstance>();
                if (product == null) continue;
                string id = product.Definition.ID;
                var line = sale.Lines.FirstOrDefault(l => l.ProductId == id);
                int quantity = product.GetTotalAmount();
                if (line != null) line.Quantity += quantity;
                else sale.Lines.Add(new SaleLine { ProductId = id, Product = product.Name, Quantity = quantity });
            }
            if (sale.Valid()) __state.Sale = sale;
            else MelonLogger.Warning("[Estadísticas/captura] La entrega no contiene productos o importes válidos para el historial.");
        }
        catch (Exception e) { MelonLogger.Warning("[Estadísticas] No se pudo leer una entrega: " + e.Message); }
    }
    public static Exception? Finalizer(CaptureState? __state, Exception? __exception)
    {
        if (__state == null) return __exception;
        depth = Math.Max(0, depth - 1);
        try { if (__exception == null && __state.Sale != null) StatsMod.Instance?.Record(__state.Sale); }
        catch (Exception e) { MelonLogger.Warning("[Estadísticas] No se pudo registrar una venta: " + e.Message); }
        return __exception;
    }
}








