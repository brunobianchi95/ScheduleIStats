using System.Text;
using System.Text.Json;
using Il2CppSteamworks;
using Il2CppScheduleOne.Networking;
using MelonLoader;

namespace ScheduleIStats;

internal sealed class Sync
{
    private const string Announcement = "s1stats_v4";
    private const string RequestKey = "s1stats_request_v4";
    private readonly StatsMod mod;
    private string nonce = Guid.NewGuid().ToString("N");
    private float nextPoll, nextRequest, waitingSince;
    private ulong lobbyId;
    private long requestId, appliedRequest;
    private readonly Dictionary<ulong, string> handled = new();
    private string lastDiagnostic = "";
    private StockReplica stockReplica = new();
    public Sync(StatsMod mod) => this.mod = mod;
    public void Reset()
    {
        nonce = Guid.NewGuid().ToString("N"); lobbyId = 0;
        handled.Clear(); nextRequest = nextPoll = waitingSince = 0;
        requestId = appliedRequest = 0;
        stockReplica = new StockReplica();
    }
    private void Diagnostic(string value)
    {
        mod.NetworkStatus = value;
        if (value == lastDiagnostic) return;
        lastDiagnostic = value;
        MelonLogger.Msg("[Estadísticas/red] " + value);
    }
    public void Tick()
    {
        float now = UnityEngine.Time.unscaledTime;
        if (now < nextPoll) return;
        nextPoll = now + .5f;
        if (!Lobby.InstanceExists || !Lobby.Instance.IsInLobby)
        {
            Diagnostic("No se detecta una sala cooperativa");
            return;
        }
        // Recent game versions store the ID in the lobby service, rather than
        // necessarily updating the legacy LobbyID property.
        var service = Lobby.Instance._lobbyService?.TryCast<SteamLobbyService>();
        ulong current = service?._lobbyID ?? 0;
        if (current == 0) current = Lobby.Instance.LobbyID;
        if (current == 0)
        {
            Diagnostic("Sala detectada, pero sin identificador de Steam");
            mod.Status = "No se pudo identificar la sala de Steam";
            return;
        }
        if (lobbyId != current)
        {
            Reset(); lobbyId = current; waitingSince = now;
            Diagnostic("Sala de Steam identificada");
        }
        var lobby = new CSteamID(lobbyId);
        var local = SteamUser.GetSteamID();
        var owner = SteamMatchmaking.GetLobbyOwner(lobby);
        int count = SteamMatchmaking.GetNumLobbyMembers(lobby);
        if (count < 1 || owner.m_SteamID == 0)
        {
            Diagnostic("Esperando los miembros de la sala");
            return;
        }
        if (mod.Host)
        {
            if (owner.m_SteamID != local.m_SteamID)
            {
                Diagnostic("El servidor y el propietario de la sala no coinciden");
                return;
            }
            if (mod.Data == null)
            {
                Diagnostic("Anfitrión detectado; esperando abrir el historial");
                return;
            }
            string announcement = "0.1.6|" + mod.Data.Id;
            if (SteamMatchmaking.GetLobbyData(lobby, Announcement) != announcement)
                SteamMatchmaking.SetLobbyData(lobby, Announcement, announcement);
            Diagnostic($"Anfitrión anunciado · {count} jugadores · transporte de sala v4");
            for (int i = 0; i < count; i++)
            {
                var peer = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                if (peer.m_SteamID == local.m_SteamID) continue;
                string raw = SteamMatchmaking.GetLobbyMemberData(lobby, peer, RequestKey);
                if (string.IsNullOrEmpty(raw) || raw.Length > 2000 || handled.GetValueOrDefault(peer.m_SteamID) == raw) continue;
                try
                {
                    var p = JsonSerializer.Deserialize<Packet>(raw, Ledger.Json);
                    if (p == null || p.App != "ScheduleIStats" || p.Protocol != 4 || p.Kind != "request" || p.Nonce.Length != 32) continue;
                    var data = mod.Data;
                    int after = p.LedgerId == data.Id && p.After >= 0 && p.After <= data.Sales.Count ? (int)p.After : 0;
                    var reply = new Packet { Kind = "batch", Nonce = p.Nonce, RequestId = p.RequestId, LedgerId = data.Id,
                        Name = data.Name, StartedUtc = data.StartedUtc, After = after, Count = data.Sales.Count,
                        Sales = data.Sales.Skip(after).Take(12).ToList() };
                    var stock = mod.Stock;
                    if (stock != null)
                    {
                        int stockAfter = p.StockRevision == stock.Revision && p.StockAfter >= 0 && p.StockAfter <= stock.Lines.Count ? p.StockAfter : 0;
                        reply.StockRevision = stock.Revision; reply.StockAfter = stockAfter;
                        reply.StockCount = stock.Lines.Count; reply.StockCheckedUtc = stock.CheckedUtc;
                        reply.StockLines = stock.Lines.Skip(stockAfter).Take(8).ToList();
                    }
                    string response = JsonSerializer.Serialize(reply, Ledger.Json);
                    while (Encoding.UTF8.GetByteCount(response) > 7000 && reply.Sales.Count > 1)
                    {
                        reply.Sales.RemoveAt(reply.Sales.Count - 1);
                        response = JsonSerializer.Serialize(reply, Ledger.Json);
                    }
                    while (Encoding.UTF8.GetByteCount(response) > 7000 && reply.StockLines.Count > 0)
                    {
                        reply.StockLines.RemoveAt(reply.StockLines.Count - 1);
                        response = JsonSerializer.Serialize(reply, Ledger.Json);
                    }
                    if (Encoding.UTF8.GetByteCount(response) > 7000)
                    {
                        mod.Status = "Una venta supera el tamaño de sincronización permitido";
                        continue;
                    }
                    if (SteamMatchmaking.SetLobbyData(lobby, ReplyKey(peer.m_SteamID), response))
                    {
                        handled[peer.m_SteamID] = raw;
                        mod.SyncSent++;
                    }
                }
                catch (Exception e) { MelonLogger.Warning("[Estadísticas/red] Solicitud descartada: " + e.Message); }
            }
            return;
        }
        string marker = SteamMatchmaking.GetLobbyData(lobby, Announcement);
        if (string.IsNullOrEmpty(marker) || !marker.StartsWith("0.1.6|", StringComparison.Ordinal))
        {
            Diagnostic($"Sala con {count} jugadores · el anfitrión aún no anuncia la versión 0.1.6");
            mod.Status = "Esperando al anfitrión con el mod 0.1.6";
            return;
        }
        Diagnostic($"Anfitrión con mod detectado · {count} jugadores · transporte de sala v4");
        if (now >= nextRequest)
        {
            requestId++;
            var request = new Packet { Kind = "request", Nonce = nonce, RequestId = requestId,
                LedgerId = mod.Data?.Id ?? "", After = mod.Data?.Sales.Count ?? 0,
                StockRevision = stockReplica.Revision, StockAfter = stockReplica.Offset };
            SteamMatchmaking.SetLobbyMemberData(lobby, RequestKey, JsonSerializer.Serialize(request, Ledger.Json));
            mod.SyncSent++;
            nextRequest = now + 5;
        }
        string replyRaw = SteamMatchmaking.GetLobbyData(lobby, ReplyKey(local.m_SteamID));
        if (!string.IsNullOrEmpty(replyRaw) && Encoding.UTF8.GetByteCount(replyRaw) <= 7000)
        {
            try
            {
                var p = JsonSerializer.Deserialize<Packet>(replyRaw, Ledger.Json);
                if (p != null && p.RequestId == requestId && p.RequestId > appliedRequest && p.Nonce == nonce)
                {
                    var result = Replica.Apply(mod.Data, p);
                    if (result != null)
                    {
                        mod.Receive(result); mod.SyncReceived++; appliedRequest = p.RequestId;
                        var receivedStock = stockReplica.Apply(p);
                        if (receivedStock != null) mod.Stock = receivedStock;
                        waitingSince = now;
                        bool complete = result.Sales.Count == p.Count &&
                            (p.StockRevision.Length == 0 || (stockReplica.Revision == p.StockRevision && stockReplica.Offset == p.StockCount));
                        mod.Status = complete ? "Sincronizado con el anfitrión" :
                            (result.Sales.Count == p.Count ? "Ventas listas · sincronizando stock…" : $"Sincronizando: {result.Sales.Count} de {p.Count} ventas…");
                        nextRequest = now + (complete ? 3 : .6f);
                    }
                }
            }
            catch (Exception e) { MelonLogger.Warning("[Estadísticas/red] Respuesta descartada: " + e.Message); }
        }
        if (now - waitingSince > 20) mod.Status = "Anfitrión detectado, pero sin respuesta al historial";
    }
    private static string ReplyKey(ulong player) => "s1stats_reply_v4_" + player;
}




