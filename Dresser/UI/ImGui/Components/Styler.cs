using Dresser.Core;
using Dresser.Services;

using Dalamud.Bindings.ImGui;

using System.Numerics;

namespace Dresser.Gui.Components {
	internal class Styler {


		
		public static float IconSizeMultiplier => ConfigurationManager.Config.IconSizeMult;
		public static Vector2 IconSizeMultiplier2D => new(ConfigurationManager.Config.IconSizeMult);

		// Variables
		public static float FactorFramePadding => ConfigurationManager.Config.StyleVariableFramePadding;
		public static float FactorItemSpacing => ConfigurationManager.Config.StyleVariableItemSpacing;
		public static float FactorFrameRounding => ConfigurationManager.Config.StyleVariableFrameRounding;
		public static float FactorFrameBorderSize => ConfigurationManager.Config.StyleVariableFrameBorderSize;
		public static float FactorScrollbarSize => ConfigurationManager.Config.StyleVariableScrollbarSize;
		public static float FactorWindowRounding => ConfigurationManager.Config.StyleVariableWindowRounding;
		public static float FactorWindowPadding => ConfigurationManager.Config.StyleVariableWindowPadding;
		public static float FactorWindowBorderSize => ConfigurationManager.Config.StyleVariableWindowBorderSize;

		// Colors
		public static Vector4 CollectionColorBackground => ConfigurationManager.Config.CollectionColorBackground;
		public static Vector4 CollectionColorBorder => ConfigurationManager.Config.CollectionColorBorder;
		public static Vector4 CollectionColorScrollbar => ConfigurationManager.Config.CollectionColorScrollbar;
		public static Vector4 ColorIconImageTintDisabled => ConfigurationManager.Config.ColorIconImageTintDisabled;
		public static Vector4 ColorIconImageTintEnabled => ConfigurationManager.Config.ColorIconImageTintEnabled;
		public static Vector4 DiscordColor = new Vector4(86, 98, 246, 255) / 255;

		public static Vector4 FilterIndicatorFrameColor => ConfigurationManager.Config.ColorFilteredIndicator * new Vector4(new Vector3(0.70f), 1f);
		public static Vector4 FilterIndicatorFrameHoveredColor => ConfigurationManager.Config.ColorFilteredIndicator * new Vector4(new Vector3(0.85f), 1f);
		public static Vector4 FilterIndicatorFrameActiveColor => ConfigurationManager.Config.ColorFilteredIndicator;


		private static bool SavedEnableCustomTheme = ConfigurationManager.Config.EnableCustomTheme;
		public static void PushStyleCollection() {
			// save SavedEnableCustomTheme, we don't want to disable/enable it in the middle of a Draw
			SavedEnableCustomTheme = ConfigurationManager.Config.EnableCustomTheme;
			if(!SavedEnableCustomTheme) return;

			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,     FactorFramePadding    * IconSizeMultiplier2D);
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,      FactorItemSpacing     * IconSizeMultiplier2D);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,    FactorFrameRounding   * IconSizeMultiplier);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize,  FactorFrameBorderSize * IconSizeMultiplier);
			ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize,    FactorScrollbarSize   * IconSizeMultiplier);
			ImGui.PushStyleColor(ImGuiCol.FrameBg, CollectionColorBackground);
			ImGui.PushStyleColor(ImGuiCol.Border, CollectionColorBorder);
			ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, CollectionColorScrollbar);
			ImGui.PushStyleColor(ImGuiCol.FrameBgActive, CollectionColorBackground);


			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding,   FactorWindowRounding   * IconSizeMultiplier);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding,    FactorWindowPadding    * IconSizeMultiplier2D);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, FactorWindowBorderSize * IconSizeMultiplier);
			ImGui.PushStyleColor(ImGuiCol.WindowBg, CollectionColorBackground);
		}
		public static void PopStyleCollection() {
			if(!SavedEnableCustomTheme) return;

			ImGui.PopStyleColor(5);
			ImGui.PopStyleVar(8);
		}



		// Sizes
		public static float BigButtonRounding = ItemIcon.IconSize.X * 0.1f;
		public static float BigButtonBorderThickness = ItemIcon.IconSize.X * 0.02f;

	}
}
