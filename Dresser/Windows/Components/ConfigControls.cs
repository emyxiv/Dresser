using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Services;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Dresser.Windows.Components {
	internal class ConfigControls {
		public static void ConfigColorVecot4(string propertyName, string label, string description = "", ImGuiColorEditFlags imguiColorEditFlag = ImGuiColorEditFlags.None) {

			var fieldInfo = typeof(Configuration).GetField(propertyName);
			if (fieldInfo == null) return;
			var colGet = fieldInfo?.GetValue(ConfigurationManager.Config);
			if (colGet == null || colGet.GetType() != typeof(Vector4)) return;
			var color = (Vector4)colGet;

			color = ImGuiComponents.ColorPickerWithPalette(propertyName.GetHashCode(), label, color, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaPreviewHalf | imguiColorEditFlag);
			ImGui.SameLine();
			GuiHelpers.TextTooltip(label, description);
			ImGui.SameLine();
			if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.Undo, $"{label}##Delay 3##ColorConfig##ConfigWindow", "Reset to default value")) {
				var colorDefaultObj = fieldInfo?.GetValue(ConfigurationManager.Default);
				if (colorDefaultObj != null && colorDefaultObj.GetType() == typeof(Vector4)) {
					color = (Vector4)colorDefaultObj;
				}
			}

			fieldInfo?.SetValue(ConfigurationManager.Config, color);
		}
		public static bool ConfigFloatFromTo(string propertyName, string label, out bool filterActiveAfter)
			=>	ConfigFloatFromTo(propertyName, label, "", out filterActiveAfter);
		public static bool ConfigFloatFromTo(string propertyName, string label)
			=>	ConfigFloatFromTo(propertyName, label, "", out _);
		public static bool ConfigFloatFromTo(string propertyName, string label, string description, out bool filterActiveAfter) {
			filterActiveAfter = false;

			// get current value
			var fieldInfo = typeof(Configuration).GetField(propertyName);
			if (fieldInfo == null) return false;
			var valueGet = fieldInfo?.GetValue(ConfigurationManager.Config);
			if (valueGet == null || valueGet.GetType() != typeof(Vector2)) return false;
			var value = (Vector2)valueGet;

			// get default value
			var valueDefaultObj = fieldInfo?.GetValue(ConfigurationManager.Default);
			if (valueDefaultObj == null || valueDefaultObj.GetType() != typeof(Vector2)) return false;
			var valueDefault = (Vector2)valueDefaultObj;

			var numberInputFrameWidth = ImGui.GetFontSize() * 2;

			// From
			// ----
			var isActiveFilter_X = value.X > valueDefault.X;
			filterActiveAfter |= isActiveFilter_X;
			if (isActiveFilter_X) {
				ImGui.PushStyleColor(ImGuiCol.FrameBg, Styler.FilterIndicatorFrameColor);
				ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, Styler.FilterIndicatorFrameHoveredColor);
				ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Styler.FilterIndicatorFrameActiveColor);
			}

			ImGui.SetNextItemWidth(numberInputFrameWidth);
			var isChangedX = ImGui.DragFloat($"##X##{propertyName}##{label}", ref value.X, 1, valueDefault.X, valueDefault.Y, "%.0f", ImGuiSliderFlags.AlwaysClamp);
			if (isActiveFilter_X) ImGui.PopStyleColor(3);

			ImGui.SameLine();
			ImGui.TextUnformatted("-");
			ImGui.SameLine();

			// To
			// --
			var isActiveFilter_Y = value.Y < valueDefault.Y;
			filterActiveAfter |= isActiveFilter_Y;
			if (isActiveFilter_Y) {
				ImGui.PushStyleColor(ImGuiCol.FrameBg, Styler.FilterIndicatorFrameColor);
				ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, Styler.FilterIndicatorFrameHoveredColor);
				ImGui.PushStyleColor(ImGuiCol.FrameBgActive, Styler.FilterIndicatorFrameActiveColor);
			}

			ImGui.SetNextItemWidth(numberInputFrameWidth);
			var isChangedY = ImGui.DragFloat($"##Y##{propertyName}##{label}", ref value.Y, 1, valueDefault.X, valueDefault.Y, "%.0f", ImGuiSliderFlags.AlwaysClamp);
			if (isActiveFilter_Y) ImGui.PopStyleColor(3);

			// Label
			// -----
			string visibleLabel = label.LabelVisibleText();

			ImGui.SameLine();
			if (!description.IsNullOrWhitespace()) {
				GuiHelpers.TextTooltip(visibleLabel, description);
			} else {
				ImGui.Text(visibleLabel);
			}

			// Update value
			// ------------
			if (isChangedX || isChangedY) {
				fieldInfo?.SetValue(ConfigurationManager.Config, value);
				return true;
			}
			return false;
		}
	}
}
