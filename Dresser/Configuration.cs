using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dalamud.Configuration;
using Dalamud.Logging;

using Dresser.Structs.Dresser;
using Dresser.Windows;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using static Dresser.Windows.GearBrowser;

namespace Dresser {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public Configuration() {
			LoadFilterInventoryCategory();
			LoadAdditionaltems();
		}
		public int Version { get; set; } = 0;

		public InventoryItemSet DisplayPlateItems { get; set; } = new();
		public Dictionary<ushort, InventoryItemSet> PendingPlateItems { get; set; } = new();
		public ushort SelectedCurrentPlate { get; set; } = 0;
		public bool CurrentGearDisplayGear = false;

		public Vector2 DyePickerDyeSize = new(30);

		// gear browser remember
		public float IconSizeMult { get; set; } = 1.0f;
		public bool FadeIconsIfNotHiddingTooltip = false;
		public bool ShowImagesInBrowser = true;
		public bool filterCurrentJob = true;
		public bool filterCurrentRace = true;
		public Dictionary<InventoryCategory, bool> FilterInventoryCategory { get; set; } = new();
		public float FilterInventoryCategoryColumnDistribution { get; set; } = 1.5f;
		public int FilterInventoryCategoryColumnNumber { get; set; } = 1;
		public int FilterInventoryTypeColumnNumber { get; set; } = 1;
		public float GearBrowserSideBarSize { get; set; } = 300f;
		public DisplayMode GearBrowserDisplayMode { get; set; } = DisplayMode.SidebarOnRight;

		public void LoadFilterInventoryCategory() {
			//PluginLog.Debug($"FilterInventoryCategory: cc:{FilterInventoryCategory.Count} nc:{GearBrowser.AllowedCategories.Count} dc:{this.FilterInventoryCategory.Select(i => i.Key).Except(GearBrowser.AllowedCategories).Count()}");
			if (this.FilterInventoryCategory.Count != GearBrowser.AllowedCategories.Count || this.FilterInventoryCategory.Select(i => i.Key).Except(GearBrowser.AllowedCategories).Count() != 0) {
				var oldFilterInventoryCategory = this.FilterInventoryCategory.Copy();
				this.FilterInventoryCategory = GearBrowser.AllowedCategories.ToDictionary(c => c, c => {
					if (oldFilterInventoryCategory != null && oldFilterInventoryCategory.TryGetValue(c, out bool d) && d)
						return d;
					return c == InventoryCategory.GlamourChest || c == InventoryCategory.Armoire;
				});
			}
		}


		public Dictionary<InventoryType, bool> FilterInventoryType { get; set; } = new();
		public void LoadAdditionaltems() {
			//PluginLog.Debug($"FilterInventoryType: cc:{FilterInventoryType.Count} nc:{Storage.FilterNames.Sum(k => k.Value.Count)} dc:{this.FilterInventoryType.Select(i => i.Key).Except(Storage.FilterNames.SelectMany(v => v.Value.Keys)).Count()}");
			if (this.FilterInventoryType.Count != PluginServices.Storage.FilterNames.Sum(k => k.Value.Count) || this.FilterInventoryType.Select(i => i.Key).Except(PluginServices.Storage.FilterNames.SelectMany(v => v.Value.Keys)).Count() != 0) {
				var oldFilterInventoryType = this.FilterInventoryType.Copy();
				this.FilterInventoryType = PluginServices.Storage.FilterNames.SelectMany(v => v.Value.Keys).ToDictionary(it => it, it => {
					if (oldFilterInventoryType != null && oldFilterInventoryType.TryGetValue(it, out bool d) && d)
						return d;
					return false;
				});
			}
		}
		public bool FilterSourceCollapse = false;
		public bool FilterAdditionalCollapse = true;
		public bool FilterAdvancedCollapse = true;
		public bool FilterSortCollapse = true;






		// inventory tool stuff
		public bool AutoSave { get; set; } = true;
		public float AutoSaveMinutes { get; set; } = 5f;


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
		public Dictionary<InventoryCategory, List<InventoryItem>> GetSavedInventoryLocalChar() {
			if (SavedInventories.TryGetValue(PluginServices.Context.LocalPlayerCharacterId, out var inventories))
				return inventories;
			return new();
		}
		public Dictionary<InventoryCategory, List<InventoryItem>> GetSavedInventoryLocalCharsRetainers() {
			Dictionary<InventoryCategory, List<InventoryItem>> returnDic = new();

			foreach ((var charId, var invs) in SavedInventories.Where(c => PluginServices.CharacterMonitor.BelongsToActiveCharacter(c.Key) && c.Key != PluginServices.CharacterMonitor.ActiveCharacterId)) {
				PluginLog.Warning($"GetSavedInventoryLocalCharsRetainers: {charId}");
				foreach ((var invCat, var list) in invs) {

					List<InventoryItem> tmpList = new();
					if (returnDic.TryGetValue(invCat, out var prevList) && prevList != null)
						tmpList = tmpList.Concat(prevList).ToList();
					tmpList = tmpList.Concat(list).ToList();
					returnDic[invCat] = tmpList;
				}
			}
			PluginLog.Debug($"total: {string.Join(",", returnDic.Select(c => c.Key))} => {string.Join(",", returnDic.Select(c => c.Value.Count))}");
			return returnDic;
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
			LoadAdditionaltems();
		}

		public void Save() {
			ConfigurationChanged?.Invoke();
		}
	}
}
