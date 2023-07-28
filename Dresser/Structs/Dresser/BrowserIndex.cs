using CsvHelper.Configuration.Attributes;

using Dresser.Logic;

using System;

namespace Dresser.Structs.Dresser {
	public class BrowserIndex {
		public uint ItemId;
		public string ModPath;

		public BrowserIndex(uint itemId, string modPath) {
			ItemId = itemId;
			ModPath = modPath;
		}

		public static BrowserIndex Zero => new(0, "");

		public static explicit operator BrowserIndex(InventoryItem item) {
			return new BrowserIndex(item.ItemId, item.ModDirectory ?? "");
		}

		public static bool operator ==(BrowserIndex? left, BrowserIndex? right) {
			if (ReferenceEquals(left, right))
				return true;
			if (left is null)
				return false;
			if (right is null)
				return false;
			return left.Equals(right);
		}
		public static bool operator !=(BrowserIndex? left, BrowserIndex? right)
			=> !(left == right);
		public override bool Equals(object? obj) {
			if (obj is null)
				return false;
			if (GetType() != obj.GetType())
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			var obj2 = (BrowserIndex)obj;
			return ItemId.Equals(obj2.ItemId)
			   && ModPath.Equals(obj2.ModPath);
		}

		public override string ToString() {
			return (ItemId, ModPath).GetHashCode().ToString();
		}
		public override int GetHashCode()
			=> HashCode.Combine(ItemId, ModPath);
	}
}
