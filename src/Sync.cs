using System.Text;
using System.Text.Json;
using Il2CppSteamworks;
using Il2CppScheduleOne.Networking;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;

namespace ScheduleIStats;

public sealed class Packet
{
    public string App { get; set; } = "ScheduleIStats";
    public int Protocol { get; set; } = 1;
    public string Kind { get; set; } = "";
    public string Nonce { get; set; } = "";
    public string LedgerId { get; set; } = "";
    public long After { get; set; }
    public long Count { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public List<Sale> Sales { get; set; } = new();
}

internal sealed class Sync
{
    // Dedicated channel: do not consume the game's or other mods' channels.
    private const int Channel = 4741;
    private readonly StatsMod mod;
    private string nonce = Guid.NewGuid().ToString("N");
    private float nextRequest, nextMembers;
    private float lastReply;
    private ulong lobbyId;
    private HashSet<ulong> members = new();
    private readonly Dictionary<ulong, float> lastRequests = new();
    public Sync(StatsMod mod) => this.mod = mod;
    public void Reset()
    {
        nonce = Guid.NewGuid().ToString("N"); lobbyId = 0;
        members.Clear(); lastRequests.Clear(); nextRequest = nextMembers = lastReply = 0;
    }
    public void Tick()
    {
        if (!Lobby.InstanceExists || !Lobby.Instance.IsInLobby) return;
        ulong current = Lobby.Instance.LobbyID;
        if (current == 0) return;
        if (lobbyId != current) { Reset(); lobbyId = current; }
        float now = UnityEngine.Time.unscaledTime;
        if (!mod.Host && lastReply > 0 && now - lastReply > 12)
            mod.Status = "Sin respuesta reciente del anfitrión · reintentando…";
        if (now >= nextMembers)
        {
            nextMembers = now + 3;
            members.Clear();
            var lobby = new CSteamID(lobbyId);
            for (int i = 0; i < SteamMatchmaking.GetNumLobbyMembers(lobby); i++)
            {
                var peer = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
                members.Add(peer.m_SteamID);
                if (peer.m_SteamID != SteamUser.GetSteamID().m_SteamID)
                    SteamNetworking.AcceptP2PSessionWithUser(peer);
            }
        }
        for (int i = 0; i < 12 && SteamNetworking.IsP2PPacketAvailable(out uint size, Channel); i++)
        {
            if (size > 1048576) break;
            var buffer = new Il2CppStructArray<byte>(size);
            if (!SteamNetworking.ReadP2PPacket(buffer, size, out uint read, out CSteamID sender, Channel)) break;
            if (read > 65536 || !members.Contains(sender.m_SteamID)) continue;
            try
            {
                var bytes = new byte[read];
                for (int j = 0; j < read; j++) bytes[j] = buffer[j];
                var packet = JsonSerializer.Deserialize<Packet>(bytes, Ledger.Json);
                if (packet != null && packet.App == "ScheduleIStats" && packet.Protocol == 1)
                    Handle(packet, sender, now);
            }
            catch (Exception e) { MelonLogger.Warning("[Estadísticas] Paquete descartado: " + e.Message); }
        }
        if (!mod.Host && now >= nextRequest)
        {
            nextRequest = now + 3;
            var host = SteamMatchmaking.GetLobbyOwner(new CSteamID(lobbyId));
            Send(host, new Packet { Kind = "request", Nonce = nonce,
                LedgerId = mod.Data?.Id ?? "", After = mod.Data?.Sales.Count ?? 0 });
        }
    }
    private void Handle(Packet p, CSteamID sender, float now)
    {
        if (mod.Host)
        {
            if (p.Kind != "request" || mod.Data == null || p.Nonce.Length != 32) return;
            if (lastRequests.TryGetValue(sender.m_SteamID, out float last) && now - last < .5f) return;
            lastRequests[sender.m_SteamID] = now;
            var data = mod.Data;
            int after = p.LedgerId == data.Id && p.After >= 0 && p.After <= data.Sales.Count ? (int)p.After : 0;
            var reply = new Packet { Kind = "batch", Nonce = p.Nonce, LedgerId = data.Id,
                Name = data.Name, StartedUtc = data.StartedUtc, After = after, Count = data.Sales.Count,
                Sales = data.Sales.Skip(after).Take(12).ToList() };
            while (reply.Sales.Count > 1 && JsonSerializer.SerializeToUtf8Bytes(reply, Ledger.Json).Length > 32000)
                reply.Sales.RemoveAt(reply.Sales.Count - 1);
            Send(sender, reply);
            return;
        }
        ulong owner = SteamMatchmaking.GetLobbyOwner(new CSteamID(lobbyId)).m_SteamID;
        if (sender.m_SteamID != owner || p.Kind != "batch" || p.Nonce != nonce || p.LedgerId.Length != 32
            || !p.LedgerId.All(Uri.IsHexDigit) || p.Sales.Count > 12 || p.Count < 0 || p.After < 0
            || p.After + p.Sales.Count > p.Count || p.Sales.Any(s => !s.Valid())) return;
        var existing = mod.Data;
        bool newLedger = existing == null || existing.Id != p.LedgerId;
        if (newLedger && p.After != 0) return;
        int expected = newLedger ? 0 : existing!.Sales.Count;
        if (p.After != expected || p.Sales.Where((s, i) => s.Sequence != expected + i + 1L).Any()) return;
        var ids = new HashSet<string>(newLedger ? Enumerable.Empty<string>() : existing!.Sales.Select(s => s.Id));
        if (p.Sales.Any(s => !ids.Add(s.Id))) return;
        var dataCopy = new LedgerData { Id = p.LedgerId, Name = p.Name, StartedUtc = p.StartedUtc,
            Sales = newLedger ? new() : new(existing!.Sales) };
        dataCopy.Sales.AddRange(p.Sales);
        mod.Receive(dataCopy);
        lastReply = now;
        bool complete = dataCopy.Sales.Count == p.Count;
        mod.Status = complete ? "Sincronizado con el anfitrión" : $"Sincronizando: {dataCopy.Sales.Count} de {p.Count} ventas…";
        nextRequest = now + (complete ? 3 : .6f);
    }
    private static void Send(CSteamID peer, Packet packet)
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(packet, Ledger.Json);
        if (bytes.Length > 65536) return;
        var buffer = new Il2CppStructArray<byte>(bytes);
        if (!SteamNetworking.SendP2PPacket(peer, buffer, (uint)bytes.Length, EP2PSend.k_EP2PSendReliable, Channel))
            MelonLogger.Warning("[Estadísticas] Steam no pudo enviar la sincronización; se reintentará.");
    }
}

