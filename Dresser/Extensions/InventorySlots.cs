using Dresser.Structs.Actor;
using Dresser.Structs.Dresser;

using System;

namespace Dresser.Extensions
{
    public static class SlotsEnumExtensions {
		public static EquipIndex? ToEquipIndex(this GlamourPlateSlot slot) {
			return slot switch {
				GlamourPlateSlot.Head => EquipIndex.Head,
				GlamourPlateSlot.Body => EquipIndex.Chest,
				GlamourPlateSlot.Hands => EquipIndex.Hands,
				GlamourPlateSlot.Legs => EquipIndex.Legs,
				GlamourPlateSlot.Feet => EquipIndex.Feet,
				GlamourPlateSlot.Ears => EquipIndex.Earring,
				GlamourPlateSlot.Neck => EquipIndex.Necklace,
				GlamourPlateSlot.Wrists => EquipIndex.Bracelet,
				GlamourPlateSlot.RightRing => EquipIndex.RingRight,
				GlamourPlateSlot.LeftRing => EquipIndex.RingLeft,
				_ => null
			};
		}
		public static WeaponIndex? ToWeaponIndex(this GlamourPlateSlot slot) {
			return slot switch {
				GlamourPlateSlot.MainHand => WeaponIndex.MainHand,
				GlamourPlateSlot.OffHand => WeaponIndex.OffHand,
				_ => null
			};
		}

		public static GlamourPlateSlot ToGlamourPlateSlot(this EquipIndex slot) {
			return slot switch {
				EquipIndex.Head => GlamourPlateSlot.Head,
				EquipIndex.Chest => GlamourPlateSlot.Body,
				EquipIndex.Hands => GlamourPlateSlot.Hands,
				EquipIndex.Legs => GlamourPlateSlot.Legs,
				EquipIndex.Feet => GlamourPlateSlot.Feet,
				EquipIndex.Earring => GlamourPlateSlot.Ears,
				EquipIndex.Necklace => GlamourPlateSlot.Neck,
				EquipIndex.Bracelet => GlamourPlateSlot.Wrists,
				EquipIndex.RingRight => GlamourPlateSlot.RightRing,
				EquipIndex.RingLeft => GlamourPlateSlot.LeftRing,
				_ => throw new NotImplementedException()
			};
		}
		public static GlamourPlateSlot ToGlamourPlateSlot(this WeaponIndex slot) {
			return slot switch {
				WeaponIndex.MainHand => GlamourPlateSlot.MainHand,
				WeaponIndex.OffHand => GlamourPlateSlot.OffHand,
				_ => throw new NotImplementedException()
			};
		}
	}
}
