using CriticalCommonLib.Models;

using Dalamud.Logging;

using Dresser.Services;

using System;
using System.Collections.Generic;
using System.Linq;


namespace Dresser.Logic {
	public class InventoryItemOrder {

		public static IEnumerable<InventoryItem> OrderItems(IEnumerable<InventoryItem> items) {
			IOrderedEnumerable<InventoryItem>? orderedItems = null;

			foreach((var kind, var direction) in ConfigurationManager.Config.SortOrder!) {

				Func<InventoryItem, uint>? sortMethod = kind switch {
					OrderMethod.Level => Level,
					OrderMethod.ItemLevel => ItemLevel,
					OrderMethod.ItemId => ItemId,
					_ => null,
				};

				if (sortMethod == null) continue;

				if (orderedItems == null) {
					if (direction == OrderDirection.Descending)
						orderedItems = items.OrderByDescending(sortMethod);
					else
						orderedItems = items.OrderBy(sortMethod);

				} else {
					if (direction == OrderDirection.Descending)
						orderedItems = orderedItems.ThenByDescending(sortMethod);
					else
						orderedItems = orderedItems.ThenBy(sortMethod);
				}


			}

			return orderedItems?.ToList() ?? new();
		}
		public static List<(OrderMethod Method, OrderDirection Direction)> Defaults()
			=> new() {
					(OrderMethod.ItemLevel, OrderDirection.Descending),
					(OrderMethod.Level, OrderDirection.Descending),
				};




		public enum OrderDirection {
			Descending,
			Ascending,
		}
		public enum OrderMethod {
			Level,
			ItemLevel,
			ItemId,
		}

		private static uint Level(InventoryItem i)
			=> i.Item.LevelEquip;
		private static uint ItemLevel(InventoryItem i)
			=> i.Item.LevelItem.Row;
		private static uint ItemId(InventoryItem i)
			=> i.ItemId;
	}
}
