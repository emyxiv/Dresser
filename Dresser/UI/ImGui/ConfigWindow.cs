using CriticalCommonLib.Enums;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;

using Dresser.Core;
using Dresser.Enums;
using Dresser.Extensions;
using Dresser.Interop.Agents;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Services.Ipc;
using Dresser.Gui.Components;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Dresser.Gui;

public class ConfigWindow : Window, IDisposable {

	public ConfigWindow(Plugin plugin) : base(
		"Dresser Settings",
		ImGuiWindowFlags.None) {
		this.Size = new Vector2(232, 75);
		this.SizeCondition = ImGuiCond.FirstUseEver;

		Tabs = new Dictionary<string, Dictionary<string, Action>>() {
			{"Dependencies",new () {
				{ "Required Plugins", RequiredPlugins },
				{ "Optional Plugins", OptionalPlugins },
			}},
			{"General",new () {
				{ "Portable Plates", DrawPlatesConfig },
				{ "Behaviors", DrawBehaviourConfigs },
				{ "Item Filters", DrawItemFiltersConfigs },
				{ "Item Tooltips", DrawItemTooltipsConfigs },
				{ "Icons", DrawIconsConfigs },
			}},
			{"Style",new () {
				{ "Windows & sizing", DrawWindowsAndSizingConfigs },
				{ "Colors", DrawColorStyleConfig },
			}},
			{"Mod Browser",new () {
				{ "Penumbra", DrawPenumbraConfigs },
			}},
			{"About",new () {
				{ "Dresser", DrawAboutConfigs },
			}},
		};

		AdjustDynamicSections();
	}

	private Dictionary<string, Dictionary<string, Action>> Tabs;

	public void Dispose() { }

	private void AdjustDynamicSections() {
		if (ConfigurationManager.Config.Debug && !Tabs.ContainsKey("Debug")) {
			Tabs.Add("Debug", new Dictionary<string, Action> {
				{ "Debug", DrawDebugSection},
			});
		} else if (!ConfigurationManager.Config.Debug && Tabs.ContainsKey("Debug")) {
			Tabs.Remove("Debug");
		}

		if (ConfigurationManager.Config.EnablePenumbraModding && !Tabs.ContainsKey("Mod Browser")) {
			Tabs.Add("Mod Browser", new Dictionary<string, Action> {
				{ "Penumbra", DrawPenumbraConfigs}
			});
		} else if (!ConfigurationManager.Config.EnablePenumbraModding && Tabs.ContainsKey("Mod Browser")) {
			Tabs.Remove("Mod Browser");
		}

	}

	public override void Draw() {
		if (!ImGui.BeginTabBar("##ConfigWindowTabs")) return;

		AdjustDynamicSections();
		foreach ((var tabName, var sections) in Tabs) {
			if (!ImGui.BeginTabItem(tabName)) continue;
			if (!ImGui.BeginChild($"##{tabName}##ConfigWindowTabs")) { ImGui.EndTabItem(); continue; }
			DrawTabContents(sections);
			ImGui.EndChild();
			ImGui.EndTabItem();
		}

		ImGui.EndTabBar();
	}
	private void DrawTabContents(Dictionary<string, Action> tabContents) {
		var fontSize = ImGui.GetFontSize();
		var textColor = ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
		var draw = ImGui.GetWindowDrawList();

		foreach ((var title, var contents) in tabContents) {

			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fontSize * 0.5f);

			GuiHelpers.TextWithFont(title, GuiHelpers.Font.Config);
			var start = ImGui.GetCursorScreenPos();
			//start += new Vector2(0, fontSize * 0.1f);
			var titelRectSize = ImGui.GetItemRectSize();
			var end = start + new Vector2(titelRectSize.X, 0);
			draw.AddLine(start, end, textColor, fontSize * 0.15f);
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fontSize * 1.5f);

			contents();
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fontSize * 1.5f);

		}


	}

	private void DrawBehaviourConfigs() {
		var behavioursOnOpen = Enum.GetValues<BehaviorOnOpen>();
		var currentIndexBehavioursOnOpen = Array.IndexOf(behavioursOnOpen, ConfigurationManager.Config.BehaviorOnOpen);
		if(currentIndexBehavioursOnOpen < 0) currentIndexBehavioursOnOpen = 0;
		if (ImGui.Combo("Behavior on open##Behaviours##Behaviours##Config", ref currentIndexBehavioursOnOpen, behavioursOnOpen.Select(i => i.ToString().AddSpaceBeforeCapital()).ToArray(), behavioursOnOpen.Length)) {
			ConfigurationManager.Config.BehaviorOnOpen = behavioursOnOpen[currentIndexBehavioursOnOpen];
		}

		ImGui.Checkbox($"Select plate linked to current gearset on open##Behaviours##Config", ref ConfigurationManager.Config.SelectCurrentGearsetOnOpenCurrentGearWindow);
		ImGui.Checkbox($"Hide empty owned item types##Behaviours##Config", ref ConfigurationManager.Config.GearBrowserSourceHideEmpty);
		ImGui.Checkbox($"(Experimental) Hotkeys after loosing window focus##Behaviours##Config", ref ConfigurationManager.Config.WindowsHotkeysAllowAfterLoosingFocus);
		GuiHelpers.Tooltip("For example, when loosing Gear Browser focus, the directional hotkeys will continue working until another window is focused or dresser is closed");

		if(ImGui.Checkbox($"(Experimental) Pass Hotkeys to the game through window##Behaviours##Config", ref ConfigurationManager.Config.WindowsHotkeysPasstoGame))
			HotkeySetup.Init();

		ImGui.Checkbox($"Enable Debug##Behaviours##Config", ref ConfigurationManager.Config.Debug);


		ImGui.Checkbox($"Offer Apply All plates On Dresser Open##Behaviours##Config", ref ConfigurationManager.Config.OfferApplyAllPlatesOnDresserOpen);
		ImGui.Checkbox($"Offer Overwrite Pending Plates After Apply All##Behaviours##Config", ref ConfigurationManager.Config.OfferOverwritePendingPlatesAfterApplyAll);


	}
	private void DrawItemFiltersConfigs() {
		var filterChanged = false;
		filterChanged |= ImGui.Checkbox($"Always Remove Ornate items from Gear Browser##displayCategory##GearBrowserConfig", ref ConfigurationManager.Config.filterOrnateObtained);

		//ImGui.SameLine();
		if (ImGui.Button("Open tag manager##ItemFilters##Config")) {
			Plugin.GetInstance().TagManager.IsOpen = !Plugin.GetInstance().TagManager.IsOpen;
		}
		GuiHelpers.Tooltip("Rename tags, assign tags to a slot, delete tags, etc.");


		if (filterChanged) GearBrowser.RecomputeItems();
	}
	private void DrawItemTooltipsConfigs() {
		ImGui.Checkbox($"Show item's sources##displayCategory##GearBrowserConfig", ref ConfigurationManager.Config.ShowItemTooltipsSources);
		if (!ConfigurationManager.Config.ShowItemTooltipsSources) ImGui.BeginDisabled();
		ImGui.Checkbox($"Only show item's sources when the item is not obtained##displayCategory##GearBrowserConfig", ref ConfigurationManager.Config.ShowItemTooltipsSourcesNotObtained);
		if (!ConfigurationManager.Config.ShowItemTooltipsSources) ImGui.EndDisabled();
	}
	private void DrawIconsConfigs() {

		ImGui.Checkbox($"Show items icons##displayCategory##GearBrowserConfig", ref ConfigurationManager.Config.ShowImagesInBrowser);
		ImGui.Checkbox($"Fade unavailable items when hidding tooltips (Hold Alt)##Images##GearBrowserConfig", ref ConfigurationManager.Config.FadeIconsIfNotHiddingTooltip);

	}

	private void DrawWindowsAndSizingConfigs() {

		//ImGui.SetNextItemWidth(ImGui.GetFontSize() * 3);
		var iconSizeMult = ConfigurationManager.Config.IconSizeMult;
		if (ImGui.DragFloat("Icon Size##IconSize##slider", ref iconSizeMult, 0.001f, 0.001f, 4f, "%.3f", ImGuiSliderFlags.AlwaysClamp)) {
			ConfigurationManager.Config.IconSizeMult = iconSizeMult;
			ConfigurationManager.SaveAsync();
		}

		var dyeSize = ConfigurationManager.Config.DyePickerDyeSize.X;
		if (ImGui.DragFloat("dye size", ref dyeSize, 0.1f, 1f, 100f,"%.2f",ImGuiSliderFlags.AlwaysClamp)) {
			ConfigurationManager.Config.DyePickerDyeSize = new Vector2(dyeSize);
		}

		ImGui.Spacing();
		var GearBrowserDisplayMode = (int)ConfigurationManager.Config.GearBrowserDisplayMode;
		var GearBrowserDisplayMode_items = Enum.GetValues<GearBrowser.DisplayMode>().Select(d => d.ToString()).ToArray();
		if (ImGui.Combo("Display mode##GearBrowserConfig", ref GearBrowserDisplayMode, GearBrowserDisplayMode_items, GearBrowserDisplayMode_items.Count())) {
			ConfigurationManager.Config.GearBrowserDisplayMode = (GearBrowser.DisplayMode)GearBrowserDisplayMode;
		}
		var GearBrowserSideBarSizePercent = ConfigurationManager.Config.GearBrowserSideBarSizePercent * 100f;
		if (ImGui.DragFloat("size - Sidebar##GearBrowserConfig", ref GearBrowserSideBarSizePercent, 0.1f, 0.01f, 100f,"%.1f %%", ImGuiSliderFlags.AlwaysClamp))
			ConfigurationManager.Config.GearBrowserSideBarSizePercent = float.Clamp(GearBrowserSideBarSizePercent / 100f,0.001f, 1f);
		//var FilterInventoryCategoryColumnDistribution = ConfigurationManager.Config.FilterInventoryCategoryColumnDistribution;
		//if(ImGui.DragFloat("Source - column distribution##GearBrowserConfig", ref FilterInventoryCategoryColumnDistribution, 0.005f, -5f, 15f))
		//	ConfigurationManager.Config.FilterInventoryCategoryColumnDistribution = FilterInventoryCategoryColumnDistribution;
		var FilterInventoryCategoryColumnNumber = ConfigurationManager.Config.FilterInventoryCategoryColumnNumber;
		if (ImGui.DragInt("Col # - Source##GearBrowserConfig", ref FilterInventoryCategoryColumnNumber, 0.05f, 1, 5, "%.0f", ImGuiSliderFlags.AlwaysClamp))
		ConfigurationManager.Config.FilterInventoryCategoryColumnNumber = FilterInventoryCategoryColumnNumber;
		var FilterInventoryTypeColumnNumber = ConfigurationManager.Config.FilterInventoryTypeColumnNumber;
		if (ImGui.DragInt("Col # - Unobtained##GearBrowserConfig", ref FilterInventoryTypeColumnNumber, 0.05f, 1, 5, "%.0f", ImGuiSliderFlags.AlwaysClamp))
		ConfigurationManager.Config.FilterInventoryTypeColumnNumber = FilterInventoryTypeColumnNumber;

	}

	private void DrawPlatesConfig() {
		var hardMaxFreePlate = 60;

		int numFreePlates = ConfigurationManager.Config.NumberOfFreePendingPlates;
		ImGui.SetNextItemWidth(ImGui.GetFontSize() * 2);
		if (ImGui.DragInt("Number of free portable plates",ref numFreePlates,1,0,hardMaxFreePlate, "%.0f", ImGuiSliderFlags.AlwaysClamp)) {
			if (numFreePlates > hardMaxFreePlate) numFreePlates = hardMaxFreePlate;
			if (numFreePlates < 0) numFreePlates = 0;
			ConfigurationManager.Config.NumberOfFreePendingPlates = (ushort)numFreePlates;
		}
		GuiHelpers.Tooltip("These are plates detached to vanilla's plates.\nTheir purpose is to try and mess around with glamours, or be used as temporary store.");
		int breakAtPlateButton = ConfigurationManager.Config.NumberofPendingPlateNextColumn;
		ImGui.SetNextItemWidth(ImGui.GetFontSize() * 2);
		if (ImGui.DragInt("Plate selection column length",ref breakAtPlateButton,1,1, hardMaxFreePlate, "%.0f", ImGuiSliderFlags.AlwaysClamp)) {
			if (breakAtPlateButton > hardMaxFreePlate) breakAtPlateButton = hardMaxFreePlate;
			if (breakAtPlateButton < 1) breakAtPlateButton = 1;

			ConfigurationManager.Config.NumberofPendingPlateNextColumn = (ushort)breakAtPlateButton;
		}
		GuiHelpers.Tooltip("Plate buttons column will break after this plate number");

		ImGui.Checkbox($"Show job icons on plate buttons", ref ConfigurationManager.Config.CurrentGearPortablePlateJobIcons);
		ImGui.Checkbox($"Show job background colors on plate buttons", ref ConfigurationManager.Config.CurrentGearPortablePlateJobBgColors);

		if (!MiragePlateAgent.IsAnyPlateSelectionOpen())
			ImGui.BeginDisabled();
		var posBefore = ImGui.GetCursorPos();
		if (ImGui.Button("Re-import Glamour Plates and erase pending plates"))
			PluginServices.ApplyGearChange.OverwritePendingWithActualPlates();
		var size = ImGui.GetItemRectSize();
		if (!MiragePlateAgent.IsAnyPlateSelectionOpen()) {
			ImGui.EndDisabled();
			ImGui.SetCursorPos(posBefore);
			ImGui.InvisibleButton($"reimportDisabledTooltim", size);
			GuiHelpers.Tooltip("The Glamour Dresser or Plate Selection must be opened to copy the plates");
		}

	}

	private void RequiredPlugins() {
		if (PluginServices.Context.AllaganToolsState) {
			GuiHelpers.Icon(FontAwesomeIcon.CheckCircle, true, ItemIcon.ColorGood);
			ImGui.SameLine();
			ImGui.TextColored(ItemIcon.ColorGood, "Allagan Tools connected");
			//PluginServices.AllaganTools.CheckMethodAvailability();
		} else {
			GuiHelpers.Icon(FontAwesomeIcon.ExclamationTriangle, true, ItemIcon.ColorBad);
			ImGui.SameLine();
			ImGui.TextColored(ItemIcon.ColorBad, "Allagan Tools not found");
		}
		ImGui.TextWrapped("Allagan Tools plugin is required to get the record of items in other inventories.");

		if (PluginServices.Context.GlamourerState) {
			ImGui.Spacing();

			var glamourerVersions = PluginServices.Glamourer.ApiVersions();
			GuiHelpers.Icon(FontAwesomeIcon.CheckCircle, true, ItemIcon.ColorGood);
			ImGui.SameLine();
			ImGui.TextColored(ItemIcon.ColorGood, $"Glamourer API connected (Version {glamourerVersions.Major}.{glamourerVersions.Minor})");

		} else {
			GuiHelpers.Icon(FontAwesomeIcon.ExclamationTriangle, true, ItemIcon.ColorBad);
			ImGui.SameLine();
			ImGui.TextColored(ItemIcon.ColorBad, "Glamourer is not found");

		}
		ImGui.TextWrapped("Glamourer is required to display glamours directly on your character.");
	}
	private void OptionalPlugins() {
/*
		if (PluginServices.Context.GlamourerState) {
			var glamourerVersions = PluginServices.Glamourer.ApiVersions();
			GuiHelpers.Icon(Dalamud.Interface.FontAwesomeIcon.CheckCircle, true, ItemIcon.ColorGood);
			ImGui.SameLine();
			ImGui.TextColored(ItemIcon.ColorGood, $"Glamourer API connected (Version {glamourerVersions.Major}.{glamourerVersions.Minor})");

			// reverse ForceStandaloneAppearanceApply
			var useGlamourerToApplyAppearance = !ConfigurationManager.Config.ForceStandaloneAppearanceApply;
			ImGui.Checkbox($"Enable Glamourer compatibility##OptionalPlugins{(ConfigurationManager.Config.EnablePenumbraModding?" (disabled for mods)":"")}##ConfigWindow", ref useGlamourerToApplyAppearance);
			GuiHelpers.Tooltip(() => {
				ImGui.TextWrapped("This will ask Glamourer to apply item's appearance when possible");
				if (PluginServices.Context.PenumbraState) {
					ImGui.Bullet(); ImGui.SameLine();
					ImGui.TextWrapped("Modded items will not be applied with Glamourer");
					ImGui.Bullet(); ImGui.SameLine();
					ImGui.TextWrapped("Will show the changes to users synchronized with Mare");
				}
			});
			ConfigurationManager.Config.ForceStandaloneAppearanceApply = !useGlamourerToApplyAppearance;

		}
*/
		if (PluginServices.Context.PenumbraState) {
			var penumbraVersions = PluginServices.Penumbra.ApiVersions();
			GuiHelpers.Icon(FontAwesomeIcon.CheckCircle, true, ItemIcon.ColorGood);
			ImGui.SameLine();
			ImGui.TextColored(ItemIcon.ColorGood, $"Penumbra API connected (Version: Breaking {penumbraVersions.Breaking}, Feature {penumbraVersions.Features})");

			ImGui.Checkbox($"Enable mod browsing with penumbra (experimental)##OptionalPlugins##ConfigWindow", ref ConfigurationManager.Config.EnablePenumbraModding);
		}

		var isItemVendorLocationEnabled = PluginServices.ItemVendorLocation.IsInitialized();
		if(isItemVendorLocationEnabled) GuiHelpers.Icon(FontAwesomeIcon.CheckCircle, true, ItemIcon.ColorGood);
		else                            GuiHelpers.Icon(FontAwesomeIcon.TimesCircle, true, ItemIcon.ColorBad);
		ImGui.SameLine();
		ImGui.TextColored(isItemVendorLocationEnabled ? ItemIcon.ColorGood : ItemIcon.ColorBad, "Item Vendor Location");
		ImGui.TextWrapped("Open vendor location from items context menu.");
	}

	private void DrawPenumbraConfigs() {

		if (ImGui.CollapsingHeader("Info##DrawPenumbraConfigs")) {
			ImGui.TextWrapped($"This mod browsing feature is experimental.");
			ImGui.TextWrapped($"There are a few quirks.");

			ImGui.Bullet(); ImGui.SameLine(); ImGui.TextWrapped($"Dresser will scan through Penumbra for all the mods that change items, see below in \"Scan\". When the scan finds an item, it will add it to the Unobtained category of item \"Modded Items\"");
			ImGui.Bullet(); ImGui.SameLine(); ImGui.TextWrapped($"Blacklisted mods or mod paths will be ignored during the scan. After modifying the blacklists, it is recommended to execute \"Reload modded items\" in \"Scan\"");
			ImGui.Bullet(); ImGui.SameLine(); ImGui.TextWrapped($"The application of mods is made through temporary sate, similar to Glamourer's \"Use Temporary Mod Settings\".");
			ImGui.Bullet(); ImGui.SameLine(); ImGui.TextWrapped($"When browsing items, changing from one to another will disable the previous and enable the next very quickly, these transitions may look odd.");
			ImGui.Bullet(); ImGui.SameLine(); ImGui.TextWrapped($"It might synchronize with friends through penumbra/glamourer, but the transition speed may upset the sync.");
			ImGui.Bullet(); ImGui.SameLine(); ImGui.TextWrapped($"Mix and matching with 2 mods affecting the same items will result in conflict, one of the items will not be displayed correctly.");
			ImGui.Bullet(); ImGui.SameLine(); ImGui.TextWrapped($"If a mod replaces the icon of the affected gear item, Dresser will try replace the icon even if the mod is disabled (Experimental)");

		}
		ImGui.Spacing();

		if (ImGui.CollapsingHeader("Collections & Behaviors##DrawPenumbraConfigs")) {
			ImGui.Checkbox($"Use collection Mod List##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraUseModListCollection);
			GuiHelpers.Tooltip($"If checked, only mods enabled on [{ConfigurationManager.Config.PenumbraCollectionModList}] will be scanned\nIf unchecked, ALL installed mods will be scanned for changed items");

			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
			if (!ConfigurationManager.Config.PenumbraUseModListCollection) ImGui.BeginDisabled();
			ImGui.InputText($"Mod List Penumbra collection##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraCollectionModList, 100);
			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.FileImport, "Get the collection currently selected in Penumbra", default, "Load Mod List from Penumbra currentcollection##PenumbraConfig##ConfigWindow")) {
				var col = PluginServices.Penumbra.GetCurrentCollectionName();
				if (col != null) {
					ConfigurationManager.Config.PenumbraCollectionModList = col;
				}
			}
			if (!ConfigurationManager.Config.PenumbraUseModListCollection) ImGui.EndDisabled();
			GuiHelpers.Tooltip($"When enabled, the mods of this collection will be scanned.\nMods can be enabled regardless of conflicts, as mods sharing the same items can be displayed at the same time");
		}



		ImGui.Spacing();
		if (ImGui.CollapsingHeader("Scan##DrawPenumbraConfigs", ImGuiTreeNodeFlags.DefaultOpen)) {

			ImGui.Text($"{ConfigurationManager.Config.PenumbraModdedItems.Count} modded items in Config, {PluginServices.Storage.AdditionalItems[(InventoryType)Storage.InventoryTypeExtra.ModdedItems].Count} in memory");

			if (PluginServices.Storage.IsReloadingMods) ImGui.BeginDisabled();
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
			if (ImGui.Button($"Reload modded items {(PluginServices.Storage.IsReloadingMods ? $" ({PluginServices.Storage.ModsReloadingCur}/{PluginServices.Storage.ModsReloadingMax})" : "")}##PenumbraConfig##ConfigWindow")) {
				PluginServices.Storage.ReloadMods();
			}
			ImGui.SameLine();
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
			if (ImGui.Button($"Clear modded items##PenumbraConfig##ConfigWindow")) {
				PluginServices.Storage.ClearMods();
				GearBrowser.RecomputeItems();
			}
			if (PluginServices.Storage.IsReloadingMods) ImGui.EndDisabled();
		}
		ImGui.Spacing();
		if (ImGui.CollapsingHeader("Individual Mod Blacklist##DrawPenumbraConfigs", ImGuiTreeNodeFlags.DefaultOpen)) {

			ImGui.AlignTextToFramePadding();
			ImGui.Text("Blacklisted mods");
			GuiHelpers.Tooltip($"Mods listed here will be ignored when creating the list of modded items");
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.Plus, "PlusButton##AddToBlackList##PenumbraConfig##ConfigWindow", ""))
				ModsBlackListSearchOpen = true;
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.FileExport, "ExportButton##AddToBlackList##PenumbraConfig##ConfigWindow", "Export list to Clipboard as JSON"))
				JsonConvert.SerializeObject(ConfigurationManager.Config.PenumbraModsBlacklist).ToClipboard();
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.FileImport, "ImportButton##AddToBlackList##PenumbraConfig##ConfigWindow", "Import list from JSON Clipboard")) {
				try {
					var decodedBlacklist = JsonConvert.DeserializeObject<List<(string Path, string Name)>>(ImGui.GetClipboardText());
					if (decodedBlacklist != null) ConfigurationManager.Config.PenumbraModsBlacklist = ConfigurationManager.Config.PenumbraModsBlacklist.Concat(decodedBlacklist).ToList();
				} catch (Exception) { }
			}
			ImGui.SameLine();
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, "Empty blacklist\nHold ctrl + Shift to to confirm", default, "TrashButton##AddToBlackList##PenumbraConfig##ConfigWindow"))
				ConfigurationManager.Config.PenumbraModsBlacklist.Clear();

			if (ImGui.BeginChildFrame(411141, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 5))) {
				for (int i = ConfigurationManager.Config.PenumbraModsBlacklist.Count - 1; i >= 0; i--) {
					var mod = ConfigurationManager.Config.PenumbraModsBlacklist[i];
					if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.Trash, $"{mod.Path}##TrashButton##AddToBlackList##PenumbraConfig##ConfigWindow", "Remove from blacklist")) {
						ConfigurationManager.Config.PenumbraModsBlacklist.RemoveAt(i);
					}
					ImGui.SameLine();
					ImGui.Text($"{mod.Name}");
					GuiHelpers.Tooltip($"{mod.Path}");

				}


				ImGui.EndChildFrame();
			}

		}


		// put that at the end
		DrawModBlacklistSelector();
		DrawModPathBlacklistSection();
		DrawModPathWhitelistSection();
		DrawModItemBlacklistSection();
	}

	private static List<string>? ModPathsAvailableToBlacklist = null;
	private static string ModPathsBlackListSearch = "";
	private static bool ModPathsBlackListSearchOpen = false;
	private static string ModPathsBlackListTextInput = "";

	private static List<string>? ModPathsAvailableForWhitelist = null;
	private static string ModPathsWhiteListTextInput = "";

	private void DrawModPathBlacklistSection() {
		ImGui.Spacing();
		if (ImGui.CollapsingHeader("Blacklist by Path##DrawPenumbraConfigs")) {
			ImGui.TextWrapped("Blacklist mods by their folder path (case-insensitive). For example, \"main1\" will blacklist \"main1/sub1/mod1\", \"main1/sub2/mod2\", etc.");
			ImGui.Spacing();

			ImGui.AlignTextToFramePadding();
			ImGui.Text("Blacklisted paths");
			GuiHelpers.Tooltip($"Mods whose path starts with any of these will be ignored when creating the list of modded items");
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.FileExport, "ExportButton##AddToPathBlackList##PenumbraConfig##ConfigWindow", "Export list to Clipboard as JSON"))
				JsonConvert.SerializeObject(ConfigurationManager.Config.PenumbraModsBlacklistByPath).ToClipboard();
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.FileImport, "ImportButton##AddToPathBlackList##PenumbraConfig##ConfigWindow", "Import list from JSON Clipboard")) {
				try {
					var decodedBlacklist = JsonConvert.DeserializeObject<List<string>>(ImGui.GetClipboardText());
					if (decodedBlacklist != null) {
						foreach (var path in decodedBlacklist) {
							if (!ConfigurationManager.Config.PenumbraModsBlacklistByPath.Contains(path)) {
								ConfigurationManager.Config.PenumbraModsBlacklistByPath.Add(path);
							}
						}
					}
				} catch (Exception) { }
			}
			ImGui.SameLine();
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, "Empty blacklist\nHold ctrl + Shift to confirm", default, "TrashButton##AddToPathBlackList##PenumbraConfig##ConfigWindow"))
				ConfigurationManager.Config.PenumbraModsBlacklistByPath.Clear();

			if (ImGui.BeginChildFrame(411142, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 5))) {
				for (int i = ConfigurationManager.Config.PenumbraModsBlacklistByPath.Count - 1; i >= 0; i--) {
					var path = ConfigurationManager.Config.PenumbraModsBlacklistByPath[i];
					if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.Trash, $"{path}##TrashButton##AddToPathBlackList##PenumbraConfig##ConfigWindow", "Remove from blacklist")) {
						ConfigurationManager.Config.PenumbraModsBlacklistByPath.RemoveAt(i);
						if (Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
					}
					ImGui.SameLine();
					ImGui.TextUnformatted(path);
				}

				ImGui.EndChildFrame();
			}

			DrawModPathBlacklistInput();
		}
	}

	private void DrawModPathBlacklistInput() {
		// Get all available mod paths once
		ModPathsAvailableToBlacklist ??= PluginServices.Penumbra.GetMods()
			.SelectMany(mod => {
				var path = PluginServices.Penumbra.GetModPathCacheCached(mod.Path);
				if (string.IsNullOrEmpty(path)) return Enumerable.Empty<string>();
				return Enumerable.Repeat(path, 1);
			})
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
			.ToList();

		ImGui.Spacing();
		ImGui.Text("Add new path to blacklist");

		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		if (ImGui.InputText("##PathBlacklistInput", ref ModPathsBlackListTextInput, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
			// Enter key pressed - add the path
			if (!string.IsNullOrWhiteSpace(ModPathsBlackListTextInput)) {
				var pathToAdd = ModPathsBlackListTextInput.Trim();
				if (!ConfigurationManager.Config.PenumbraModsBlacklistByPath.Contains(pathToAdd, StringComparer.OrdinalIgnoreCase)) {
					ConfigurationManager.Config.PenumbraModsBlacklistByPath.Add(pathToAdd);
					ModPathsBlackListTextInput = "";
					if (Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
				}
			}
		}

		ImGui.SameLine();
		if (ImGui.Button("Add##PathBlacklistAdd")) {
			if (!string.IsNullOrWhiteSpace(ModPathsBlackListTextInput)) {
				var pathToAdd = ModPathsBlackListTextInput.Trim();
				if (!ConfigurationManager.Config.PenumbraModsBlacklistByPath.Contains(pathToAdd, StringComparer.OrdinalIgnoreCase)) {
					ConfigurationManager.Config.PenumbraModsBlacklistByPath.Add(pathToAdd);
					ModPathsBlackListTextInput = "";
					if (Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
				}
			}
		}

		// Live preview
		if (!string.IsNullOrWhiteSpace(ModPathsBlackListTextInput)) {
			ImGui.Spacing();
			ImGui.TextDisabled("Blacklisted mods preview from input:");
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			if (ImGui.BeginChildFrame(411143, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 4))) {
				var matchingPaths = ModPathsAvailableToBlacklist
					.Where(modPath => PluginServices.Penumbra.PathMatchesBlacklistPattern(modPath, ModPathsBlackListTextInput))
					.ToList();

				if (matchingPaths.Count == 0) {
					ImGui.TextDisabled("(no mods match this pattern)");
				} else {
					foreach (var modPath in matchingPaths) {
						ImGui.TextUnformatted(modPath);
					}
				}

				ImGui.EndChildFrame();
			}
		}
	}

	private void DrawModPathWhitelistSection() {
		ImGui.Spacing();
		if (ImGui.CollapsingHeader("Whitelist by Path##DrawPenumbraConfigs")) {
			ImGui.TextWrapped("Whitelist mods by their folder path (case-insensitive). When a whitelist is active, ONLY mods matching these patterns will be used. For example, \"main1\" will only allow \"main1/sub1/mod1\", \"main1/sub2/mod2\", etc.");
			ImGui.Spacing();

			ImGui.AlignTextToFramePadding();
			ImGui.Text("Whitelisted paths");
			GuiHelpers.Tooltip($"Only mods whose path starts with any of these will be used when creating the list of modded items. The blacklist is ignored when a whitelist is active.");
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.FileExport, "ExportButton##AddToPathWhitelist##PenumbraConfig##ConfigWindow", "Export list to Clipboard as JSON"))
				JsonConvert.SerializeObject(ConfigurationManager.Config.PenumbraModsWhitelistByPath).ToClipboard();
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.FileImport, "ImportButton##AddToPathWhitelist##PenumbraConfig##ConfigWindow", "Import list from JSON Clipboard")) {
				try {
					var decodedWhitelist = JsonConvert.DeserializeObject<List<string>>(ImGui.GetClipboardText());
					if (decodedWhitelist != null) {
						foreach (var path in decodedWhitelist) {
							if (!ConfigurationManager.Config.PenumbraModsWhitelistByPath.Contains(path)) {
								ConfigurationManager.Config.PenumbraModsWhitelistByPath.Add(path);
							}
						}
					}
				} catch (Exception) { }
			}
			ImGui.SameLine();
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, "Clear whitelist\nHold ctrl + Shift to confirm", default, "TrashButton##AddToPathWhitelist##PenumbraConfig##ConfigWindow"))
				ConfigurationManager.Config.PenumbraModsWhitelistByPath.Clear();

			if (ImGui.BeginChildFrame(411144, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 5))) {
				for (int i = ConfigurationManager.Config.PenumbraModsWhitelistByPath.Count - 1; i >= 0; i--) {
					var path = ConfigurationManager.Config.PenumbraModsWhitelistByPath[i];
					if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.Trash, $"{path}##TrashButton##AddToPathWhitelist##PenumbraConfig##ConfigWindow", "Remove from whitelist")) {
						ConfigurationManager.Config.PenumbraModsWhitelistByPath.RemoveAt(i);
						if (Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
					}
					ImGui.SameLine();
					ImGui.TextUnformatted(path);
				}

				ImGui.EndChildFrame();
			}

			DrawModPathWhitelistInput();
		}
	}

	private void DrawModPathWhitelistInput() {
		// Get all available mod paths once
		ModPathsAvailableForWhitelist ??= PluginServices.Penumbra.GetMods()
			.SelectMany(mod => {
				var path = PluginServices.Penumbra.GetModPathCacheCached(mod.Path);
				if (string.IsNullOrEmpty(path)) return Enumerable.Empty<string>();
				return Enumerable.Repeat(path, 1);
			})
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
			.ToList();

		ImGui.Spacing();
		ImGui.Text("Add new path to whitelist");

		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		if (ImGui.InputText("##PathWhitelistInput", ref ModPathsWhiteListTextInput, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
			// Enter key pressed - add the path
			if (!string.IsNullOrWhiteSpace(ModPathsWhiteListTextInput)) {
				var pathToAdd = ModPathsWhiteListTextInput.Trim();
				if (!ConfigurationManager.Config.PenumbraModsWhitelistByPath.Contains(pathToAdd, StringComparer.OrdinalIgnoreCase)) {
					ConfigurationManager.Config.PenumbraModsWhitelistByPath.Add(pathToAdd);
					ModPathsWhiteListTextInput = "";
					if (Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
				}
			}
		}

		ImGui.SameLine();
		if (ImGui.Button("Add##PathWhitelistAdd")) {
			if (!string.IsNullOrWhiteSpace(ModPathsWhiteListTextInput)) {
				var pathToAdd = ModPathsWhiteListTextInput.Trim();
				if (!ConfigurationManager.Config.PenumbraModsWhitelistByPath.Contains(pathToAdd, StringComparer.OrdinalIgnoreCase)) {
					ConfigurationManager.Config.PenumbraModsWhitelistByPath.Add(pathToAdd);
					ModPathsWhiteListTextInput = "";
					if (Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
				}
			}
		}

		// Live preview
		if (!string.IsNullOrWhiteSpace(ModPathsWhiteListTextInput)) {
			ImGui.Spacing();
			ImGui.TextDisabled("Whitelisted mods preview from input:");
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			if (ImGui.BeginChildFrame(411145, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 4))) {
				var matchingPaths = ModPathsAvailableForWhitelist
					.Where(modPath => PluginServices.Penumbra.PathMatchesBlacklistPattern(modPath, ModPathsWhiteListTextInput))
					.ToList();

				if (matchingPaths.Count == 0) {
					ImGui.TextDisabled("(no mods match this pattern)");
				} else {
					foreach (var modPath in matchingPaths) {
						ImGui.TextUnformatted(modPath);
					}
				}

				ImGui.EndChildFrame();
			}
		}
	}

	private void DrawModItemBlacklistSection() {
		ImGui.Spacing();
		if (ImGui.CollapsingHeader("Blacklist by Mod+Item##DrawPenumbraConfigs")) {
			ImGui.TextWrapped("Blacklist specific items from specific mods. For example, if a mod changes both boots and gloves, you can blacklist just the gloves.");
			ImGui.Spacing();

			ImGui.AlignTextToFramePadding();
			ImGui.Text("Blacklisted mod items");
			GuiHelpers.Tooltip("These mod+item combinations will be ignored when creating the list of modded items");
			ImGui.SameLine();
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, "Clear all\nHold ctrl + Shift to confirm", default, "TrashButton##AddToItemBlackList##PenumbraConfig##ConfigWindow"))
				ConfigurationManager.Config.PenumbraModsBlacklistByItemId.Clear();

			if (ImGui.BeginChildFrame(411146, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 5))) {
				var itemsToRemove = new List<(string, uint)>();
				foreach (var (modPath, itemId) in ConfigurationManager.Config.PenumbraModsBlacklistByItemId) {
					var modName = PluginServices.Penumbra.GetModNameCache(modPath) ?? modPath;
					if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.Trash, $"{modPath}#{itemId}##TrashButton##AddToItemBlackList##PenumbraConfig##ConfigWindow", "Remove from blacklist")) {
						itemsToRemove.Add((modPath, itemId));
					}
					ImGui.SameLine();
					ImGui.TextUnformatted($"{modName} - Item {itemId}");
					GuiHelpers.Tooltip($"Mod: {modPath}\nItem ID: {itemId}");
				}
				foreach (var item in itemsToRemove) {
					ConfigurationManager.Config.PenumbraModsBlacklistByItemId.Remove(item);
					if (Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
				}
				ImGui.EndChildFrame();
			}
		}
	}

	private void DrawAboutConfigs() {
		ImGui.TextWrapped("Dresser is a plugin to manage your glamours and inventories, with a special focus on portable glamours.\n" +
			"Feel free to look for help or provide feedback on our Discord");
		ImGui.Spacing();
		ImGui.PushStyleColor(ImGuiCol.Button, Styler.DiscordColor);
		if (ImGui.Button("Dresser Discord"))
			"https://discord.gg/kXwKDFjXBd".OpenBrowser();
		ImGui.PopStyleColor();

	}

	private static List<(string Path, string Name)>? ModsAvailableToBlacklist = null;
	private static string ModsBlackListSearch = "";
	private static bool ModsBlackListSearchOpen = false;
	public static void AddModToBlacklist((string Path, string Name) mod) {
		ConfigurationManager.Config.PenumbraModsBlacklist.Add(mod);
		ModsAvailableToBlacklist = null;
		PluginServices.Storage.ReloadMods();
	}
	public static void AddModItemToBlacklist((string Path, uint ItemId) modItem) {
		ConfigurationManager.Config.PenumbraModsBlacklistByItemId.Add(modItem);
		PluginServices.Storage.ReloadMods();
	}
	private void DrawModBlacklistSelector() {
		ModsAvailableToBlacklist ??= PluginServices.Penumbra.GetNotBlacklistedMods().ToList();
		if (ModsBlackListSearchOpen)
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SelectorList
				| PopupSelect.HoverPopupWindowFlags.SearchBar,
				ModsAvailableToBlacklist,
				(e, input) => e.Where(t => t.Name.Contains(input, StringComparison.OrdinalIgnoreCase)),
				(mod, a) => {
					bool selected = ImGui.Selectable($"{mod.Name}##{mod.Name}##{mod.Path}##BlacklistSelect##PenumbraConfig##ConfigWindow", a);
					bool focus = ImGui.IsItemFocused();
					GuiHelpers.Tooltip(() => {
						ImGui.Text($"{mod.Name}");
						ImGui.TextDisabled($"{mod.Path}");
					});
					return (selected, focus);
				},
				AddModToBlacklist, // on select
				() => { ModsBlackListSearchOpen = false; }, // on close
				ref ModsBlackListSearch,
				"Blacklist Mod",
				"##blacklist_mod",
				"##blacklist_mod2");
	}

	private void DrawDebugSection() {
		if (ImGui.CollapsingHeader("Toggle debug stuff")) {
			ImGui.Checkbox($"Display debug info##Debug##GearBrowserConfig", ref ConfigurationManager.Config.IconTooltipShowDev);
			//ImGui.Checkbox($"Force Standalone Appearance Apply##Debug##GearBrowserConfig", ref ConfigurationManager.Config.ForceStandaloneAppearanceApply);


		}
		if (ImGui.CollapsingHeader("Modded items debug")) {
			ImGui.Checkbox($"Display how many items applied in title bar##Debug##GearBrowserConfig", ref ConfigurationManager.Config.DebugDisplayModedInTitleBar);
		}
		if (ImGui.CollapsingHeader("Manual triggers")) {
			if (ImGui.Button("AppearanceUpdateNakedOrWearing##Manual triggers##Debug##ConfigWindow")) PluginServices.ApplyGearChange.AppearanceUpdateNakedOrWearing();
			if (ImGui.Button("ReApplyAppearanceAfterEquipUpdate##Manual triggers##Debug##ConfigWindow")) PluginServices.ApplyGearChange.ReApplyAppearanceAfterEquipUpdate();
			//if (ImGui.Button($"CleanDresserApplyCollection ({PluginServices.Penumbra.CountModsDresserApplyCollection()})##Manual triggers##Debug##ConfigWindow")) PluginServices.Penumbra.CleanDresserApplyCollection();
			if (ImGui.Button($"Save config##Manual triggers##Debug##ConfigWindow")) ConfigurationManager.SaveAsync();
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Broom, $"Remove EVERY (ALL) items from ALL plates for THIS character {PluginServices.Context.LocalPlayer?.Name}", default, $"##clear##Manual triggers##Debug##ConfigWindow")) {
				ConfigurationManager.Config.PendingPlateItemsCurrentChar = new();
			}




		}
		if (ImGui.CollapsingHeader("Test Currencies"))
		{
			DrawDebugCurrencies();
		}		if (ImGui.CollapsingHeader("Test Actions"))
		{
			DrawDebugActions();
		}
		if (ImGui.CollapsingHeader("TestUld")) {
			ImageGuiCrop.TestParts();
		}
		if (ImGui.CollapsingHeader("Glamour Plate Interop")) {
			GlamourPlateDebug.Draw();
		}
		if (ImGui.CollapsingHeader("Glamourer IPC")) {
			DrawGlamourerIpcDebug();
		}
		if (ImGui.CollapsingHeader("Penumbra IPC")) {
			DrawPenumbraIpcDebug();
		}
		if (ImGui.CollapsingHeader("Allagan Tools IPC")) {
			ImGui.TextDisabled($"characters owned by player from AT IPC");

			ImGui.BeginTable("##tablecharacterIdsFromATIPC##Info##Debug##ConfigWindow", 2);
			ImGui.TableSetupColumn("Character ID");
			ImGui.TableSetupColumn("Character Type");
			ImGui.TableHeadersRow();

			foreach ( var characterId in PluginServices.AllaganTools.GetCharactersOwnedByActive(true)) {
				ImGui.TableNextColumn();
				ImGui.TextUnformatted($"{characterId}");
				ImGui.TableNextColumn();
				ImGui.TextUnformatted($"{(characterId.ToString().StartsWith("3") ? "Retainer": (PluginServices.ClientState.LocalContentId == characterId ? "Player Character" : "Unknown"))}");
			}

			ImGui.EndTable();
			ImGui.Separator();ImGui.Spacing();ImGui.Spacing();

			ImGui.TextDisabled($"allitems retainerId distinct from AT IPC");

			ImGui.BeginTable("##tablecharacterIdsFromItems##Info##Debug##ConfigWindow", 1);
			ImGui.TableSetupColumn("Character ID");
			ImGui.TableHeadersRow();

			foreach (var characterId in PluginServices.AllaganTools.GetItemsLocalCharsRetainers(true).SelectMany(t => t.Value).Select(i=>i.RetainerId).Distinct()) {
				ImGui.TableNextColumn();
				ImGui.TextUnformatted($"{characterId}");
			}

			ImGui.EndTable();
			ImGui.Separator();ImGui.Spacing();ImGui.Spacing();

			ImGui.TextDisabled($"GetItemsLocalCharsRetainers");

			ImGui.BeginTable("##tablecharacterIdsFromItems##Info##Debug##ConfigWindow", 1);
			ImGui.TableSetupColumn("Character ID");
			ImGui.TableHeadersRow();

			foreach (var characterId in PluginServices.AllaganTools.GetItemsLocalCharsRetainers(true).SelectMany(t => t.Value).Select(i=>i.RetainerId).Distinct()) {
				ImGui.TableNextColumn();
				ImGui.TextUnformatted($"{characterId}");
			}

			ImGui.EndTable();

		}
		if (ImGui.CollapsingHeader("ItemVendorLocation IPC")) {

			ImGui.Text($"Initialized: {PluginServices.ItemVendorLocation.IsInitialized()}");
			DrawItemVendorButton(38035);
			DrawItemVendorButton(47929);


		}
	}
	private void DrawItemVendorButton(uint itemId) {
		if (ImGui.Button($"GetItemInfoProvider {itemId}##ItemVendorLocationIPC##Debug##ConfigWindow")) {

			var zz = PluginServices.ItemVendorLocation.GetItemInfoProvider(itemId);
			if(zz != null) foreach (var rr in zz) {
					if(rr == null) continue;
					PluginLog.Debug($"{rr.NpcId}, {rr.TerritoryId}, {rr.Coordinates}, {rr.GetNpcName()}, {rr.GetPlaceName()}");
			}
		}
		ImGui.SameLine();
		if (ImGui.Button($"OpenUiWithItemId {itemId}##ItemVendorLocationIPC##Debug##ConfigWindow")) {
			PluginServices.ItemVendorLocation.OpenUiWithItemId(itemId);
		}

	}

	private void DrawGlamourerIpcDebug() {
		if(PluginServices.Context.LocalPlayer == null) return;

		if (ImGui.Button("GetState##GlamourerIPC##Debug##ConfigWindow")) {
			PluginLog.Debug($"{PluginServices.Glamourer.GetState()}");
		}
		if (ImGui.Button("SetMetaData Hat##GlamourerIPC##Debug##ConfigWindow")) {
			PluginLog.Debug($"{PluginServices.Glamourer.SetMetaData(PluginServices.Context.LocalPlayer, GlamourerService.MetaData.Hat)}");
		}
		ImGui.SameLine();
		if (ImGui.Button("SetMetaData Visor##GlamourerIPC##Debug##ConfigWindow")) {
			PluginLog.Debug($"{PluginServices.Glamourer.SetMetaData(PluginServices.Context.LocalPlayer, GlamourerService.MetaData.Visor)}");
		}
		ImGui.SameLine();
		if (ImGui.Button("SetMetaData Weapon##GlamourerIPC##Debug##ConfigWindow")) {
			PluginLog.Debug($"{PluginServices.Glamourer.SetMetaData(PluginServices.Context.LocalPlayer, GlamourerService.MetaData.Weapon)}");
		}

	}
	private void DrawPenumbraIpcDebug() {
		if(!PluginServices.Context.PenumbraState) return;

		var slot = ConfigurationManager.Config.CurrentGearSelectedSlot;
		var item = PluginServices.ApplyGearChange.GetCurrentPlateItem(slot);
		var isItemModded = item != null && item.IsModded();

		ImGui.Text($"Current item ({slot}):");
		ImGui.Bullet(); ImGui.TextWrapped($"Name: {(item != null ? item.FormattedName : "null")}");
		ImGui.Bullet(); ImGui.TextWrapped($"Is Modded: {isItemModded}");
		if (isItemModded && item != null) {
			ImGui.Bullet(); ImGui.TextWrapped($"Mod Directory: {item.ModDirectory}");
		}
		ImGui.Spacing();

		if (ImGui.Button("Get Changed Item Adapter Dictionary Print To Log##PenumbraIPC##Debug##ConfigWindow")) {
			PluginServices.Penumbra.GetChangedItemAdapterDictionaryPrintToLog();
		}

		if (ImGui.Button("Get Changed Item Adapter List Print To Log##PenumbraIPC##Debug##ConfigWindow")) {
			PluginServices.Penumbra.GetChangedItemAdapterListPrintToLog();
		}

		if(!isItemModded) ImGui.BeginDisabled();
		if (ImGui.Button("Penumbra.SetTemporaryModSettings (Current item)##PenumbraIPC##Debug##ConfigWindow") && item != null && isItemModded) {
			PluginLog.Debug($" - Item for mod application -" +
				$"\n name: {item.FormattedName}" +
				$"\n modDirectory: {item.ModDirectory}" +
				$"\n ModIconPath: {item.ModIconPath}" +
				$"\n ModModelPath: {item.ModModelPath}" +
				$"\n ModAuthor: {item.ModAuthor}" +
				$"");


			// load mod in penumbra if not loaded, and enable it
			var res5 = PluginServices.Penumbra.SetTemporaryModSettings(item);
			PluginLog.Debug($"SetTemporaryModSettings result: {res5}");
		}

		ImGui.SameLine();
		if (ImGui.Button("Glamourer.SetItem (Current item)##PenumbraIPC##Debug##ConfigWindow") && item != null && isItemModded) {
			PluginLog.Debug($" - Item for mod application -" +
				$"\n name: {item.FormattedName}" +
				$"\n modDirectory: {item.ModDirectory}" +
				$"\n ModIconPath: {item.ModIconPath}" +
				$"\n ModModelPath: {item.ModModelPath}" +
				$"\n ModAuthor: {item.ModAuthor}" +
				$"");

			var character = PluginServices.Context.LocalPlayer;
			if (character != null) {
				PluginServices.Glamourer.SetItem(character, item, slot);
			}
		}
		if (!isItemModded) ImGui.EndDisabled();

		if (ImGui.Button("GetAllModSettings##PenumbraIPC##Debug##ConfigWindow")) {
			PluginServices.Penumbra.GetAllModSettings();
		}

		if (!isItemModded) ImGui.BeginDisabled();
		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Rocket, "Print Meta To Log (Current item)##GetModMeta##PenumbraIPC##Debug##ConfigWindow") && item != null && isItemModded && item.ModDirectory != null) {
			var meta = PluginServices.Penumbra.GetModMeta(item.ModDirectory);
			PluginLog.Debug($"Meta for {item.ModDirectory}:\n  {meta}");	
		}
		if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Rocket, "Print ModPath To Log (Current item)##GetModPath##PenumbraIPC##Debug##ConfigWindow") && item != null && isItemModded && item.ModDirectory != null) {
			var path = PluginServices.Penumbra.GetModPath(item.ModDirectory);
			if (path != null) {
				PluginLog.Debug($"Path for {item.ModDirectory}:" +
					$"\n   FullPath:    {path.Value.FullPath}" +
					$"\n   FullDefault: {path.Value.FullDefault}" +
					$"\n   NameDefault: {path.Value.NameDefault}");
			} else {
				PluginLog.Debug($"No path for mod {item.ModDirectory}");
			}
		}

		if (!isItemModded) ImGui.EndDisabled() ;




	}

	private void DrawColorStyleConfig() {
		ImGui.TextDisabled("Frames");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.CollectionColorBackground), "Window Background", "Background color for the frames that contain items", ImGuiColorEditFlags.None);
		ConfigControls.ConfigColorVecot4(nameof(Configuration.CollectionColorBorder), "Window Border", "Border color for the frames that contain items", ImGuiColorEditFlags.None);
		ConfigControls.ConfigColorVecot4(nameof(Configuration.CollectionColorScrollbar), "Scroll bar", "Scroll bar color for the frames that contain items (in Gear Browser)", ImGuiColorEditFlags.None);
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ColorIconImageTintEnabled), "Icon Tint (Enabled)", "Tint of enabled icons (currencies)", ImGuiColorEditFlags.None);
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ColorIconImageTintDisabled), "Icon Tint (Disabled)", "Tint of disabled icons (currencies)", ImGuiColorEditFlags.None);
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ColorFilteredIndicator), "Filter Indicator", "Will show on various controls when filters are active", ImGuiColorEditFlags.None);

		ImGui.Separator();
		ImGui.TextDisabled("Text");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.PlateSelectorColorTitle), "Title");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ColorGood),"Good");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ColorGoodLight),"Good (light)");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ColorBad),"Bad");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ColorGrey),"Grey");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ColorGreyDark),"Grey (dark)");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ColorBronze),"Bronze");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ModdedItemColor), "Modded item name");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.ModdedItemWatermarkColor),"Modded item tooltip watermark");

		ImGui.Separator();
		ImGui.TextDisabled("Plate selector");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.PlateSelectorActiveColor), "Active");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.PlateSelectorHoverColor), "Hover");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.PlateSelectorRestColor), "Rest");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.PlateSelectorColorRadio), "Radio text");

		ImGui.Separator();
		ImGui.TextDisabled("Browser");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.DyePickerHighlightSelection  ), "Selection");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.BrowserVerticalTabButtonsBg  ), "Inactive tab bg");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.DyePickerDye1Or2SelectedBg   ), "Active tab bg");
		ConfigControls.ConfigColorVecot4(nameof(Configuration.DyePickerDye1Or2SelectedColor), "Active dye text");

	}



	private void DrawDebugCurrencies()
	{
		var currencyUsed = PluginServices.SheetManager.ItemInfoCache.GetNpcShops()?.Values.SelectMany(v=>v.SelectMany(fg=>fg.CostItems)).Distinct() ?? [];
		currencyUsed = currencyUsed.Where(c => 
			c.IsCurrency
			// !c.CanBeAcquired
			// && !c.CanTryOn
			// && !c.CanBeDesynthed
			// && !c.CanBeCrafted
			// && !c.CanBeGathered
			);
		currencyUsed = currencyUsed.OrderBy(c => c.RowId);
		foreach (var currency in currencyUsed)
		{
			ImGui.Image(currency.IconTextureWrap().GetWrapOrEmpty().Handle, ItemIcon.IconSize * 0.4f);
			ImGui.SameLine();
			ImGui.Text(currency.RowId.ToString());
			ImGui.SameLine();
			ImGui.Text(currency.NameString);
		}
	}
	private void DrawDebugActions()
	{
		if (ImGui.Button("get emote state##actions##config"))
		{
			
			PluginLog.Debug($"GetPoseKind   : {PluginServices.Actions.GetEmoteController()?.GetPoseKind()}");
			PluginLog.Debug($"EmoteId       : {PluginServices.Actions.GetEmoteController()?.EmoteId}");
			PluginLog.Debug($"CPoseState    : {PluginServices.Actions.GetEmoteController()?.CPoseState}");

			PluginLog.Debug($"GetAvailablePoses: {PluginServices.Actions.GetAvailablePoses()}");
			PluginLog.Debug($"GetPoseKind      : {PluginServices.Actions.GetPoseKind()}");
		}
		if (ImGui.Button("test action##actions##config"))
		{
			PluginServices.Actions.ExcuteAction();
		}
		if (ImGui.Button("print emote list##actions##config"))
		{
			PluginServices.Actions.EmoteList();
		}
		ImGui.SameLine();
		if (ImGui.Button("Favorites##print emote list##actions##config"))
		{

			var favEmotes = PluginServices.Actions.EmoteFavorites();
			PluginLog.Debug($"Fav Emotes      : {favEmotes.Length}");
			foreach (var emote in favEmotes)
			{
				PluginLog.Debug($"   {emote}");
			}
			
		}
		ImGui.SameLine();
		if (ImGui.Button("History##print emote list##actions##config"))
		{
			var histEmotes = PluginServices.Actions.EmoteHistory();
			PluginLog.Debug($"History Emotes   : {histEmotes.Length}");
			foreach (var emote in histEmotes)
			{
				PluginLog.Debug($"   {emote}");
			}
		}
		if (ImGui.Button("print action list##actions##config"))
		{
			PluginServices.Actions.ActionList();
		}
		if (ImGui.Button("execute change posture##actions##config"))
		{
			PluginServices.Actions.ExecuteChangePosture();
		}
			
	}
}
