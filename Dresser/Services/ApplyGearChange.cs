using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Structs.Dresser;
using Dresser.Windows;
using Dresser.Windows.Components;

using ImGuiNET;

using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

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
		public void Dispose() {
			ClearApplyAppearanceQueue();
		}


		private InventoryItemSet? BackedUpItems = null;

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

			
			var slot = GearBrowser.SelectedSlot;

			if (slot != null) {
				if(!ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out InventoryItemSet plate)) {
					plate = new();
					ConfigurationManager.Config.PendingPlateItems[ConfigurationManager.Config.SelectedCurrentPlate] = plate;
				}
				plate.SetSlot(slot.Value, clonedItem);

				PrepareModsAndDo(clonedItem, slot.Value, ApplyItemAppearanceOnPlayer);
				CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
			}
		}

		private void ApplyItemsAppearancesOnPlayer(InventoryItemSet set) {
			//foreach ((var s, var item) in set.Items)
			//	if (item != null) PrepareModsAndDo(item);

			//foreach ((var s, var item) in set.Items)
			//	if (item != null) ApplyItemAppearanceOnPlayer(item);


			var numberOfMods = set.Items.Where(i => i.Value?.IsModded() ?? false).DistinctBy(i => i.Value?.ModDirectory).Count();
			bool isModInstant = numberOfMods <= 1;
			PluginLog.Debug($"numberOfMods: {numberOfMods} => {(isModInstant ? "instant" : "notinstant")}");
			Dictionary<string, List<(GlamourPlateSlot Slot,InventoryItem Item)>> itemsByMods = new();
			foreach ((var s, var item) in set.Items)
				if (item != null) {
					if(item.IsModded() && !isModInstant && item.ModDirectory != null) {
						PluginLog.Warning($"putting in queue {item.FormattedName} => {item.ModDirectory}");
						itemsByMods.TryAdd(item.ModDirectory, new());
						itemsByMods[item.ModDirectory].Add((s,item));
						//ApplyItemAppearanceOnPlayer(InventoryItem.Zero,s);
					} else {
						PrepareModsAndDo(item, s, ApplyItemAppearanceOnPlayer);
					}
				}


			if (!isModInstant) {
				foreach((var modDir, var itemsForThisMod) in itemsByMods)
					AddToApplyAppearanceQueue(itemsForThisMod);
				ApplyAppearanceQueueTick(true);
			}
		}

		private void PrepareModsAndDo(InventoryItem item, GlamourPlateSlot slot, Action<InventoryItem,GlamourPlateSlot>? callback = null, bool ignore_PenumbraDelayAfterModEnableBeforeApplyAppearance = false) {
			if (item.Container == (InventoryType)Storage.InventoryTypeExtra.ModdedItems && item.IsModded() && PluginServices.Penumbra.GetEnabledState()) {
				PluginLog.Verbose($"applying modded item: {item.FormattedName} => {item.ModName}");

				var personalCollection = PluginServices.Penumbra.GetCollectionForLocalPlayerCharacter();
				//PluginLog.Debug($"PENUMBRA YOURSELF COLLECTION: {personalCollection}");
				var modSettings = PluginServices.Penumbra.GetCurrentModSettings(personalCollection, item.ModDirectory ?? "", item.ModName ?? "", true);
				//PluginLog.Debug($"GetCurrentModSettings: {modSettings.Item1} | {item.ModName}");

				if (modSettings.Item1 == Penumbra.Api.Enums.PenumbraApiEc.Success && modSettings.Item2.HasValue) {
					foreach ((var optionGroup, var options) in modSettings.Item2.Value.EnabledOptions) {
						var res1 = PluginServices.Penumbra.TrySetModSettings(ConfigurationManager.Config.PenumbraCollectionApply, item.ModDirectory!, item.ModName!, optionGroup, options.ToList());
						//PluginLog.Debug($"TrySetModSettings: {res1} | {item.ModName}");
					}
				}

				var res2 = PluginServices.Penumbra.TrySetMod(ConfigurationManager.Config.PenumbraCollectionApply, item.ModDirectory!, true);
				//PluginLog.Debug($"TrySetMod TRUE: {res2} | {item.ModName}");

				var tast = Task.Run(async delegate {
					// delay before apply
					if(!ignore_PenumbraDelayAfterModEnableBeforeApplyAppearance)
						await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);
					PluginLog.Warning($"Applying appearance...");
					callback?.Invoke(item, slot);
					await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterApplyAppearanceBeforeModDisable);
					var res3 = PluginServices.Penumbra.TryInheritMod(ConfigurationManager.Config.PenumbraCollectionApply, item.ModDirectory!, item.ModName!, true);
					PluginLog.Debug($"Disable mod after apply {res3} | {item.ModName}");
					await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModDisableBeforeNextModLoop);
					if (ApplyAppearanceQueue.Any()) {
						ApplyAppearanceQueueTick();
					}
				});

			} else {
				callback?.Invoke(item, slot);
			}
		}

		private Queue<List<(GlamourPlateSlot Slot,InventoryItem Item)>> ApplyAppearanceQueue = new();
		private void AddToApplyAppearanceQueue(List<(GlamourPlateSlot Slot,InventoryItem Item)> items) => ApplyAppearanceQueue.Enqueue(items);
		public void ClearApplyAppearanceQueue() => ApplyAppearanceQueue.Clear();
		private void ApplyAppearanceQueueTick(bool ignoreFirstDelay = false) {
			if (ApplyAppearanceQueue.Count > 0) {
				var list = ApplyAppearanceQueue.Dequeue();
				var ffff = list.First();
				PrepareModsAndDo(ffff.Item,ffff.Slot, (i,s)=> {

					foreach (var item in list) {
						PluginLog.Error($"Process ApplyAppearanceQueue... item {item.Item.ModDirectory} ===> {item.Item.FormattedName}");
						ApplyItemAppearanceOnPlayer(item.Item,item.Slot);
					}

				}, ignoreFirstDelay);
			}
		}

		public void FrameworkUpdate() {
		}
		public void SelectCurrentSlot(GlamourPlateSlot slot) {
			GearBrowser.SelectedSlot = slot;
			ConfigurationManager.Config.CurrentGearSelectedSlot = slot;
			GearBrowser.RecomputeItems();
		}
		public void ExecuteCurrentItem(GlamourPlateSlot slot) {
			SelectCurrentSlot(slot);
			Plugin.OpenGearBrowserIfClosed();
			Plugin.UncollapseGearBrowserIfCollapsed();
		}
		public void ExecuteCurrentContextRemoveItem(InventoryItem item, GlamourPlateSlot? slot) {
			if (slot == null) return;
			item.Clear();
			Service.ClientState.LocalPlayer?.Equip(item, slot.Value);
			//RestoreAppearance();
			//ReApplyAppearanceAfterEquipUpdate();
		}


		private void ApplyItemAppearanceOnPlayer(InventoryItem item, GlamourPlateSlot slot)
			=> Service.ClientState.LocalPlayer?.Equip(item,slot);


		public void AppearanceUpdateNakedOrWearing() {
			var set = new InventoryItemSet();


			// this is where we change every items on
			//   - empty slot = naked
			//   - empty slot = show wearing (backed up)


			// get current plate slots
			if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				var glamourPlateSlots = Enum.GetValues<GlamourPlateSlot>().Cast<GlamourPlateSlot>();

				// for each glam plate slots
				foreach (var g in glamourPlateSlots) {
					if (currentPlate.Items.TryGetValue(g, out var i) && (i?.ItemId ?? 0) != 0) {
						// if slot is equipped...
						// this is the case where we assume no change is required
					} else {
						// if slot is empty...
						if (ConfigurationManager.Config.CurrentGearDisplayGear) {
							// if should show wearing weapons... show backed up stuff
							// TODO: optimization: check this item is currently displayed, don't do anything in this case
							set.SetSlot(g, BackedUpItems?.GetSlot(g));
						} else {
							// if should show empty slots as naked
							// TODO: optimization: check this item is currently empty, don't do anything in this case
							set.SetSlot(g, InventoryItem.Zero);
						}
					}
				}
			}
			

			set.ApplyAppearance();
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
					PluginServices.Context.LocalPlayer?.Equip(item, slot);
				}
			}
			CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
		}

		public void OpenGlamourDresser() {
			if (!ConfigurationManager.Config.PendingPlateItems.Any(s=>!s.Value.IsEmpty())) {
				PluginLog.Verbose($"Found found no portable plates, populating them with current");
				PluginServices.ApplyGearChange.OverwritePendingWithActualPlates();
			}
		}

		public void BackupAppearance() {
			PluginLog.Verbose("Backing up appearance");

			var appearanceBackupWeaponMain = PluginServices.Context.LocalPlayer?.MainHandModels().Equip ?? new();
			var modelMainHand = new CustomItemId(
				(SetId)appearanceBackupWeaponMain.Set,
				(WeaponType)appearanceBackupWeaponMain.Base,
				(Variant)appearanceBackupWeaponMain.Variant,
				FullEquipType.Unknown);
			//PluginLog.Debug($"appearance: set:{appearanceBackupWeaponMain.Set}, base:{appearanceBackupWeaponMain.Base}, variant:{appearanceBackupWeaponMain.Variant}  ==> {modelMainHand.ToString()}");
			var appearanceBackupWeaponOff = PluginServices.Context.LocalPlayer?.OffHandModels().Equip ?? new();
			var appearanceBackupEquip = PluginServices.Context.LocalPlayer?.EquipmentModels().Dictionary();

			BackedUpItems = new(appearanceBackupEquip);
			BackedUpItems?.SetSlot(GlamourPlateSlot.MainHand, InventoryItem.FromWeaponEquip(appearanceBackupWeaponMain, GlamourPlateSlot.MainHand));
			BackedUpItems?.SetSlot(GlamourPlateSlot.OffHand,  InventoryItem.FromWeaponEquip(appearanceBackupWeaponOff, GlamourPlateSlot.OffHand));
			
			
			//foreach((var slot,var item) in BackedUpItems?.Items) {
			//	PluginLog.Debug($"Backup {slot} => {(item == null ? "null" : item.ItemId)}");
			//}
		}
		public void RestoreAppearance() {
			PluginLog.Verbose("Restoring appearance");

			if (PluginServices.Context.GlamourerState) {
				PluginServices.Glamourer.RevertCharacter(PluginServices.Context.LocalPlayer);
				PluginServices.Glamourer.RevertToAutomationCharacter(PluginServices.Context.LocalPlayer);
				return;
			}
			BackedUpItems?.ApplyAppearance();
			BackedUpItems = null;
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
				ApplyItemsAppearancesOnPlayer(currentPlate);
			}
			//if (!ConfigurationManager.Config.CurrentGearDisplayGear) StripEmptySlotCurrentPendingPlateAppearance();
			//else ShowStrippedSlots();
		}
		public void ReApplyAppearanceAfterEquipUpdate() {
			BackupAppearance();
			ApplyCurrentPendingPlateAppearance();
			PluginServices.ApplyGearChange.AppearanceUpdateNakedOrWearing();

		}
		public void ToggleDisplayGear() {
			PluginServices.ApplyGearChange.AppearanceUpdateNakedOrWearing();
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
					ItemIcon.DrawIcon(replacementItem, ref isHovering, ref isAnotherTooltipActive, out bool clickedMiddle, slot, null, sizeMod);
					if (isHovering) HoveredIcon = iconKey;
					iconKey++;

					ImGui.BeginDisabled();
					ImGui.SameLine(); GuiHelpers.Icon(Dalamud.Interface.FontAwesomeIcon.ChevronRight); ImGui.SameLine();
					ImGui.EndDisabled();


					isHovering = iconKey == HoveredIcon;
					ItemIcon.DrawIcon(diffToReplacePlate.GetSlot(slot), ref isHovering, ref isAnotherTooltipActive, out clickedMiddle, slot, null, sizeMod);
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
