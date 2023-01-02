﻿using System.Collections.Generic;

using ImGuiNET;

using Dresser.Data;
using Dresser.Structs.FFXIV;
using CriticalCommonLib.Models;
using static FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;
using CriticalCommonLib.Enums;

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


		private static uint? HoveredItem = null;
		public static void DrawSlots(IEnumerable<GlamourPlateSlot> slots) {
			bool isTooltipActive = false;
			foreach (var slot in slots) {
				Storage.SlotInventoryItems.TryGetValue(slot, out var item);
				bool isHovered = item?.ItemId == HoveredItem;
				bool wasHovered = isHovered;
				var iconClicked = ItemIcon.DrawIcon(item, ref isHovered, ref isTooltipActive);
				if (isHovered)
					HoveredItem = item?.ItemId;
				else if (!isHovered && wasHovered)
					HoveredItem = null;
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