using Dresser.Services;

using ImGuiNET;

using System.Numerics;

namespace Dresser.Windows.Components {
	internal class Styler {

		public static Vector4 CollectionColorBackground => ConfigurationManager.Config.CollectionColorBackground;
		public static Vector4 CollectionColorBorder => ConfigurationManager.Config.CollectionColorBorder;
		public static Vector4 CollectionColorScrollbar => ConfigurationManager.Config.CollectionColorScrollbar;
		public static Vector4 ColorIconImageTintDisabled => ConfigurationManager.Config.ColorIconImageTintDisabled;
		public static Vector4 ColorIconImageTintEnabled => ConfigurationManager.Config.ColorIconImageTintEnabled;
		public static Vector4 DiscordColor = new Vector4(86, 98, 246, 255) / 255;

		public static void PushStyleCollection() {
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ItemIcon.IconSize / 5f);
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ItemIcon.IconSize / 8f);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 10 * ConfigurationManager.Config.IconSizeMult);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 3 * ConfigurationManager.Config.IconSizeMult);
			ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 7 * ConfigurationManager.Config.IconSizeMult);
			ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(CollectionColorBackground));
			ImGui.PushStyleColor(ImGuiCol.Border, ImGui.ColorConvertFloat4ToU32(CollectionColorBorder));
			ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, ImGui.ColorConvertFloat4ToU32(CollectionColorScrollbar));


			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10 * ConfigurationManager.Config.IconSizeMult);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, ItemIcon.IconSize / 8f);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 3 * ConfigurationManager.Config.IconSizeMult);
			ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.ColorConvertFloat4ToU32(CollectionColorBackground));
		}
		public static void PopStyleCollection() {
			ImGui.PopStyleColor(4);
			ImGui.PopStyleVar(8);
		}
	}
}
