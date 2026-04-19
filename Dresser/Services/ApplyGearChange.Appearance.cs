/// ApplyGearChange.Appearance.cs
/// Browsing mode lifecycle, appearance backup/restore, and item application on the local player.
/// This partial handles the core flow: entering/exiting the gear browser, applying individual
/// item appearances (via Glamourer), and managing the backup/restore cycle so the player's
/// original look is preserved while browsing.

using CriticalCommonLib.Enums;

using Dresser.Enums;
using Dresser.Extensions;
using Dresser.Interop.Agents;
using Dresser.Logic;
using Dresser.Models;
using Dresser.Gui;

using System;
using System.Linq;
using System.Threading.Tasks;

using InventoryItem = Dresser.Models.InventoryItem;

namespace Dresser.Services {
	public partial class ApplyGearChange {

		// ── Browsing Mode Lifecycle ───────────────────────────────────────

		/// <summary>
		/// Called when the CurrentGear window opens.
		/// Configures the starting plate based on user preference and applies its appearance.
		/// </summary>
		public void EnterBrowsingMode() {
			switch (ConfigurationManager.Config.BehaviorOnOpen)
			{
				case BehaviorOnOpen.LastOpenedPortablePlate:
					break;
				case BehaviorOnOpen.SandboxPlateAndStrip:
					ConfigurationManager.Config.SelectedCurrentPlate = ushort.MaxValue;
					ConfigurationManager.Config.CurrentGearDisplayGear = false;
					ConfigurationManager.Config.PendingPlateItemsCurrentChar[ushort.MaxValue] = new();
					break;
				case BehaviorOnOpen.SandboxPlateWithWearingGlam:
					ConfigurationManager.Config.PendingPlateItemsCurrentChar[ushort.MaxValue] = GetCurrentAppearance();
					ConfigurationManager.Config.SelectedCurrentPlate = ushort.MaxValue;
					break;
			}
			Task.Run(ReApplyAppearanceAfterEquipUpdate);
		}

		/// <summary>
		/// Called when the CurrentGear window closes.
		/// Closes the browser and restores the player's original appearance.
		/// </summary>
		public void ExitBrowsingMode() {
			PluginLog.Verbose("Closing Dresser");
			Plugin.CloseBrowser();
			RestoreAppearance();
		}


		// ── Item Application ─────────────────────────────────────────────

		/// <summary>
		/// Called when the user selects an item from the gear browser.
		/// Clones the item, updates the current plate, applies mods if needed, and shows it on the player.
		/// </summary>
		public void ExecuteBrowserItem(InventoryItem item) {
			PluginLog.Verbose($"Execute apply item {item.Item.NameString} {item.Item.RowId}");

			var clonedItem = item.Clone();
			DyePickerRefreshNewItem(clonedItem, true);

			var slot = clonedItem.Item.GlamourPlateSlot();

			if (slot != null) {
				if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out InventoryItemSet plate)) {
					plate = new();
					ConfigurationManager.Config.PendingPlateItemsCurrentChar[ConfigurationManager.Config.SelectedCurrentPlate] = plate;
				}
				CurrentPreviousModdedItem = plate.GetSlot(slot.Value);
				plate.SetSlot(slot.Value, clonedItem);

				PrepareModsAndDo(clonedItem, slot.Value, ApplyItemAppearanceOnPlayer);
				CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
			}
		}

		/// <summary>
		/// Applies a single item's appearance on the player via Glamourer, handling mods first if needed.
		/// </summary>
		public void ApplyItemAppearanceOnPlayerWithMods(InventoryItem item, GlamourPlateSlot slot)
			=> PrepareModsAndDo(item, slot, ApplyItemAppearanceOnPlayer);

		/// <summary>
		/// Applies a single item's appearance on the local player character via Glamourer.
		/// Also ensures head visibility is toggled on when equipping a head piece.
		/// </summary>
		public void ApplyItemAppearanceOnPlayer(InventoryItem item, GlamourPlateSlot slot) {
			var character = PluginServices.Context.LocalPlayer;
			if (character == null) return;
			PluginServices.Glamourer.SetItem(character, item, slot);
			if (slot == GlamourPlateSlot.Head && !ConfigurationManager.Config.CurrentGearDisplayHat) {
				ConfigurationManager.Config.CurrentGearDisplayHat = true;
				PluginServices.Context.LocalPlayer?.SetHatVisibility();
			}
		}

		/// <summary>
		/// Applies all items in an InventoryItemSet on the player, preparing mods for any modded items first.
		/// </summary>
		private void ApplyItemsAppearancesOnPlayer(InventoryItemSet set) {
			var mods = set.Items.Where(i => i.Value?.IsModded() ?? false).DistinctBy(i => i.Value?.GetMod()).Select(i => i.Value?.GetMod());
			if (mods.Any()) {
				PrepareMods(set);
			}
			set.ApplyAppearance();
		}


		// ── Slot Selection & Interaction ─────────────────────────────────

		/// <summary>
		/// Selects a glamour plate slot: switches browser to clothes mode, updates the selected slot,
		/// refreshes the dye picker, and recomputes browser items for the slot's category.
		/// </summary>
		public void SelectCurrentSlot(GlamourPlateSlot slot) {
			Plugin.GetInstance().GearBrowser.SwitchToClothesMode();
			GearBrowser.SelectedSlot = slot;
			ConfigurationManager.Config.CurrentGearSelectedSlot = slot;
			DyePicker.SetSelection(GetCurrentPlateItem(slot));
			GearBrowser.RecomputeItems();
		}

		/// <summary>Opens the gear browser window and un-collapses it if collapsed.</summary>
		public void OpenBrowserAndUncollapse() {
			Plugin.OpenGearBrowserIfClosed();
			Plugin.UncollapseGearBrowserIfCollapsed();
		}

		/// <summary>
		/// Called when the user clicks an existing item slot in CurrentGear.
		/// Selects the slot, refreshes dye picker, and opens the browser.
		/// </summary>
		public void ExecuteCurrentItem(GlamourPlateSlot slot) {
			SelectCurrentSlot(slot);
			DyePickerRefreshNewItem(GetCurrentPlateItem(slot));
			OpenBrowserAndUncollapse();
		}

		/// <summary>
		/// Removes an item from the current plate slot and shows an empty appearance on the player.
		/// </summary>
		public void ExecuteCurrentContextRemoveItem(InventoryItem item, GlamourPlateSlot? slot) {
			if (slot == null) return;
			if (item.ItemId == 0) return;
			CurrentPreviousModdedItem = GetCurrentPlateItem(slot.Value);
			GetCurrentPlate()?.RemoveSlot(slot.Value);

			CleanupMod(slot.Value, CurrentPreviousModdedItem);
			ApplyItemAppearanceOnPlayer(InventoryItem.Zero, slot.Value);
		}


		// ── Appearance Backup / Restore ──────────────────────────────────

		/// <summary>Backs up the player's current appearance so it can be restored later.</summary>
		public void BackupAppearance() {
			PluginLog.Verbose("Backing up appearance");
			BackedUpItems = GetCurrentAppearance();
		}

		/// <summary>Gets the player's current appearance from Glamourer.</summary>
		public InventoryItemSet GetCurrentAppearance(GlamourPlateSlot? slot = null)
			=> PluginServices.Glamourer.GetSet();

		/// <summary>
		/// Restores the player's appearance to what was backed up before browsing.
		/// Removes all temporary mod settings and reverts Glamourer state.
		/// </summary>
		public void RestoreAppearance() {
			PluginLog.Verbose("Restoring appearance");
			RemoveAllModsFromPenumbra();

			if (PluginServices.Context.MustGlamourerApply()) {
				PluginServices.Glamourer.RevertCharacter(PluginServices.Context.LocalPlayer);
				PluginServices.Glamourer.RevertToAutomationCharacter(PluginServices.Context.LocalPlayer);
				return;
			}
			BackedUpItems?.ApplyAppearance();
			BackedUpItems = null;
		}

		/// <summary>Returns the backed-up appearance (before browsing started), or null.</summary>
		public InventoryItemSet? GetBackedUpAppearance() => BackedUpItems;


		// ── Plate Appearance Application ─────────────────────────────────

		/// <summary>
		/// Applies the entire current pending plate appearance on the player.
		/// Updates owned-item sources and compiles todo tasks.
		/// </summary>
		public void ApplyCurrentPendingPlateAppearance() {
			if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				currentPlate.UpdateSourcesForOwnedItems();
				CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
				ApplyItemsAppearancesOnPlayer(currentPlate);
			}
		}

		/// <summary>
		/// Un-applies the current plate by cleaning up any active Penumbra mods for modded items.
		/// </summary>
		public void UnApplyCurrentPendingPlateAppearance() {
			if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var currentPlate)) {
				foreach ((var key, var item) in currentPlate.Items) {
					if (item != null && item.IsModded()) PluginServices.Penumbra.CleanDresserApplyMod(item);
				}
			}
		}

		/// <summary>
		/// Re-applies the current plate appearance after an equipment update.
		/// Backs up the current look first so it can be restored on exit.
		/// </summary>
		public void ReApplyAppearanceAfterEquipUpdate() {
			BackupAppearance();
			ApplyCurrentPendingPlateAppearance();
		}

		/// <summary>Re-applies the current plate when the "display gear" toggle changes.</summary>
		public void ToggleDisplayGear() {
			ApplyCurrentPendingPlateAppearance();
		}


		// ── Naked / Wearing Toggle ───────────────────────────────────────

		/// <summary>
		/// Updates empty slots to show either the backed-up wearing gear or naked,
		/// depending on the CurrentGearDisplayGear config toggle.
		/// Equipped slots are left unchanged.
		/// </summary>
		public void AppearanceUpdateNakedOrWearing() {
			var set = new InventoryItemSet();
			var currentPlate = GetCurrentPlate();

			if (currentPlate.HasValue) {
				var glamourPlateSlots = Enum.GetValues<GlamourPlateSlot>().Cast<GlamourPlateSlot>();
				foreach (var g in glamourPlateSlots) {
					if (currentPlate.Value.Items.TryGetValue(g, out var i) && (i?.ItemId ?? 0) != 0) {
						// slot is equipped, no change needed
					} else {
						if (ConfigurationManager.Config.CurrentGearDisplayGear) {
							// show backed up (wearing) gear
							set.SetSlot(g, BackedUpItems?.GetSlot(g));
						} else {
							// show naked
							set.SetSlot(g, InventoryItem.Zero);
						}
					}
				}
			}

			set.ApplyAppearance();
		}
	}
}
