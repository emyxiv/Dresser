using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dresser.Structs {
	public class ItemModel {
		public ushort Id { get; set; }
		public ushort Base { get; set; }
		public ushort Variant { get; set; }

		public ItemModel(ulong var, bool isWep = false) {
			Id = (ushort)var;
			Base = (ushort)(isWep ? var >> 16 : 0);
			Variant = (ushort)(isWep ? var >> 32 : var >> 16);
		}
	}
}
