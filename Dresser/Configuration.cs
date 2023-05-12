using CriticalCommonLib.Models;
using CriticalCommonLib.Extensions;

using Dalamud.Configuration;
using Dalamud.Plugin;

using Dresser.Structs.FFXIV;
using Dresser.Windows;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Dresser {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public Configuration() {
			LoadFilterInventoryCategory();
		}
		public int Version { get; set; } = 0;

		public Dictionary<GlamourPlateSlot, InventoryItem> DisplayPlateItems { get; set; } = new();
		public Dictionary<ushort, Dictionary<GlamourPlateSlot, InventoryItem>> PendingPlateItems { get; set; } = new();
		public ushort SelectedCurrentPlate { get; set; } = 0;
		public bool CurrentGearDisplayGear = false;

		// gear browser remember
		public float IconSizeMult { get; set; } = 1.0f;
		public bool FadeIconsIfNotHiddingTooltip = false;
		public bool ShowImagesInBrowser = true;
		public bool filterCurrentJob = true;
		public bool filterCurrentRace = true;
		public Dictionary<InventoryCategory, bool> FilterInventoryCategory { get; set; } = new();
		public void LoadFilterInventoryCategory() {
			if (this.FilterInventoryCategory.Count != GearBrowser.AllowedCategories.Count || this.FilterInventoryCategory.Select(i => i.Key).Except(GearBrowser.AllowedCategories).Count() != 0) {
				//PluginLog.Debug($"FilterInventoryCategory different, recreating it");
				var oldFilterInventoryCategory = this.FilterInventoryCategory.Copy();
				this.FilterInventoryCategory = GearBrowser.AllowedCategories.ToDictionary(c => c, (c) => {
					if (oldFilterInventoryCategory != null && oldFilterInventoryCategory.TryGetValue(c, out bool d) && d)
						return d;
					return true;
				});
			}
		}
		public bool FilterSourceCollapse = false;
		public bool FilterAdvancedCollapse = true;







		// inventory tool stuff
		public bool AutoSave { get; set; } = true;
		public float AutoSaveMinutes { get; set; } = 0.1f;


		public Dictionary<ulong, Character> SavedCharacters = new();
		public bool SaveBackgroundFilter { get; set; } = false;
		public string? ActiveBackgroundFilter { get; set; } = null;
		public int InventoriesMigrated { get; set; } = 0;
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


		/// Migrations
		public void Migrate() {
			LoadFilterInventoryCategory();
		}

		public void Save() {
			ConfigurationChanged?.Invoke();
		}
	}
}
