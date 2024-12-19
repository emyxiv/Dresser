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
				if(ImGui.BeginTable("TaskTooltip##CurrentGear", 4)) {

					ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch, 100.0f, 0);
					ImGui.TableSetupColumn("O", ImGuiTableColumnFlags.WidthStretch, 5.0f, 1);
					ImGui.TableSetupColumn("N", ImGuiTableColumnFlags.WidthStretch, 5.0f, 2);
					ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthStretch, 100.0f, 3);
					ImGui.TableHeadersRow();

					var ddd = PluginServices.DataManager.Excel.GetSheet<StainTransient>();
					foreach (var taskedItem in taskedItems) {
						var isDye = PluginServices.DataManager.Excel.GetSheet<StainTransient>()
							.Any(t => t.Item1.RowId == taskedItem.ItemId
									|| t.Item2.RowId == taskedItem.ItemId);

						ImGui.TableNextRow();
						ImGui.TableNextColumn();
						// ImGui.AlignTextToFramePadding();
						ImGui.Image(PluginServices.TextureProvider.GetFromGameIcon((uint)taskedItem.Icon).GetWrapOrEmpty().ImGuiHandle, new Vector2(ImGui.GetFontSize()));
						ImGui.SameLine();

						var lineColor = ImGui.GetStyle().Colors[(int)ImGuiCol.Text];
						if (taskedItem.SortedCategory == InventoryCategory.CharacterBags) {
							if(isDye)
								lineColor = ItemIcon.ColorGood;
							else
								lineColor = ItemIcon.ColorBronze;
						}

						ImGui.TextColored(lineColor,$" {taskedItem.FormattedName}");

						var ownedQuantity = taskedItem.Quantity;
						if (taskedItem.SortedCategory < InventoryCategory.CharacterBags) ownedQuantity = 0; // fix the "1" quantity for not owned dyes

						var hasEnough = ownedQuantity >= taskedItem.Quantity;

						ImGui.TableNextColumn();
						ImGui.TextColored(lineColor, $"{ownedQuantity}");
						ImGui.TableNextColumn();
						ImGui.TextColored(hasEnough ? ItemIcon.ColorGood : ItemIcon.ColorBad, $"{taskedItem.QuantityNeeded}");
						ImGui.TableNextColumn();
						ImGui.TextColored(lineColor, $"{taskedItem.FormattedInventoryCategoryType()}");
					}
					ImGui.EndTable();
				}
			}
			if (ImGui.GetIO().KeyShift) {
				DrawLegend();
			} else {
				ImGui.TextDisabled($"Hold shift to display the legend.");
			}
		}
	}

	private static void DrawLegend() {
		ImGui.Spacing();
		ImGui.Separator();
		ImGui.Spacing();
		ImGui.Text($"Columns");

		ImGui.Indent();
		ImGui.Text($"Item : An item that is not in the Glamour Dresser or the Armoire");
		ImGui.Text($"O : Owned quantity");
		ImGui.Text($"N : Needed quantity");
		ImGui.Text($"Location : Where to find it in your inventories if owned somewhere else");
		ImGui.Unindent();

		ImGui.Text($"Row colors");
		ImGui.Indent();
		ImGui.TextColored(ItemIcon.ColorGood, "The dye is already in the inventory in good quantity");
		ImGui.TextColored(ItemIcon.ColorBronze, "The item is in the inventory and needs to be inserted in Glamour Dresser or Armoire");
		ImGui.Unindent();
	}
}
