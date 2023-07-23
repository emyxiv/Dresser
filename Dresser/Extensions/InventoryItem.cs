using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dresser.Services;

using static Dresser.Services.Storage;

using CriticalInventoryItem = Dresser.Structs.Dresser.InventoryItem;

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
		public static CriticalInventoryItem Clone(this CriticalInventoryItem item)
			=> item.Copy()!;

		public static CriticalInventoryItem New(uint itemId, byte stain)
			=> new(0, 0, itemId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, stain, 0);

		public static bool IsAppearanceDifferent(this CriticalInventoryItem item, CriticalInventoryItem? item2)
			=> (item?.ItemId ?? 0) != (item2?.ItemId ?? 0) || (item?.Stain ?? 0) != (item2?.Stain ?? 0);
		public static bool IsInFilterLevelRanges(this CriticalInventoryItem item) {
			var elmin = ConfigurationManager.Config.filterEquipLevel.X;
			var elmax = ConfigurationManager.Config.filterEquipLevel.Y;
			var el = item.Item.LevelEquip;
			var ilmin = ConfigurationManager.Config.filterItemLevel.X;
			var ilmax = ConfigurationManager.Config.filterItemLevel.Y;
			var il = item.Item.LevelItem.Row;

			return elmin <= el && el <= elmax && ilmin <= il && il <= ilmax;
		}

		public static string FormattedInventoryCategoryType(this CriticalInventoryItem item) {
			var cat = item.SortedCategory;
			var catForm = cat.ToFriendlyName();
			var type = item.SortedContainer;
			var typeForm = type.ToFormattedName();
			if ( cat == 0) {
				if ((int)type >= (int)InventoryTypeExtra.AllItems)
					catForm = "Not Owned";
				else if (type == 0) {
					catForm = "Location Not Found";
					typeForm = "";
				}
			}

			return cat switch {
				InventoryCategory.GlamourChest or InventoryCategory.Armoire
					=> catForm,
				InventoryCategory.RetainerBags
					=> $"{item.FormattedOwnerName()}  -  {typeForm}",
				InventoryCategory.RetainerEquipped or InventoryCategory.RetainerMarket
					=> $"{item.FormattedOwnerName()}  -  {cat.FormattedName()}",
				_
					=> $"{catForm}  -  {typeForm}".Trim("\r\n -".ToCharArray())
			};
		}
		public static string FormattedOwnerName(this CriticalInventoryItem item) {
			var id = item.InRetainer ? item.RetainerId : PluginServices.CharacterMonitor.ActiveCharacterId;
			var character = PluginServices.CharacterMonitor.GetCharacterById(id);
			return character?.FormattedName ?? "";
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
