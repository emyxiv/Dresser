using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;

using Dresser.Structs.Actor;
using Dresser.Structs.Dresser;

using ImGuiScene;

using System.Linq;

using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;
using CriticalItemEx = CriticalCommonLib.Sheets.ItemEx;

namespace Dresser.Extensions
{
    internal static class ItemExExtention {
		public static GlamourPlateSlot? GlamourPlateSlot(this CriticalItemEx item) {
			var slot = item.EquipSlotCategoryEx;
			if (slot == null) return null;
			if (slot.MainHand == 1) return Structs.Dresser.GlamourPlateSlot.MainHand;
			if (slot.OffHand == 1) return Structs.Dresser.GlamourPlateSlot.OffHand;
			if (slot.Head == 1) return Structs.Dresser.GlamourPlateSlot.Head;
			if (slot.Body == 1) return Structs.Dresser.GlamourPlateSlot.Body;
			if (slot.Gloves == 1) return Structs.Dresser.GlamourPlateSlot.Hands;
			if (slot.Legs == 1) return Structs.Dresser.GlamourPlateSlot.Legs;
			if (slot.Feet == 1) return Structs.Dresser.GlamourPlateSlot.Feet;
			if (slot.Ears == 1) return Structs.Dresser.GlamourPlateSlot.Ears;
			if (slot.Neck == 1) return Structs.Dresser.GlamourPlateSlot.Neck;
			if (slot.Wrists == 1) return Structs.Dresser.GlamourPlateSlot.Wrists;
			if (slot.FingerR == 1) return Structs.Dresser.GlamourPlateSlot.RightRing;
			if (slot.FingerL == 1) return Structs.Dresser.GlamourPlateSlot.LeftRing;
			return null;
			//throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
		}
		public static EquipIndex? EquipIndex(this CriticalItemEx item) {
			var slot = item.EquipSlotCategoryEx;
			if (slot == null) return null;
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
			var gender = PluginServices.Context.LocalPlayerGender;
			var race = PluginServices.Context.LocalPlayerRace;
			if (gender == null || race == null) return false;

			return item.CanBeEquippedByRaceGender((CharacterRace)race, (CharacterSex)gender);
		}
		public static bool CanBeEquipedByPlayedJob(this CriticalItemEx item) {
			var job = PluginServices.Context.LocalPlayerClass;
			if (job == null) return false;

			return Service.ExcelCache.IsItemEquippableBy(item.ClassJobCategory.Row, job.RowId);
		}
		public static CriticalInventoryItem ToInventoryItem(this CriticalItemEx itemEx, InventoryType inventoryType) {
			return new CriticalInventoryItem(inventoryType, 0, itemEx.RowId, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		}

		public static bool ObtainedWithSpecialShopCurrency2(this CriticalItemEx itemEx, uint currencyItemId) {
			if (Service.ExcelCache.SpecialShopItemRewardCostLookup.TryGetValue(itemEx.RowId, out var specialShop)) {
				return specialShop.Any(c => c.Item2 == currencyItemId);
			}

			return false;
		}
		public static TextureWrap IconTextureWrap(this CriticalItemEx itemEx) {
			return PluginServices.IconStorage.Get(itemEx);
		}

	}
}
