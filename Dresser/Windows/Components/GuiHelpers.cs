using System;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Utility;
using ImGuiNET;

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
