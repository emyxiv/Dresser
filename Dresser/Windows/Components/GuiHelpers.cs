using Dalamud.Interface;
using Dalamud.Interface.GameFonts;
using Dalamud.Utility;

using Dresser.Services;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Dresser.Windows.Components {
	internal class GuiHelpers {
		public static bool IconButtonHoldConfirm(FontAwesomeIcon icon, string tooltip, bool isHoldingKey, Vector2 size = default, string hiddenLabel = "") {
			if (!isHoldingKey) ImGui.PushStyleVar(ImGuiStyleVar.Alpha, ImGui.GetStyle().DisabledAlpha);
			bool accepting = IconButton(icon, size, hiddenLabel);
			if (!isHoldingKey) ImGui.PopStyleVar();

			Tooltip(tooltip);

			return accepting && isHoldingKey;
		}
		public static bool IconButtonHoldConfirm(FontAwesomeIcon icon, string tooltip, Vector2 size = default, string hiddenLabel = "") =>
			IconButtonHoldConfirm(icon, tooltip, ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift, size, hiddenLabel);


		public static bool IconButtonTooltip(FontAwesomeIcon icon, string tooltip, Vector2 size = default, string hiddenLabel = "") {
			bool accepting = IconButton(icon, size, hiddenLabel);
			Tooltip(tooltip);
			return accepting;
		}
		public static bool IconButton(FontAwesomeIcon icon, Vector2 size = default, string hiddenLabel = "") {
			ImGui.PushFont(UiBuilder.IconFont);
			bool accepting = ImGui.Button((icon.ToIconString() ?? "") + "##" + hiddenLabel, size);
			ImGui.PopFont();
			return accepting;
		}


		private static readonly Vector4 invisible = new(1, 1, 1, 0);
		private static readonly Vector4 hoveredAlpha = new(1, 1, 1, 0.7f);
		private static readonly Dictionary<string, bool> IconButtonNoBgHovers = new();
		public static bool IconButtonNoBg(FontAwesomeIcon icon, string hiddenLabel, string tooltip = "", Vector2 size = default, Vector4? textColor = null) {
			return ButtonNoBg((label,h) => IconButton(icon, size, label), hiddenLabel, tooltip, textColor);
		}
		public static bool GameButton(UldBundle cropItemId, string hiddenLabel, string tooltip = "", Vector2 size = default, Vector4? color = null) {
			var tint = color?? Vector4.One;
			return ButtonNoBg((label, hovered) => {
				var z = PluginServices.ImageGuiCrop.GetPart(cropItemId);
				if (z == null) return false;
				var pos = ImGui.GetCursorScreenPos();
				ImGui.GetWindowDrawList().AddImage(z.ImGuiHandle, pos, pos + size, Vector2.Zero, Vector2.One,ImGui.ColorConvertFloat4ToU32(hovered ? tint : tint * 0.9f));
				return ImGui.InvisibleButton(hiddenLabel, size);
			}, hiddenLabel, tooltip, Vector4.Zero);
		}
		public static bool GameButtonCircleToggle(UldBundle cropCircleBUttonItemId, ref bool value, string hiddenLabel, string tooltip = "", Vector2 size = default) {

			if (value) {
				var z = PluginServices.ImageGuiCrop.GetPart(UldBundle.CircleLargeHighlight);
				if (z != null) {
					var pos = ImGui.GetCursorScreenPos();
					ImGui.GetWindowDrawList().AddImage(z.ImGuiHandle, pos, pos + size);
				}
			}
			var accepting = GameButton(cropCircleBUttonItemId, hiddenLabel, tooltip, size);
			if (accepting) {
				value = !value;
			}

			return accepting;
		}

		public static bool ButtonNoBg(Func<string,bool, bool> buttonFunc, string hiddenLabel, string tooltip = "", Vector4? textColor = null) {
			IconButtonNoBgHovers.TryGetValue(hiddenLabel, out bool wasHovered);
			if(textColor.HasValue) ImGui.PushStyleColor(ImGuiCol.Text, textColor.Value);
			if (wasHovered) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] * hoveredAlpha);
			ImGui.PushStyleColor(ImGuiCol.Button, invisible);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, invisible);
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, invisible);
			bool accepting = buttonFunc.Invoke(hiddenLabel, wasHovered);
			ImGui.PopStyleColor(3);
			if (wasHovered) ImGui.PopStyleColor();
			if (textColor.HasValue) ImGui.PopStyleColor();

			IconButtonNoBgHovers[hiddenLabel] = ImGui.IsItemHovered();
			Tooltip(tooltip);
			return accepting;
		}
		public static bool IconToggleButtonNoBg(FontAwesomeIcon icon, ref bool valueToggled, string hiddenLabel, string tooltip = "", Vector2 size = default) {
			var textColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
			if (!valueToggled) textColor *= new Vector4(1, 1, 1, 0.5f);

			var toggled = IconButtonNoBg(icon, hiddenLabel, tooltip, size, textColor);
			if (toggled) valueToggled = !valueToggled;
			return toggled;
		}



		public static bool TextButtonTooltip(string label, string tooltip, Vector2 size = default) {
			bool accepting = ImGui.Button(label, size);
			Tooltip(tooltip);
			return accepting;
		}
		public static void TextTooltip(string label, string tooltip) {
			ImGui.Text(label);
			Tooltip(tooltip);
		}
		public static void TextDisabledTooltip(string label, string tooltip) {
			ImGui.TextDisabled(label);
			Tooltip(tooltip);
		}

		public static void Icon(FontAwesomeIcon icon, bool enabled = true, Vector4? color = null) {
			string iconText = icon.ToIconString() ?? "";
			int num = 0;
			if (color.HasValue) {
				ImGui.PushStyleColor(ImGuiCol.Text, color.Value);
				num++;
			}

			ImGui.PushFont(UiBuilder.IconFont);
			if (enabled) ImGui.Text(iconText);
			else ImGui.TextDisabled(iconText);
			ImGui.PopFont();
			if (num > 0) {
				ImGui.PopStyleColor(num);
			}
		}
		public enum Font {
			Default,
			Icon,
			Axis_12,
			Axis_14,
			Axis_18,
			Axis_36,
			Axis_96,
			TrumpGothic_184,
			TrumpGothic_23,
			TrumpGothic_34,
			TrumpGothic_68,

			// alias
			None = Default,
			Title = TrumpGothic_68,
			Radio = Axis_36,
		}
		public static void TextWithFont(string text, Font font) {
			ImGui.PushFont(FontToImFontPtr(font));
			ImGui.Text(text);
			ImGui.PopFont();
		}

		private static ImFontPtr FontToImFontPtr(Font font) {
			return font switch {
				//Font.Title => PluginServices.Storage.FontTitle.ImFont,
				Font.Radio => PluginServices.Storage.FontRadio.ImFont,
				Font.Axis_12 => PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.Axis12)).ImFont,
				Font.Axis_14 => PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.Axis14)).ImFont,
				Font.Axis_18 => PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.Axis18)).ImFont,
				Font.Axis_96 => PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.Axis96)).ImFont,
				Font.TrumpGothic_68 => PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic68)).ImFont,
				Font.TrumpGothic_184 => PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic184)).ImFont,
				Font.TrumpGothic_23 => PluginServices.Storage.FontConfigHeaders.ImFont,
				Font.TrumpGothic_34 => PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic34)).ImFont,
				Font.Icon => UiBuilder.IconFont,
				_ => UiBuilder.DefaultFont,
			};
		}
		public static void TextWithFontDrawlist(string text, Font font, Vector4? color = null, float size = 1.0f) {
			ImGui.GetWindowDrawList().AddText(
				FontToImFontPtr(font),
				size,
				ImGui.GetCursorScreenPos(),
				ImGui.ColorConvertFloat4ToU32(color ?? ImGui.GetStyle().Colors[(int)ImGuiCol.Text]),
				text);
		}
		public static void TextRight(string text, float offset = 0) {
			// Careful: use of ImGui.GetContentRegionAvail().X without - WidthMargin()
			offset = ImGui.GetContentRegionAvail().X - offset - ImGui.CalcTextSize(text).X - ImGui.GetStyle().ItemSpacing.X ;
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
			ImGui.TextUnformatted(text);
		}
		public static void TextCenter(string text, float offset = 0) {
			// Careful: use of ImGui.GetContentRegionAvail().X without - WidthMargin()
			offset = ImGui.GetContentRegionAvail().X * 0.5f - offset - ImGui.CalcTextSize(text).X * 0.5f;
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
			ImGui.TextUnformatted(text);
		}
		public static Vector2 CalcIconSize(FontAwesomeIcon icon) {
			ImGui.PushFont(UiBuilder.IconFont);
			var size = ImGui.CalcTextSize(icon.ToIconString());
			ImGui.PopFont();
			return size;
		}
		public static void IconTooltip(FontAwesomeIcon icon, string tooltip, bool enabled = true, Vector4? color = null) {
			Icon(icon, enabled, color);
			Tooltip(tooltip);
		}
		public static Vector4 ColorAddHSV(Vector4 color, float h_add, float s_add, float v_add) {
			ImGui.ColorConvertRGBtoHSV(color.X, color.Y, color.Z, out var h, out var s, out var v);
			h += h_add;
			s += s_add;
			v += v_add;
			ImGui.ColorConvertHSVtoRGB(h, s, v, out var r, out var g, out var b);
			return new(r, g, b, color.W);
		}


		public static bool AnyItemTooltiping = false;
		public static void Tooltip(string text) {
			if (!AnyItemTooltiping && !text.IsNullOrWhitespace() && ImGui.IsItemHovered()) {
				AnyItemTooltiping = true;
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				ImGui.TextUnformatted(text);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			} else
				AnyItemTooltiping = false;
		}
		public static void Tooltip(Action action) {
			if (!AnyItemTooltiping && ImGui.IsItemHovered()) {
				AnyItemTooltiping = true;
				ImGui.BeginTooltip();
				action();
				ImGui.EndTooltip();
			} else
				AnyItemTooltiping = false;
		}

	}
}
