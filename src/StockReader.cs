using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Storage;

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
                var slot = storage.ItemSlots[i];
                if (slot == null || !seenSlots.Add(slot.Pointer)) continue;
                var product = slot.ItemInstance?.TryCast<ProductItemInstance>();
                if (product == null) continue;
                int quantity = product.GetTotalAmount();
                if (quantity <= 0) continue;
                lines.Add(new StockLine { ProductId = product.Definition.ID, Product = product.Name,
                    Location = location, Quantity = quantity });
            }
        }
        return StockTotals.Create(lines, DateTime.UtcNow);
    }
}
