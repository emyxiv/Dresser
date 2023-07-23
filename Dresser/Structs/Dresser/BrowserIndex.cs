using CsvHelper.Configuration.Attributes;

using Dalamud.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dresser.Structs.Dresser {
	public class BrowserIndex {
		public uint ItemId;
		public string ModPath;

		public BrowserIndex(uint itemId,string modPath) {
			ItemId = itemId;
			ModPath = modPath;
		}

		public static BrowserIndex Zero => new(0, "");

		public static explicit operator BrowserIndex(InventoryItem item) {
			return new BrowserIndex(item.ItemId, item.ModDirectory);
		}

		public static bool operator ==(BrowserIndex left, BrowserIndex? right) {
			return
				left.ItemId == right?.ItemId && left.ModPath == right.ModPath;
		}
		public static bool operator !=(BrowserIndex left, BrowserIndex? right) {
			return
				left.ItemId != right?.ItemId
				|| left.ModPath != right?.ModPath;
		}

		public override string ToString() {
			return (ItemId, ModPath).GetHashCode().ToString();
		}
	}
}
