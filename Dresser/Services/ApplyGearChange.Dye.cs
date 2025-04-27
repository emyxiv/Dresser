
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Structs.Dresser;
using Dresser.Structs.Dresser.DyeHistory;
using Dresser.Windows;

namespace Dresser.Services {
	public partial class ApplyGearChange {

		private static History DyeHistory = new();

		public static void DyeHistoryAdd(ushort plate, GlamourPlateSlot slot, ushort dyeIndex, ushort dyeIdFrom, ushort dyeIdTo, bool isUndoOrRedo = false) {
			if (isUndoOrRedo) return; // do not add a history entry if this is an undo or redo
			DyeHistory.GetHistory(plate).AddEntry(slot, dyeIndex, dyeIdFrom, dyeIdTo);
		}
		public void DyeHistoryUndo()
			=> DyeHistoryUndoOrRedo(false);
		public void DyeHistoryRedo()
			=> DyeHistoryUndoOrRedo(true);
		private void DyeHistoryUndoOrRedo(bool forward) {
			var previous = DyeHistory.GetHistory(ConfigurationManager.Config.SelectedCurrentPlate).UndoOrRedo(forward);
			if (previous == null) return;
			ApplyDye(ConfigurationManager.Config.SelectedCurrentPlate, (GlamourPlateSlot)previous.Slot, (byte)previous.DyeIdFrom, previous.DyeIndex, true);
		}
		public Plate GetCurrentPlateDyeHistory() {
			return DyeHistory.GetHistory(ConfigurationManager.Config.SelectedCurrentPlate);
		}


		public void ExecuteCurrentContextDye(InventoryItem item) {
			PluginLog.Warning("TODO: open dye picker");
		}
		public void ExecuteCurrentContextRemoveDye(InventoryItem item) {
			item.Stain = 0;
			item.Stain2 = 0;
		}
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
		public void SwapDyesForAllItemsInCurrentPlate() {
			if (!ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(ConfigurationManager.Config.SelectedCurrentPlate, out var plate)) return;
			foreach((var z,var x) in plate.Items) {
				if(x == null) continue;
				SwapDyeCurrentPlateForItem(x, z);
			}
			CompileTodoTasks(ConfigurationManager.Config.SelectedCurrentPlate);
		}
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
