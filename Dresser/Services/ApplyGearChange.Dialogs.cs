/// ApplyGearChange.Dialogs.cs
/// ImGui dialog and popup rendering for the dresser sync flow.
/// These methods create DialogInfo objects with ImGui draw lambdas for user confirmations
/// when applying portable plate changes to actual glamour plates.

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

using Dresser.Gui;
using Dresser.Gui.Components;
using Dresser.Models;

using System.Linq;

using InventoryItem = Dresser.Models.InventoryItem;

namespace Dresser.Services {
	public partial class ApplyGearChange {

		/// <summary>ImGui hover state for item icons rendered in dialog item lists.</summary>
		public static int HoveredIcon = -1;

		/// <summary>
		/// Draws a list of item icons showing the differences to apply for each plate.
		/// Used inside dialog popups to show the user what will change.
		/// </summary>
		public void DrawListOfItemsForDialogs(ushort? focusOnPlateIndex = null) {
			bool isAnotherTooltipActive = false;
			int iconKey = 0;
			var sizeMod = 0.33f;

			ImGui.BeginGroup();
			foreach ((var plateIndex, var plateValues) in DifferencesToApply) {
				if (focusOnPlateIndex != null && plateIndex != focusOnPlateIndex) continue;
				DifferencesToReplace.TryGetValue(plateIndex, out var diffToReplacePlate);

				ImGui.BulletText($"Plate {plateIndex + 1}: ");
				foreach ((var slot, var replacementItem) in plateValues.Items) {
					ImGui.AlignTextToFramePadding();

					bool isHovering = iconKey == HoveredIcon;
					ItemIcon.DrawIcon(replacementItem, ref isHovering, ref isAnotherTooltipActive, out var _, out var _, slot, null, sizeMod);
					if (isHovering) HoveredIcon = iconKey;
					iconKey++;

					ImGui.BeginDisabled();
					ImGui.SameLine(); GuiHelpers.Icon(FontAwesomeIcon.ChevronRight); ImGui.SameLine();
					ImGui.EndDisabled();

					isHovering = iconKey == HoveredIcon;
					ItemIcon.DrawIcon(diffToReplacePlate.GetSlot(slot), ref isHovering, ref isAnotherTooltipActive, out var _, out var _, slot, null, sizeMod);
					if (isHovering) HoveredIcon = iconKey;
					iconKey++;
				}
				ImGui.EndGroup();
				ImGui.SameLine();
				ImGui.Text("   ");
				ImGui.SameLine();
				ImGui.BeginGroup();
			}
			if (!isAnotherTooltipActive) HoveredIcon = -1;
			ImGui.EndGroup();
		}

		/// <summary>
		/// Opens a confirmation dialog asking the user whether to apply detected plate changes.
		/// On confirm, proceeds with applying. On cancel, clears the dresser sync state.
		/// </summary>
		public void Popup_AskApplyOnPlates() {
			var dialog = new DialogInfo("AskApplyOnPlates",
			() => {
				ImGui.Text($"Glamour plate changes detected, would you like to apply them?");
				ImGui.Text($"{DifferencesToApply.Count} Glamour plate affected");
				PluginServices.ApplyGearChange.DrawListOfItemsForDialogs();
				return Dialogs.GenericButtonConfirmCancel("Continue", "Stop");
			}, (choice) => {
				if (choice == 1)
					PluginServices.ApplyGearChange.ProceedWithFirstChangesAndHiglights();
				else
					ClearApplyDresser();
			},
			2
			);

			Plugin.OpenDialog(dialog);
		}

		/// <summary>
		/// Opens a dialog when some items failed to apply to a plate,
		/// offering Retry, Ignore, or Stop All options.
		/// </summary>
		public void Popup_FailedSomeAskWhatToDo(ushort plateIndex) {
			var dialog = new DialogInfo("FailedSomeAskWhatToDo",
				() => {
				ImGui.Text($"The following items could could not be applied to the plate.");
				PluginServices.ApplyGearChange.DrawListOfItemsForDialogs(plateIndex);

				if (ImGui.Button("Retry##Dialog##Dresser")) return 3;
				ImGui.SameLine();
				if (ImGui.Button("Ignore##Dialog##Dresser")) return 1;
				ImGui.SameLine();
				if (ImGui.Button("Stop All##Dialog##Dresser")) return 2;
				return -1;

			}, (choice) => {
				if (choice == 1) {
					// ignore and continue — offer saving
					HighlightSaveButton = true;
				} else if (choice == 2) {
					PluginServices.ApplyGearChange.ClearApplyDresser();
				} else if (choice == 3) {
					PluginServices.ApplyGearChange.ExecuteChangesOnSelectedPlate();
				}
			},
			1 // if closed with escape, pick choice 1
			);

			Plugin.OpenDialog(dialog);
		}

		/// <summary>
		/// Opens a summary dialog when leaving the dresser.
		/// Shows unsaved changes and offers to forget (overwrite pending with actual plates) or close.
		/// </summary>
		public void Popup_AllDone() {
			if (DifferencesToApply.Any()) {

				var dialog = new DialogInfo("AllDone",
				() => {
					ImGui.Text($"Some change were not saved.");
					PluginServices.ApplyGearChange.DrawListOfItemsForDialogs();

					ImGui.BeginDisabled();
					ImGui.TextWrapped($"\"Forget\" will copy the contents of the plates into portable plates.");
					ImGui.EndDisabled();

					if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, $"CTRL + Shift to \"Forget\".\nIt will copy the contents of the plates into portable plates.")) {
						return 2;
					}
					ImGui.SameLine();
					return Dialogs.GenericButtonClose();

				}, (choice) => {
					if (choice == 1)
						PluginServices.ApplyGearChange.ClearApplyDresser();
					else if (choice == 2) {
						PluginServices.ApplyGearChange.ClearApplyDresser();
						PluginServices.ApplyGearChange.OverwritePendingWithActualPlates();
					}
				},
				1
				);

				Plugin.OpenDialog(dialog);
			} else {
				PluginServices.ApplyGearChange.ClearApplyDresser();
			}
		}
	}
}
