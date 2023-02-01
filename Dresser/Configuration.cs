using CriticalCommonLib.Models;

using Dalamud.Configuration;
using Dalamud.Plugin;

using Dresser.Structs.FFXIV;

using System;
using System.Collections.Generic;

namespace Dresser {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public int Version { get; set; } = 0;

		public static Dictionary<GlamourPlateSlot, InventoryItem> SlotInventoryItems { get; set; } = new();

		// inventory tool stuff
		public bool AutoSave { get; set; } = true;
		public int AutoSaveMinutes { get; set; } = 1;

		public bool SaveBackgroundFilter { get; set; } = false;
		public string? ActiveBackgroundFilter { get; set; } = null;
		public bool InventoriesMigrated { get; set; } = false;
		[NonSerialized]
		public Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> SavedInventories = new();

		private Dictionary<ulong, HashSet<uint>> _acquiredItems = new();
		public Dictionary<ulong, HashSet<uint>> AcquiredItems {
			get => _acquiredItems ?? new Dictionary<ulong, HashSet<uint>>();
			set => _acquiredItems = value;
		}

		public void MarkReloaded() {
			if (!SaveBackgroundFilter) {
				ActiveBackgroundFilter = null;
			}
		}


		// the below exist just to make saving less cumbersome
		[NonSerialized]
		private DalamudPluginInterface? PluginInterface;

		public void Initialize(DalamudPluginInterface pluginInterface) {
			this.PluginInterface = pluginInterface;
		}

		public void Save() {
			this.PluginInterface!.SavePluginConfig(this);
		}
	}
}
