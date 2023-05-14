
using CriticalItemEx = CriticalCommonLib.Sheets.ItemEx;
using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;
using System.Collections.Generic;
using CriticalCommonLib.Models;
using CriticalCommonLib.Extensions;
using Dresser.Logic;
using Dresser.Data;
using System.Linq;

namespace Dresser.Extensions {
	internal static class InventoryItemExtensions {
		public static bool IsGlamourPlateApplicable(this CriticalInventoryItem item)
			=> item.SortedContainer == CriticalCommonLib.Enums.InventoryType.GlamourChest || item.SortedContainer == CriticalCommonLib.Enums.InventoryType.Armoire;
		public static bool IsFadedInBrowser(this CriticalInventoryItem item)
			=> ConfigurationManager.Config.FadeIconsIfNotHiddingTooltip && !item.IsGlamourPlateApplicable();
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
		public static bool IsFilterDisplayable(this CriticalInventoryItem item) {
			var returnVal = false;
			// inventory categories
			var displayInventoryCategories = ConfigurationManager.Config.FilterInventoryCategory;
			if (displayInventoryCategories.TryGetValue(item.SortedContainer.ToInventoryCategory(), out bool shouldCategoryBeDisplayed))
				returnVal |= shouldCategoryBeDisplayed;
			// OR inventory types
			var displayInventoryTypes = ConfigurationManager.Config.FilterInventoryType;
			if (displayInventoryTypes.TryGetValue(item.SortedContainer, out bool shouldTypeBeDisplayed))
				returnVal |= shouldTypeBeDisplayed;


			return returnVal;
		}
		//public static bool IsSoldBy(this CriticalInventoryItem item, string VendorFilterName) {
		//	if (PluginServices.Storage.VendorItems.TryGetValue(VendorFilterName, out var itemList))
		//		return itemList.Contains(item.ItemId);
		//	return false;
		//}
		//public static bool IsSoldByVendorFilter(this CriticalInventoryItem item) {
		//	return PluginServices.Storage.VendorItems.Where(v => ConfigurationManager.Config.FilterVendors.TryGetValue(v.Key, out bool b) && b).Any(v=>v.Value.Contains(item.ItemId));
		//}

	}
}
