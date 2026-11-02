namespace ScheduleIStats;

public sealed class Packet
{
    public string App { get; set; } = "ScheduleIStats";
    public int Protocol { get; set; } = 4;
    public string Kind { get; set; } = "";
    public string Nonce { get; set; } = "";
    public long RequestId { get; set; }
    public string LedgerId { get; set; } = "";
    public long After { get; set; }
    public long Count { get; set; }
    public string Name { get; set; } = "";
    public DateTime StartedUtc { get; set; }
    public List<Sale> Sales { get; set; } = new();
    public string StockRevision { get; set; } = "";
    public int StockAfter { get; set; }
    public int StockCount { get; set; }
    public DateTime StockCheckedUtc { get; set; }
    public List<StockLine> StockLines { get; set; } = new();
}

public static class Replica
{
    public static LedgerData? Apply(LedgerData? existing, Packet p)
    {
        if (p.App != "ScheduleIStats" || p.Protocol != 4 || p.Kind != "batch"
            || p.LedgerId == null || p.LedgerId.Length != 32 || !p.LedgerId.All(Uri.IsHexDigit)
            || p.Sales == null || p.Sales.Count > 12 || p.Count < 0 || p.After < 0
            || p.After > p.Count || p.Sales.Count > p.Count - p.After || p.Sales.Any(s => !s.Valid())) return null;
        bool newLedger = existing == null || existing.Id != p.LedgerId;
        int expected = newLedger ? 0 : existing!.Sales.Count;
        if (p.After != expected || p.Sales.Where((s, i) => s.Sequence != expected + i + 1L).Any()) return null;
        var ids = new HashSet<string>(newLedger ? Enumerable.Empty<string>() : existing!.Sales.Select(s => s.Id));
        if (p.Sales.Any(s => !ids.Add(s.Id))) return null;
        var result = new LedgerData { Id = p.LedgerId, Name = p.Name, StartedUtc = p.StartedUtc,
            Sales = newLedger ? new() : new(existing!.Sales) };
        result.Sales.AddRange(p.Sales);
        return result;
    }
}

