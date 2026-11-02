namespace ScheduleIStats;

public sealed class StockPlacePage
{
    public string Place { get; set; } = "";
    public long Total { get; set; }
    public int Part { get; set; }
    public int Parts { get; set; }
    public List<StockLine> Items { get; set; } = new();
}

public static class StockPlaces
{
    public static List<StockPlacePage> Groups(IEnumerable<StockLine> lines)
    {
        return lines.GroupBy(l => PlaceName(l.Location)).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(place => new StockPlacePage { Place = place.Key,
                Total = place.Sum(l => l.Quantity), Part = 1, Parts = 1,
                Items = place.Where(l => !l.EmptyPlace).GroupBy(l => l.ProductId)
                    .Select(g => new StockLine { ProductId = g.Key, Product = g.First().Product,
                        Location = place.Key, Quantity = g.Sum(l => l.Quantity) })
                    .OrderBy(l => l.Product, StringComparer.Ordinal).ToList() }).ToList();
    }
    public static List<StockPlacePage> Pages(IEnumerable<StockLine> lines)
    {
        var pages = new List<StockPlacePage>();
        foreach (var place in lines.GroupBy(l => PlaceName(l.Location)).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var products = place.Where(l => !l.EmptyPlace).GroupBy(l => l.ProductId)
                .Select(g => new StockLine { ProductId = g.Key, Product = g.First().Product,
                    Location = place.Key, Quantity = g.Sum(l => l.Quantity) })
                .OrderBy(l => l.Product, StringComparer.Ordinal).ToList();
            int parts = Math.Max(1, (products.Count + 3) / 4);
            for (int i = 0; i < parts; i++) pages.Add(new StockPlacePage { Place = place.Key,
                Total = products.Sum(l => l.Quantity), Part = i + 1, Parts = parts,
                Items = products.Skip(i * 4).Take(4).ToList() });
        }
        return pages;
    }

    private static string PlaceName(string location)
    {
        if (location.StartsWith("Jugador: ", StringComparison.Ordinal) || location.StartsWith("Camello: ", StringComparison.Ordinal)) return location;
        int separator = location.IndexOf(" / ", StringComparison.Ordinal);
        return separator >= 0 ? location[..separator] : location;
    }
}
