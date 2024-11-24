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
using System.Text.Json.Serialization;

using Dresser.Enums;
using Dresser.Interop.Hooks;

namespace Dresser {
	[Serializable]
	public class Configuration : IPluginConfiguration {
		public Configuration() {
		}
		public void Load() {
			LoadFilterInventoryCategory();
			LoadAdditionaltems();
			SortOrder = InventoryItemOrder.Defaults();
			MarkReloaded();
		}
		public int Version { get; set; } = 0;

		public bool Debug = false;
		public bool EnablePenumbraModding = false;
		public bool ForceStandaloneAppearanceApply = true;
		public bool DebugDisplayModedInTitleBar = false;

		public InventoryItemSet DisplayPlateItems { get; set; } = new();
		[Obsolete]
		public Dictionary<ushort, InventoryItemSet> PendingPlateItems { get; set; } = new();
		[JsonIgnore]
		public Dictionary<ushort, InventoryItemSet> PendingPlateItemsCurrentChar {
			get {
				if (PendingPlateItemsAllChar.TryGetValue(PluginServices.Context.LocalPlayerCharacterId, out var result) && result != null)
					return result;
				else {

#pragma warning disable CS0612 // Type or member is obsolete
					if (PendingPlateItems.Count != 0) {
						PendingPlateItemsAllChar[PluginServices.Context.LocalPlayerCharacterId] = PendingPlateItems;
						return PendingPlateItemsAllChar[PluginServices.Context.LocalPlayerCharacterId];
					}
#pragma warning restore CS0612 // Type or member is obsolete
					else {
						return new();
					}
				}
			} set {
				if(!PendingPlateItemsAllChar.ContainsKey(PluginServices.Context.LocalPlayerCharacterId)) {
					PendingPlateItemsAllChar.Add(PluginServices.Context.LocalPlayerCharacterId, new());
				}
				PendingPlateItemsAllChar[PluginServices.Context.LocalPlayerCharacterId] = value;
			}
		}
		[JsonIgnore]
		public InventoryItemSet PendingPlateCurrentCharPlate { get {
				if (PendingPlateItemsCurrentChar.TryGetValue(this.SelectedCurrentPlate, out var result))
					return result;
				else
					return new();
			} set {
				PendingPlateItemsCurrentChar[this.SelectedCurrentPlate] = value;
			}
		}
		public Dictionary<ulong, Dictionary<ushort, InventoryItemSet>> PendingPlateItemsAllChar = new();
		public ushort NumberOfFreePendingPlates { get; set; } = 19;
		public ushort NumberofPendingPlateNextColumn { get; set; } = 20;
		public ushort SelectedCurrentPlate { get; set; } = 0;
		public bool CurrentGearPortablePlateJobIcons = true;
		public bool SelectCurrentGearsetOnOpenCurrentGearWindow = false;
		public bool CurrentGearPortablePlateJobBgColors = false;
		public GlamourPlateSlot CurrentGearSelectedSlot = GlamourPlateSlot.Body;

		public bool OfferApplyAllPlatesOnDresserOpen = false;
		public bool OfferOverwritePendingPlatesAfterApplyAll = false;
		public BehaviorOnOpen BehaviorOnOpen = BehaviorOnOpen.SandboxPlateWithWearingGlam;


		public bool CurrentGearDisplayGear = false;
		public bool CurrentGearDisplayHat = true;
		public bool CurrentGearDisplayWeapon = true;
		public bool CurrentGearDisplayVisor = true;

		public Vector2 DyePickerDyeSize = new(30);
		public bool DyePickerKeepApplyOnNewItem = false;

		public bool WindowsHotkeysAllowAfterLoosingFocus = false;
		public bool WindowsHotkeysPasstoGame = false;
		public bool GearBrowserSourceHideEmpty = true;

		// Help popup
		public bool CollapsibleIntroductionDisclaimer = true;
		public bool CollapsibleStarterTips = true;
		public bool CollapsibleGeneralInformation = true;
		public bool CollapsibleOtherTips = true;
		public bool CollapsibleKnownIssues = true;

		// ---- Colors ----
		// frames
		public Vector4 CollectionColorBackground = new Vector4(113, 98, 119, 200) / 255;
		public Vector4 CollectionColorBorder = (new Vector4(116, 123, 98, 255) / 255 * 0.4f) + new Vector4(0, 0, 0, 1);
		public Vector4 CollectionColorScrollbar = (new Vector4(116, 123, 98, 255) / 255 * 0.2f) + new Vector4(0, 0, 0, 1);
		public Vector4 ColorIconImageTintDisabled = new(1, 1, 1, 0.5f);
		public Vector4 ColorIconImageTintEnabled = Vector4.One;

		// plate selector
		public Vector4 PlateSelectorRestColor = new(1, 1, 1, 0.70f);
		public Vector4 PlateSelectorHoverColor = new(1, 1, 1, 1);
		public Vector4 PlateSelectorActiveColor = new(1, 0.95f, 0.8f, 1);
		public Vector4 PlateSelectorColorTitle = (new Vector4(116, 123, 98, 255) / 255 * 0.3f) + new Vector4(0, 0, 0, 1);
		public Vector4 PlateSelectorColorRadio = ((new Vector4(116, 123, 98, 255) / 255 * 0.3f) * new Vector4(1, 1, 1, 0)) + new Vector4(0, 0, 0, 0.70f);

		// text and icons
		public Vector4 ColorGood                = new Vector4(124, 236, 56, 255) / 255;
		public Vector4 ColorGoodLight           = new Vector4(180, 244, 170, 255) / 255;
		public Vector4 ColorBad                 = new Vector4(237, 107, 89, 255) / 255;
		public Vector4 ColorGrey                = new Vector4(199, 198, 197, 255) / 255;
		public Vector4 ColorGreyDark            = new Vector4(199, 198, 197, 255) / 255 / 1.1f;
		public Vector4 ColorBronze              = new Vector4(240, 223, 191, 255) / 255;
		public Vector4 ModdedItemWatermarkColor = new Vector4(240, 161, 223, 15) / 255;
		public Vector4 ModdedItemColor          = new Vector4(223, 101, 240, 255) / 255;

		// dye picker
		public Vector4 DyePickerHighlightSelection    = new Vector4(240, 161, 223, 15) / 255;
		public Vector4 DyePickerDye1Or2SelectedBg     = new Vector4(1f, 0.8f, 0.45f, 1f);
		public Vector4 DyePickerDye1Or2SelectedColor  = new Vector4(0f, 0f, 0f, 1f);

		// browser
		public Vector4 BrowserVerticalTabButtonsBg     = new Vector4(47, 53, 75, 255) / 255;


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
		public float GearBrowserSideBarSizePercent { get; set; } = 0.33f;
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
		public bool PenumbraDisableModRightAfterApply = false;
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
