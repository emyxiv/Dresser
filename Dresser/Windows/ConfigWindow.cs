using CriticalCommonLib.Extensions;

using Dalamud.Interface.Windowing;
using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Windows.Components;

using ImGuiNET;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Dresser.Windows;

public class ConfigWindow : Window, IDisposable {

	public ConfigWindow(Plugin plugin) : base(
		"Dresser Settings",
		ImGuiWindowFlags.None) {
		this.Size = new Vector2(232, 75);
		this.SizeCondition = ImGuiCond.FirstUseEver;

		sections = new() {
			{ "Dependencies", Dependencies },
			{ "Portable Plates", DrawPlatesConfig },
			{ "Windows & sizing", DrawWindowsAndSizingConfigs },
			{ "Icons", DrawIconsConfigs },
			{ "Behaviors", DrawBehaviourConfigs },
			{ "Penumbra", DrawPenumbraConfigs },
		};
	}

	private Dictionary<string, Action> sections;

	public void Dispose() { }

	public override void Draw() {
		// can't ref a property, so use a local copy
		//var configValue = this.Configuration.SomePropertyToBeSavedAndWithADefault;
		//if (ImGui.Checkbox("Random Config Bool", ref configValue)) {
		//	Configuration.SomePropertyToBeSavedAndWithADefault = configValue;
		//	//can save immediately on change, if you don't want to provide a "Save and Close" button
		//	Configuration.Save();
		//}

		var draw = ImGui.GetWindowDrawList();
		var fontSize = ImGui.GetFontSize();
		var textColor = ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
		foreach ((var title, var contents) in sections) {

			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fontSize * 0.5f);

			GuiHelpers.TextWithFont(title, GuiHelpers.Font.TrumpGothic_23);
			var start = ImGui.GetCursorScreenPos();
			//start += new Vector2(0, fontSize * 0.1f);
			var titelRectSize = ImGui.GetItemRectSize();
			var end = start + new Vector2(titelRectSize.X, 0);
			draw.AddLine(start, end, textColor, fontSize * 0.15f);
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fontSize*1.5f);

			contents();
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + fontSize * 1.5f);


		}
	}

	private void DrawBehaviourConfigs() {
		ImGui.Checkbox($"(Experimental) Hotkeys after loosing window focus##Behaviours##Config", ref ConfigurationManager.Config.WindowsHotkeysAllowAfterLoosingFocus);
		GuiHelpers.Tooltip("For example, when loosing Gear Browser focus, the directional hotkeys will continue working until another window is focused or dresser is closed");

		if(ImGui.Checkbox($"(Experimental) Pass Hotkeys to the game through window##Behaviours##Config", ref ConfigurationManager.Config.WindowsHotkeysPasstoGame))
			HotkeySetup.Init();

	}
	private void DrawIconsConfigs() {

		ImGui.Checkbox($"Show items icons##displayCategory##GearBrowserConfig", ref ConfigurationManager.Config.ShowImagesInBrowser);
		ImGui.Checkbox($"Fade unavailable items when hidding tooltips (Hold Alt)##Images##GearBrowserConfig", ref ConfigurationManager.Config.FadeIconsIfNotHiddingTooltip);
		ImGui.Checkbox($"Display debug info##Images##GearBrowserConfig", ref ConfigurationManager.Config.IconTooltipShowDev);

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
		var GearBrowserSideBarSize = ConfigurationManager.Config.GearBrowserSideBarSize;
		if (ImGui.DragFloat("size - Sidebar##GearBrowserConfig", ref GearBrowserSideBarSize, 10f, 20, 2000,"%.1f", ImGuiSliderFlags.AlwaysClamp))
			ConfigurationManager.Config.GearBrowserSideBarSize = GearBrowserSideBarSize;
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

	private void Dependencies() {
		if (PluginServices.AllaganTools.IsInitialized()) {
			ImGui.TextColored(ItemIcon.ColorGood, "Allagan Tools Found");
			//PluginServices.AllaganTools.CheckMethodAvailability();
		} else {
			GuiHelpers.Icon(Dalamud.Interface.FontAwesomeIcon.ExclamationTriangle, true, ItemIcon.ColorBad);
			ImGui.SameLine();
			ImGui.TextColored(ItemIcon.ColorBad, "Allagan Tools not found");
			ImGui.TextWrapped("To find items in inventories, please install Allagan Tools plugin from Critical Impact.");

		}


	}

	private void DrawPenumbraConfigs() {
		ImGui.Checkbox($"Use collection Mod List##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraUseModListCollection);
		GuiHelpers.Tooltip($"If checked, only mods enabled on [{ConfigurationManager.Config.PenumbraCollectionModList}] will be scanned\nIf unchecked, ALL installed mods will be scanned for changed items");
		ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
		if(!ConfigurationManager.Config.PenumbraUseModListCollection) ImGui.BeginDisabled();
		ImGui.InputText($"Mod List Penumbra collection##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraCollectionModList, 100);
		if (!ConfigurationManager.Config.PenumbraUseModListCollection) ImGui.EndDisabled();
		GuiHelpers.Tooltip($"When enabled, the mods of this collection will be scanned.\nMods can be enabled regardless of conflicts, as mods sharing the same items can be displayed at the same time");
		ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
		ImGui.InputText($"Temporary Penumbra collection##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraCollectionTmp, 100);
		GuiHelpers.Tooltip($"This collection must be activated and assigned (to fake object) in order to find modded items");
		ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
		ImGui.InputText($"Penumbra collection to apply##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraCollectionApply, 100);
		ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
		ImGui.DragInt($"Delay 1##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance, 10, 0, int.MaxValue,"%.0f",ImGuiSliderFlags.AlwaysClamp);
		GuiHelpers.Tooltip($"Penumbra delay\nAfter the mod was enabled\nBefore apply appearance");
		ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
		ImGui.DragInt($"Delay 2##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraDelayAfterApplyAppearanceBeforeModDisable, 10, 0, int.MaxValue,"%.0f",ImGuiSliderFlags.AlwaysClamp);
		GuiHelpers.Tooltip($"Penumbra delay\nAfter apply appearance\nBefore disabling the mod");
		ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
		ImGui.DragInt($"Delay 3##PenumbraConfig##ConfigWindow", ref ConfigurationManager.Config.PenumbraDelayAfterModDisableBeforeNextModLoop, 10, 0, int.MaxValue,"%.0f",ImGuiSliderFlags.AlwaysClamp);
		GuiHelpers.Tooltip($"Penumbra delay\nAfter the mod was disabled\nBefore next mod loop");

		ImGui.Spacing();
		ImGui.Text($"{ConfigurationManager.Config.PenumbraModdedItems.Count} modded items in Config, {PluginServices.Storage.AdditionalItems[(CriticalCommonLib.Enums.InventoryType)Storage.InventoryTypeExtra.ModdedItems].Count} in memory");

		if (PluginServices.Storage.IsReloadingMods) ImGui.BeginDisabled();
		ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10);
		if(ImGui.Button($"Reload modded items {(PluginServices.Storage.IsReloadingMods?$" ({PluginServices.Storage.ModsReloadingCur}/{PluginServices.Storage.ModsReloadingMax})":"")}##PenumbraConfig##ConfigWindow")) {
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
		if (GuiHelpers.IconButtonNoBg(Dalamud.Interface.FontAwesomeIcon.Plus, "PlusButton##AddToBlackList##PenumbraConfig##ConfigWindow", ""))
			ModsBlackListSearchOpen = true;
		ImGui.SameLine();
		if (GuiHelpers.IconButtonNoBg(Dalamud.Interface.FontAwesomeIcon.FileExport, "ExportButton##AddToBlackList##PenumbraConfig##ConfigWindow", "Export list to Clipboard as JSON"))
			JsonConvert.SerializeObject(ConfigurationManager.Config.PenumbraModsBlacklist).ToClipboard();
		ImGui.SameLine();
		if (GuiHelpers.IconButtonNoBg(Dalamud.Interface.FontAwesomeIcon.FileImport, "ImportButton##AddToBlackList##PenumbraConfig##ConfigWindow", "Import list from JSON Clipboard")) {
			try {
				var decodedBlacklist = JsonConvert.DeserializeObject<List<(string Path, string Name)>>(ImGui.GetClipboardText());
				if (decodedBlacklist != null) ConfigurationManager.Config.PenumbraModsBlacklist = ConfigurationManager.Config.PenumbraModsBlacklist.Concat(decodedBlacklist).ToList();
			} catch (Exception) { }
		}
		ImGui.SameLine();
		if (GuiHelpers.IconButtonHoldConfirm(Dalamud.Interface.FontAwesomeIcon.Trash, "Empty blacklist\nHold ctrl + Shift to to confirm", default, "TrashButton##AddToBlackList##PenumbraConfig##ConfigWindow"))
			ConfigurationManager.Config.PenumbraModsBlacklist.Clear();

		if (ImGui.BeginChildFrame(411141, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 5))) {
			for (int i = ConfigurationManager.Config.PenumbraModsBlacklist.Count - 1; i >= 0; i--) {
				var mod = ConfigurationManager.Config.PenumbraModsBlacklist[i];
				if (GuiHelpers.IconButtonNoBg(Dalamud.Interface.FontAwesomeIcon.Trash, $"{mod.Path}##TrashButton##AddToBlackList##PenumbraConfig##ConfigWindow", "Remove from blacklist")) {
					ConfigurationManager.Config.PenumbraModsBlacklist.RemoveAt(i);
				}
				ImGui.SameLine();
				ImGui.Text($"{mod.Name}");
				GuiHelpers.Tooltip($"{mod.Path}");

			}


			ImGui.EndChildFrame();
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



}
