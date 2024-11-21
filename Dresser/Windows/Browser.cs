using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using AllaganLib.GameSheets.Sheets.Rows;

using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Dresser;
using Dresser.Windows.Components;

using ImGuiNET;

using InventoryItem = Dresser.Structs.Dresser.InventoryItem;

using static Dresser.Services.Storage;

namespace Dresser.Windows {
	public partial class GearBrowser : Window, IWindowWithHotkey, IDisposable {
		private Plugin Plugin;

		public GearBrowser(Plugin plugin) : base(
			"Gear Browser", ImGuiWindowFlags.None) {
			this.SizeConstraints = new WindowSizeConstraints {
				MinimumSize = new Vector2(ImGui.GetFontSize() * 4),
				MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
			};
			this.Plugin = plugin;
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

		public override void Draw() {
			if (this.Collapsed == false) this.Collapsed = null; // restore collapsed state after uncollapse


			DrawSearchBar();

			DrawClothes();
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