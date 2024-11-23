using System;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;

using Dresser.Logic;
using Dresser.Services;
using Dresser.Windows.Components;

using ImGuiNET;

namespace Dresser.Windows {
	public partial class GearBrowser : Window, IWindowWithHotkey, IDisposable {
		private Plugin Plugin;

		public GearBrowser(Plugin plugin) : base(
			"Gear Browser", ImGuiWindowFlags.NoScrollbar) {
			this.SizeConstraints = new WindowSizeConstraints {
				MinimumSize = new Vector2(ImGui.GetFontSize() * 4),
				MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
			};
			this.Plugin = plugin;
			this._dyePicker = new DyePicker();
		}
		public void Dispose() { }

		public override void OnOpen() {
			RecomputeItems();
		}

		public bool OnHotkey(HotkeyPurpose hotkeyType) {
			switch (hotkeyType) {
				case HotkeyPurpose.Up:
					HotkeyNextSelect = HoveredIncrement - RowSize;
					if (HotkeyNextSelect < 0) HotkeyNextSelect = HoveredIncrement;
					return true;
				case HotkeyPurpose.Down:
					HotkeyNextSelect = HoveredIncrement + RowSize;
					if (HotkeyNextSelect > ItemsCount) HotkeyNextSelect = HoveredIncrement;
					return true;
				case HotkeyPurpose.Left:
					HotkeyNextSelect = HoveredIncrement - 1;
					return true;
				case HotkeyPurpose.Right:
					HotkeyNextSelect = HoveredIncrement + 1;
					return true;
				default:
					return false;
			}
		}

		private enum VerticalTab
		{
			Clothes,
			Dyes,
		}
		private VerticalTab CurrentVerticalTab = VerticalTab.Clothes;
		public void SwitchToDyesMode() => CurrentVerticalTab = VerticalTab.Dyes;
		public void SwitchToClothesMode() => CurrentVerticalTab = VerticalTab.Clothes;
		public override void Draw() {
			if (this.Collapsed == false) this.Collapsed = null; // restore collapsed state after uncollapse


			DrawSearchBar();

			var fontSize = ImGui.GetFontSize();

			ImGui.BeginGroup();
			// ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
			// ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
			// ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, Vector2.Zero);

			var buttonSize = new Vector2(
					fontSize * 1.25f,
					(
					ImGui.GetContentRegionAvail().Y
					// - ImGui.GetStyle().FramePadding.Y
					- ImGui.GetStyle().ItemSpacing.Y
					) / 2f
				);

			foreach (var vB in Enum.GetValues<VerticalTab>()) {
				if(vB == CurrentVerticalTab) ImGui.PushStyleColor(ImGuiCol.Button, ConfigurationManager.Config.DyePickerDye1Or2SelectedBg);
				var isClicked = ImGui.Button($"{vB}", buttonSize);
				if(vB == CurrentVerticalTab) ImGui.PopStyleColor();
				if(isClicked) CurrentVerticalTab = vB;
			}

			// ImGui.PopStyleVar(3);
			ImGui.EndGroup();

			ImGui.SameLine();

			ImGui.BeginGroup();
			switch (CurrentVerticalTab)
			{
				case VerticalTab.Clothes: DrawClothes(); break;
				case VerticalTab.Dyes: DrawDyes(); break;
			}
			ImGui.EndGroup();

		}

		private void DrawSearchBar() {

			ImGui.AlignTextToFramePadding();
			var available = ImGui.GetContentRegionAvail().X;
			float sideBarSize = ConfigurationManager.Config.GearBrowserSideBarSize;

			var isSidebarFitting = available > sideBarSize;
			float searchFrameMult = isSidebarFitting ? 2.5f : 1f;
			if (ConfigurationManager.Config.GearBrowserSideBarHide) isSidebarFitting = false;
			var size = isSidebarFitting ? available - sideBarSize : available;
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding * searchFrameMult);
			ImGui.SetNextItemWidth(size);
			if (ImGui.InputTextWithHint("##SearchByName##GearBrowser", "Search", ref Search, 100))
				RecomputeItems();

			if(isSidebarFitting) ImGui.SameLine();
			ImGui.Text($"{ItemsCount}");
			GuiHelpers.Tooltip($"{ItemsCount} items found with the selected filters");

			if (ConfigurationManager.Config.DebugDisplayModedInTitleBar) {
				ImGui.SameLine();
				var modedItemsCountInApplyCollection = PluginServices.Context.PenumbraModCountInApplyCollection;
				if (modedItemsCountInApplyCollection > 0) {
					ImGui.TextColored(ConfigurationManager.Config.ModdedItemColor, $" {modedItemsCountInApplyCollection}");
					GuiHelpers.Tooltip($"{ItemsCount} modded items are applied in {ConfigurationManager.Config.PenumbraCollectionApply} collection");
				}
			}


			ImGui.SameLine();

			var sidebarShowHideIcon = ConfigurationManager.Config.GearBrowserSideBarHide ? FontAwesomeIcon.Columns : FontAwesomeIcon.Expand;

			var numberOfButtons = 3;

			var spacing = ImGui.GetContentRegionAvail().X

				- GuiHelpers.CalcIconSize(FontAwesomeIcon.ArrowDownUpLock).X // setting icon
				- GuiHelpers.CalcIconSize(sidebarShowHideIcon).X // setting icon
				- GuiHelpers.CalcIconSize(FontAwesomeIcon.Cog).X // setting icon

				- ImGui.GetStyle().ItemSpacing.X * (numberOfButtons -1 ) // * by number of icon -1, cause it's between them
				- ImGui.GetStyle().FramePadding.X * 2 * numberOfButtons; // * by number of icons x2, cause on each sides of the icon

			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing);


			GuiHelpers.IconToggleButton(FontAwesomeIcon.ArrowDownUpLock, ref ConfigurationManager.Config.WindowsHotkeysAllowAfterLoosingFocus, "##EnableKeyOnLostFocus##GearBrowser", "Allow using keybinds when the window is not focused\nThis allows to keep using directionnal keys to browse items while moving the camera");
			ImGui.SameLine();
			if (GuiHelpers.IconButton(sidebarShowHideIcon)) {
				ConfigurationManager.Config.GearBrowserSideBarHide = !ConfigurationManager.Config.GearBrowserSideBarHide;
			}
			ImGui.SameLine();
			if (GuiHelpers.IconButton(FontAwesomeIcon.Cog)) {
				this.Plugin.ToggleConfigUI();
			}

			ImGui.PopStyleVar();

		}

	}
}