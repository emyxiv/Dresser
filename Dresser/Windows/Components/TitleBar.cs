using Dresser.Services;

using ImGuiNET;

using System.Numerics;

namespace Dresser.Windows.Components {
	internal class TitleBar {

		private static float HeaderBarThickness = 5;
		private static float HeaderFontSize = 80;

		public static ImDrawListPtr Draw() {
			var draw = ImGui.GetWindowDrawList();

			draw.AddText(
				PluginServices.Storage.FontTitle.ImFont,
				HeaderFontSize * ConfigurationManager.Config.IconSizeMult,
				ImGui.GetCursorScreenPos() + new Vector2(0, -10 * ConfigurationManager.Config.IconSizeMult),
				ImGui.ColorConvertFloat4ToU32(CurrentGear.CollectionColorTitle),
				"Plate Creation");

			var avail = ImGui.GetContentRegionAvail();
			var start = ImGui.GetCursorScreenPos() + new Vector2(0, ConfigurationManager.Config.IconSizeMult * (HeaderFontSize * 0.75f));
			var end = start + new Vector2(avail.X, 0);

			draw.AddLine(start, end, ImGui.ColorConvertFloat4ToU32(CurrentGear.CollectionColorTitle), HeaderBarThickness * ConfigurationManager.Config.IconSizeMult);
			ImGui.SetCursorScreenPos(start + new Vector2(0, HeaderBarThickness));
			ImGui.Spacing();
			return draw;
		}
	}
}
