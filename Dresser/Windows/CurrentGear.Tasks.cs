using System;
using System.Linq;
using System.Numerics;

using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Windows.Components;

using ImGuiNET;

using Lumina.Excel.Sheets;

namespace Dresser.Windows;

public partial class CurrentGear
{
	public static bool DrawTasks() {
		if (!PluginServices.ApplyGearChange.TasksOnCurrentPlate.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var taskedItems) || taskedItems.Count == 0) return false;

		ImGui.BeginGroup();
		var tint = ItemIcon.ColorBad * new Vector4(1.75f, 1.75f, 1.75f, 1);
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(-5* ConfigurationManager.Config.IconSizeMult, 0));
		try {
			GuiHelpers.GameButton(UldBundle.CircleLargeExclamationMark, "OverwritePendingWithCurrent##CurrentGear", "", SizeGameCircleIcons, tint);
			ImGui.SameLine();

			//var tasksText = $"{taskedItems.Count} Task{(taskedItems.Count > 1 ? "s" : "")}";
			var tasksText = taskedItems.Count.ToString();
			GuiHelpers.TextWithFontDrawlist(
				tasksText,
				GuiHelpers.Font.Task,
				ConfigurationManager.Config.PlateSelectorColorRadio,
				SizeGameCircleIcons.Y);

		} catch (Exception e) {
			PluginLog.Error(e, $"Error during DrawTasks");
		}	finally {
			ImGui.PopStyleVar();
			ImGui.EndGroup();
		}
		GuiHelpers.Tooltip(DrawTasksTooltip);
		return true;
	}
	private static void DrawTasksTooltip() {
		if (PluginServices.ApplyGearChange.TasksOnCurrentPlate.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var taskedItems)) {
			if(taskedItems.Any()) {
				ImGui.TextDisabled($"Some items are neither in {InventoryCategory.GlamourChest.FormattedName()} or {InventoryCategory.Armoire.FormattedName()}");
				ImGui.Spacing();
				if(ImGui.BeginTable("TaskTooltip##CurrentGear", 2)) {

					ImGui.TableSetupColumn("Item Name", ImGuiTableColumnFlags.WidthStretch, 100.0f, 0);
					ImGui.TableSetupColumn("Where", ImGuiTableColumnFlags.WidthStretch, 100.0f, 1);
					ImGui.TableHeadersRow();

					var ddd = PluginServices.DataManager.Excel.GetSheet<StainTransient>();
					foreach (var taskedItem in taskedItems) {
						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						ImGui.AlignTextToFramePadding();
						ImGui.Text($"{taskedItem.FormattedName}");

						if(PluginServices.DataManager.Excel.GetSheet<StainTransient>()
							.Any(t=>t.Item1.RowId == taskedItem.ItemId
									|| t.Item2.RowId == taskedItem.ItemId)) {

							ImGui.SameLine();
							ImGui.Text($" ( {taskedItem.Quantity}");
							ImGui.SameLine();
							ImGui.TextColored(ItemIcon.ColorBad, $"- {taskedItem.QuantityNeeded}");
							ImGui.SameLine();
							ImGui.Text($")");
						}
						ImGui.TableNextColumn();
						ImGui.Text($"{taskedItem.FormattedInventoryCategoryType()}");
					}
					ImGui.EndTable();
				}
			}
		}
	}
}
