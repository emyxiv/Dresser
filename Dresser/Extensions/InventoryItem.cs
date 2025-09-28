using System.Linq;

using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dalamud.Utility;

using Dresser.Services;

using Lumina.Excel.Sheets;

using static Dresser.Services.Storage;

using CriticalInventoryItem = Dresser.Structs.Dresser.InventoryItem;

namespace Dresser.Extensions {
	internal static class InventoryItemExtensions {
		public static bool IsGlamourPlateApplicable(this CriticalInventoryItem item)
			=> item.SortedContainer == InventoryType.GlamourChest || item.SortedContainer == InventoryType.Armoire;
		public static bool IsFadedInBrowser(this CriticalInventoryItem item)
			=> ConfigurationManager.Config.FadeIconsIfNotHiddingTooltip && !item.IsGlamourPlateApplicable();

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

		public static CriticalInventoryItem New(uint itemId, byte stain, byte stain2)
			=> new(0, 0, itemId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, stain, stain2, 0);

		public static bool IsAppearanceDifferent(this CriticalInventoryItem item, CriticalInventoryItem? item2)
			=> (item?.ItemId ?? 0) != (item2?.ItemId ?? 0) || (item?.Stain ?? 0) != (item2?.Stain ?? 0);
		public static bool IsInFilterLevelRanges(this CriticalInventoryItem item) {
			var elmin = (int)ConfigurationManager.Config.filterEquipLevel.X;
			var elmax = (int)ConfigurationManager.Config.filterEquipLevel.Y;
			var el = item.Item.Base.LevelEquip;
			var ilmin = (int)ConfigurationManager.Config.filterItemLevel.X;
			var ilmax = (int)ConfigurationManager.Config.filterItemLevel.Y;
			var il = item.Item.Base.LevelItem.RowId;

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
			var id = item.InRetainer ? item.RetainerId : PluginServices.ClientState.LocalContentId;
			// todo: fix retainer id and names, thoe following line has very little chance to work, as the retainer won't be be in the object table.
			var charaName = PluginServices.Objects.SearchById(id)?.Name;
			if (charaName != null) return charaName.TextValue;
			return "Retainer";

			//var character = PluginServices.CharacterMonitor.GetCharacterById(id);
			//return character?.FormattedName ?? "";
		}

		//public static bool IsSoldBy(this CriticalInventoryItem item, string VendorFilterName) {
		//	if (PluginServices.Storage.VendorItems.TryGetValue(VendorFilterName, out var itemList))
		//		return itemList.Contains(item.ItemId);
		//	return false;
		//}
		//public static bool IsSoldByVendorFilter(this CriticalInventoryItem item) {
		//	return PluginServices.Storage.VendorItems.Where(v => ConfigurationManager.Config.FilterVendors.TryGetValue(v.Key, out bool b) && b).Any(v=>v.Value.Contains(item.ItemId));
		//}

		public static string StainName(this CriticalInventoryItem item) {
			var stainEntry = item.StainEntry;
			return stainEntry?.Name.ToDalamudString().ToString() ?? "";
		}
		public static string Stain2Name(this CriticalInventoryItem item)
		{
			var stainEntry = PluginServices.DataManager.GetExcelSheet<Stain>().First(s => s.RowId == item.Stain2);
			return stainEntry.Name.ToDalamudString().ToString() ?? "";
		}
	}
}
