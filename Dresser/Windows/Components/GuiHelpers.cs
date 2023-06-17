using Dalamud.Interface;
using Dalamud.Interface.GameFonts;
using Dalamud.Utility;

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
		public static bool IconButtonNoBg(FontAwesomeIcon icon, string hiddenLabel, string tooltip = "", Vector2 size = default) {
			IconButtonNoBgHovers.TryGetValue(hiddenLabel, out bool wasHovered);

			if (wasHovered) ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text] * hoveredAlpha);
			ImGui.PushStyleColor(ImGuiCol.Button, invisible);
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, invisible);
			ImGui.PushStyleColor(ImGuiCol.ButtonActive, invisible);
			bool accepting = IconButton(icon, size, hiddenLabel);
			ImGui.PopStyleColor(3);
			if (wasHovered) ImGui.PopStyleColor();

			IconButtonNoBgHovers[hiddenLabel] = ImGui.IsItemHovered();
			Tooltip(tooltip);
			return accepting;
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
				ImGui.PushStyleColor(ImGuiCol.Button, color.Value);
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
			Axis_36,
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
			ImFontPtr fontPtr = font switch {
				//Font.Title => PluginServices.Storage.FontTitle.ImFont,
				Font.Radio => PluginServices.Storage.FontRadio.ImFont,
				Font.TrumpGothic_68 => PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic68)).ImFont,
				Font.TrumpGothic_184 => PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic184)).ImFont,
				Font.TrumpGothic_23 => PluginServices.Storage.FontConfigHeaders.ImFont,
				Font.TrumpGothic_34 => PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic34)).ImFont,
				Font.Icon => UiBuilder.IconFont,
				_ => UiBuilder.DefaultFont,
			};

			ImGui.PushFont(fontPtr);
			ImGui.Text(text);
			ImGui.PopFont();
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


		public static void Tooltip(string text) {
			if (!text.IsNullOrWhitespace() && ImGui.IsItemHovered()) {
				ImGui.BeginTooltip();
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				ImGui.TextUnformatted(text);
				ImGui.PopTextWrapPos();
				ImGui.EndTooltip();
			}
		}
		public static void Tooltip(Action action) {
			if (ImGui.IsItemHovered()) {
				ImGui.BeginTooltip();
				action();
				ImGui.EndTooltip();
			}
		}
	}
}
