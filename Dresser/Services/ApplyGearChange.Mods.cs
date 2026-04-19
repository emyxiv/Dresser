/// ApplyGearChange.Mods.cs
/// Penumbra mod handling: enabling/disabling temporary mod settings when previewing
/// modded items in the gear browser. Manages the lifecycle of temporary mods so that
/// only the mods needed by the current plate are active, and previous mods are cleaned up.

using CriticalCommonLib.Enums;

using Dresser.Interop.Agents;
using Dresser.Logic;
using Dresser.Models;

using System;
using System.Threading.Tasks;

using InventoryItem = Dresser.Models.InventoryItem;

namespace Dresser.Services {
	public partial class ApplyGearChange {

		/// <summary>
		/// Checks if the item is a modded item and, if so, configures its mod in Penumbra on a background
		/// thread before invoking the callback. For non-modded items, cleans up any previous mod in the
		/// slot and invokes the callback directly.
		/// </summary>
		private void PrepareModsAndDo(InventoryItem item, GlamourPlateSlot slot, Action<InventoryItem, GlamourPlateSlot>? callback = null) {
			if (item.Container == (InventoryType)Storage.InventoryTypeExtra.ModdedItems && item.IsModded() && PluginServices.Penumbra.GetEnabledState()) {
				PluginLog.Verbose($"applying modded item: {item.FormattedName} => {item.ModName}");

				Task.Run(delegate {
					PluginLog.Verbose($"Executing modded item in Penumbra: {item.FormattedName} => {item.ModName}");
					ConfigureModInPenumbra(item, slot, callback);
				});

			} else {
				CleanupMod(slot, CurrentPreviousModdedItem);
				callback?.Invoke(item, slot);
			}
		}

		/// <summary>
		/// Prepares all modded items in a set by enabling their temporary mod settings in Penumbra.
		/// Blocks the calling thread until all mods are configured.
		/// </summary>
		private void PrepareMods(InventoryItemSet set) {
			Task.Run(delegate {
				foreach ((var slot, var item) in set.Items) {
					if (item?.Container == (InventoryType)Storage.InventoryTypeExtra.ModdedItems && item.IsModded() && PluginServices.Penumbra.GetEnabledState()) {
						ConfigureModInPenumbra(item, slot);
					}
				}
			}).Wait();
		}

		/// <summary>
		/// Configures temporary mod settings in Penumbra for a single modded item.
		/// Cleans up the previous mod in the slot first, then enables the new mod.
		/// </summary>
		private void ConfigureModInPenumbra(InventoryItem item, GlamourPlateSlot slot, Action<InventoryItem, GlamourPlateSlot>? callback = null) {
			CleanupMod(slot, CurrentPreviousModdedItem);
			if (PluginServices.Context.PenumbraState) {
				var result = PluginServices.Penumbra.SetTemporaryModSettings(item);
				PluginLog.Debug($"SetTemporaryModSettings: {result} | {item.ModName}");
			}

			PluginLog.Warning($"Applying appearance...");
			callback?.Invoke(item, slot);
		}

		/// <summary>
		/// Removes the previous mod from Penumbra if it is no longer used by any item in the current plate.
		/// This avoids removing a mod that is still needed by another slot.
		/// </summary>
		private void CleanupMod(GlamourPlateSlot slot, InventoryItem? item) {
			if (!PluginServices.Context.PenumbraState) return;
			if (item == null) return;

			var curPlate = GetCurrentPlate();
			if (!(curPlate?.HasMod(item.GetMod()) ?? false)) {
				RemoveModFromPenumbra(item);
			}
		}

		/// <summary>Forcefully removes a mod from Penumbra regardless of whether other slots use it.</summary>
		private void CleanupModForce(InventoryItem? item) {
			if (!PluginServices.Context.PenumbraState) return;
			if (item == null) return;
			RemoveModFromPenumbra(item);
		}

		/// <summary>Removes a single item's temporary mod settings from Penumbra.</summary>
		private void RemoveModFromPenumbra(InventoryItem item) {
			if (!PluginServices.Context.PenumbraState) return;
			if (!item.IsModded()) return;
			if (PluginServices.Penumbra.RemoveTemporaryModSettings(item)) {
				PluginLog.Debug($"Removing mod from Penumbra: {item.FormattedName} => {item.ModName}");
			}
		}

		/// <summary>Removes all temporary mod settings from Penumbra (used on appearance restore).</summary>
		private void RemoveAllModsFromPenumbra() {
			if (!PluginServices.Context.PenumbraState) return;
			PluginServices.Penumbra.RemoveAllTemporaryModSettings();
		}
	}
}
