using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ScheduleIStats;

public sealed class SaleLine
{
    public string ProductId { get; set; } = "";
    public string Product { get; set; } = "";
    public int Quantity { get; set; }
}

public sealed class Sale
{
    public string Id { get; set; } = "";
    public long Sequence { get; set; }
    public int Day { get; set; }
    public int Time { get; set; }
    public int WorldMinute { get; set; }
    public string Customer { get; set; } = "";
    public string Seller { get; set; } = "";
    public decimal Total { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
    public List<SaleLine> Lines { get; set; } = new();
    public bool Valid() => Id.Length is > 0 and < 200 && Day >= 0 && Time is >= 0 and <= 2359
        && Total >= 0 && Total <= 1000000000m && Lines.Count is > 0 and <= 64
        && Lines.All(l => l.Quantity is > 0 and <= 1000000 && l.Product.Length <= 200 && l.ProductId.Length <= 200);
}

public sealed class LedgerData
{
    public int Format { get; set; } = 1;
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Partida";
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public int LastWorldMinute { get; set; }
    public List<Sale> Sales { get; set; } = new();
}

public static class Ledger
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = false };
    public static int Minute(int day, int time) => day * 1440 + time / 100 * 60 + time % 100;
    public static string Key(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..24];
    public static bool Append(LedgerData data, Sale sale)
    {
        if (!sale.Valid() || data.Sales.Any(s => s.Id == sale.Id)) return false;
        sale.Sequence = data.Sales.Count + 1L;
        data.Sales.Add(sale);
        data.LastWorldMinute = Math.Max(data.LastWorldMinute, sale.WorldMinute);
        return true;
    }
    public static void Save(string path, LedgerData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp";
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, data, Json);
            stream.Flush(true);
        }
        if (File.Exists(path)) File.Copy(path, path + ".bak", true);
        File.Move(temp, path, true);
    }
    public static LedgerData Load(string path)
    {
        var data = JsonSerializer.Deserialize<LedgerData>(File.ReadAllText(path), Json)
            ?? throw new InvalidDataException("El historial está vacío.");
        if (data.Format != 1 || data.Id.Length != 32 || data.Sales.Where((s, i) => !s.Valid() || s.Sequence != i + 1L).Any()
            || data.Sales.Select(s => s.Id).Distinct().Count() != data.Sales.Count)
            throw new InvalidDataException("El historial tiene un formato inválido; se conserva sin sobrescribir.");
        return data;
    }
}
