using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Economy;

namespace ScheduleIStats;

internal static class StockReader
{
    public static StockSnapshot Read()
    {
        var lines = new List<StockLine>();
        var seenSlots = new HashSet<IntPtr>();
        var containers = UnityEngine.Object.FindObjectsOfType<PlaceableStorageEntity>(true);
        foreach (var container in containers)
        {
            if (container == null || container.ParentProperty == null || !container.ParentProperty.IsOwned) continue;
            StorageEntity storage = container.StorageEntity;
            if (storage == null || storage.ItemSlots == null) continue;
            string location = container.ParentProperty.PropertyName + " / " + storage.StorageEntityName;
            for (int i = 0; i < storage.ItemSlots.Count; i++)
            {
                AddSlot(storage.ItemSlots[i], location, lines, seenSlots);
            }
        }
        var names = new Dictionary<string, int>(StringComparer.Ordinal);
        var players = Player.PlayerList;
        if (players != null) for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (player == null || !player.PlayerLoaded) continue;
            string name = string.IsNullOrWhiteSpace(player.PlayerName) ? "Sin nombre" : player.PlayerName;
            int duplicate = names.GetValueOrDefault(name) + 1;
            names[name] = duplicate;
            string location = "Jugador: " + name + (duplicate > 1 ? $" ({duplicate})" : "");
            int before = lines.Count;
            if (player.IsLocalPlayer && PlayerInventory.InstanceExists)
            {
                var slots = PlayerInventory.Instance.GetAllInventorySlots();
                if (slots == null) continue;
                for (int j = 0; j < slots.Count; j++) AddSlot(slots[j], location, lines, seenSlots);
            }
            else
            {
                var slots = player._inventory;
                if (slots == null) continue;
                for (int j = 0; j < slots.Length; j++) AddSlot(slots[j], location, lines, seenSlots);
            }
            if (lines.Count == before) lines.Add(new StockLine { Location = location, EmptyPlace = true });
        }
        var cash = new List<DealerBalance>();
        var dealers = Dealer.AllPlayerDealers;
        if (dealers != null) for (int i = 0; i < dealers.Count; i++)
        {
            var dealer = dealers[i];
            if (dealer == null || !dealer.IsRecruited) continue;
            string location = "Camello: " + dealer.FullName;
            int before = lines.Count;
            if (dealer.ItemSlots != null) for (int j = 0; j < dealer.ItemSlots.Count; j++)
                AddSlot(dealer.ItemSlots[j], location, lines, seenSlots);
            if (dealer.overflowSlots != null) for (int j = 0; j < dealer.overflowSlots.Length; j++)
                AddSlot(dealer.overflowSlots[j], location, lines, seenSlots);
            if (lines.Count == before) lines.Add(new StockLine { Location = location, EmptyPlace = true });
            if (float.IsFinite(dealer.Cash) && dealer.Cash >= 0)
            {
                var balance = new DealerBalance { Id = dealer.ID, Name = dealer.FullName, Location = location,
                    Cash = Math.Round((decimal)dealer.Cash, 2) };
                if (balance.Valid()) cash.Add(balance);
            }
        }
        return StockTotals.Create(lines, DateTime.UtcNow, cash);
    }

    private static void AddSlot(ItemSlot slot, string location, List<StockLine> lines, HashSet<IntPtr> seenSlots)
    {
        if (slot == null || !seenSlots.Add(slot.Pointer)) return;
        var product = slot.ItemInstance?.TryCast<ProductItemInstance>();
        if (product == null) return;
        int quantity = product.GetTotalAmount();
        if (quantity <= 0) return;
        lines.Add(new StockLine { ProductId = product.Definition.ID, Product = product.Name,
            Location = location, Quantity = quantity });
    }
}
