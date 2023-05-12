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

		public Dictionary<GlamourPlateSlot, InventoryItem> DisplayPlateItems { get; set; } = new();
		public Dictionary<ushort, Dictionary<GlamourPlateSlot, InventoryItem>> PendingPlateItems { get; set; } = new();
		public ushort SelectedCurrentPlate { get; set; } = 0;
		public bool CurrentGearDisplayGear = false;

		// gear browser remember
		public float IconSizeMult { get; set; } = 1.0f;
		public bool ShowImagesInBrowser = true;
		public bool filterCurrentJob = true;
		public bool filterCurrentRace = true;








		// inventory tool stuff
		public bool AutoSave { get; set; } = true;
		public float AutoSaveMinutes { get; set; } = 0.1f;


		public Dictionary<ulong, Character> SavedCharacters = new();
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
		public Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> GetSavedInventory() {
			return SavedInventories;
		}
		public Dictionary<ulong, Character> GetSavedRetainers() {
			return SavedCharacters;
		}
		public void MarkReloaded() {
			if (!SaveBackgroundFilter) {
				ActiveBackgroundFilter = null;
			}
		}



		public delegate void ConfigurationChangedDelegate();
		public event ConfigurationChangedDelegate? ConfigurationChanged;

		public void Save() {
			ConfigurationChanged?.Invoke();
		}
	}
}
