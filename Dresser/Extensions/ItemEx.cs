using System;

using CriticalCommonLib.Extensions;
using CriticalItemEx = CriticalCommonLib.Sheets.ItemEx;
using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;

using Dresser.Structs.Actor;
using Dresser.Structs.FFXIV;
using Dresser.Windows.Components;
using CriticalCommonLib;

namespace Dresser.Extensions {
	internal static class ItemExExtention {
		public static GlamourPlateSlot? GlamourPlateSlot(this CriticalItemEx item) {
			var slot = item.EquipSlotCategoryEx;
			if(slot == null) return null;
			if (slot.MainHand == 1) return Structs.FFXIV.GlamourPlateSlot.MainHand;
			if (slot.OffHand == 1) return Structs.FFXIV.GlamourPlateSlot.OffHand;
			if (slot.Head == 1) return Structs.FFXIV.GlamourPlateSlot.Head;
			if (slot.Body == 1) return Structs.FFXIV.GlamourPlateSlot.Body;
			if (slot.Gloves == 1) return Structs.FFXIV.GlamourPlateSlot.Hands;
			if (slot.Legs == 1) return Structs.FFXIV.GlamourPlateSlot.Legs;
			if (slot.Feet == 1) return Structs.FFXIV.GlamourPlateSlot.Feet;
			if (slot.Ears == 1) return Structs.FFXIV.GlamourPlateSlot.Ears;
			if (slot.Neck == 1) return Structs.FFXIV.GlamourPlateSlot.Neck;
			if (slot.Wrists == 1) return Structs.FFXIV.GlamourPlateSlot.Wrists;
			if (slot.FingerR == 1) return Structs.FFXIV.GlamourPlateSlot.RightRing;
			if (slot.FingerL == 1) return Structs.FFXIV.GlamourPlateSlot.LeftRing;
			throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
		}
		public static EquipIndex? EquipIndex(this CriticalItemEx item) {
			var slot = item.EquipSlotCategoryEx;
			if(slot == null) return null;
			//if (slot.MainHand == 1) return EquipIndex.MainHand;
			//if (slot.OffHand == 1) return EquipIndex.OffHand;
			if (slot.Head == 1) return Structs.Actor.EquipIndex.Head;
			if (slot.Body == 1) return Structs.Actor.EquipIndex.Chest;
			if (slot.Gloves == 1) return Structs.Actor.EquipIndex.Hands;
			if (slot.Legs == 1) return Structs.Actor.EquipIndex.Legs;
			if (slot.Feet == 1) return Structs.Actor.EquipIndex.Feet;
			if (slot.Ears == 1) return Structs.Actor.EquipIndex.Earring;
			if (slot.Neck == 1) return Structs.Actor.EquipIndex.Necklace;
			if (slot.Wrists == 1) return Structs.Actor.EquipIndex.Bracelet;
			if (slot.FingerR == 1) return Structs.Actor.EquipIndex.RingRight;
			if (slot.FingerL == 1) return Structs.Actor.EquipIndex.RingLeft;
			return null;
		}
		public static bool IsWeapon(this CriticalItemEx item)
			=> item.EquipSlotCategoryEx?.MainHand == 1 || item.EquipSlotCategoryEx?.OffHand == 1;
		public static bool CanBeEquipedByPlayedRaceGender(this CriticalItemEx item) {
			var gender = ItemIcon.LocalPlayerGender;
			var race = ItemIcon.LocalPlayerRace;
			if (gender == null || race == null) return false;

			return item.CanBeEquippedByRaceGender((CharacterRace)race, (CharacterSex)gender);
		}
		public static bool CanBeEquipedByPlayedJob(this CriticalItemEx item) {
			var job = ItemIcon.LocalPlayerClass;
			if (job == null) return false;

			return Service.ExcelCache.IsItemEquippableBy(item.ClassJobCategory.Row, job.RowId);
		}


	}
}
