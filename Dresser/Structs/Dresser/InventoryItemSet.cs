using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using InventoryItem = Dresser.Structs.Dresser.InventoryItem;


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
		public void EmptyAllItemsToNull() {
			InventoryItem? nullItem = null;
			Items = Enum.GetValues<GlamourPlateSlot>().ToDictionary(s => s, s => nullItem);
		}
		public void UpdateSourcesForOwnedItems() {
			var set = this;
			Task.Run(()=>{
				var ownedItems = ConfigurationManager.Config.GetSavedInventoryLocalCharsRetainers(true).SelectMany(c => c.Value.Copy()!);
				foreach ((var slot, var item) in set.Items) {
					if (item == null) continue;
					if (item.IsModded()) continue;
					var foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId && i.Stain == item.Stain);
					if (!foundMatchingItem.Any())
						foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId);

					if (foundMatchingItem.Any()) {
						var matchingItem = foundMatchingItem.First();
						if (matchingItem != null) {
							var matchingItemToProcess = matchingItem.Copy()!;
							if (matchingItemToProcess.Stain != item.Stain)
								matchingItemToProcess.Stain = item.Stain;

							set.Items[slot] = matchingItemToProcess;
						}
					}
				}
			});
		}
		public List<InventoryItem> FindNotOwned() {
			var list = new List<InventoryItem>();
			var ownedItems = ConfigurationManager.Config.GetSavedInventoryLocalCharsRetainers(true).Where(i=>i.Key == InventoryCategory.Armoire || i.Key == InventoryCategory.GlamourChest).SelectMany(c => c.Value);

			foreach ((var slot, var item) in Items) {
				if (item == null) continue;
				var foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId && i.Stain == item.Stain);
				if (!foundMatchingItem.Any()) {
					foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId);
					if( foundMatchingItem.Any()) {
						// found items with unmatching dye
						// check for dyes
						// get only the first item found
						list.Add(item.GetDyesInInventories().First());

					}
				}
				if(!foundMatchingItem.Any()) {
					list.Add(item.Clone());
					if (item.Item.IsDyeable && item.Stain != 0) // also add needed dye in the list
						list.Add(item.GetDyesInInventories().First());
				}

			}

			return FindDuplicatesAndIncreaseQuantity(list)
				.OrderBy(i=>i.SortedContainer)
				.ToList();
		}

		public static IEnumerable<InventoryItem> FindDuplicatesAndIncreaseQuantity(IEnumerable<InventoryItem> items) {
			var itemGroups = items
				.GroupBy(item => item) // Group items by their values
				.ToList(); // Convert to list to avoid re-evaluation of the grouping

			foreach (var group in itemGroups) {
				var occurrences = (uint)group.Count();
				var item = group.Key; // get lead item
				item.QuantityNeeded = occurrences; // set needed quantity in lead item
			}

			return itemGroups
				.Select(group => group.Key); // Include all items from each group
		}


	}
}
