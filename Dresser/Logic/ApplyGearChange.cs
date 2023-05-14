using System;
using System.Collections.Generic;
using System.Linq;

using CriticalCommonLib.Models;
using CriticalCommonLib.Extensions;
using CriticalCommonLib;
using Dalamud.Logging;
using Dresser.Extensions;
using Dresser.Structs.FFXIV;
using Dresser.Windows;
using Dresser.Structs.Actor;
using Dresser.Data;

namespace Dresser.Logic {
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
			PluginLog.Warning("Entering Dresser");

			ReApplyAppearanceAfterEquipUpdate();
		}
		public void ExitBrowsingMode() {
			PluginLog.Warning("Closing Dresser");
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

			var slot = item.Item.GlamourPlateSlot();

			if (slot != null && ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plate)) {
				plate[(GlamourPlateSlot)slot] = item.Copy()!;
			}

			Service.ClientState.LocalPlayer?.Equip(item);
		}
		public void ExecuteCurrentItem(GlamourPlateSlot slot) {
			GearBrowser.SelectedSlot = slot;
			GearBrowser.RecomputeItems();
			this.Plugin.OpenGearBrowserIfClosed();
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


		public void BackupAppearance() {
			AppearanceBackupWeaponMain = PluginServices.Context.LocalPlayer?.MainHandModels().Equip ?? new();
			AppearanceBackupWeaponOff = PluginServices.Context.LocalPlayer?.OffHandModels().Equip ?? new();
			AppearanceBackupEquip = PluginServices.Context.LocalPlayer?.EquipmentModels().Dictionary();
			PluginLog.Verbose("Backed up appearance");
		}
		public void RestoreAppearance() {
			PluginLog.Verbose("Restoring appearance");

			PluginServices.Context.LocalPlayer?.Equip(WeaponIndex.MainHand, AppearanceBackupWeaponMain);
			PluginServices.Context.LocalPlayer?.Equip(WeaponIndex.OffHand, AppearanceBackupWeaponOff);
			if (AppearanceBackupEquip != null)
				foreach ((var index, var item) in AppearanceBackupEquip) {
					PluginServices.Context.LocalPlayer?.Equip(index, item);
				}
		}
		public void ApplyCurrentPendingPlateAppearance() {
			if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				foreach ((var s, var item) in currentPlate) {
					PluginServices.Context.LocalPlayer?.Equip(item);
				}
			}
		}
		public void ReApplyAppearanceAfterEquipUpdate() {
			BackupAppearance();
			ApplyCurrentPendingPlateAppearance();
			if (!ConfigurationManager.Config.CurrentGearDisplayGear) StripEmptySlotCurrentPendingPlateAppearance();

		}
		public void StripEmptySlotCurrentPendingPlateAppearance() {
			if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				var glamourPlates = Enum.GetValues<GlamourPlateSlot>().Cast<GlamourPlateSlot>();
				foreach(var g in glamourPlates) {
					if(!currentPlate.TryGetValue(g, out var item) || item.ItemId == 0) {
						var index = g.ToEquipIndex();
						if(index != null) {
							PluginServices.Context.LocalPlayer?.Equip((EquipIndex)index, new ItemEquip() { Id = 0, Variant = 0, Dye = 0 });
						} else {
							var indexW = g.ToWeaponIndex();
							if (indexW != null)
								PluginServices.Context.LocalPlayer?.Equip((WeaponIndex)indexW, new WeaponEquip() { Base = 0, Dye = 0, Set = 0, Variant = 0 });
						}
					}
				}
			}
		}
		public void ShowStrippedSlots() {
			if (ConfigurationManager.Config.PendingPlateItems.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				var glamourPlates = Enum.GetValues<GlamourPlateSlot>().Cast<GlamourPlateSlot>();
				foreach (var g in glamourPlates) {
					if (!currentPlate.TryGetValue(g, out var item) || item.ItemId == 0) {
						var index = g.ToEquipIndex();
						if (index != null && AppearanceBackupEquip!.TryGetValue((EquipIndex)index, out var equip)) {
							PluginServices.Context.LocalPlayer?.Equip((EquipIndex)index, equip);
						} else {
							var indexW = g.ToWeaponIndex();
							if (indexW == WeaponIndex.MainHand)
								PluginServices.Context.LocalPlayer?.Equip((WeaponIndex)indexW, AppearanceBackupWeaponMain);
							else if(indexW == WeaponIndex.OffHand)
								PluginServices.Context.LocalPlayer?.Equip((WeaponIndex)indexW, AppearanceBackupWeaponOff);
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
			ConfigurationManager.Config.PendingPlateItems[ConfigurationManager.Config.SelectedCurrentPlate] = ConfigurationManager.Config.DisplayPlateItems;
		}

		public void CheckModificationsOnPendingPlates() {
			PluginLog.Debug("Start CheckModificationsOnPendingPlates ...");
			var pendingPlates = ConfigurationManager.Config.PendingPlateItems;
			var actualPlates = Storage.Pages!.Select((value,index) => new { value, index }).ToDictionary(pair=>(ushort)pair.index,pair=> Gathering.MirageToInvItems(pair.value));


			Dictionary<ushort, Dictionary<GlamourPlateSlot, InventoryItem?>> differencesToApply = new();

			foreach ((var plateIndex, var actualPlateValues) in actualPlates) {
				pendingPlates.TryGetValue(plateIndex, out var pendingPlateValues_tmp);
				if (pendingPlateValues_tmp == null) PluginLog.Debug($"pending plate {plateIndex} is NULL");
				Dictionary<GlamourPlateSlot, InventoryItem> pendingPlateValues = pendingPlateValues_tmp ?? new();


				foreach ((var slot, var actualInventoryItem) in actualPlateValues) {
					PluginLog.Verbose($"checking {(int)slot} on actual plate {plateIndex} (slot name: {slot})");

					bool toChange = false;
					InventoryItem? itemReplacement = null;
					if (!pendingPlateValues.TryGetValue(slot, out var pendingInventoryItem)) {
						PluginLog.Verbose($"Item {slot} not present on pending plate");
						toChange = true;
					} else {
						PluginLog.Verbose($"item id difference? : {actualInventoryItem.ItemId} != {pendingInventoryItem.ItemId}");
						bool itemDifferent = actualInventoryItem.ItemId != pendingInventoryItem.ItemId;
						bool dyeDifferent = actualInventoryItem.Stain != pendingInventoryItem.Stain;
						if (itemDifferent) {
							PluginLog.Verbose($"Plate {plateIndex} slot {(int)slot}: Item is different: pending:{pendingInventoryItem.ItemId} => actual:{actualInventoryItem.ItemId}  (slot name: {slot})");
						}
						if (dyeDifferent) {
							PluginLog.Verbose($"Plate {plateIndex} slot {(int)slot}: Dye is different pending:{pendingInventoryItem.Stain} => actual:{actualInventoryItem.Stain}  (slot name: {slot})");
						}
						toChange = itemDifferent || dyeDifferent;
						itemReplacement = pendingInventoryItem;

					}

					if(toChange) {
						if (!differencesToApply.TryGetValue(plateIndex, out var plateChanges) || plateChanges == null)
							differencesToApply[plateIndex] = new();

						differencesToApply[plateIndex][slot] = itemReplacement;
					}
				}
			}

			ApplyChangesDialog(differencesToApply);
		}


		public void ApplyChangesDialog(Dictionary<ushort, Dictionary<GlamourPlateSlot, InventoryItem?>> differencesToApply) {

			PluginLog.Debug($"start apply changes on plates ...");

			foreach ((var plateIndex, var changeValues) in differencesToApply) {
				PluginLog.Debug($"inserting glams on plate {plateIndex} ...");



				// todo change plate
				foreach ((var slot, var itemToApply) in changeValues) {


					// todo find item container in armoire or prismbox
					// todo if one item missing, report to the user, offer options: "still apply witout missing", "skip this plate", "stop"
					// todo change container after this finding
					PluginServices.GlamourPlates.ModifyGlamourPlateSlot(itemToApply, slot);
				}

				// todo auto click save
				// todo wait the window open
				// todo if simple "save", ask user to click Yes (or auto...)
				// todo if missing dye and window "missing dye" opens, offer options: "skip this plate", "stop"

				// todo apply actual plate number change
				break; // temprorary only do the first plate

			}
		}

	}
}
