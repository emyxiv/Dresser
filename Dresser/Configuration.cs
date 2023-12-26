using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dalamud.Configuration;

using Dresser.Logic;
using Dresser.Structs.Dresser;
using Dresser.Windows;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using InventoryItem = Dresser.Structs.Dresser.InventoryItem;
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
		public ushort NumberOfFreePendingPlates { get; set; } = 19;
		public ushort NumberofPendingPlateNextColumn { get; set; } = 20;
		public ushort SelectedCurrentPlate { get; set; } = 0;
		public bool CurrentGearPortablePlateJobIcons = true;
		public bool CurrentGearPortablePlateJobBgColors = false;
		public GlamourPlateSlot CurrentGearSelectedSlot = GlamourPlateSlot.Body;

		public bool CurrentGearDisplayGear = false;
		public bool CurrentGearDisplayHat = true;
		public bool CurrentGearDisplayWeapon = true;
		public bool CurrentGearDisplayVisor = true;

		public Vector2 DyePickerDyeSize = new(30);
		public bool DyePickerKeepApplyOnNewItem = false;

		public bool WindowsHotkeysAllowAfterLoosingFocus = false;
		public bool WindowsHotkeysPasstoGame = false;

		// Help popup
		public bool CollapsibleIntroductionDisclaimer = true;
		public bool CollapsibleStarterTips = true;
		public bool CollapsibleGeneralInformation = true;
		public bool CollapsibleOtherTips = true;
		public bool CollapsibleKnownIssues = true;




		// gear browser remember
		public float IconSizeMult { get; set; } = 1.0f;
		public bool FadeIconsIfNotHiddingTooltip = false;
		public bool IconTooltipShowDev = false;
		public bool ShowImagesInBrowser = true;
		public bool filterCurrentJob = true;
		public bool filterCurrentJobStrict = false;
		public bool filterCurrentRace = true;
		public Vector2 filterEquipLevel = new(1, 90);
		public Vector2 filterItemLevel = new(1, 665);
		public byte? filterRarity = null;

		public Dictionary<InventoryCategory, bool> FilterInventoryCategory { get; set; } = new();
		public float FilterInventoryCategoryColumnDistribution { get; set; } = 1.5f;
		public int FilterInventoryCategoryColumnNumber { get; set; } = 1;
		public int FilterInventoryTypeColumnNumber { get; set; } = 1;
		public float GearBrowserSideBarSize { get; set; } = 300f;
		public bool GearBrowserSideBarHide = false;
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
		public bool FilterSourceCollapse = true;
		public bool FilterAdditionalCollapse = false;
		public bool FilterAdvancedCollapse = true;
		public bool FilterSortCollapse = true;

		public List<(InventoryItemOrder.OrderMethod Method, InventoryItemOrder.OrderDirection Direction)>? SortOrder = null;
		public Dictionary<string, List<(InventoryItemOrder.OrderMethod Method, InventoryItemOrder.OrderDirection Direction)>>? SavedSortOrders = null;


		// Penumbra

		public bool PenumbraUseModListCollection = false;
		public string PenumbraCollectionModList = "Dresser Mod List";
		public string PenumbraCollectionApply = "Dresser Apply";
		public string PenumbraCollectionTmp = "Dresser TMP";

		public int PenumbraDelayAfterModEnableBeforeApplyAppearance = 60;
		public int PenumbraDelayAfterApplyAppearanceBeforeModDisable = 500;
		public int PenumbraDelayAfterModDisableBeforeNextModLoop = 100;

		public List<InventoryItem> PenumbraModdedItems = new List<InventoryItem>();
		public List<(string Path, string Name)> PenumbraModsBlacklist = new();

		// inventory tool stuff
		public bool AutoSave { get; set; } = true;
		public float AutoSaveMinutes { get; set; } = 5f;

		public bool SaveBackgroundFilter { get; set; } = false;
		public string? ActiveBackgroundFilter { get; set; } = null;
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
