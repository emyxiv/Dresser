using Dalamud.Interface.Windowing;
using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Windows.Components;

using ImGuiNET;

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
}
