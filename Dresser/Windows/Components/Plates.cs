using System.Collections.Generic;

using ImGuiNET;

using Dresser.Data;
using Dresser.Structs.FFXIV;

namespace Dresser.Windows.Components {
	internal class Plates {
		public static List<GlamourPlateSlot> SlotsLeft = new() {
			GlamourPlateSlot.MainHand,
			GlamourPlateSlot.Head,
			GlamourPlateSlot.Body,
			GlamourPlateSlot.Hands,
			GlamourPlateSlot.Legs,
			GlamourPlateSlot.Feet,
		};
		public static List<GlamourPlateSlot> SlotsRight = new() {
			GlamourPlateSlot.OffHand,
			GlamourPlateSlot.Ears,
			GlamourPlateSlot.Neck,
			GlamourPlateSlot.Wrists,
			GlamourPlateSlot.RightRing,
			GlamourPlateSlot.LeftRing,
		};


		public static void DrawLeftSlots() => DrawSlots(SlotsLeft);

		public static void DrawRightSlots() => DrawSlots(SlotsRight);

		public static void DrawSlots(IEnumerable<GlamourPlateSlot> slots) {
			foreach (var slot in slots) {
				Storage.SlotItemsEx.TryGetValue(slot, out var item);

				var image = PluginServices.IconStorage.Get(item);
				ImGui.ImageButton(image.ImGuiHandle, ItemIcon.IconSize);
			}
		}


		public static void Draw() {
			if (Storage.DisplayPage == null) return;

			DrawDisplay(Storage.DisplayPage.Value);
		}
		private static void DrawDisplay(MiragePage plate) {
			ImGui.Columns(2, "Plate##DrawDisplay", false);
			DrawLeftSlots();
			ImGui.NextColumn();
			DrawRightSlots();
			ImGui.Columns();

		}

	}
}
