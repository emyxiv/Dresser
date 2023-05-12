using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Plugin;

using Lumina.Excel.GeneratedSheets;

using CriticalCommonLib.Models;
using CriticalCommonLib.Extensions;
using CriticalCommonLib;
using Dalamud.Logging;
using Dresser.Extensions;
using Dresser.Structs.FFXIV;
using Dresser.Windows;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dresser.Structs.Actor;

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
			PluginLog.Debug("Backed up appearance");
		}
		public void RestoreAppearance() {
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



	}
}
