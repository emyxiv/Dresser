
using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Models;
using Dresser.Models.DyeHistory;
using Dresser.Gui;

namespace Dresser.Services {
	public partial class ApplyGearChange {

		private static History DyeHistory = new();

		/// <summary>Records a dye change in the history unless it originates from an undo/redo action.</summary>
		public static void DyeHistoryAdd(ushort plate, GlamourPlateSlot slot, ushort dyeIndex, ushort dyeIdFrom, ushort dyeIdTo, bool isUndoOrRedo = false) {
			if (isUndoOrRedo) return; // do not add a history entry if this is an undo or redo
			DyeHistory.GetHistory(plate).AddEntry(slot, dyeIndex, dyeIdFrom, dyeIdTo);
		}
		/// <summary>Undoes the last dye change on the current plate.</summary>
		public void DyeHistoryUndo()
			=> DyeHistoryUndoOrRedo(false);
		/// <summary>Redoes the last undone dye change on the current plate.</summary>
		public void DyeHistoryRedo()
			=> DyeHistoryUndoOrRedo(true);
		/// <summary>Applies an undo or redo step from the dye history of the current plate.</summary>
		private void DyeHistoryUndoOrRedo(bool forward) {
			var previous = DyeHistory.GetHistory(ConfigurationManager.Config.SelectedCurrentPlate).UndoOrRedo(forward);
			if (previous == null) return;
			ApplyDye(ConfigurationManager.Config.SelectedCurrentPlate, previous.Slot, (byte)previous.DyeIdTo, previous.DyeIndex, true);
		}
		/// <summary>Returns the dye history entries for the currently selected plate.</summary>
		public Plate GetCurrentPlateDyeHistory() {
			return DyeHistory.GetHistory(ConfigurationManager.Config.SelectedCurrentPlate);
		}


		/// <summary>Opens the dye picker for the given item (not yet implemented).</summary>
		public void ExecuteCurrentContextDye(InventoryItem item) {
			PluginLog.Warning("TODO: open dye picker");
		}
		/// <summary>Removes both dye channels from the given item.</summary>
		public void ExecuteCurrentContextRemoveDye(InventoryItem item) {
			item.Stain = 0;
			item.Stain2 = 0;
		}
		/// <summary>Applies a dye to a specific slot/channel on a plate and refreshes the player appearance.</summary>
		public void ApplyDye(ushort plateNumber, GlamourPlateSlot slot, byte stain, ushort stainIndex, bool isUndoOrRedo = false) {
			if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(plateNumber, out var plate)) {
				var item = plate.GetSlot(slot);
				if (item != null) {

					switch (stainIndex) {
						case 1: DyeHistoryAdd(plateNumber, slot, stainIndex, item.Stain,  stain, isUndoOrRedo); item.Stain  = stain; break;
						case 2: DyeHistoryAdd(plateNumber, slot, stainIndex, item.Stain2, stain, isUndoOrRedo); item.Stain2 = stain; break;
					}
					ApplyItemAppearanceOnPlayerWithMods(item, slot);
				}
			}
			CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
		}
		/// <summary>Swaps dye channel 1 and 2 for the currently selected slot. Returns false if no slot is selected.</summary>
		public bool SwapDyesForCurrentSlotInCurrentPlate() {
			if (GearBrowser.SelectedSlot == null) return false;
			var slot = GearBrowser.SelectedSlot.Value;
			var plateNumber = ConfigurationManager.Config.SelectedCurrentPlate;

			if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(plateNumber, out var plate)) return false;
			var item = plate.GetSlot(slot);
			if (item == null) return false;

			SwapDyeCurrentPlateForItem(item, slot);

			CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
			return true;
		}
		/// <summary>Swaps dye channel 1 and 2 for every item on the current plate.</summary>
		public void SwapDyesForAllItemsInCurrentPlate() {
			if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plate)) return;
			foreach((var z,var x) in plate.Items) {
				if(x == null) continue;
				SwapDyeCurrentPlateForItem(x, z);
			}
			CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
		}
		/// <summary>Swaps dye channels on a single item and updates its appearance on the player.</summary>
		private void SwapDyeCurrentPlateForItem(InventoryItem item, GlamourPlateSlot slot) {
			var s1 = item.Stain;
			var s2 = item.Stain2;
			if (s1 == s2) return;
			DyeHistoryAdd(ConfigurationManager.Config.SelectedCurrentPlate, slot, 1, item.Stain, s2);
			DyeHistoryAdd(ConfigurationManager.Config.SelectedCurrentPlate, slot, 2, item.Stain2, s1);
			item.Stain = s2;
			item.Stain2 = s1;

			// change appearance for item
			ApplyItemAppearanceOnPlayerWithMods(item, slot);
		}

		/// <summary>Applies the dyes of the currently selected slot to all items on the plate.</summary>
		public void DyeAllWithCurrentSelectedSlot() {
			if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plate)) return;
			var itemModel = plate.GetSlot(ConfigurationManager.Config.CurrentGearSelectedSlot);
			if (itemModel == null) return;
			DyeAllWith(itemModel.Stain, itemModel.Stain2);
		}
		/// <summary>Removes dyes from all items on the current plate.</summary>
		public void DyeAllWithNone() {
			if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plate)) return;
			var itemModel = plate.GetSlot(ConfigurationManager.Config.CurrentGearSelectedSlot);
			if (itemModel == null) return;
			DyeAllWith(0, 0);
		}
		/// <summary>Sets both dye channels on every dyeable item in the current plate and updates appearances.</summary>
		private void DyeAllWith(byte stain1, byte stain2) {
			if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plate)) return;
			foreach ((var slot, var item) in plate.Items) {
				if (item == null) continue;
				if (!item.Item.IsDyeable1()) continue;
				DyeHistoryAdd(ConfigurationManager.Config.SelectedCurrentPlate, slot, 1, item.Stain, stain1);
				item.Stain  = stain1;
				if (item.Item.IsDyeable2()) {
					DyeHistoryAdd(ConfigurationManager.Config.SelectedCurrentPlate, slot, 2, item.Stain2, stain2);
					item.Stain2 = stain2;
				}

				ApplyItemAppearanceOnPlayerWithMods(item, slot);
			}
			CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
		}
		/// <summary>Clears both dye channels on the currently selected slot.</summary>
		public void DyeWithNone() {
			if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plate)) return;
			var item = plate.GetSlot(ConfigurationManager.Config.CurrentGearSelectedSlot);
			if (item == null) return;
			DyeHistoryAdd(ConfigurationManager.Config.SelectedCurrentPlate, ConfigurationManager.Config.CurrentGearSelectedSlot, 1, item.Stain, 0);
			item.Stain = 0;
			DyeHistoryAdd(ConfigurationManager.Config.SelectedCurrentPlate, ConfigurationManager.Config.CurrentGearSelectedSlot, 2, item.Stain2, 0);
			item.Stain2 = 0;
			ApplyItemAppearanceOnPlayerWithMods(item, ConfigurationManager.Config.CurrentGearSelectedSlot);
			CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
		}
		/// <summary>Updates the dye picker state for a newly selected item, optionally carrying over previous dyes.</summary>
		public void DyePickerRefreshNewItem(InventoryItem? item, bool applyPreviousDyesToNewItem = false) {
			if (applyPreviousDyesToNewItem && ConfigurationManager.Config.DyePickerKeepApplyOnNewItem) {
				foreach ((var dyeIndex, var currentDye) in DyePicker.CurrentDyeList) {
					if (currentDye == null || item == null) continue;

					switch (dyeIndex) {
						case 1: item.Stain = currentDye.Value; break;
						case 2: item.Stain2 = currentDye.Value; break;
					}
				}
			}
			DyePicker.CurrentItem = item;
			if (item?.Item.Base.DyeCount < DyePicker.DyeIndex) DyePicker.DyeIndex = item?.Item.Base.DyeCount ?? 1;
			if (item?.Item.Base.DyeCount > 0 && DyePicker.DyeIndex == 0) DyePicker.DyeIndex = 1;

		}
	}
}
