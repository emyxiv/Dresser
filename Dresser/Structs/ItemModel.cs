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
		public static ulong ToUlong(ItemModel itemModel, bool isWep = false) {
			ulong result = itemModel.Id;

			if (isWep) {
				result |= ((ulong)itemModel.Base) << 16;
				result |= ((ulong)itemModel.Variant) << 32;
			} else {
				result |= ((ulong)itemModel.Variant) << 16;
			}
			return result;
		}
		public override string ToString() {
			return $"({Id}, {Variant}, {Base})";
		}
	}
}
