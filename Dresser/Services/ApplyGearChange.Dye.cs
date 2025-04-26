
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Structs.Dresser;
using Dresser.Windows;

namespace Dresser.Services {
	public partial class ApplyGearChange {

		public void ExecuteCurrentContextDye(InventoryItem item) {
			PluginLog.Warning("TODO: open dye picker");
		}
		public void ExecuteCurrentContextRemoveDye(InventoryItem item) {
			item.Stain = 0;
			item.Stain2 = 0;
		}
		public void ApplyDye(ushort plateNumber, GlamourPlateSlot slot, byte stain, ushort stainIndex) {
			if (ConfigurationManager.Config.PendingPlateItemsCurrentChar.TryGetValue(plateNumber, out var plate)) {
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
