using CriticalCommonLib;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dalamud.Logging;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Structs.Actor;
using Dresser.Structs.Dresser;
using Dresser.Windows;
using Dresser.Windows.Components;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using InventoryItem = Dresser.Structs.Dresser.InventoryItem;

namespace Dresser.Services {
	public class ApplyGearChange : IDisposable {
		private Plugin Plugin;
		public ApplyGearChange(Plugin plugin) {
			Plugin = plugin;
		}
		public void Dispose() { }



		private WeaponEquip AppearanceBackupWeaponMain = new();
		private WeaponEquip AppearanceBackupWeaponOff = new();
		private Dictionary<EquipIndex, ItemEquip>? AppearanceBackupEquip = new();

		public void EnterBrowsingMode() {
			ReApplyAppearanceAfterEquipUpdate();
		}
		public void ExitBrowsingMode() {
			PluginLog.Verbose("Closing Dresser");
			Plugin.CloseBrowser();

			RestoreAppearance();
		}

		public void ExecuteBrowserItem(InventoryItem item) {
			PluginLog.Verbose($"Execute apply item {item.Item.NameString} {item.Item.RowId}");

			// TODO: make sure the item is still in glam chest or armoire
			//if (GlamourPlates.IsGlamingAtDresser() && (item.Container == InventoryType.GlamourChest || item.Container == InventoryType.Armoire)) {
			//	PluginServices.GlamourPlates.ModifyGlamourPlateSlot(item,
			//		(i) => Gathering.ParseGlamourPlates()
			//		);
			//}


			var clonedItem = item.Clone();
			if (Plugin.DyePicker.IsOpen && ConfigurationManager.Config.DyePickerKeepApplyOnNewItem && DyePicker.CurrentDye != null) {
				clonedItem.Stain = (byte)DyePicker.CurrentDye.RowId;
			}

			var slot = clonedItem.Item.GlamourPlateSlot();

			if (slot != null) {
				if(!ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out InventoryItemSet plate)) {
					plate = new();
					ConfigurationManager.Config.PendingPlateItems[ConfigurationManager.Config.SelectedCurrentPlate] = plate;
				}
				plate.SetSlot((GlamourPlateSlot)slot, clonedItem);
			}

			Service.ClientState.LocalPlayer?.Equip(clonedItem);
			CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
		}

		public void FrameworkUpdate() {
		}
		public void SelectCurrentSlot(GlamourPlateSlot slot) {
			GearBrowser.SelectedSlot = slot;
			GearBrowser.RecomputeItems();
		}
		public void ExecuteCurrentItem(GlamourPlateSlot slot) {
			SelectCurrentSlot(slot);
			Plugin.OpenGearBrowserIfClosed();
			Plugin.UncollapseGearBrowserIfCollapsed();
		}
		public void ExecuteCurrentContextRemoveItem(InventoryItem item) {
			item.Clear();
			Service.ClientState.LocalPlayer?.Equip(item);
			RestoreAppearance();
			ReApplyAppearanceAfterEquipUpdate();
		}
		public void ExecuteCurrentContextDye(InventoryItem item) {
			PluginLog.Warning("TODO: open dye picker");
		}
		public void ExecuteCurrentContextRemoveDye(InventoryItem item) {
			item.Stain = 0;
		}
		public void ApplyDye(ushort PlateNumber, GlamourPlateSlot slot, byte stain) {
			if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(PlateNumber, out var plate)) {
				var item = plate.GetSlot(slot);
				if (item != null) {
					item.Stain = stain;
					PluginServices.Context.LocalPlayer?.Equip(item);
				}
			}
		}

		public void OpenGlamourDresser() {
			if (!ConfigurationManager.Config.PendingPlateItems.Any(s=>!s.Value.IsEmpty())) {
				PluginLog.Verbose($"Found found no portable plates, populating them with current");
				PluginServices.ApplyGearChange.OverwritePendingWithActualPlates();
			}
		}

		public void BackupAppearance() {
			PluginLog.Verbose("Backing up appearance");
			AppearanceBackupWeaponMain = PluginServices.Context.LocalPlayer?.MainHandModels().Equip ?? new();
			AppearanceBackupWeaponOff = PluginServices.Context.LocalPlayer?.OffHandModels().Equip ?? new();
			AppearanceBackupEquip = PluginServices.Context.LocalPlayer?.EquipmentModels().Dictionary();
		}
		public void RestoreAppearance() {
			PluginLog.Verbose("Restoring appearance");

			PluginServices.Context.LocalPlayer?.Equip(WeaponIndex.MainHand, AppearanceBackupWeaponMain);
			PluginServices.Context.LocalPlayer?.Equip(WeaponIndex.OffHand, AppearanceBackupWeaponOff);
			if (AppearanceBackupEquip != null)
				foreach ((var index, var item) in AppearanceBackupEquip) {
					PluginServices.Context.LocalPlayer?.Equip(index, item);
				}
			AppearanceBackupEquip = null;

		}



		public Dictionary<ushort, List<InventoryItem>> TasksOnCurrentPlate = new();
		public void CompileTodoTasks(ushort? plateNumber = null) {
			foreach((var plateN, var set) in ConfigurationManager.Config.PendingPlateItems) {
				if (plateNumber != null && plateN != plateNumber) continue;
				TasksOnCurrentPlate[plateN] = set.FindNotOwned();
			}
		}
		public void ApplyCurrentPendingPlateAppearance() {
			if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				currentPlate.UpdateSourcesForOwnedItems();
				CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
				foreach ((var s, var item) in currentPlate.Items) {
					if (item != null) PluginServices.Context.LocalPlayer?.Equip(item);
				}
			}
			if (!ConfigurationManager.Config.CurrentGearDisplayGear) StripEmptySlotCurrentPendingPlateAppearance();
			else ShowStrippedSlots();
		}
		public void ReApplyAppearanceAfterEquipUpdate() {
			BackupAppearance();
			ApplyCurrentPendingPlateAppearance();
			if (!ConfigurationManager.Config.CurrentGearDisplayGear) StripEmptySlotCurrentPendingPlateAppearance();

		}
		public void StripEmptySlotCurrentPendingPlateAppearance() {
			if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				var glamourPlates = Enum.GetValues<GlamourPlateSlot>().Cast<GlamourPlateSlot>();
				foreach (var g in glamourPlates) {
					if (!(!currentPlate.Items.TryGetValue(g, out var item) || (item?.ItemId ?? 0) == 0)) {
						var indexW = g.ToWeaponIndex();
						if (indexW != null)
							PluginServices.Context.LocalPlayer?.Equip((WeaponIndex)indexW, new WeaponEquip() { Base = 0, Dye = 0, Set = 0, Variant = 0 });
					} else {
						var index = g.ToEquipIndex();
						if (index != null) {
							PluginServices.Context.LocalPlayer?.Equip((EquipIndex)index, new ItemEquip() { Id = 0, Variant = 0, Dye = 0 });
						}
					}
				}
			}
		}
		public void ShowStrippedSlots() {
			if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				var glamourPlates = Enum.GetValues<GlamourPlateSlot>().Cast<GlamourPlateSlot>();
				foreach (var g in glamourPlates) {
					if (!(!currentPlate.Items.TryGetValue(g, out var item) || (item?.ItemId ?? 0) == 0)) {
						var indexW = g.ToWeaponIndex();
						if (indexW == WeaponIndex.MainHand)
							PluginServices.Context.LocalPlayer?.Equip((WeaponIndex)indexW, AppearanceBackupWeaponMain);
						else if (indexW == WeaponIndex.OffHand)
							PluginServices.Context.LocalPlayer?.Equip((WeaponIndex)indexW, AppearanceBackupWeaponOff);
					} else {
						var index = g.ToEquipIndex();
						if (index != null && AppearanceBackupEquip!.TryGetValue((EquipIndex)index, out var equip)) {
							PluginServices.Context.LocalPlayer?.Equip((EquipIndex)index, equip);
						}
					}
				}
			}

		}
		public void ToggleDisplayGear() {
			if (!ConfigurationManager.Config.CurrentGearDisplayGear) StripEmptySlotCurrentPendingPlateAppearance();
			else ShowStrippedSlots();

		}

		public void OverwritePendingWithCurrentPlate() {
			ConfigurationManager.Config.PendingPlateItems[ConfigurationManager.Config.SelectedCurrentPlate] = ConfigurationManager.Config.DisplayPlateItems.Copy().RemoveEmpty();
			ReApplyAppearanceAfterEquipUpdate();
		}
		public void OverwritePendingWithActualPlates() {
			Task.Run(async delegate {
				Gathering.ParseGlamourPlates();
				await Task.Delay(2500);
				if (PluginServices.Storage.Pages == null) {
					return;
				}
				foreach ((var plateNumber, var set) in Storage.PagesInv) {
					if (plateNumber == Storage.PlateNumber || set.IsEmpty()) continue;
					ConfigurationManager.Config.PendingPlateItems[plateNumber] = set;
				}
			});
		}

		public Dictionary<ushort, InventoryItemSet> DifferencesToApply = new();
		private Dictionary<ushort, InventoryItemSet> DifferencesToReplace = new();
		private Dictionary<ushort, InventoryItemSet> AppliedPending = new();

		public void CheckModificationsOnPendingPlates() {
			PluginLog.Verbose("Calculating Modifications On Pending Plates ...");
			var pendingPlates = ConfigurationManager.Config.PendingPlateItems;
			var actualPlates = Storage.PagesInv;



			// make a list with all the changes between pending and actual plates
			// ()
			Dictionary<ushort, InventoryItemSet> differencesToApply = new();

			foreach ((var plateIndex, var pendingInvSet) in pendingPlates) {
				if (plateIndex >= Storage.PlateNumber) continue;
				if (actualPlates.TryGetValue(plateIndex, out var actualInvSet)) {
					if (pendingInvSet.IsEmpty()) continue; // (todo: maybe offer them the option to also clean untouched plates)

					if (pendingInvSet.IsDifferentGlam(actualInvSet, out var diffLeft, out var diffRight)) {
						differencesToApply[plateIndex] = diffLeft;
						DifferencesToReplace[plateIndex] = diffRight;
					}
				}
			}

			if (differencesToApply.Count == 0) return;

			DifferencesToApply = differencesToApply;
			Popup_AskApplyOnPlates();
		}


		public static int HoveredIcon = -1;
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

					// item icon
					//ImGui.SameLine();


					bool isHovering = iconKey == HoveredIcon;
					ItemIcon.DrawIcon(replacementItem, ref isHovering, ref isAnotherTooltipActive, slot, null, sizeMod);
					if (isHovering) HoveredIcon = iconKey;
					iconKey++;

					ImGui.BeginDisabled();
					ImGui.SameLine(); GuiHelpers.Icon(Dalamud.Interface.FontAwesomeIcon.ChevronRight); ImGui.SameLine();
					ImGui.EndDisabled();


					isHovering = iconKey == HoveredIcon;
					ItemIcon.DrawIcon(diffToReplacePlate.GetSlot(slot), ref isHovering, ref isAnotherTooltipActive, slot, null, sizeMod);
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

		//private unsafe void Dddddddddd(CriticalCommonLib.Services.Ui.AtkBaseWrapper? addon) {
		//	PluginLog.Debug($"attempting change plate to 5");
		//	if (addon != null && addon.AtkUnitBase != null) {
		//		PluginLog.Debug($">>>>>>>>>>>>>>>>>>>>>>>>>");
		//		var actualAddon = (InventoryMiragePrismMiragePlateAddon*)addon.AtkUnitBase;
		//		actualAddon->SelectedPlate = 5;
		//	}
		//}

		public Dictionary<ushort, Vector4?> HighlightPlatesRadio = new();
		private Vector4? Highlight_apply_todo = new Vector4(8, 63, 153, 255) / 255f; // blue
		private Vector4? Highlight_apply_none = new Vector4(153, 8, 8, 255) / 255f; // red
		private Vector4? Highlight_apply_partial = new Vector4(153, 37, 8, 255 / 255f); // orange
		private Vector4? Highlight_apply_all = new Vector4(8, 153, 44, 255) / 255f; // green
																					//private Vector4 Highlight_save_todo = new Vector4();
																					//private Vector4 Highlight_save_fail = new Vector4();
		private Vector4? Highlight_save_ok = null; // remove highlight
		public bool? HighlightSaveButton = false;
		public void ProceedWithFirstChangesAndHiglights() {
			//if(PluginServices.OverlayService.Overlays.TryGetValue(CriticalCommonLib.Services.Ui.WindowName.MiragePrismMiragePlate, out var state)){
			//	Dddddddddd(state.AtkUnitBase);
			//}
			if (DifferencesToApply.Count == 0) return;

			// put all todo tab in Highlight_apply_todo color
			HighlightPlatesRadio = DifferencesToApply.ToDictionary(p => p.Key, p => Highlight_apply_todo);
			PluginServices.OverlayService.RefreshOverlayStates();

			ExecuteChangesOnSelectedPlate();
		}

		public void CheckIfLeavingPlateWasApplied(ushort? previousPlateNumber) {
			if (previousPlateNumber == null) return;
			var previousPlateNumber2 = (ushort)previousPlateNumber;
			if (!DifferencesToApply.ContainsKey(previousPlateNumber2)) return;
			if (!IsGlamPlateDifferentFromPending(previousPlateNumber2) && HighlightPlatesRadio.ContainsKey(previousPlateNumber2)) {
				// change highlight color as "saved"
				HighlightPlatesRadio[previousPlateNumber2] = Highlight_save_ok;
				// change Di
				if (AppliedPending.TryGetValue(previousPlateNumber2, out var appliedPlate)) {
					if (appliedPlate.IsEmpty()) {
						DifferencesToApply.Remove(previousPlateNumber2);
					} else {
						Gathering.ParseGlamourPlates();
						var dd = PluginServices.Storage.Pages?[previousPlateNumber2];
						if (dd != null) {
							DifferencesToApply[previousPlateNumber2] = (InventoryItemSet)dd;
						}

					}
				}
			} else {
				HighlightPlatesRadio[previousPlateNumber2] = Highlight_apply_todo;

			}
		}
		private bool IsGlamPlateDifferentFromPending(ushort platelateNumber) {
			Gathering.ParseGlamourPlates();
			if (PluginServices.Storage.Pages != null && platelateNumber >= 0 && platelateNumber < PluginServices.Storage.Pages.Length) {
				var miragePlate = PluginServices.Storage.Pages[platelateNumber];
				var ggg = (InventoryItemSet)miragePlate;
				if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(platelateNumber, out var pendingPlate)) {
					return pendingPlate.IsDifferentGlam(ggg, out var _, out var _);
				}
			}
			return true;
		}
		public void ExecuteChangesOnSelectedPlate() {
			Task.Run(async delegate {
				await Task.Delay(250);
			}).Wait();

			if (PluginServices.Context.SelectedPlate == null) return;
			var plateIndex = (ushort)PluginServices.Context.SelectedPlate;
			if (!DifferencesToApply.ContainsKey(plateIndex)) return;

			if (PluginServices.Context.SelectedPlate != plateIndex) return;
			// todo change plate
			if (DifferencesToApply.TryGetValue(plateIndex, out var replacementGlams)) {

				//HashSet<GlamourPlateSlot> successfullyApplied = new();

				var glamaholicPlate = new Interop.Hooks.SavedPlate();
				foreach ((var slot, var replacementItem) in replacementGlams.Items) {
					glamaholicPlate.Items.Add(slot, new Interop.Hooks.SavedGlamourItem {
						ItemId = replacementItem?.ItemId ?? 0,
						StainId = replacementItem?.Stain ?? 0
					});
					//if (PluginServices.GlamourPlates.ModifyGlamourPlateSlot(replacementItem, slot))
					//	successfullyApplied.Add(slot);

				}

				var successfullyApplied = PluginServices.GlamourPlates.LoadPlate(glamaholicPlate);

				if (successfullyApplied.Any()) {
					AppliedPending[plateIndex] = replacementGlams.Copy();
					foreach (var slotDone in successfullyApplied) {
						AppliedPending[plateIndex].RemoveSlot(slotDone);
					}
				}

				if (successfullyApplied.Count() == replacementGlams.Items.Count) {
					PluginLog.Verbose($"Apply Glame to plate: success all");
					// success all
					HighlightPlatesRadio[plateIndex] = Highlight_apply_all;
					HighlightSaveButton = true;
					PluginServices.OverlayService.RefreshOverlayStates();

				} else if (successfullyApplied.Any()) {
					PluginLog.Verbose($"Apply Glame to plate: success partial");
					// success partial
					HighlightPlatesRadio[plateIndex] = Highlight_apply_partial;
					PluginServices.OverlayService.RefreshOverlayStates();
					Popup_FailedSomeAskWhatToDo(plateIndex);

				} else {
					PluginLog.Verbose($"Apply Glame to plate: fail");
					// fail
					HighlightPlatesRadio[plateIndex] = Highlight_apply_none;
					PluginServices.OverlayService.RefreshOverlayStates();
					Popup_FailedSomeAskWhatToDo(plateIndex);
				}

				return;
			}
			PluginLog.Verbose($"No plate ({plateIndex}) found in DifferencesToApply");


			// todo auto click save
			// todo wait the window open
			// todo if simple "save", ask user to click Yes (or auto...)
			// todo if missing dye and window "missing dye" opens, offer options: "skip this plate", "stop"

			// todo apply actual plate number change

		}
		public HashSet<ushort> PlatesFailed = new();
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
				if (choice == 1) { // ignore and continue
								   //DifferencesToApply.Remove(plateIndex);
								   //PlatesFailed.Add(plateIndex);
								   // offer saving
					HighlightSaveButton = true;
					PluginServices.OverlayService.RefreshOverlayStates();

				} else if (choice == 2) { // stop all
					PluginServices.ApplyGearChange.ClearApplyDresser();
				} else if (choice == 3) { // stop all
					PluginServices.ApplyGearChange.ExecuteChangesOnSelectedPlate();
				}
			},
			1 // if closed with escape, pick choice 1
			);

			Plugin.OpenDialog(dialog);

		}
		public void ExecuteSavingPlateChanges() {
			var currentPlateNumber = PluginServices.Context.SelectedPlate;
			if (currentPlateNumber != null) {
				DifferencesToApply.Remove((ushort)currentPlateNumber);
				HighlightPlatesRadio[(ushort)currentPlateNumber] = Highlight_save_ok;
				HighlightSaveButton = false;
				PluginServices.OverlayService.RefreshOverlayStates();
			}

			if (!DifferencesToApply.Any()) {
				PluginServices.ApplyGearChange.Popup_AllDone();
			}
		}
		public void Popup_AllDone() {
			CleanOverlayColors();
			if (DifferencesToApply.Any()) {

				var dialog = new DialogInfo("AllDone",
				() => {
					ImGui.Text($"Some change were not saved.");

					PluginServices.ApplyGearChange.DrawListOfItemsForDialogs();

					ImGui.BeginDisabled();
					ImGui.TextWrapped($"\"Forget\" will copy the contents of the plates into portable plates.");
					ImGui.EndDisabled();

					if (GuiHelpers.IconButtonHoldConfirm(Dalamud.Interface.FontAwesomeIcon.Trash, $"CTRL + Shift to \"Forget\".\nIt will copy the contents of the plates into portable plates.")) {
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
		public void CleanOverlayColors() {
			Vector4? n = null;
			HighlightPlatesRadio = HighlightPlatesRadio.ToDictionary(h => h.Key, h => n);
			HighlightSaveButton = null;
			PluginServices.OverlayService.RefreshOverlayStates();
			HighlightPlatesRadio.Clear();

		}
		public void ClearApplyDresser() {
			PluginLog.Debug(" -- Clean apply dresser -- ");
			CleanOverlayColors();
			DifferencesToApply.Clear();
			DifferencesToReplace.Clear();
			PlatesFailed.Clear();
		}
	}
}
