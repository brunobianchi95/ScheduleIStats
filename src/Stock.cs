using System.Text.Json;

namespace ScheduleIStats;

public sealed class StockLine
{
    public string ProductId { get; set; } = "";
    public string Product { get; set; } = "";
    public string Location { get; set; } = "";
    public long Quantity { get; set; }
    public bool EmptyPlace { get; set; }
    public bool Valid() => ProductId != null && ProductId.Length <= 200
        && Product != null && Product.Length <= 200 && Location != null && Location.Length <= 300
        && (EmptyPlace ? ProductId == "" && Product == "" && Quantity == 0 :
            ProductId.Length > 0 && Quantity is > 0 and <= 1000000000);
}

public sealed class StockSnapshot
{
    public string Revision { get; set; } = "";
    public DateTime CheckedUtc { get; set; }
    public List<StockLine> Lines { get; set; } = new();
    public List<DealerBalance>? DealerCash { get; set; }
    public string CashRevision => DealerCash == null ? "" : Ledger.Key(JsonSerializer.Serialize(DealerCash, Ledger.Json));
}

public sealed class DealerBalance
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
    public decimal Cash { get; set; }
    public bool Valid() => !string.IsNullOrEmpty(Id) && Id.Length <= 200 && Name != null && Name.Length <= 200
        && Location != null && Location.Length <= 300 && Cash is >= 0 and <= 1000000000m;
}

public static class StockTotals
{
    public static StockSnapshot Create(IEnumerable<StockLine> input, DateTime checkedUtc, List<DealerBalance>? dealerCash = null)
    {
        var lines = input.Where(l => l.Valid()).GroupBy(l => (l.ProductId, l.Location))
            .Select(g => new StockLine { ProductId = g.Key.ProductId, Product = g.First().Product,
                Location = g.Key.Location, Quantity = g.Sum(l => l.Quantity), EmptyPlace = g.All(l => l.EmptyPlace) })
            .OrderBy(l => l.ProductId, StringComparer.Ordinal).ThenBy(l => l.Location, StringComparer.Ordinal).ToList();
        return new StockSnapshot { Revision = Ledger.Key(JsonSerializer.Serialize(lines, Ledger.Json)), CheckedUtc = checkedUtc, Lines = lines,
            DealerCash = dealerCash?.OrderBy(d => d.Id, StringComparer.Ordinal).ToList() };
    }
}

public sealed class StockReplica
{
    public string Revision { get; private set; } = "";
    public int Offset => pending.Count;
    private readonly List<StockLine> pending = new();
    private int expectedCount;
    public StockSnapshot? Apply(Packet p)
    {
        if (p.DealerCash != null && (p.DealerCash.Count > 128 || p.DealerCash.Any(d => !d.Valid())
            || p.DealerCash.Select(d => d.Id).Distinct().Count() != p.DealerCash.Count)) return null;
        if (p.StockRevision == null || p.StockRevision.Length != 24 || !p.StockRevision.All(Uri.IsHexDigit)
            || p.StockLines == null || p.StockLines.Count > 8 || p.StockCount is < 0 or > 100000
            || p.StockAfter < 0 || p.StockAfter > p.StockCount || p.StockLines.Count > p.StockCount - p.StockAfter
            || p.StockLines.Any(l => !l.Valid())) return null;
        bool changed = Revision != p.StockRevision;
        int offset = changed ? 0 : pending.Count;
        if (p.StockAfter != offset || (!changed && expectedCount != p.StockCount)) return null;
        var keys = new HashSet<(string, string)>(changed ? Enumerable.Empty<(string, string)>() : pending.Select(l => (l.ProductId, l.Location)));
        if (p.StockLines.Any(l => !keys.Add((l.ProductId, l.Location)))) return null;
        if (changed) { Revision = p.StockRevision; pending.Clear(); expectedCount = p.StockCount; }
        pending.AddRange(p.StockLines);
        if (pending.Count != p.StockCount) return null;
        var snapshot = StockTotals.Create(pending, p.StockCheckedUtc, p.DealerCash);
        return snapshot.Revision == Revision ? snapshot : null;
    }
}
