using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Structs.Dresser;
using Dresser.Windows;
using Dresser.Windows.Components;

using ImGuiNET;

using Penumbra.GameData.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using Dresser.Enums;

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
			// PluginServices.Context.LocalPlayer?.SetMetaVisibility();
			switch (ConfigurationManager.Config.BehaviorOnOpen)
			{
				case BehaviorOnOpen.LastOpenedPortablePlate:
					// this was the default, so the logic is based on "last open", there is nothing to do here
					break;
				case BehaviorOnOpen.SandboxPlateAndStrip:
					// set the last open and gear to reflect sandbox plate and strip behavior
					ConfigurationManager.Config.SelectedCurrentPlate = ushort.MaxValue;
					ConfigurationManager.Config.CurrentGearDisplayGear = false;
					ConfigurationManager.Config.PendingPlateItemsCurrentChar[ushort.MaxValue] = new();
					break;
				case BehaviorOnOpen.SandboxPlateWithWearingGlam:
					// set the last open and gear to reflect sandbox plate and display wearing gear
					ConfigurationManager.Config.PendingPlateItemsCurrentChar[ushort.MaxValue] = GetCurrentAppearance();
					ConfigurationManager.Config.SelectedCurrentPlate = ushort.MaxValue;
					break;
			}
			Task.Run(ReApplyAppearanceAfterEquipUpdate);
		}
		public void ExitBrowsingMode() {
			PluginLog.Verbose("Closing Dresser");
			Plugin.CloseBrowser();

			RestoreAppearance();
		}

		private (string, string)? CurrentPreviousMod = null;
		private int CurrentIncrement = 0;
		public void ExecuteBrowserItem(InventoryItem item) {
			PluginLog.Verbose($"Execute apply item {item.Item.NameString} {item.Item.RowId}");

			// TODO: make sure the item is still in glam chest or armoire
			//if (GlamourPlates.IsGlamingAtDresser() && (item.Container == InventoryType.GlamourChest || item.Container == InventoryType.Armoire)) {
			//	PluginServices.GlamourPlates.ModifyGlamourPlateSlot(item,
			//		(i) => Gathering.ParseGlamourPlates()
			//		);
			//}


			var clonedItem = item.Clone();
			DyePickerRefreshNewItem(clonedItem,true);

			var slot = GearBrowser.SelectedSlot;

			if (slot != null) {
				if(!ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out InventoryItemSet plate)) {
					plate = new();
					ConfigurationManager.Config.PendingPlateItemsCurrentChar[ConfigurationManager.Config.SelectedCurrentPlate] = plate;
				}
				CurrentPreviousMod = plate.GetSlot(slot.Value)?.GetMod();
				plate.SetSlot(slot.Value, clonedItem);

				PrepareModsAndDo(clonedItem, slot.Value, ApplyItemAppearanceOnPlayer);
				CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
			}
		}
		public void DyePickerRefreshNewItem(InventoryItem? item, bool applyPreviousDyesToNewItem = false) {
			if (applyPreviousDyesToNewItem && Plugin.DyePicker.IsOpen && ConfigurationManager.Config.DyePickerKeepApplyOnNewItem) {
				foreach ((var dyeIndex, var currentDye) in DyePicker.CurrentDyeList) {
					if (currentDye == null || item == null) continue;

					switch (dyeIndex) {
						case 1: item.Stain = currentDye.Value; break;
						case 2: item.Stain2 = currentDye.Value; break;
					}
				}
			}
			DyePicker.CurrentItem = item;
			if (item?.Item.DyeCount < DyePicker.DyeIndex) DyePicker.DyeIndex = item?.Item.DyeCount ?? 1;
			if (item?.Item.DyeCount > 0 && DyePicker.DyeIndex == 0) DyePicker.DyeIndex = 1;

		}

		private async void ApplyItemsAppearancesOnPlayer(InventoryItemSet set) {
			set.ApplyAppearance();
			// return;
			//foreach ((var s, var item) in set.Items)
			//	if (item != null) PrepareModsAndDo(item);

			//foreach ((var s, var item) in set.Items)
			//	if (item != null) ApplyItemAppearanceOnPlayer(item);

			var mods = set.Items.Where(i => i.Value?.IsModded() ?? false).DistinctBy(i => i.Value?.GetMod()).Select(i=>i.Value?.GetMod());
			var numberOfMods = mods.Count();
			if (numberOfMods == 0) return;
			bool isModInstant = !ConfigurationManager.Config.PenumbraDisableModRightAfterApply || numberOfMods <= 1;

			//PluginLog.Debug($"numberOfMods: {numberOfMods} => {(isModInstant ? "instant" : "notinstant")}");

			if (!isModInstant) {
				Dictionary<string, List<(GlamourPlateSlot Slot, InventoryItem Item)>> itemsByMods = new();
				foreach ((var s, var item) in set.Items)
					if (item != null) {
						if (item.IsModded() && !isModInstant && item.ModDirectory != null) {
							PluginLog.Warning($"putting in queue {item.FormattedName} => {item.ModDirectory}");
							itemsByMods.TryAdd(item.ModDirectory, new());
							itemsByMods[item.ModDirectory].Add((s, item));
							//ApplyItemAppearanceOnPlayer(InventoryItem.Zero,s);
						} else {
							PrepareModsAndDo(item, s, ApplyItemAppearanceOnPlayer);
						}
					}
				foreach ((var modDir, var itemsForThisMod) in itemsByMods)
					AddToApplyAppearanceQueue(itemsForThisMod);
				ApplyAppearanceQueueTick(true);
			} else {
				PrepareMods(set);
				var character = PluginServices.Context.LocalPlayer;
				foreach ((var s, var item) in set.Items)
				{
					
					if(item == null || !item.IsModded() || character == null) continue;
				
					await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);
					// PluginLog.Debug($"==============> SetItem HERE {item.ItemId} {item.ModName} <============");
					// PluginServices.Glamourer.SetItem(character, item, s);
					

				}
				//Task.Run(async delegate {
					//PluginServices.Penumbra.CleanDresserApplyCollection();
					//await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);

					// PrepareMods(set);
					//await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);

					// set.ApplyAppearance();
				//});
			}
		}
		private void PrepareMods(InventoryItemSet set) {
			Task.Run(async delegate {

				foreach ((var slot, var item) in set.Items) {
					if (item?.Container == (InventoryType)Storage.InventoryTypeExtra.ModdedItems && item.IsModded() && PluginServices.Penumbra.GetEnabledState()) {
						ExecuteConfigModsInPenumbra(item, slot);
					}
				}
				await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);
				await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);
				await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);
				await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);

			}).Wait();

		}
		private void PrepareModsAndDo(InventoryItem item, GlamourPlateSlot slot, Action<InventoryItem,GlamourPlateSlot>? callback = null, bool ignore_PenumbraDelayAfterModEnableBeforeApplyAppearance = false) {
			if (item.Container == (InventoryType)Storage.InventoryTypeExtra.ModdedItems && item.IsModded() && PluginServices.Penumbra.GetEnabledState()) {
				PluginLog.Verbose($"applying modded item: {item.FormattedName} => {item.ModName}");


				var tast = Task.Run(delegate {
					ExecuteConfigModsInPenumbra(item, slot, callback, ignore_PenumbraDelayAfterModEnableBeforeApplyAppearance);
				});

			} else {
				callback?.Invoke(item, slot);
				CleanupMod(slot);
			}

		}
		private async void ExecuteConfigModsInPenumbra(InventoryItem item, GlamourPlateSlot slot, Action<InventoryItem, GlamourPlateSlot>? callback = null, bool ignore_PenumbraDelayAfterModEnableBeforeApplyAppearance = false) {
			var personalCollection = PluginServices.Penumbra.GetCollectionForLocalPlayerCharacter();

			await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);
			//PluginLog.Debug($"PENUMBRA YOURSELF COLLECTION: {personalCollection}");
			var modSettings = PluginServices.Penumbra.GetCurrentModSettings(personalCollection, item.ModDirectory ?? "", item.ModName ?? "", true);
			//PluginLog.Debug($"GetCurrentModSettings: {modSettings.Item1} | {item.ModName}");
			//modSettings.Item2.Value.Priority

			await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);
			if (modSettings.Item1 == Penumbra.Api.Enums.PenumbraApiEc.Success && modSettings.Item2.HasValue) {
				foreach ((var optionGroup, var options) in modSettings.Item2.Value.EnabledOptions) {
					var res1 = PluginServices.Penumbra.TrySetModSettings(ConfigurationManager.Config.PenumbraCollectionApply, item.ModDirectory!, item.ModName!, optionGroup, options.ToList());
					//PluginLog.Debug($"TrySetModSettings: {res1} | {item.ModName}");
				}
			}

			var res6 = PluginServices.Penumbra.TrySetModPriority(ConfigurationManager.Config.PenumbraCollectionApply, item.ModDirectory!, item.ModName!, CurrentIncrement++);
			await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);
			var res2 = PluginServices.Penumbra.TrySetMod(ConfigurationManager.Config.PenumbraCollectionApply, item.ModDirectory!, true);
			//PluginLog.Debug($"TrySetMod TRUE: {res2} | {item.ModName}");


			// delay before apply
			if (!ignore_PenumbraDelayAfterModEnableBeforeApplyAppearance)
				await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModEnableBeforeApplyAppearance);
			PluginLog.Warning($"Applying appearance...");
			callback?.Invoke(item, slot);
			if (ConfigurationManager.Config.PenumbraDisableModRightAfterApply) {
				// this is where we remove the mod right after applying it
				RemoveModFromPenumbra(item.GetMod());
			} else {
				CleanupMod(slot);
			}
			CurrentPreviousMod = null;
			await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterModDisableBeforeNextModLoop);
			if (ApplyAppearanceQueue.Any()) {
				ApplyAppearanceQueueTick();
			}
		}
		private void CleanupMod(GlamourPlateSlot slot) {

			// this is where we keep the mod, but we remove the previous one IF it is not used in any items
			if (CurrentPreviousMod.HasValue) {
				var curPlate2 = GetCurrentPlate();

				// that's when there was a mod before
				if (! (curPlate2?.HasMod(CurrentPreviousMod) ?? false)) {
					// ok we can remove it
					RemoveModFromPenumbra(CurrentPreviousMod);
				} else {
					// we gotta leave it
				}
			}
		}
	
		private async void RemoveModFromPenumbra((string Path, string Name)? mod) {
			if (mod == null) return;
			await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterApplyAppearanceBeforeModDisable);
			Penumbra.Api.Enums.PenumbraApiEc res3 = Penumbra.Api.Enums.PenumbraApiEc.UnknownError;
			for (var i = 0; i < 10; i++) {

				try {
					res3 = PluginServices.Penumbra.TryInheritMod(ConfigurationManager.Config.PenumbraCollectionApply, mod.Value.Path, mod.Value.Name, true);
					break;
				} catch {

					PluginLog.Debug($"Failed to disable mod after apply, retrying... | {mod.Value.Name}");
					await Task.Delay(ConfigurationManager.Config.PenumbraDelayAfterApplyAppearanceBeforeModDisable);
				}

			}
			PluginLog.Debug($"Disable mod after apply {res3} | {mod.Value.Name}");
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
						PluginServices.ApplyGearChange.ApplyItemAppearanceOnPlayer(item.Item,item.Slot);
					}

				}, ignoreFirstDelay);
			}
		}

		public void FrameworkUpdate() {
		}
		public void SelectCurrentSlot(GlamourPlateSlot slot) {
			GearBrowser.SelectedSlot = slot;
			ConfigurationManager.Config.CurrentGearSelectedSlot = slot;
			DyePicker.SetSelection(GetCurrentPlateItem(slot));
			GearBrowser.RecomputeItems();
		}
		public void ExecuteCurrentItem(GlamourPlateSlot slot) {
			SelectCurrentSlot(slot);
			DyePickerRefreshNewItem(GetCurrentPlateItem(slot));

			Plugin.OpenGearBrowserIfClosed();
			Plugin.UncollapseGearBrowserIfCollapsed();
		}
		public void ExecuteCurrentContextRemoveItem(InventoryItem item, GlamourPlateSlot? slot) {
			if (slot == null) return;
			if (item.ItemId == 0) return;
			CurrentPreviousMod = GetCurrentPlateItem(slot.Value)?.GetMod();
			//GetCurrentPlate()?.RemoveSlot(slot.Value);
			item.Clear();
			PluginLog.Debug($"dddddd ddddddd => {item.ItemId} {item.Item.RowId}");
			ApplyItemAppearanceOnPlayer(item, slot.Value);
			CleanupMod(slot.Value);
			//RestoreAppearance();
			//ReApplyAppearanceAfterEquipUpdate();
		}


		public void ApplyItemAppearanceOnPlayerWithMods(InventoryItem item, GlamourPlateSlot slot)
			=> PrepareModsAndDo(item, slot, ApplyItemAppearanceOnPlayer);
		//public void ApplyItemAppearanceOnPlayer(InventoryItem item, GlamourPlateSlot slot)
		//	=> ApplyItemAppearanceOnPlayer(item, slot);
		public void ApplyItemAppearanceOnPlayer(InventoryItem item, GlamourPlateSlot slot) {
			//if(forceStandalone) Service.ClientState.LocalPlayer?.EquipStandalone(item, slot);
			//else
			var character = PluginServices.Context.LocalPlayer;
			if(character == null) return;
			PluginServices.Glamourer.SetItem(character, item, slot);
				// Service.ClientState.LocalPlayer?.Equip(item,slot);
		}

		public InventoryItemSet? GetCurrentPlate() {
			if(ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				return currentPlate;
			}
			return null;
		}
		public InventoryItem? GetCurrentPlateItem(GlamourPlateSlot slot) {
			return GetCurrentPlate()?.GetSlot(slot);
		}

		public void AppearanceUpdateNakedOrWearing() {
			var currentPlate = GetCurrentPlate();
			if (currentPlate.HasValue && currentPlate.Value.HasModdedItem()) return;

			AppearanceUpdateNakedOrWearing2();
		}
		public void AppearanceUpdateNakedOrWearing2() {
			var set = new InventoryItemSet();


			// this is where we change every items on
			//   - empty slot = naked
			//   - empty slot = show wearing (backed up)


			// get current plate slots
			var currentPlate = GetCurrentPlate();

			if (currentPlate.HasValue) {
				var glamourPlateSlots = Enum.GetValues<GlamourPlateSlot>().Cast<GlamourPlateSlot>();

				// for each glam plate slots
				foreach (var g in glamourPlateSlots) {
					if (currentPlate.Value.Items.TryGetValue(g, out var i) && (i?.ItemId ?? 0) != 0) {
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
			item.Stain2 = 0;
		}
		public void ApplyDye(ushort PlateNumber, GlamourPlateSlot slot, byte stain, ushort stainIndex) {
			if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(PlateNumber, out var plate)) {
				var item = plate.GetSlot(slot);
				if (item != null) {
					switch (stainIndex) {
						case 1: item.Stain  = stain; break;
						case 2: item.Stain2 = stain; break;
					}
					ApplyItemAppearanceOnPlayerWithMods(item, slot);
				}
			}
			CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
		}
		public bool swapDyes() {
			if (GearBrowser.SelectedSlot == null) return false;
			var slot = GearBrowser.SelectedSlot.Value;
			var plateNumber = ConfigurationManager.Config.SelectedCurrentPlate;

			if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(plateNumber, out var plate)) return false;
			var item = plate.GetSlot(slot);
			if (item == null) return false;

			var s1 = item.Stain;
			var s2 = item.Stain2;
			item.Stain = s2;
			item.Stain2 = s1;

			ApplyItemAppearanceOnPlayerWithMods(item, slot);
			CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
			return true;
		}

		public void OpenGlamourDresser() {
			if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.Any(s=>!s.Value.IsEmpty())) {
				PluginLog.Verbose($"Found found no portable plates, populating them with current");
				PluginServices.ApplyGearChange.OverwritePendingWithActualPlates();
			}
		}

		public void BackupAppearance() {
			PluginLog.Verbose("Backing up appearance");
			BackedUpItems = GetCurrentAppearance();
		}
		/*public InventoryItem? GetCurrentAppearanceSlot(GlamourPlateSlot slot) {
			return GetCurrentAppearance(slot).GetSlot(slot);
		}*/
		public InventoryItemSet GetCurrentAppearance(GlamourPlateSlot? slot = null)
		{
			return PluginServices.Glamourer.GetSet();
			InventoryItemSet foundAppearance;

			if(slot == null || !slot.Value.IsWeapon()) {
				var appearanceBackupEquip = PluginServices.Context.LocalPlayer?.EquipmentModels().Dictionary();
				foundAppearance = new InventoryItemSet(appearanceBackupEquip);
			} else {
				foundAppearance = new InventoryItemSet();
			}

			if(slot == null || slot == GlamourPlateSlot.MainHand) {
				var appearanceBackupWeaponMain = PluginServices.Context.LocalPlayer?.MainHandModels().Equip ?? new();
				foundAppearance.SetSlot(GlamourPlateSlot.MainHand, InventoryItem.FromWeaponEquip(appearanceBackupWeaponMain, GlamourPlateSlot.MainHand));
			}

			if (slot == null || slot == GlamourPlateSlot.OffHand) {
				var appearanceBackupWeaponOff = PluginServices.Context.LocalPlayer?.OffHandModels().Equip ?? new();
				foundAppearance.SetSlot(GlamourPlateSlot.OffHand, InventoryItem.FromWeaponEquip(appearanceBackupWeaponOff, GlamourPlateSlot.OffHand));
			}

			return foundAppearance;
		}
		public void RestoreAppearance() {
			PluginLog.Verbose("Restoring appearance");

			if (PluginServices.Context.MustGlamourerApply()) {
				PluginServices.Glamourer.RevertCharacter(PluginServices.Context.LocalPlayer);
				PluginServices.Glamourer.RevertToAutomationCharacter(PluginServices.Context.LocalPlayer);
				return;
			}
			BackedUpItems?.ApplyAppearance();
			BackedUpItems = null;
		}
		public InventoryItemSet? GetBackedUpAppearance() => BackedUpItems;



		public Dictionary<ushort, List<InventoryItem>> TasksOnCurrentPlate = new();
		public void CompileTodoTasks(ushort? plateNumber = null) {
			foreach((var plateN, var set) in ConfigurationManager.Config.PendingPlateItemsCurrentChar) {
				if (plateNumber != null && plateN != plateNumber) continue;
				TasksOnCurrentPlate[plateN] = set.FindNotOwned();
			}
		}

		public void changeCurrentPendingPlate(ushort plateNumber) {
			Task.Run(delegate {
				PluginServices.ApplyGearChange.UnApplyCurrentPendingPlateAppearance();
				ConfigurationManager.Config.SelectedCurrentPlate = plateNumber;
				PluginServices.ApplyGearChange.ApplyCurrentPendingPlateAppearance();
				return Task.CompletedTask;
			});
		}
		public void UnApplyCurrentPendingPlateAppearance() {
			ClearApplyAppearanceQueue();
			if(ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				foreach (var mod in currentPlate.Mods()) {
					if(mod.HasValue) PluginServices.Penumbra.CleanDresserApplyMod(mod.Value);
				}
			}
		}
		public void ApplyCurrentPendingPlateAppearance() {
			if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				//if (currentPlate.HasModdedItem()) PluginServices.Glamourer.RevertCharacter(PluginServices.Context.LocalPlayer);
				currentPlate.UpdateSourcesForOwnedItems();
				CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
				ApplyItemsAppearancesOnPlayer(currentPlate);
			}
			//if (!ConfigurationManager.Config.CurrentGearDisplayGear) StripEmptySlotCurrentPendingPlateAppearance();
			//else ShowStrippedSlots();
		}
		public void ReApplyAppearanceAfterEquipUpdate() {
			BackupAppearance();
			//PluginServices.Glamourer.RevertCharacter(PluginServices.Context.LocalPlayer);
			ApplyCurrentPendingPlateAppearance();
			// PluginServices.ApplyGearChange.AppearanceUpdateNakedOrWearing();

		}
		public void ToggleDisplayGear() {
			ApplyCurrentPendingPlateAppearance();
			// PluginServices.ApplyGearChange.AppearanceUpdateNakedOrWearing();
		}

		public void OverwritePendingWithCurrentPlate() {
			ConfigurationManager.Config.PendingPlateItemsCurrentChar[ConfigurationManager.Config.SelectedCurrentPlate] = ConfigurationManager.Config.DisplayPlateItems.Copy().RemoveEmpty();
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
					ConfigurationManager.Config.PendingPlateItemsCurrentChar[plateNumber] = set;
				}
			});
		}

		public Dictionary<ushort, InventoryItemSet> DifferencesToApply = new();
		private Dictionary<ushort, InventoryItemSet> DifferencesToReplace = new();
		private Dictionary<ushort, InventoryItemSet> AppliedPending = new();

		public void CheckModificationsOnPendingPlates() {
			PluginLog.Verbose("Calculating Modifications On Pending Plates ...");
			var pendingPlates = ConfigurationManager.Config.PendingPlateItemsCurrentChar;
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
			if(ConfigurationManager.Config.OfferApplyAllPlatesOnDresserOpen) Popup_AskApplyOnPlates();
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
			PluginServices.Context.HasConfirmedApplyIntoDresser = true;

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
				if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(platelateNumber, out var pendingPlate)) {
					return pendingPlate.IsDifferentGlam(ggg, out var _, out var _);
				}
			}
			return true;
		}
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
			// todo change plate
			if (DifferencesToApply.TryGetValue(plateIndex, out var replacementGlams)) {
				var successfullyApplied = ApplyToDresserPlateAndRecord(replacementGlams, plateIndex);

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
		public void LeaveGlamourPlateDresser() {
			CleanOverlayColors();
			if(ConfigurationManager.Config.OfferOverwritePendingPlatesAfterApplyAll) Popup_AllDone();
		}
		public void Popup_AllDone() {
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
			PluginServices.Context.HasConfirmedApplyIntoDresser = false;
			PluginLog.Debug(" -- Clean apply dresser -- ");
			CleanOverlayColors();
			DifferencesToApply.Clear();
			DifferencesToReplace.Clear();
			PlatesFailed.Clear();
		}
	}
}
