using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Sheets;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Textures;

using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Logic.Glamourer;
using Dresser.Structs;
using Dresser.Structs.Actor;

using Lumina.Data;

using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

using System;
using System.Collections.Generic;
using System.Linq;

using CriticalInventoryItem = Dresser.Structs.Dresser.InventoryItem;
using CriticalItemEx = CriticalCommonLib.Sheets.ItemEx;
using InteropGlamourPlateSlot = Dresser.Interop.Hooks.GlamourPlateSlot;

namespace Dresser.Extensions {
	internal static class ItemExExtention {
		public static GlamourPlateSlot? GlamourPlateSlot(this CriticalItemEx item) {
			var slot = item.EquipSlotCategoryEx;
			if (slot == null) return null;
			if (slot.MainHand == 1) return InteropGlamourPlateSlot.MainHand;
			if (slot.OffHand == 1) return InteropGlamourPlateSlot.OffHand;
			if (slot.Head == 1) return InteropGlamourPlateSlot.Head;
			if (slot.Body == 1) return InteropGlamourPlateSlot.Body;
			if (slot.Gloves == 1) return InteropGlamourPlateSlot.Hands;
			if (slot.Legs == 1) return InteropGlamourPlateSlot.Legs;
			if (slot.Feet == 1) return InteropGlamourPlateSlot.Feet;
			if (slot.Ears == 1) return InteropGlamourPlateSlot.Ears;
			if (slot.Neck == 1) return InteropGlamourPlateSlot.Neck;
			if (slot.Wrists == 1) return InteropGlamourPlateSlot.Wrists;
			if (slot.FingerR == 1) return InteropGlamourPlateSlot.RightRing;
			if (slot.FingerL == 1) return InteropGlamourPlateSlot.LeftRing;
			return null;
			//throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
		}
		public static byte ToEquipSlotCategoryByte(this GlamourPlateSlot slot) {
			return slot switch {
				InteropGlamourPlateSlot.MainHand => 0 + 1,
				InteropGlamourPlateSlot.OffHand => 1 + 1,
				InteropGlamourPlateSlot.Head => 2 + 1,
				InteropGlamourPlateSlot.Body => 3 + 1,
				InteropGlamourPlateSlot.Hands => 4 + 1,
				//InteropGlamourPlateSlot.Waist => 5 + 1,
				InteropGlamourPlateSlot.Legs => 6 + 1,
				InteropGlamourPlateSlot.Feet => 7 + 1,
				InteropGlamourPlateSlot.Ears => 8 + 1,
				InteropGlamourPlateSlot.Neck => 9 + 1,
				InteropGlamourPlateSlot.Wrists => 10 + 1,
				InteropGlamourPlateSlot.LeftRing => 11 + 1,
				InteropGlamourPlateSlot.RightRing => 12 + 1,
				//InteropGlamourPlateSlot.SoulCrystal => 13 + 1,
				_ => throw new Exception($"Unidentified GlamourPlateSlot: {slot}")
			};
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

		public static Penumbra.GameData.Enums.EquipSlot? PenumbraEquipIndex(this CriticalItemEx item) {

			var slot = item.EquipSlotCategoryEx;
			if (slot == null) return null;
			if (slot.MainHand == 1) return Penumbra.GameData.Enums.EquipSlot.MainHand;
			if (slot.OffHand == 1) return Penumbra.GameData.Enums.EquipSlot.OffHand;
			if (slot.Head == 1) return Penumbra.GameData.Enums.EquipSlot.Head;
			if (slot.Body == 1) return Penumbra.GameData.Enums.EquipSlot.Body;
			if (slot.Gloves == 1) return Penumbra.GameData.Enums.EquipSlot.Hands;
			if (slot.Legs == 1) return Penumbra.GameData.Enums.EquipSlot.Legs;
			if (slot.Feet == 1) return Penumbra.GameData.Enums.EquipSlot.Feet;
			if (slot.Ears == 1) return Penumbra.GameData.Enums.EquipSlot.Ears;
			if (slot.Neck == 1) return Penumbra.GameData.Enums.EquipSlot.Neck;
			if (slot.Wrists == 1) return Penumbra.GameData.Enums.EquipSlot.Wrists;
			if (slot.FingerR == 1) return Penumbra.GameData.Enums.EquipSlot.RFinger;
			if (slot.FingerL == 1) return Penumbra.GameData.Enums.EquipSlot.LFinger;
			return null;
		}

		public static FullEquipType ToFullEquipType(this CriticalItemEx item, bool isMainHand) {
			var slot = (Penumbra.GameData.Enums.EquipSlot)item.EquipSlotCategory.Row;
			var weapon = (WeaponCategory)item.ItemUICategory.Row;
			return slot.ToEquipType(weapon, isMainHand);
		}
		public static InteropGlamourPlateSlot? ToWeaponSlot(this CriticalItemEx item) {
			return item.ToFullEquipType(true).ToSlot() switch {
				Penumbra.GameData.Enums.EquipSlot.MainHand => InteropGlamourPlateSlot.MainHand,
				Penumbra.GameData.Enums.EquipSlot.OffHand => InteropGlamourPlateSlot.OffHand,
				_ => null
			};
		}
		public static bool IsMainModelOnOffhand(this CriticalItemEx item) {
			return item.ToFullEquipType(true).ToSlot() == Penumbra.GameData.Enums.EquipSlot.OffHand;
		}





		private static ItemModel ItemModelFromUlong(this CriticalItemEx item, ulong model, bool isWeapon)
			=> new(model, isWeapon);
		public static ItemModel ModelMainItemModel(this CriticalItemEx item)
			=> item.ItemModelFromUlong(item.ModelMain, item.IsWeapon());
		public static ItemModel ModelSubItemModel(this CriticalItemEx item)
			=> item.ItemModelFromUlong(item.ModelSub, item.IsWeapon());


		public static bool IsWeapon(this CriticalItemEx item)
			=> item.EquipSlotCategoryEx?.MainHand == 1 || item.EquipSlotCategoryEx?.OffHand == 1;
		public static bool CanBeEquipedByPlayedRaceGender(this CriticalItemEx item) {
			var gender = PluginServices.Context.LocalPlayerGender;
			var race = PluginServices.Context.LocalPlayerRace;
			if (gender == null || race == null) return false;

			return item.CanBeEquippedByRaceGender((CharacterRace)race, (CharacterSex)gender);
		}
		public static bool CanBeEquipedByPlayedJob(this CriticalItemEx item, bool strict = false) {
			var job = PluginServices.Context.LocalPlayerClass;
			if (job == null) return false;
			var categoryId = item.ClassJobCategory.Row;


			var isEquipable = Service.ExcelCache.IsItemEquippableBy(categoryId, job.RowId);
			if(!strict || !isEquipable) return isEquipable;

			// ensure there there is only one category
			return Service.ExcelCache.ClassJobCategoryLookup[categoryId].Count == 1;
		}
		public static CriticalInventoryItem ToInventoryItem(this CriticalItemEx itemEx, InventoryType inventoryType) {
			return new CriticalInventoryItem(inventoryType, 0, itemEx.RowId, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		}

		public static bool ObtainedWithSpecialShopCurrency2(this CriticalItemEx itemEx, uint currencyItemId) {
			if (Service.ExcelCache.SpecialShopItemRewardCostLookup.TryGetValue(itemEx.RowId, out var specialShop)) {
				return specialShop.Any(c => c.Item2 == currencyItemId);
			}

			return false;
		}
		public static ISharedImmediateTexture IconTextureWrap(this CriticalItemEx itemEx) {
			return IconWrapper.Get(itemEx);
		}



		public static void OpenInGarlandTools(this CriticalItemEx item)
			=> $"https://www.garlandtools.org/db/#item/{item.RowId}".OpenBrowser();
		public static void OpenInTeamcraft(this CriticalItemEx item)
			=> $"https://ffxivteamcraft.com/db/en/item/{item.RowId}".OpenBrowser();

		public static void OpenInGamerEscape(this CriticalItemEx item) {
			var enItem = Service.Data.Excel.GetSheet<CriticalItemEx>(Language.English)?.GetRow(item.RowId);
			if (enItem != null)
				$"https://ffxiv.gamerescape.com/w/index.php?search={Uri.EscapeDataString(enItem.NameString)}".OpenBrowser();
		}
		public static void OpenInUniversalis(this CriticalItemEx item)
			=> $"https://universalis.app/market/{item.RowId}".OpenBrowser();
		public static void CopyNameToClipboard(this CriticalItemEx item)
			=> item.NameString.ToClipboard();
		public static void LinkInChatHistory(this CriticalItemEx item) {
			if (item.RowId == ItemEx.FreeCompanyCreditItemId) {
				return;
			}
			var payloadList = new List<Payload> {
				new UIForegroundPayload((ushort) (0x223 + item.Rarity * 2)),
				new UIGlowPayload((ushort) (0x224 + item.Rarity * 2)),
				new ItemPayload(item.RowId, item.CanBeHq && Service.KeyState[0x11]),
				new UIForegroundPayload(500),
				new UIGlowPayload(501),
				new TextPayload($"{(char) SeIconChar.LinkMarker}"),
				new UIForegroundPayload(0),
				new UIGlowPayload(0),
				new TextPayload(item.Name + (item.CanBeHq && Service.KeyState[0x11] ? $" {(char)SeIconChar.HighQuality}" : "")),
				new RawPayload(new byte[] {0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03}),
				new RawPayload(new byte[] {0x02, 0x13, 0x02, 0xEC, 0x03})
			};

			var payload = new SeString(payloadList);

			PluginServices.ChatGui.Print(new XivChatEntry {
				Message = payload
			});
		}
		//=> PluginServices.ChatUtilities.LinkItem(item);
		public static void TryOn(this CriticalItemEx item) {
			if (item.CanTryOn && PluginServices.TryOn.CanUseTryOn)
				PluginServices.TryOn.TryOnItem(item);
		}
		//public static void OpenCraftingLog(this CriticalItemEx item) {
		//	if (item.CanOpenCraftLog)
		//		PluginServices.GameInterface.OpenCraftingLog(item.RowId);
		//}

		public static bool IsSoldByAnyVendor(this CriticalItemEx item, IEnumerable<string> vendorNames)
			=> Service.ExcelCache.ShopCollection?.GetShops(item.RowId).Any(s => s.ENpcs.Any(n => vendorNames.Any(av => av == n.Resident!.Singular))) ?? false;

		public static CustomItemId ToCustomItemId(this CriticalItemEx item, GlamourPlateSlot slot)
			=> Design.FromInventoryItem(item, slot);

		public static bool IsDyeable1(this CriticalItemEx item) {
			return item.DyeCount > 0;
		}
		public static bool IsDyeable2(this CriticalItemEx item) {
			return item.DyeCount > 1;
		}
	}
}
