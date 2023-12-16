using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;

using Dresser.Logic;
using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Services;
using Dresser.Structs.Actor;

using Lumina.Excel.GeneratedSheets;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using static Dresser.Services.Storage;

using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;

namespace Dresser.Structs.Dresser {
	public class InventoryItem : CriticalInventoryItem {

		public string? ModName = "";
		public string? ModDirectory = "";
		public string? ModModelPath = "";
		public uint QuantityNeeded = 1;


		public InventoryItem(InventoryItem inventoryItem) : base(inventoryItem) {
			if(inventoryItem.IsModded()) PluginLog.Warning($"B Copy InventoryItem {inventoryItem.ModDirectory}");

			this.ModName = inventoryItem.ModName;
			this.ModDirectory = inventoryItem.ModDirectory;
			this.ModModelPath = inventoryItem.ModModelPath;
			if (inventoryItem.IsModded()) PluginLog.Warning($"A Copy InventoryItem {this.ModDirectory}");
		}

		public InventoryItem(InventoryType container, short slot, uint itemId, uint quantity, ushort spiritbond, ushort condition, FFXIVClientStructs.FFXIV.Client.Game.InventoryItem.ItemFlags flags, ushort materia0, ushort materia1, ushort materia2, ushort materia3, ushort materia4, byte materiaLevel0, byte materiaLevel1, byte materiaLevel2, byte materiaLevel3, byte materiaLevel4, byte stain, uint glamourId) : base(container, slot, itemId, quantity, spiritbond, condition, flags, materia0, materia1, materia2, materia3, materia4, materiaLevel0, materiaLevel1, materiaLevel2, materiaLevel3, materiaLevel4, stain, glamourId) {
			this.ModName = "";
			this.ModDirectory = "";
			this.ModModelPath = "";
		}
		public InventoryItem(InventoryType inventoryType, uint itemId) : this(inventoryType, 0, itemId, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) {
			this.SortedContainer = inventoryType;
		}
		public InventoryItem(InventoryType inventoryType, uint itemId, string modName, string modDirectory, string modModelPath) : this(inventoryType, itemId) {
			this.ModName = modName;
			this.ModDirectory = modDirectory;
			this.ModModelPath = modModelPath;
		}


		[JsonConstructor]
		public InventoryItem() {

		}


		public static CriticalInventoryItem ToCritical(InventoryItem item) {
			return new CriticalInventoryItem(item);
		}
		public static InventoryItem FromCritical(CriticalInventoryItem item) {
			return new InventoryItem { Container = item.Container, Slot = item.Slot, ItemId = item.ItemId, Quantity = item.Quantity, Spiritbond = item.Spiritbond, Condition = item.Condition, Flags = item.Flags, Materia0 = item.Materia0, Materia1 = item.Materia1, Materia2 = item.Materia2, Materia3 = item.Materia3, Materia4 = item.Materia4, MateriaLevel0 = item.MateriaLevel0, MateriaLevel1 = item.MateriaLevel1, MateriaLevel2 = item.MateriaLevel2, MateriaLevel3 = item.MateriaLevel3, MateriaLevel4 = item.MateriaLevel4, Stain = item.Stain, GlamourId = item.GlamourId, SortedContainer = item.SortedContainer, SortedCategory = item.SortedCategory, SortedSlotIndex = item.SortedSlotIndex, RetainerId = item.RetainerId, RetainerMarketPrice = item.RetainerMarketPrice, GearSets = item.GearSets, };
		}
		public static InventoryItem Zero => new InventoryItem();
		public bool IsModded() {
			return !this.ModDirectory.IsNullOrWhitespace();
		}

		public bool IsModDifferent(InventoryItem? item2) {
			return this.ModName == item2?.ModName
				&& this.ModDirectory == item2?.ModDirectory
				&& this.ModModelPath == item2?.ModModelPath;
		}


		public ItemEquip ToItemEquip() {
			var itemModelMain = this.Item.ModelMainItemModel();

			return new() {
				Id = itemModelMain.Id,
				Variant = (byte)itemModelMain.Variant,
				Dye = this.Item.IsDyeable ? this.Stain : (byte)0,
			};
		}
		public WeaponEquip ToWeaponEquip(WeaponIndex index) {
			var itemModel = index == WeaponIndex.OffHand ? Item.ModelSubItemModel() : Item.ModelMainItemModel();
			return new() {
				Set = (ushort)(index == WeaponIndex.OffHand ? Item.ModelSub : Item.ModelMain),
				Base = itemModel.Base,
				Variant = itemModel.Variant,
				Dye = this.Item.IsDyeable ? this.Stain : (byte)0,
			};
		}
		public WeaponEquip ToWeaponEquipMain()
			=> ToWeaponEquip(WeaponIndex.MainHand);
		public WeaponEquip ToWeaponEquipSub()
			=> ToWeaponEquip(WeaponIndex.OffHand);
		public IEnumerable<InventoryItem> GetDyesInInventories() {
			var stainTransient = Service.ExcelCache.GetSheet<StainTransient>().FirstOrDefault(st => st.RowId == this.Stain);

			var inventories = PluginServices.AllaganTools.GetItemsLocalCharsRetainers(true);
			var foundDyes = inventories.SelectMany(ip => ip.Value.Where(v => v.ItemId == stainTransient?.Item1.Value?.RowId || v.ItemId == stainTransient?.Item2.Value?.RowId)).Where(i=>i.ItemId != 0);

			if(!foundDyes.Any()) {
				var defaultStainRowId = stainTransient?.Item1.Value?.RowId;
				if(defaultStainRowId != null) {
					var unobtainedDye = new InventoryItem((InventoryType)InventoryTypeExtra.AllItems, (uint)defaultStainRowId);

					foundDyes = new List<InventoryItem>() { unobtainedDye };
				}
			}
			//if(excludeBags)
			//	return foundDyes.Where(i=>i.SortedCategory != CriticalCommonLib.Models.InventoryCategory.CharacterBags);

			return foundDyes.Select(i=>i.Copy()!);
		}

	}
}
