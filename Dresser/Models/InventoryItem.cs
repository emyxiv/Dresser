using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;

using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;

using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Models.Actor;
using Dresser.Gui;

using Lumina.Excel.Sheets;

using static Dresser.Services.Storage;

using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;
using InventoryCategory = CriticalCommonLib.Models.InventoryCategory;

namespace Dresser.Models {
	public partial class InventoryItem : CriticalInventoryItem {

		public string? ModName = null;
		public string? ModDirectory = null;
		public string? ModModelPath = null;
		public string? ModAuthor = null;
		public string? ModVersion = null;
		public string? ModWebsite = null;
		public string? ModIconPath = null;
		public uint QuantityNeeded = 1;


		public InventoryItem(InventoryItem inventoryItem) : base(PluginServices.InventoryItemFactory.ItemSheet, PluginServices.InventoryItemFactory.StainSheet) {
            FromInventoryItem(inventoryItem);
			if(inventoryItem.IsModded()) PluginLog.Warning($"B Copy InventoryItem {inventoryItem.ModDirectory}");

			this.ModName = inventoryItem.ModName;
			this.ModDirectory = inventoryItem.ModDirectory;
			this.ModModelPath = inventoryItem.ModModelPath;
			if (inventoryItem.IsModded()) PluginLog.Warning($"A Copy InventoryItem {this.ModDirectory}");
		}

		public InventoryItem(InventoryType container,
			short slot,
			uint itemId,
			uint quantity,
			ushort spiritbond,
			ushort condition,
			FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags flags,
			ushort materia0,
			ushort materia1,
			ushort materia2,
			ushort materia3,
			ushort materia4,
			byte materiaLevel0,
			byte materiaLevel1,
			byte materiaLevel2,
			byte materiaLevel3,
			byte materiaLevel4,
			byte stain,
			byte stain2,
			uint glamourId) : base(PluginServices.InventoryItemFactory.ItemSheet, PluginServices.InventoryItemFactory.StainSheet) {
			FromRaw(container, slot, itemId, quantity, spiritbond, condition, flags, materia0, materia1, materia2, materia3, materia4, materiaLevel0, materiaLevel1, materiaLevel2, materiaLevel3, materiaLevel4, stain, stain2, glamourId);
		}
		public InventoryItem(InventoryType inventoryType, uint itemId) : this(inventoryType, 0, itemId, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) {
			this.SortedContainer = inventoryType;
		}
		public InventoryItem(InventoryType inventoryType, uint itemId, string modName, string modDirectory, string modModelPath) : this(inventoryType, itemId) {
			this.ModName = modName;
			this.ModDirectory = modDirectory;
			this.ModModelPath = modModelPath;
		}

		public static InventoryItem FromSavedGlamourItem(SavedGlamourItem item)
			=> New(item.ItemId,item.Stain1,item.Stain2);
		public static InventoryItem New(uint itemId, byte stain, byte stain2)
			=> new(0, 0, itemId, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, stain, stain2, 0);


		[JsonConstructor]
		public InventoryItem() : base(PluginServices.InventoryItemFactory.ItemSheet, PluginServices.InventoryItemFactory.StainSheet) {

		}


		public static CriticalInventoryItem ToCritical(InventoryItem item) {
            var cclItem = new CriticalInventoryItem(PluginServices.InventoryItemFactory.ItemSheet, PluginServices.InventoryItemFactory.StainSheet);
            cclItem.FromInventoryItem(item);
			return cclItem;
		}
		public static InventoryItem FromCritical(CriticalInventoryItem item) {
			return new InventoryItem { Container = item.Container, Slot = item.Slot, ItemId = item.ItemId, Quantity = item.Quantity, Spiritbond = item.Spiritbond, Condition = item.Condition, Flags = item.Flags, Materia0 = item.Materia0, Materia1 = item.Materia1, Materia2 = item.Materia2, Materia3 = item.Materia3, Materia4 = item.Materia4, MateriaLevel0 = item.MateriaLevel0, MateriaLevel1 = item.MateriaLevel1, MateriaLevel2 = item.MateriaLevel2, MateriaLevel3 = item.MateriaLevel3, MateriaLevel4 = item.MateriaLevel4, Stain = item.Stain, Stain2 = item.Stain2, GlamourId = item.GlamourId, SortedContainer = item.SortedContainer, SortedCategory = item.SortedCategory, SortedSlotIndex = item.SortedSlotIndex, RetainerId = item.RetainerId, RetainerMarketPrice = item.RetainerMarketPrice, GearSets = item.GearSets, };
		}
		public InventoryItem Copy() {
			var item = this;
			return new InventoryItem {
				Container = item.Container, Slot = item.Slot, ItemId = item.ItemId, Quantity = item.Quantity, Spiritbond = item.Spiritbond, Condition = item.Condition, Flags = item.Flags, Materia0 = item.Materia0, Materia1 = item.Materia1, Materia2 = item.Materia2, Materia3 = item.Materia3, Materia4 = item.Materia4, MateriaLevel0 = item.MateriaLevel0, MateriaLevel1 = item.MateriaLevel1, MateriaLevel2 = item.MateriaLevel2, MateriaLevel3 = item.MateriaLevel3, MateriaLevel4 = item.MateriaLevel4, Stain = item.Stain, Stain2 = item.Stain2, GlamourId = item.GlamourId, SortedContainer = item.SortedContainer, SortedCategory = item.SortedCategory, SortedSlotIndex = item.SortedSlotIndex, RetainerId = item.RetainerId, RetainerMarketPrice = item.RetainerMarketPrice, GearSets = item.GearSets,
				ModName = item.ModName,
				ModDirectory = item.ModDirectory,
				ModModelPath = item.ModModelPath,
				ModAuthor = item.ModAuthor,
				ModIconPath = item.ModIconPath,
				ModVersion = item.ModVersion,
				ModWebsite = item.ModWebsite,
			};
		}
		public InventoryItem Clone() => this.Copy();
		public static InventoryItem Zero => new InventoryItem();

		public void Clear() {

			this.Container = 0;
			//this.Slot = 0;
			this.ItemId = 0;
			this.Quantity = 0;
			this.Spiritbond = 0;
			this.Condition = 0;
			this.Flags = 0;
			this.Materia0 = 0;
			this.Materia1 = 0;
			this.Materia2 = 0;
			this.Materia3 = 0;
			this.Materia4 = 0;
			this.MateriaLevel0 = 0;
			this.MateriaLevel1 = 0;
			this.MateriaLevel2 = 0;
			this.MateriaLevel3 = 0;
			this.MateriaLevel4 = 0;
			this.Stain = 0;
			this.Stain2 = 0;

			this.ModName = null;
			this.ModDirectory = null;
			this.ModModelPath = null;
		}

		public bool IsModded() {
			return !this.ModDirectory.IsNullOrWhitespace();
		}

		public bool IsModDifferent(InventoryItem? item2) {
			return this.ModName == item2?.ModName
				&& this.ModDirectory == item2?.ModDirectory
				&& this.ModModelPath == item2?.ModModelPath;
		}
		public bool IsMod((string Path, string Name)? mod) {
			if (mod == null) return false; // always say it's different, even if both are null, it's not a mod
			return this.ModName == mod?.Name
				&& this.ModDirectory == mod?.Path;
		}
		public (string Path, string Name)? GetMod() {
			if(this.ModDirectory.IsNullOrWhitespace() && this.ModName.IsNullOrWhitespace()) return null;
			return (this.ModDirectory ?? "", this.ModName ?? "");
		}

		public ItemEquip ToItemEquip() {
			var itemModelMain = this.Item.ModelMainItemModel();

			return new() {
				Id = itemModelMain.Id,
				Variant = (byte)itemModelMain.Variant,
				Dye = this.Item.IsDyeable1() ? this.Stain : (byte)0,
				Dye2 = this.Item.IsDyeable2() ? this.Stain2 : (byte)0,
			};
		}
		public WeaponEquip ToWeaponEquip(WeaponIndex index) {

			var itemModel = index == WeaponIndex.MainHand || Item.IsMainModelOnOffhand() ? Item.ModelMainItemModel() : Item.ModelSubItemModel();
			return new() {
				Set = itemModel.Id,
				Base = itemModel.Base,
				Variant = itemModel.Variant,
				Dye = this.Item.IsDyeable1() ? this.Stain : (byte)0,
				Dye2 = this.Item.IsDyeable2() ? this.Stain2 : (byte)0,
			};
		}

		public static InventoryItem? FromItemEquip(ItemEquip? itemEquip, GlamourPlateSlot slot) {
			if (itemEquip == null || itemEquip.Value.Id == 0) return null;
			var dd = FromModelMain(itemEquip.Value.ToModelId(), slot);
			if (dd != null) return new InventoryItem(InventoryType.Bag0, dd.RowId) {
				Stain = itemEquip.Value.Dye,
				Stain2 = itemEquip.Value.Dye
			};
			return null;
		}

		public static InventoryItem? FromWeaponEquip(WeaponEquip? weaponEquip, GlamourPlateSlot slot) {
			if (weaponEquip == null || weaponEquip.Value.Set == 0) return null;

			//PluginLog.Debug($"appearance: Set:{weaponEquip.Value.Set}, Variant:{weaponEquip.Value.Base}, variant:{weaponEquip.Value.Variant}  ==> {weaponEquip.Value.ToModelId()}");
			//uint stoladressId = 39937;
			//var stoladress = Service.ExcelCache.AllItems.Where(item => item.Value.RowId == stoladressId).First().Value;
			//PluginLog.Debug($"manderville wings: {stoladress.RowId} => model:m:{stoladress.ModelMain} s:{stoladress.ModelSub}");


			var dd = slot.ToWeaponIndex() == WeaponIndex.MainHand || weaponEquip.Value.IsMainModelOnOffhand() ? FromModelWeaponMain(weaponEquip.Value.ToModelId(), slot) : FromModelWeaponSub(weaponEquip.Value.ToModelId(), slot);
			if (dd != null) return new InventoryItem(InventoryType.Bag0, dd.RowId) {
				Stain = weaponEquip.Value.Dye,
				Stain2 = weaponEquip.Value.Dye2
			};
			return null;
		}
		public static ItemRow? FromModelMain(ulong model, GlamourPlateSlot slot) {

			var equipSlotCategory = slot.ToEquipSlotCategoryByte();
			var ddddd = PluginServices.SheetManager.GetSheet<ItemSheet>().Where(item => item.Base.ModelMain == model && item.Base.EquipSlotCategory.RowId == equipSlotCategory);
			//PluginLog.Debug($"looking for item... {model} => {ddddd.Count()}");
			//foreach((var id,var item) in ddddd) {
			//	PluginLog.Debug($"     found item {id} {item.NameString} {item.ModelMain} {item.ModelSub} => {item.EquipSlotCategory.Row == slot.ToEquipSlotCategoryByte()}");

			//}
			return ddddd.FirstOrDefault();
		}
		public static ItemRow? FromModelWeaponMain(ulong model, GlamourPlateSlot slot) {

			//var equipSlotCategory = slot.ToEquipSlotCategoryByte();
			var ddddd = PluginServices.SheetManager.GetSheet<ItemSheet>().Where(item => item.Base.ModelMain == model);
			//PluginLog.Debug($"looking for item... {model} => {ddddd.Count()}");
			//foreach ((var id, var item) in ddddd) {
			//	PluginLog.Debug($"     found item {id} {item.NameString} {item.ModelMain} {item.ModelSub} => {item.EquipSlotCategory.Row == slot.ToEquipSlotCategoryByte()}");

			//}
			return ddddd.FirstOrDefault();
		}
		public static ItemRow? FromModelWeaponSub(ulong model, GlamourPlateSlot slot) {
			return PluginServices.SheetManager.GetSheet<ItemSheet>().FirstOrDefault(item => item.Base.ModelSub == model);
		}


		public WeaponEquip ToWeaponEquipMain()
			=> ToWeaponEquip(WeaponIndex.MainHand);
		public WeaponEquip ToWeaponEquipSub()
			=> ToWeaponEquip(WeaponIndex.OffHand);
		public IEnumerable<InventoryItem> GetDyesInInventories(int dyeIndex) {
			var stainTransient = PluginServices.DataManager.GetExcelSheet<StainTransient>().FirstOrDefault(st => st.RowId == (dyeIndex == 1 ? this.Stain : this.Stain2));

			var inventories = PluginServices.AllaganTools.GetItemsLocalCharsRetainers(true);
			var foundDyes = inventories.SelectMany(ip => ip.Value.Where(v => v.ItemId == stainTransient.Item1.RowId || v.ItemId == stainTransient.Item2.RowId)).Where(i=>i.ItemId != 0);

			if(!foundDyes.Any()) {
				var defaultStainRowId = stainTransient.Item1.RowId;
				if(defaultStainRowId != null) {
					var unobtainedDye = new InventoryItem((InventoryType)InventoryTypeExtra.AllItems, (uint)defaultStainRowId);

					foundDyes = new List<InventoryItem>() { unobtainedDye };
				}
			}
			//if(excludeBags)
			//	return foundDyes.Where(i=>i.SortedCategory != CriticalCommonLib.Models.InventoryCategory.CharacterBags);

			return foundDyes.Select(i=>i.Copy()!);
		}
		public bool IsNotInBlackList() {
			if (!IsModded()) return true;
			if (ConfigurationManager.Config.PenumbraModsBlacklist.Any(m => m.Path == this.ModDirectory))
				return false;
			// Check modpath blacklist
			if (PluginServices.Penumbra.IsModPathBlacklisted(this.ModDirectory ?? ""))
				return false;
			return true;
		}

		public bool IsInGearBrowserSelectedSlot() {
			var itemSlot = this.Item.GlamourPlateSlot();
			//PluginLog.Debug($"IsInGearBrowserSelectedSlot: {itemSlot}");
			return itemSlot == GearBrowser.SelectedSlot || (itemSlot == GlamourPlateSlot.RightRing && GearBrowser.SelectedSlot == GlamourPlateSlot.LeftRing);
		}

		public bool HasTagContains(string searchTerm) {

			// check if the item has any tags that contain the search term
			if (!TagStore.itemToTags.TryGetValue(this.ItemId, out var tagIds)) {
				if(searchTerm.Contains("t:none", System.StringComparison.OrdinalIgnoreCase)) {
					return true;
				}
				return false;
			}

			// get all tags that match the search term
			var tags = Tag.TagNameContains(searchTerm).Select(t => t.Id);

			// check if the item has any of the matching tags
			return tagIds.Overlaps(tags);
		}




		private IEnumerable<Dresser.Services.Ipc.ItemProviderInfo>? _providerInfo = null;
		private bool _providerInfoSet = false;
		public IEnumerable<Dresser.Services.Ipc.ItemProviderInfo>? GetItemProvider() {
			if (_providerInfoSet) return _providerInfo;
			_providerInfoSet = true;
			if (!PluginServices.ItemVendorLocation.IsInitialized()) return null;
			_providerInfo = PluginServices.ItemVendorLocation.GetItemInfoProvider(ItemId);
			return _providerInfo;
		}

		// --- Merged from Extensions/InventoryItem.cs ---

		public bool IsGlamourPlateApplicable()
			=> SortedContainer == InventoryType.GlamourChest || SortedContainer == InventoryType.Armoire;
		public bool IsFadedInBrowser()
			=> ConfigurationManager.Config.FadeIconsIfNotHiddingTooltip && !IsGlamourPlateApplicable();

		public bool IsFilterDisplayable() {
			var returnVal = false;
			var displayInventoryCategories = ConfigurationManager.Config.FilterInventoryCategory;
			if (displayInventoryCategories.TryGetValue(SortedContainer.ToInventoryCategory(), out bool shouldCategoryBeDisplayed))
				returnVal |= shouldCategoryBeDisplayed;
			var displayInventoryTypes = ConfigurationManager.Config.FilterInventoryType;
			if (displayInventoryTypes.TryGetValue(SortedContainer, out bool shouldTypeBeDisplayed))
				returnVal |= shouldTypeBeDisplayed;
			return returnVal;
		}

		public bool IsAppearanceDifferent(InventoryItem? item2)
			=> (ItemId) != (item2?.ItemId ?? 0) || (Stain) != (item2?.Stain ?? 0);
		public bool IsInFilterLevelRanges() {
			var elmin = (int)ConfigurationManager.Config.filterEquipLevel.X;
			var elmax = (int)ConfigurationManager.Config.filterEquipLevel.Y;
			var el = Item.Base.LevelEquip;
			var ilmin = (int)ConfigurationManager.Config.filterItemLevel.X;
			var ilmax = (int)ConfigurationManager.Config.filterItemLevel.Y;
			var il = Item.Base.LevelItem.RowId;
			return elmin <= el && el <= elmax && ilmin <= il && il <= ilmax;
		}

		public bool IsObtained() {
			return !(SortedCategory == 0 && ((int)SortedContainer >= (int)InventoryTypeExtra.AllItems || (int)SortedContainer == 0));
		}
		public string FormattedInventoryCategoryType() {
			var cat = SortedCategory;
			var catForm = cat.ToFriendlyName();
			var type = SortedContainer;
			var typeForm = type.ToFormattedName();
			if (cat == 0) {
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
					=> $"{FormattedOwnerName()}  -  {typeForm}",
				InventoryCategory.RetainerEquipped or InventoryCategory.RetainerMarket
					=> $"{FormattedOwnerName()}  -  {cat.FormattedName()}",
				_
					=> $"{catForm}  -  {typeForm}".Trim("\r\n -".ToCharArray())
			};
		}
		public string FormattedOwnerName() {
			var id = InRetainer ? RetainerId : PluginServices.ClientState.LocalContentId;
			var charaName = PluginServices.Objects.SearchById(id)?.Name;
			if (charaName != null) return charaName.TextValue;
			return "Retainer";
		}

		public string StainName() {
			var stainEntry = StainEntry;
			return stainEntry?.Name.ToDalamudString().ToString() ?? "";
		}
		public string Stain2Name() {
			var stainEntry = PluginServices.DataManager.GetExcelSheet<Stain>().First(s => s.RowId == Stain2);
			return stainEntry.Name.ToDalamudString().ToString() ?? "";
		}

	}
}
