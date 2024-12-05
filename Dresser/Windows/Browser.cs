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


			ImGui.BeginGroup();
			DrawVerticalTab();
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

		private void DrawVerticalTab() {

			var buttonSize = new Vector2(
				ItemIcon.IconSize.X * 0.33f,
				(
				ImGui.GetContentRegionAvail().Y
				// - ImGui.GetStyle().FramePadding.Y
				- ImGui.GetStyle().ItemSpacing.Y
				) / 2f
			);

			var rounding = ItemIcon.IconSize.X * 0.1f;

			foreach (var vB in Enum.GetValues<VerticalTab>()) {
				var label = $"##{vB}##VerticalTab#Browser";
				var isHovered = GuiHelpers.IsHovered(label);
				var isActive = vB == CurrentVerticalTab;
				var pos = ImGui.GetCursorScreenPos();

				// draw the tab
				var isClicked = GuiHelpers.ButtonDrawList(label, buttonSize, ConfigurationManager.Config.BrowserVerticalTabButtonsBg, ConfigurationManager.Config.DyePickerDye1Or2SelectedBg, rounding, isActive);
				var hovered = ImGui.IsItemHovered();
				GuiHelpers.Hovering(label,hovered);

				// draw the icon
				var colorIcon = Vector4.One;
				if(!isHovered) colorIcon = colorIcon.WithAlpha(0.8f);
				var iconUld =  vB switch {
					VerticalTab.Clothes => UldBundle.ArmouryBoard_ChestPiece,
					VerticalTab.Dyes => UldBundle.ColorantToggleButton_DyeIndicatorActive,
				};

				var iconTexWrap = PluginServices.ImageGuiCrop.GetPart(iconUld);
				if (iconTexWrap != null) ImGui.GetWindowDrawList().AddImage(
					iconTexWrap.ImGuiHandle,
					pos,
					pos + new Vector2(buttonSize.X, buttonSize.X * ((float)iconTexWrap.Width / iconTexWrap.Height)),
					Vector2.Zero,
					Vector2.One,
					ImGui.ColorConvertFloat4ToU32(colorIcon));

				// change tab if clicked
				if(isClicked) CurrentVerticalTab = vB;
			}

		}

		private static string Search = "";
		private static int JustErasedSearch = 0;
		public static string SearchText() => JustErasedSearch>1?"":Search;
		public static void EraseSearchText() {
			Search = "";
			JustErasedSearch = 2;
		}
		private void DrawSearchBar() {

			ImGui.AlignTextToFramePadding();
			var available = ImGui.GetContentRegionAvail().X;
			// float searchFrameMult = isSidebarFitting ? 2.5f : 1f;
			float searchFrameMult = 2.5f;

			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ImGui.GetStyle().FramePadding * searchFrameMult);


			// calculate right size icon size
			var numberOfButtons = 3;
			var sidebarShowHideIcon = ConfigurationManager.Config.GearBrowserSideBarHide ? FontAwesomeIcon.Columns : FontAwesomeIcon.Expand;
			var rightButtonWidth = 0
				+ GuiHelpers.CalcIconSize(FontAwesomeIcon.ArrowDownUpLock).X // setting icon
				+ GuiHelpers.CalcIconSize(sidebarShowHideIcon).X // setting icon
				+ GuiHelpers.CalcIconSize(FontAwesomeIcon.Cog).X // setting icon

				+ ImGui.GetStyle().ItemSpacing.X * (numberOfButtons -1 ) // * by number of icon -1, cause it's between them
				+ ImGui.GetStyle().FramePadding.X * 2 * numberOfButtons; // * by number of icons x2, cause on each sides of the icon

			// the buttons on the left will only be displayed if they fit + 4 characters font size for the search bar
			var isSidebarFitting = available > rightButtonWidth + (ImGui.GetFontSize() * 4);
			if (ConfigurationManager.Config.GearBrowserSideBarHide) isSidebarFitting = false;


			float sizeSearchBar = isSidebarFitting ? (available - rightButtonWidth - ImGui.GetStyle().ItemSpacing.X) : available;
			var posInfoSearchInitial = ImGui.GetCursorScreenPos() + new Vector2(sizeSearchBar,ImGui.GetStyle().FramePadding.Y * 0.05f);


			ImGui.SetNextItemWidth(sizeSearchBar);
			if (ImGui.InputTextWithHint("##SearchByName##GearBrowser", "Search", ref Search, 100) && JustErasedSearch == 0) {
				RecomputeItems();
			} else if (JustErasedSearch > 1) {
				// on first frame after reset, focus somewhere else because if we stay focused, the search content goes back to before erase ...
				ImGui.SetKeyboardFocusHere(2);
				Search = "";
				JustErasedSearch = 1;
			} else if (JustErasedSearch == 1) {
				// on second frame after erase, focus the textbox again
				ImGui.SetKeyboardFocusHere(-1);
				JustErasedSearch = 0;
			}

			// draw searchbar info bits
			var fontHandle = GuiHelpers.FontHandle(GuiHelpers.Font.Title);
			fontHandle.Push();
			try {
				DrawInfoSearchBar(posInfoSearchInitial, 0.4f);
			} finally {
				fontHandle.Pop();
			}

			if(isSidebarFitting) ImGui.SameLine();

			var spacing = ImGui.GetContentRegionAvail().X - rightButtonWidth;

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
		private Vector2 DrawInfoSearchBar(Vector2 posInfoSearchInitial, float darkenAmount) {

			Vector2 newPos = CurrentVerticalTab switch {
				VerticalTab.Clothes => DrawInfoSearchBarClothes(posInfoSearchInitial, darkenAmount),
				VerticalTab.Dyes => DrawInfoSearchBarDyes(posInfoSearchInitial, darkenAmount),
				var _ => posInfoSearchInitial
			};

			var fontHandle = GuiHelpers.FontHandle(GuiHelpers.Font.Icon);

			var eraserLabel = "##Eraser##Browser";
			fontHandle.Push();
			var eraserText = FontAwesomeIcon.Backspace.ToIconString();

			var eraserSize = ImGui.CalcTextSize(eraserText);
			var eraserPos = newPos - new Vector2(eraserSize.X + (ImGui.GetStyle().ItemSpacing.X * 1), -ImGui.GetStyle().FramePadding.Y);
			var isHovered = GuiHelpers.IsHovered(eraserLabel);

			ImGui.GetWindowDrawList().AddText(eraserPos, ImGui.ColorConvertFloat4ToU32(Vector4.One.WithAlpha(isHovered?1f:0.8f)), eraserText);



			fontHandle.Pop();


			var isHovering = ImGui.IsMouseHoveringRect(eraserPos, eraserPos + eraserSize);
			GuiHelpers.Hovering(eraserLabel, isHovering);

			var clickedEraser = isHovering && ImGui.IsItemClicked();

			if (clickedEraser) {
				PluginLog.Debug("Erase text in searchbar");
				EraseSearchText();
				RecomputeItems();
			}


			return newPos;
		}

	}
}