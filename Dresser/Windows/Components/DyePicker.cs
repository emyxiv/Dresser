using Dalamud.Logging;

using Dresser.Data;
using Dresser.Data.Excel;
using Dresser.Services;
using Dresser.Structs.Dresser;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Dresser.Windows.Components
{
    internal class DyePicker {

		public static readonly IEnumerable<Dye> Dyes = Sheets.GetSheet<Dye>()
			.Where(i => i.IsValid())
			.OrderBy(i => i.Shade).ThenBy(i => i.SubOrder);


		private static string DyeSearch = "";

		public static void CloseDyePicker() => CurrentGear.SlotSelectDye = null;

		private static int DyeLastSubOrder = -1;
		private const int DyePickerWidth = 485;
		public static unsafe void DrawDyePicker(GlamourPlateSlot slot) {
			PopupSelect.HoverPopupWindow(
				PopupSelect.HoverPopupWindowFlags.SearchBar
				| PopupSelect.HoverPopupWindowFlags.TwoDimenssion
				| PopupSelect.HoverPopupWindowFlags.Header
				| PopupSelect.HoverPopupWindowFlags.Grabbable
				| PopupSelect.HoverPopupWindowFlags.StayWhenLoseFocus,
				Dyes,
				(e, input) => e.Where(i => i.Name.Contains(input, StringComparison.OrdinalIgnoreCase)),
				DrawDyePickerHeader,
				DrawDyePickerItem,
				(i) => { // on Select

					PluginServices.ApplyGearChange.ApplyDye(ConfigurationManager.Config.SelectedCurrentPlate, slot,(byte)i.RowId);
					//if (equipObj is WeaponEquip wep) {
					//	wep.Dye = (byte)i.RowId;
					//	Target->Equip((int)slot, wep);
					//} else if (equipObj is ItemEquip item) {
					//	item.Dye = (byte)i.RowId;
					//	Target->Equip(SlotToIndex(slot), item);
					//}
				},
				CloseDyePicker, // on close
				ref DyeSearch,
				$"Item Dyeing",
				"",
				$"##dye_search",
				"Search...", // searchbar hint
				DyePickerWidth, // window width
				12 // number of columns
			);
		}
		private static (bool, bool) DrawDyePickerItem(dynamic i, bool isActive) {
			bool isThisRealNewLine = PopupSelect.HoverPopupWindowIndexKey % PopupSelect.HoverPopupWindowColumns == 0;
			bool isThisANewShade = i.SubOrder == 1;

			if (!isThisRealNewLine && isThisANewShade) {
				// skip some index key if we don't finish the row
				int howManyMissedButtons = 12 - (DyeLastSubOrder % 12);
				PopupSelect.HoverPopupWindowIndexKey += howManyMissedButtons;
			} else if (!isThisRealNewLine && !isThisANewShade)
				ImGui.SameLine();
			if (isThisANewShade)
				ImGui.Spacing();

			DyeLastSubOrder = i.SubOrder;

			// as we previously changed the index key, let's calculate calculate isActive again
			isActive = PopupSelect.HoverPopupWindowIndexKey == PopupSelect.HoverPopupWindowLastSelectedItemKey;

			Vector2 startPos = default;
			if (isActive) {
				startPos = ImGui.GetCursorScreenPos();
			}
			var selecting = false;
			try {
				selecting = ImGui.ColorButton($"{i.Name}##{i.RowId}", i.ColorVector4,ImGuiColorEditFlags.None, ConfigurationManager.Config.DyePickerDyeSize);
			} catch (Exception e) {
				PluginLog.Error(e, "Error in DrawDyePickerItem");
			}
			if (isActive) {
				var draw = ImGui.GetWindowDrawList();
				var endPos = startPos + ConfigurationManager.Config.DyePickerDyeSize;
				var thickness1 = ConfigurationManager.Config.DyePickerDyeSize.X / 10;
				draw.AddRect(
					startPos,
					endPos,
					ImGui.ColorConvertFloat4ToU32(new Vector4(70 / 255f, 133 / 255f, 158 / 255f, 1)),
					ConfigurationManager.Config.DyePickerDyeSize.X / 5,
					ImDrawFlags.None,
					thickness1
					);
				var thickness2 = thickness1 / 2.5f;


				draw.AddRect(
					startPos,
					endPos,
					ImGui.ColorConvertFloat4ToU32(new Vector4(192 / 255f, 233 / 255f, 237 / 255f, 1)),
					ConfigurationManager.Config.DyePickerDyeSize.X / 5,
					ImDrawFlags.None,
					thickness2
					);

			}

			return (selecting, ImGui.IsItemFocused());
		}

		private static void DrawDyePickerHeader(dynamic i) {
			// TODO: configuration to not show this
			var textSize = ImGui.CalcTextSize(i.Name);
			//float dyeShowcaseWidth = (DyePickerWidth - textSize.X - (ImGui.GetStyle().ItemSpacing.X * 2)) / 2;
			//ImGui.ColorButton($"{i.Name}##{i.RowId}##selected1", i.ColorVector4, ImGuiColorEditFlags.None, new Vector2(dyeShowcaseWidth, textSize.Y));
			ImGui.ColorButton($"{i.Name}##{i.RowId}##selected1", i.ColorVector4, ImGuiColorEditFlags.None, ConfigurationManager.Config.DyePickerDyeSize);
			ImGui.SameLine();
			ImGui.Text(i.Name);
			//ImGui.SameLine();
			//ImGui.ColorButton($"{i.Name}##{i.RowId}##selected2", i.ColorVector4, ImGuiColorEditFlags.None, new Vector2(dyeShowcaseWidth, textSize.Y));
		}
	}
}
