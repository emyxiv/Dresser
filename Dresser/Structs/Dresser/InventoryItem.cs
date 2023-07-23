using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;

using Dalamud.Logging;
using Dalamud.Utility;

using Dresser.Extensions;

using Lumina.Excel.GeneratedSheets;
using System.ComponentModel;
using System.Text.Json.Serialization;

using static Dresser.Services.Storage;

using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;

namespace Dresser.Structs.Dresser {
	public class InventoryItem : CriticalInventoryItem {

		public string? ModName = "";
		public string? ModDirectory = "";
		public string? ModModelPath = "";


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
			return new InventoryItem(item.Container, item.Slot, item.ItemId, item.Quantity, item.Spiritbond, item.Condition, item.Flags, item.Materia0, item.Materia1, item.Materia2, item.Materia3, item.Materia4, item.MateriaLevel0, item.MateriaLevel1, item.MateriaLevel2, item.MateriaLevel3, item.MateriaLevel4, item.Stain, item.GlamourId);
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


	}
}
