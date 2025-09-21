﻿using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Actor;

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
			EmptyAllItemsToNull();
		}
		public InventoryItemSet(Dictionary<GlamourPlateSlot, InventoryItem?> items) {
			Items = items;
		}
		public InventoryItemSet(Dictionary<EquipIndex, ItemEquip>? modelItems) {
			Items = new();
			if (modelItems != null)
				foreach ((var e, var i) in modelItems) {
					//PluginLog.Debug($"store item {e} => {i.Id}");

					var slot = e.ToGlamourPlateSlot();
					this.SetSlot(slot, InventoryItem.FromItemEquip(i, slot));
				}
		}
		public InventoryItemSet(GlamourPlateSlot slot, InventoryItem item) {
			Items = new Dictionary<GlamourPlateSlot, InventoryItem?> { { slot, item } };
		}
		public static explicit operator InventoryItemSet(FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData.GlamourPlate a) {
			Dictionary<GlamourPlateSlot, InventoryItem?> dictionary = new();
			var array = a.Items.ToArray();
			for (int i = 0; i < array.Length; i++) {
				dictionary.Add((GlamourPlateSlot)i, InventoryItemExtensions.New(array[i].ItemId, array[i].StainIds[0], array[i].StainIds[1]));
			}
			return new InventoryItemSet(dictionary);
		}

		public void SetSlot(GlamourPlateSlot slot, InventoryItem? item)
			=> Items[slot] = item;
		public InventoryItem? GetSlot(GlamourPlateSlot slot) {
			return Items.GetValueOrDefault(slot);
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
			foreach((var key, var item) in Items)
				Items[key] = new();
		}
		public void UpdateSourcesForOwnedItems() {
			var set = this;
			Task.Run(()=>{
				try {
					var ownedItems = PluginServices.AllaganTools.GetItemsLocalCharsRetainers(true).SelectMany(c => c.Value);
					if (ownedItems == null) return;

					if (!(ownedItems?.Count() > 0)) return;

					foreach ((var slot, var item) in set.Items) {
						if (item == null) continue;
						if (item.IsModded()) continue;

						var foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId && i.Stain == item.Stain && i.Stain2 == item.Stain2);
						if (!foundMatchingItem.Any())
							foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId && i.Stain == item.Stain);
						if (!foundMatchingItem.Any())
							foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId && i.Stain2 == item.Stain2);
						if (!foundMatchingItem.Any())
							foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId);

						if (foundMatchingItem.Any()) {
							var matchingItem = foundMatchingItem.First();
							if (matchingItem != null) {
								var matchingItemToProcess = matchingItem.Copy()!;
								if (matchingItemToProcess.Stain != item.Stain)
									matchingItemToProcess.Stain = item.Stain;
								if (matchingItemToProcess.Stain2 != item.Stain2)
									matchingItemToProcess.Stain2 = item.Stain2;

								set.Items[slot] = matchingItemToProcess;
							}
						}
					}
				} catch (Exception e) {
					PluginLog.Error(e, "Error on UpdateSourcesForOwnedItems");
				}
			});
		}
		public List<InventoryItem> FindNotOwned() {
			var list = new List<InventoryItem>();
			var ownedItems = PluginServices.AllaganTools.GetItemsLocalCharsRetainers(true).SelectMany(c => c.Value).Where(i=>i.SortedCategory == InventoryCategory.Armoire || i.SortedCategory == InventoryCategory.GlamourChest);

			foreach ((var slot, var item) in Items) {
				if (item == null) continue;
				var foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId && i.Stain == item.Stain && i.Stain2 == item.Stain2);
				if (!foundMatchingItem.Any())
					foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId && i.Stain == item.Stain);
				if (!foundMatchingItem.Any())
					foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId && i.Stain2 == item.Stain2);
				if (!foundMatchingItem.Any())
					foundMatchingItem = ownedItems.Where(i => i.ItemId == item.ItemId);

				if ( foundMatchingItem.Any(i=>i.Stain != item.Stain)) {
					// found items with unmatching dye
					// check for dyes
					// get only the first item found
					list.Add(item.GetDyesInInventories(1).First());
				}
				if ( foundMatchingItem.Any(i=>i.Stain2 != item.Stain2)) {
					list.Add(item.GetDyesInInventories(2).First());
				}
				if(!foundMatchingItem.Any()) {
					list.Add(item.Clone());
					if (item.Item.IsDyeable1() && item.Stain != 0) // also add needed dye in the list
						list.Add(item.GetDyesInInventories(1).First());
					if (item.Item.IsDyeable2() && item.Stain2 != 0) // also add needed dye in the list
						list.Add(item.GetDyesInInventories(2).First());
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

		public readonly bool HasModdedItem()
			=> Items.Any(i => i.Value?.IsModded() ?? false );
		public readonly bool HasMod((string Path, string Name)? mod)
			=> Items.Any(i => i.Value?.IsMod(mod) ?? false);
		public readonly IEnumerable<(string Path, string Name)?> Mods() {
			return Items.Where(i => i.Value?.IsModded() ?? false).Select(i => i.Value?.GetMod()).Distinct();
		}
		public readonly void ApplyAppearance() {
			var character = PluginServices.Context.LocalPlayer;
			if(character == null) return;
			// PluginLog.Debug($"================== > SET SET ITEM ApplyAppearance =============");
			PluginServices.Glamourer.SetSet(character, this);
			// PluginServices.Context.LocalPlayer?.EquipSet(this);
		}

		public readonly override string ToString() {
			string ret = "Plate contents:";

			foreach ((var s, var i) in Items) {
				ret += $"\n{i?.FormattedName ?? "Unknown item"} {i?.ItemId}";
			}
			return ret;
		}
	}
}
