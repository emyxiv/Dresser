using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using CriticalCommonLib.Enums;

using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;

using Dresser.Enums;
using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Windows.Components;

using Dalamud.Bindings.ImGui;

using Newtonsoft.Json;

namespace Dresser.Windows;

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

		if (!GlamourPlates.IsAnyPlateSelectionOpen())
			ImGui.BeginDisabled();
		var posBefore = ImGui.GetCursorPos();
		if (ImGui.Button("Re-import Glamour Plates and erase pending plates"))
			PluginServices.ApplyGearChange.OverwritePendingWithActualPlates();
		var size = ImGui.GetItemRectSize();
		if (!GlamourPlates.IsAnyPlateSelectionOpen()) {
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
	}

	private void DrawPenumbraConfigs() {

		if (ImGui.CollapsingHeader("Info##DrawPenumbraConfigs")) {
			ImGui.TextWrapped($"This is feature may be nerdy to configure. Due to current technical limitations (or knowledge limitation) it is not straightforward to set-up and might require manipulations in Penumbra.");

			ImGui.Spacing();
			ImGui.TextWrapped($"The method to apply appearance will only work for one mod per item, it will not work with conversion mods " +
				$"(For example, a mod replaces an item, and then another mod changes the shape of the item while preserving the texture of the first mod.)");

			ImGui.Spacing();
			ImGui.Spacing();

			ImGui.BeginGroup();
			ImGui.Text("1. ");
			ImGui.SameLine();
			ImGui.TextWrapped($"The collection reffered as \"Temporary Penumbra collection\" (by default named \"Dresser TMP\") must be created AND assigned");
			ImGui.SameLine();
			GuiHelpers.Icon(FontAwesomeIcon.QuestionCircle,false);

			ImGui.EndGroup();
			GuiHelpers.Tooltip(() => {
				ImGui.Bullet();
				ImGui.TextWrapped($"This will be used as a workaround to find which mod change which items in your list of mods.");
				ImGui.Bullet();
				ImGui.TextWrapped($"The collection should be assigned to unrelatted object.\n A way to achieve that is, in Penumbra, to navigate to Collection > Individual Assignments > then enter an improbable name and select a world you would never visit. Then drag the collection \"Dresser TMP\" and drop it in \"New Player\" box.");
			});
			ImGui.Spacing();


			ImGui.BeginGroup();
			ImGui.Text("2. ");
			ImGui.SameLine();
			ImGui.TextWrapped($"The collection reffered as \"Penumbra collection to apply\" (by default named \"Dresser Apply\") must be created AND assigned to \"Your character\" ");
			ImGui.SameLine();
			GuiHelpers.Icon(FontAwesomeIcon.QuestionCircle, false);

			ImGui.EndGroup();
			GuiHelpers.Tooltip(() => {
				ImGui.Bullet();
				ImGui.TextWrapped($"This collection will be used to apply the preview on your character.");
				ImGui.Bullet();
				ImGui.TextWrapped($"The collection will often be populated and cleaned." +
					$"\nDresser will manage it, so you should not enable any mode in this collection");
				ImGui.Bullet();
				ImGui.TextWrapped($"It should be set at the top of the collection tree to not be overwritten by any mods, and still take base collections." +
					$"\nIf you don't use multiple collections");
			});

			ImGui.AlignTextToFramePadding();
			ImGui.TextWrapped($"Feel free to look for help or provide feedback on");
			ImGui.SameLine();
			ImGui.PushStyleColor(ImGuiCol.Button, Styler.DiscordColor);
			if (ImGui.Button("Our Discord Server"))
				"https://discord.gg/kXwKDFjXBd".OpenBrowser();
			ImGui.PopStyleColor();
			ImGui.SameLine();
			ImGui.Text(".");




		}
		ImGui.Spacing();

		if (ImGui.CollapsingHeader("Collections & Behaviors##DrawPenumbraConfigs")) {
			ImGui.Checkbox($"Use collection Mod List##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraUseModListCollection);
			GuiHelpers.Tooltip($"If checked, only mods enabled on [{ConfigurationManager.Config.PenumbraCollectionModList}] will be scanned\nIf unchecked, ALL installed mods will be scanned for changed items");

			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
			if (!ConfigurationManager.Config.PenumbraUseModListCollection) ImGui.BeginDisabled();
			ImGui.InputText($"Mod List Penumbra collection##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraCollectionModList, 100);
			if (!ConfigurationManager.Config.PenumbraUseModListCollection) ImGui.EndDisabled();
			GuiHelpers.Tooltip($"When enabled, the mods of this collection will be scanned.\nMods can be enabled regardless of conflicts, as mods sharing the same items can be displayed at the same time");

			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
			ImGui.InputText($"Temporary Penumbra collection##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraCollectionTmp, 100);
			GuiHelpers.Tooltip($"This collection must be activated and assigned (to fake object) in order to find modded items");

			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
			ImGui.InputText($"Penumbra collection to apply##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraCollectionApply, 100);
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
			ImGui.DragInt($"Delay 1##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance, 10, 0, int.MaxValue, "%.0f", ImGuiSliderFlags.AlwaysClamp);
			GuiHelpers.Tooltip($"Penumbra delay\nAfter the mod was enabled\nBefore apply appearance");
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.Undo, "ResetDefault##Delay 1##PenumbraConfig##ConfigWindow", "Reset to default value"))
				ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance = ConfigurationManager.Default.PenumbraDelayAfterModEnableBeforeApplyAppearance;


			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
			ImGui.DragInt($"Delay 2##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraDelayAfterApplyAppearanceBeforeModDisable, 10, 0, int.MaxValue, "%.0f", ImGuiSliderFlags.AlwaysClamp);
			GuiHelpers.Tooltip($"Penumbra delay\nAfter apply appearance\nBefore disabling the mod");
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.Undo, "ResetDefault##Delay 2##PenumbraConfig##ConfigWindow", "Reset to default value"))
				ConfigurationManager.Config.PenumbraDelayAfterApplyAppearanceBeforeModDisable = ConfigurationManager.Default.PenumbraDelayAfterApplyAppearanceBeforeModDisable;

			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
			ImGui.DragInt($"Delay 3##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraDelayAfterModDisableBeforeNextModLoop, 10, 0, int.MaxValue, "%.0f", ImGuiSliderFlags.AlwaysClamp);
			GuiHelpers.Tooltip($"Penumbra delay\nAfter the mod was disabled\nBefore next mod loop");
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.Undo, "ResetDefault##Delay 3##PenumbraConfig##ConfigWindow", "Reset to default value"))
				ConfigurationManager.Config.PenumbraDelayAfterModDisableBeforeNextModLoop = ConfigurationManager.Default.PenumbraDelayAfterModDisableBeforeNextModLoop;

			ImGui.Checkbox($"Disable the mod instantly after applied##Debug##GearBrowserConfig", ref ConfigurationManager.Config.PenumbraDisableModRightAfterApply);
			GuiHelpers.Tooltip($"Right after the item appearance is applied on the character, the mod applied during  application will be disabled. The item will still be displaed with the mod (Except in Mare). This allows displaying mods that affects the same items.");
		}



		ImGui.Spacing();
		if (ImGui.CollapsingHeader("Scan and Blacklist##DrawPenumbraConfigs", ImGuiTreeNodeFlags.DefaultOpen)) {

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
			}
			if (PluginServices.Storage.IsReloadingMods) ImGui.EndDisabled();


			ImGui.Spacing();
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
	}
	private static List<(string Path, string Name)>? ModsAvailableToBlacklist = null;
	private static string ModsBlackListSearch = "";
	private static bool ModsBlackListSearchOpen = false;
	public static void AddModToBlacklist((string Path, string Name) mod) {
		ConfigurationManager.Config.PenumbraModsBlacklist.Add(mod);
		ModsAvailableToBlacklist = null;
		if (Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
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
			if (ImGui.Button("AppearanceUpdateNakedOrWearing2##Manual triggers##Debug##ConfigWindow")) PluginServices.ApplyGearChange.AppearanceUpdateNakedOrWearing2();
			if (ImGui.Button("ReApplyAppearanceAfterEquipUpdate##Manual triggers##Debug##ConfigWindow")) PluginServices.ApplyGearChange.ReApplyAppearanceAfterEquipUpdate();
			if (ImGui.Button($"CleanDresserApplyCollection ({PluginServices.Penumbra.CountModsDresserApplyCollection()})##Manual triggers##Debug##ConfigWindow")) PluginServices.Penumbra.CleanDresserApplyCollection();
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
