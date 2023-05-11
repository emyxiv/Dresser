
using CriticalItemEx = CriticalCommonLib.Sheets.ItemEx;
using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;


namespace Dresser.Extensions {
	internal static class InventoryItemExtention {
		public static bool IsGlamourPlateApplicable(this CriticalInventoryItem item)
			=> item.Container == CriticalCommonLib.Enums.InventoryType.GlamourChest || item.Container == CriticalCommonLib.Enums.InventoryType.Armoire;
		public static void Clear(this CriticalInventoryItem item) {

			item.Container = 0;
			//item.Slot = 0;
			item.ItemId = 0;
			item.Quantity = 0;
			item.Spiritbond = 0;
			item.Condition = 0;
			item.Flags = 0;
			item.Materia0 = 0;
			item.Materia1 = 0;
			item.Materia2 = 0;
			item.Materia3 = 0;
			item.Materia4 = 0;
			item.MateriaLevel0 = 0;
			item.MateriaLevel1 = 0;
			item.MateriaLevel2 = 0;
			item.MateriaLevel3 = 0;
			item.MateriaLevel4 = 0;
			item.Stain = 0;
		}
	}
}
