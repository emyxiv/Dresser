
using CriticalItemEx = CriticalCommonLib.Sheets.ItemEx;
using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;


namespace Dresser.Extensions {
	internal static class InventoryItemExtention {
		public static bool IsGlamourPlateApplicable(this CriticalInventoryItem item)
			=> item.Container == CriticalCommonLib.Enums.InventoryType.GlamourChest || item.Container == CriticalCommonLib.Enums.InventoryType.Armoire;
	}
}
