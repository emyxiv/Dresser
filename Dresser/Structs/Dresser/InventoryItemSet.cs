using CriticalCommonLib.Models;

using Dresser.Extensions;
using Dresser.Interop.Hooks;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Dresser.Structs.Dresser {
	public struct InventoryItemSet {
		public Dictionary<GlamourPlateSlot, InventoryItem?> Items;
		public InventoryItemSet() {
			Items = new();
		}
		public InventoryItemSet(Dictionary<GlamourPlateSlot, InventoryItem?> items) {
			Items = items;
		}
		public static explicit operator SavedPlate(InventoryItemSet a)
			=> new() {
				Items = a.Items.ToDictionary(i => i.Key, i => new SavedGlamourItem() {
					ItemId = i.Value?.ItemId ?? 0,
					StainId = i.Value?.Stain ?? 0
				}),
			};

		public static explicit operator InventoryItemSet(SavedPlate a)
			=> new() {
				Items = a.Items.ToDictionary(i => i.Key, i => i.Value.ItemId == 0 ? null : new InventoryItem(0, 0, i.Value.ItemId, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, i.Value.StainId, 0)),
			};
		public static explicit operator InventoryItemSet(MiragePage a)
			 => new InventoryItemSet(a.ToDictionary().ToDictionary(p => p.Key, p => (InventoryItem?)p.Value));

		public void SetSlot(GlamourPlateSlot slot, InventoryItem? item)
			=> Items[slot] = item;
		public InventoryItem? GetSlot(GlamourPlateSlot slot) {
			if (!Items.TryGetValue(slot, out var item)) return null;
			return item;
		}
		public void RemoveSlot(GlamourPlateSlot slot)
			=> Items.Remove(slot);
		public InventoryItemSet RemoveEmpty() {
			var glamourPlates = Enum.GetValues<GlamourPlateSlot>().Cast<GlamourPlateSlot>();
			foreach (var g in glamourPlates) {
				// 3 methods to detect empty items:
				//   - key/item doesn't exist
				//   - null
				//   - ItemId == 0
				if (Items.TryGetValue(g, out var item)) {
					if (item == null || item.ItemId == 0) {
						//this.Items.Remove(g); // here we use remove method
						Items[g] = null; // here we make it null
					}
				}
			}
			return this;
		}
		//=> new InventoryItemSet {
		//	Items = this.Items.Where(i => i.Value != null && i.Value.ItemId != 0).ToDictionary(i => i.Key, i => i.Value)
		//};

		public bool IsDifferentGlam(InventoryItemSet set2, out InventoryItemSet diffLeft, out InventoryItemSet diffRight) {
			var set1Items = Items;
			var set2Items = set2.Items;

			// diffLeft = items from set1 when there is a difference;
			diffLeft = new();
			diffRight = new();

			var slots = Enum.GetValues<GlamourPlateSlot>().ToList();

			foreach (var slot in slots) {
				set1Items.TryGetValue(slot, out var item1);
				set2Items.TryGetValue(slot, out var item2);

				if (item1?.IsAppearanceDifferent(item2) ?? false) {
					diffLeft.SetSlot(slot, item1?.Clone());
					diffRight.SetSlot(slot, item2?.Clone());
				}

			}

			return diffLeft.Items.Any() || diffRight.Items.Any();
		}
		public bool IsEmpty() {
			return Items.Count == 0 || !Items.Any(i => i.Value != null);
		}


	}
}
