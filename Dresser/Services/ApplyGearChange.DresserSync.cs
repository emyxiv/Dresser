/// ApplyGearChange.DresserSync.cs
/// Glamour dresser synchronization: detects differences between portable plates and
/// actual in-game plates, applies those changes to the real glamour dresser, tracks
/// success/failure per plate, and manages the overlay highlight state that shows
/// which plates still need saving.

using CriticalCommonLib.Enums;

using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Models;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Dresser.Services {
	public partial class ApplyGearChange {

		// ── Difference Tracking ──────────────────────────────────────────

		/// <summary>Per-plate sets of items that differ from the actual glamour plates (to apply).</summary>
		public Dictionary<ushort, InventoryItemSet> DifferencesToApply = new();

		/// <summary>Per-plate sets of items that will be replaced in the actual plates (the "old" side).</summary>
		private Dictionary<ushort, InventoryItemSet> DifferencesToReplace = new();

		/// <summary>Per-plate sets that were successfully applied, with remaining failed slots.</summary>
		private Dictionary<ushort, InventoryItemSet> AppliedPending = new();

		/// <summary>Set of plate indices where application failed.</summary>
		public HashSet<ushort> PlatesFailed = new();


		// ── Overlay Highlight State ──────────────────────────────────────
		// These are read by OverlayService to color the plate tabs in the dresser UI.

		public Dictionary<ushort, Vector4?> HighlightPlatesRadio = new();
		public bool? HighlightSaveButton = false;

		private Vector4? Highlight_apply_todo    = new Vector4(  8,  63, 153, 255) / 255f; // blue
		private Vector4? Highlight_apply_none    = new Vector4(153,   8,   8, 255) / 255f; // red
		private Vector4? Highlight_apply_partial = new Vector4(153,  37,   8, 255 / 255f); // orange
		private Vector4? Highlight_apply_all     = new Vector4(  8, 153,  44, 255) / 255f; // green
		private Vector4? Highlight_save_ok       = null; // remove highlight


		// ── Modification Detection ───────────────────────────────────────

		/// <summary>
		/// Compares all portable plates against actual in-game plates and builds
		/// the DifferencesToApply dictionary. If differences exist and the config
		/// option is enabled, opens the confirmation popup.
		/// </summary>
		public void CheckModificationsOnPendingPlates() {
			PluginLog.Verbose("Calculating Modifications On Pending Plates ...");
			var pendingPlates = ConfigurationManager.Config.PendingPlateItemsCurrentChar;
			var actualPlates = Storage.PagesInv;

			Dictionary<ushort, InventoryItemSet> differencesToApply = new();

			foreach ((var plateIndex, var pendingInvSet) in pendingPlates) {
				if (plateIndex >= Storage.PlateNumber) continue;
				if (actualPlates.TryGetValue(plateIndex, out var actualInvSet)) {
					if (pendingInvSet.IsEmpty()) continue;

					if (pendingInvSet.IsDifferentGlam(actualInvSet, out var diffLeft, out var diffRight)) {
						differencesToApply[plateIndex] = diffLeft;
						DifferencesToReplace[plateIndex] = diffRight;
					}
				}
			}

			if (differencesToApply.Count == 0) return;

			DifferencesToApply = differencesToApply;
			if (ConfigurationManager.Config.OfferApplyAllPlatesOnDresserOpen) Popup_AskApplyOnPlates();
		}


		// ── Apply Flow ───────────────────────────────────────────────────

		/// <summary>
		/// Entry point after the user confirms they want to apply plate changes.
		/// Sets all pending plates to "todo" highlight and begins applying the selected plate.
		/// </summary>
		public void ProceedWithFirstChangesAndHiglights() {
			if (DifferencesToApply.Count == 0) return;

			HighlightPlatesRadio = DifferencesToApply.ToDictionary(p => p.Key, p => Highlight_apply_todo);
			PluginServices.Context.HasConfirmedApplyIntoDresser = true;
			ExecuteChangesOnSelectedPlate();
		}

		/// <summary>
		/// Starts applying changes to the currently selected plate after a short delay
		/// (allows the UI to finish transitioning).
		/// </summary>
		public void ExecuteChangesOnSelectedPlate(bool ignorePlateDifference = false) {
			Task.Run(async delegate {
				await Task.Delay(500);
				ExecuteChangesOnSelectedPlateDelayed(ignorePlateDifference);
			});
		}

		private void ExecuteChangesOnSelectedPlateDelayed(bool ignorePlateDifference) {
			PluginLog.Debug($"zzz 1");
			if (PluginServices.Context.SelectedPlate == null) return;
			PluginLog.Debug($"zzz 2");
			var plateIndex = (ushort)PluginServices.Context.SelectedPlate;

			if (ignorePlateDifference) {
				ApplyToDresserPlateAndRecord(GetCurrentPlate() ?? new(), plateIndex);
				return;
			}
			if (!DifferencesToApply.ContainsKey(plateIndex)) return;
			if (PluginServices.Context.SelectedPlate != plateIndex) return;

			if (DifferencesToApply.TryGetValue(plateIndex, out var replacementGlams)) {
				var successfullyApplied = ApplyToDresserPlateAndRecord(replacementGlams, plateIndex);

				if (successfullyApplied.Count() == replacementGlams.Items.Count) {
					PluginLog.Verbose($"Apply Glam to plate: success all");
					HighlightPlatesRadio[plateIndex] = Highlight_apply_all;
					HighlightSaveButton = true;

				} else if (successfullyApplied.Any()) {
					PluginLog.Verbose($"Apply Glam to plate: success partial");
					HighlightPlatesRadio[plateIndex] = Highlight_apply_partial;
					Popup_FailedSomeAskWhatToDo(plateIndex);

				} else {
					PluginLog.Verbose($"Apply Glam to plate: fail");
					HighlightPlatesRadio[plateIndex] = Highlight_apply_none;
					Popup_FailedSomeAskWhatToDo(plateIndex);
				}
				return;
			}
			PluginLog.Verbose($"No plate ({plateIndex}) found in DifferencesToApply");
		}

		/// <summary>
		/// Applies an InventoryItemSet to the actual glamour plate via GlamourPlates service
		/// and records which slots succeeded/failed.
		/// </summary>
		private IEnumerable<GlamourPlateSlot> ApplyToDresserPlateAndRecord(InventoryItemSet set, ushort plateIndex) {
			var successfullyApplied = PluginServices.GlamourPlates.SetGlamourPlateSlot(set);

			if (successfullyApplied.Any()) {
				AppliedPending[plateIndex] = set.Copy();
				foreach (var slotDone in successfullyApplied) {
					AppliedPending[plateIndex].RemoveSlot(slotDone);
				}
			}
			return successfullyApplied;
		}


		// ── Plate Navigation Tracking ────────────────────────────────────

		/// <summary>
		/// Called when the user navigates away from a plate in the dresser.
		/// Checks if the plate was actually saved and updates highlights accordingly.
		/// </summary>
		public void CheckIfLeavingPlateWasApplied(ushort? previousPlateNumber) {
			if (previousPlateNumber == null) return;
			var prev = (ushort)previousPlateNumber;
			if (!DifferencesToApply.ContainsKey(prev)) return;

			if (!IsGlamPlateDifferentFromPending(prev) && HighlightPlatesRadio.ContainsKey(prev)) {
				HighlightPlatesRadio[prev] = Highlight_save_ok;
				if (AppliedPending.TryGetValue(prev, out var appliedPlate)) {
					if (appliedPlate.IsEmpty()) {
						DifferencesToApply.Remove(prev);
					} else {
						Gathering.ParseGlamourPlates();
						var dd = PluginServices.Storage.Pages?[prev];
						if (dd != null) {
							DifferencesToApply[prev] = (InventoryItemSet)dd;
						}
					}
				}
			} else {
				HighlightPlatesRadio[prev] = Highlight_apply_todo;
			}
		}

		private bool IsGlamPlateDifferentFromPending(ushort plateNumber) {
			Gathering.ParseGlamourPlates();
			if (PluginServices.Storage.Pages != null && plateNumber >= 0 && plateNumber < PluginServices.Storage.Pages.Length) {
				var miragePlate = PluginServices.Storage.Pages[plateNumber];
				var ggg = (InventoryItemSet)miragePlate;
				if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(plateNumber, out var pendingPlate)) {
					return pendingPlate.IsDifferentGlam(ggg, out var _, out var _);
				}
			}
			return true;
		}


		// ── Save / Leave / Cleanup ───────────────────────────────────────

		/// <summary>
		/// Called after the user successfully saves a plate in the dresser.
		/// Removes the plate from DifferencesToApply and clears its highlight.
		/// </summary>
		public void ExecuteSavingPlateChanges() {
			var currentPlateNumber = PluginServices.Context.SelectedPlate;
			if (currentPlateNumber != null) {
				DifferencesToApply.Remove((ushort)currentPlateNumber);
				HighlightPlatesRadio[(ushort)currentPlateNumber] = Highlight_save_ok;
				HighlightSaveButton = false;
			}

			if (!DifferencesToApply.Any()) {
				PluginServices.ApplyGearChange.Popup_AllDone();
			}
		}

		/// <summary>Called when the user leaves the glamour plate dresser window.</summary>
		public void LeaveGlamourPlateDresser() {
			CleanOverlayColors();
			if (ConfigurationManager.Config.OfferOverwritePendingPlatesAfterApplyAll) Popup_AllDone();
		}

		/// <summary>Resets all overlay highlight colors.</summary>
		public void CleanOverlayColors() {
			Vector4? n = null;
			HighlightPlatesRadio = HighlightPlatesRadio.ToDictionary(h => h.Key, h => n);
			HighlightSaveButton = null;
			HighlightPlatesRadio.Clear();
		}

		/// <summary>Fully resets all dresser-sync state (differences, highlights, failed plates).</summary>
		public void ClearApplyDresser() {
			PluginServices.Context.HasConfirmedApplyIntoDresser = false;
			PluginLog.Debug(" -- Clean apply dresser -- ");
			CleanOverlayColors();
			DifferencesToApply.Clear();
			DifferencesToReplace.Clear();
			PlatesFailed.Clear();
		}
	}
}
