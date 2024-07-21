using Dalamud.Interface;
using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Utility;

using Dresser.Services;

using ImGuiNET;

using System;
using System.Collections.Generic;
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
			Icon, // not game font (fontawesome)
			Mono,

			Title,
			Config,
			Radio,
			Task,
			BubblePlateNumber,

			// alias
			None = Default,
		}
		public static Dictionary<Font, GameFontFamilyAndSize> FontGameFont = new() {
			{Font.Radio,GameFontFamilyAndSize.Axis36 },
			{Font.Title,GameFontFamilyAndSize.TrumpGothic23 },
			{Font.Config,GameFontFamilyAndSize.TrumpGothic23 }, // FontConfigHeaders
			{Font.Task,GameFontFamilyAndSize.TrumpGothic23 },
			{Font.BubblePlateNumber,GameFontFamilyAndSize.TrumpGothic23 },
		};
		public static void TextWithFont(string text, Font font) {
			var fontHandle = FontHandle(font,null,text);
			fontHandle.Push();
			ImGui.Text(text);
			fontHandle.Pop();
		}

		private static IFontHandle FontHandle(Font font, float? size = null,string someText = "") {
			if(font == Font.Icon) return PluginServices.PluginInterface.UiBuilder.IconFontHandle;
			if(font == Font.Mono) return PluginServices.PluginInterface.UiBuilder.MonoFontHandle;


			if (PluginServices.Storage.FontHandles.TryGetValue(font, out var handlePair)) {

				if(handlePair.handle == null) {
					// if borken handle ? remove it (should never happen)
					PluginServices.Storage.FontHandles.Remove(font);
				} else if (handlePair.size == (size ?? 0f)) {
					// if no size change, use it
					return handlePair.handle;
				}
			}

			return SetFont(font,size ?? 0f) ?? PluginServices.PluginInterface.UiBuilder.DefaultFontHandle;
		}

		private static IFontHandle? SetFont(Font font, float? size = null) {
			if (!FontGameFont.TryGetValue(font, out var gameFont)) return null;

			var fontStyle = new GameFontStyle(gameFont);
			if((size?? 0f) > 0f) fontStyle.SizePx = (size ?? 0f) * 0.63f;

			var newFontHandle = PluginServices.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(fontStyle);
			PluginServices.Storage.FontHandles[font] = (newFontHandle, size ?? 0f);
			return newFontHandle;
		}

		public static void TextWithFontDrawlist(string text, Font font, Vector4? color = null, float? size = null, Vector2? offset = null) {
			var prevPos = ImGui.GetCursorPos();

			if(offset.HasValue) {
				ImGui.SetCursorPos(prevPos + offset.Value);
			}
			var fontHandle = FontHandle(font, size,text);
			fontHandle.Push();
			if(color.HasValue) ImGui.TextColored(color.Value, text);
			else ImGui.Text(text);
			fontHandle.Pop();
			ImGui.SetCursorPos(prevPos);


			//ImGui.GetWindowDrawList().AddText(
			//	FontToImFontPtr(font),
			//	size,
			//	ImGui.GetCursorScreenPos(),
			//	ImGui.ColorConvertFloat4ToU32(color ?? ImGui.GetStyle().Colors[(int)ImGuiCol.Text]),
			//	text);
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
				ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
				action();
				ImGui.EndTooltip();
			} else
				AnyItemTooltiping = false;
		}

	}
}
