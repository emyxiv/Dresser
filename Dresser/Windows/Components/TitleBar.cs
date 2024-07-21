using Dalamud.Interface.Windowing;

using Dresser.Services;

using ImGuiNET;

using System.Collections.Generic;
using System.Numerics;

namespace Dresser.Windows.Components {
	internal class TitleBar {

		private static float HeaderBarThickness = 5;
		private static float HeaderFontSize = 80;

		private static Dictionary<Window, bool> IsHoveringCloseButton = new();

		public static ImDrawListPtr Draw(Window window) {
			var draw = ImGui.GetWindowDrawList();

			var collectionColorTitleV4 = ConfigurationManager.Config.PlateSelectorColorTitle;
			var collectionColorTitleU32 = ImGui.ColorConvertFloat4ToU32(collectionColorTitleV4);
			var hoverTextColor = ImGui.ColorConvertFloat4ToU32(GuiHelpers.ColorAddHSV(Styler.CollectionColorBackground * new Vector4(1, 1, 1, 0) + new Vector4(0.2f, 0.2f, 0.2f, 1),0, 0.2f,0.2f));

			GuiHelpers.TextWithFontDrawlist(
				"Plate Creation",
				GuiHelpers.Font.Title,
				collectionColorTitleV4,
				HeaderFontSize * ConfigurationManager.Config.IconSizeMult,
				new Vector2(0, -10 * ConfigurationManager.Config.IconSizeMult));

			var avail = ImGui.GetContentRegionAvail();
			var start = ImGui.GetCursorScreenPos() + new Vector2(0, ConfigurationManager.Config.IconSizeMult * (HeaderFontSize * 0.75f));
			var end = start + new Vector2(avail.X, 0);

			draw.AddLine(start, end, collectionColorTitleU32, HeaderBarThickness * ConfigurationManager.Config.IconSizeMult);


			var closeButtonSize = Vector2.One * 40 * ConfigurationManager.Config.IconSizeMult;
			var closeButtonPosBotRight = end - new Vector2(ConfigurationManager.Config.IconSizeMult * (HeaderFontSize * 0.15f));
			var closeButtonPosBotLeft = closeButtonPosBotRight - new Vector2(closeButtonSize.X, 0);
			var closeButtonPosTopRight = closeButtonPosBotRight - new Vector2(0, closeButtonSize.Y);
			var closeButtonPosTopLeft = closeButtonPosBotLeft - new Vector2(0, closeButtonSize.Y);

			IsHoveringCloseButton.TryGetValue(window, out bool isHoveringClose);
			var closeButtonColor = isHoveringClose ? hoverTextColor : collectionColorTitleU32;

			draw.AddLine(closeButtonPosTopLeft, closeButtonPosBotRight, closeButtonColor, HeaderBarThickness * ConfigurationManager.Config.IconSizeMult);
			draw.AddLine(closeButtonPosTopRight, closeButtonPosBotLeft, closeButtonColor, HeaderBarThickness * ConfigurationManager.Config.IconSizeMult);


			ImGui.SetCursorScreenPos(closeButtonPosTopLeft);
			if (ImGui.InvisibleButton($"CloseButton##{window.WindowName}", closeButtonSize)) {
				window.IsOpen = false;
			}
			IsHoveringCloseButton[window] = ImGui.IsItemHovered();


			ImGui.SetCursorScreenPos(start + new Vector2(0, HeaderBarThickness));
			ImGui.Spacing();
			return draw;
		}
	}
}
