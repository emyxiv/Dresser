using System;
using System.Collections.Generic;
using System.Linq;

using AllaganLib.GameSheets.Model;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Helpers;
using AllaganLib.GameSheets.Sheets.Rows;

using CriticalCommonLib.Enums;

using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Textures;
using Dalamud.Utility;

using Dresser.Logic;
using Dresser.Logic.Glamourer;
using Dresser.Structs;
using Dresser.Structs.Actor;
using Dresser.Windows;

using Lumina.Data;
using Lumina.Excel.Sheets;

using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

using CriticalInventoryItem = Dresser.Structs.Dresser.InventoryItem;
using EquipSlot = Penumbra.GameData.Enums.EquipSlot;
using InteropGlamourPlateSlot = Dresser.Interop.Hooks.GlamourPlateSlot;

namespace Dresser.Extensions {
	internal static class ItemExExtention {
		public static InteropGlamourPlateSlot? GlamourPlateSlot(this ItemRow item)
		{
			var slot = item.EquipSlotCategory?.Base;
			if(slot == null) return null;

			if (slot.Value.MainHand == 1) return InteropGlamourPlateSlot.MainHand;
			if (slot.Value.OffHand == 1) return InteropGlamourPlateSlot.OffHand;
			if (slot.Value.Head == 1) return InteropGlamourPlateSlot.Head;
			if (slot.Value.Body == 1) return InteropGlamourPlateSlot.Body;
			if (slot.Value.Gloves == 1) return InteropGlamourPlateSlot.Hands;
			if (slot.Value.Legs == 1) return InteropGlamourPlateSlot.Legs;
			if (slot.Value.Feet == 1) return InteropGlamourPlateSlot.Feet;
			if (slot.Value.Ears == 1) return InteropGlamourPlateSlot.Ears;
			if (slot.Value.Neck == 1) return InteropGlamourPlateSlot.Neck;
			if (slot.Value.Wrists == 1) return InteropGlamourPlateSlot.Wrists;
			if (slot.Value.FingerR == 1) return InteropGlamourPlateSlot.RightRing;
			if (slot.Value.FingerL == 1) return InteropGlamourPlateSlot.LeftRing;
			return null;
			//throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
		}
		public static byte ToEquipSlotCategoryByte(this InteropGlamourPlateSlot slot) {
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
		public static EquipIndex? EquipIndex(this ItemRow item) {
			var slot = item.EquipSlotCategory?.Base;
			if(slot == null) return null;

			//if (slot.Value.MainHand == 1) return EquipIndex.MainHand;
			//if (slot.Value.OffHand == 1) return EquipIndex.OffHand;
			if (slot.Value.Head == 1) return Structs.Actor.EquipIndex.Head;
			if (slot.Value.Body == 1) return Structs.Actor.EquipIndex.Chest;
			if (slot.Value.Gloves == 1) return Structs.Actor.EquipIndex.Hands;
			if (slot.Value.Legs == 1) return Structs.Actor.EquipIndex.Legs;
			if (slot.Value.Feet == 1) return Structs.Actor.EquipIndex.Feet;
			if (slot.Value.Ears == 1) return Structs.Actor.EquipIndex.Earring;
			if (slot.Value.Neck == 1) return Structs.Actor.EquipIndex.Necklace;
			if (slot.Value.Wrists == 1) return Structs.Actor.EquipIndex.Bracelet;
			if (slot.Value.FingerR == 1) return Structs.Actor.EquipIndex.RingRight;
			if (slot.Value.FingerL == 1) return Structs.Actor.EquipIndex.RingLeft;
			return null;
		}

		public static EquipSlot? PenumbraEquipIndex(this ItemRow item) {
			var slot = item.EquipSlotCategory?.Base;
			if(slot == null) return null;

			if (slot.Value.MainHand == 1) return EquipSlot.MainHand;
			if (slot.Value.OffHand == 1) return EquipSlot.OffHand;
			if (slot.Value.Head == 1) return EquipSlot.Head;
			if (slot.Value.Body == 1) return EquipSlot.Body;
			if (slot.Value.Gloves == 1) return EquipSlot.Hands;
			if (slot.Value.Legs == 1) return EquipSlot.Legs;
			if (slot.Value.Feet == 1) return EquipSlot.Feet;
			if (slot.Value.Ears == 1) return EquipSlot.Ears;
			if (slot.Value.Neck == 1) return EquipSlot.Neck;
			if (slot.Value.Wrists == 1) return EquipSlot.Wrists;
			if (slot.Value.FingerR == 1) return EquipSlot.RFinger;
			if (slot.Value.FingerL == 1) return EquipSlot.LFinger;
			return null;
		}

		public static FullEquipType ToFullEquipType(this ItemRow item, bool isMainHand) {
			var slot = (EquipSlot)item.EquipSlotCategory.RowId;
			var weapon = (WeaponCategory)item.Base.ItemUICategory.RowId;
			return slot.ToEquipType(weapon, isMainHand);
		}
		public static InteropGlamourPlateSlot? ToWeaponSlot(this ItemRow item) {
			return item.ToFullEquipType(true).ToSlot() switch {
				EquipSlot.MainHand => InteropGlamourPlateSlot.MainHand,
				EquipSlot.OffHand => InteropGlamourPlateSlot.OffHand,
				_ => null
			};
		}
		public static bool IsMainModelOnOffhand(this ItemRow item) {
			return item.ToFullEquipType(true).ToSlot() == EquipSlot.OffHand;
		}





		private static ItemModel ItemModelFromUlong(this ItemRow item, ulong model, bool isWeapon)
			=> new(model, isWeapon);
		public static ItemModel ModelMainItemModel(this ItemRow item)
			=> item.ItemModelFromUlong(item.Base.ModelMain, item.IsWeapon());
		public static ItemModel ModelSubItemModel(this ItemRow item)
			=> item.ItemModelFromUlong(item.Base.ModelSub, item.IsWeapon());


		public static bool IsWeapon(this ItemRow item)
			=> item.EquipSlotCategory?.Base.MainHand == 1 || item.EquipSlotCategory?.Base.OffHand == 1;
		public static bool CanBeEquipedByPlayedRaceGender(this ItemRow item) {
			var gender = PluginServices.Context.LocalPlayerGender;
			var race = PluginServices.Context.LocalPlayerRace;
			if (gender == null || race == null) return false;

			return item.CanBeEquippedByRaceGender((CharacterRace)race, (CharacterSex)gender);
		}
		public static bool CanBeEquipedByPlayedJob(this ItemRow item, JobFilterType strict = JobFilterType.None)
			=> item.Base.CanBeEquipedByPlayedJob(strict);
		public static bool CanBeEquipedByPlayedJob(this Item item, JobFilterType strict = JobFilterType.None) {
			var job = PluginServices.Context.LocalPlayerClass;
			if (job == null) return false;
			var jobCategory = item.ClassJobCategory;
			// if (jobCategory == null) return false;

			var isEquipable = PluginServices.SheetManager.GetSheet<ClassJobCategorySheet>().IsItemEquippableBy(jobCategory.RowId, job.Value.RowId);
			if (isEquipable && (strict == JobFilterType.Strict || strict == JobFilterType.Relax)) {
				// ensure there is only one category
				isEquipable = PluginServices.SheetManager.GetSheet<ClassJobCategorySheet>()
					.Any(jc=>
					{
						if (jc.RowId != jobCategory.RowId) return false;
						if(strict == JobFilterType.Strict) return jc.ClassJobIds.Count == 1;
						return jc.ClassJobIds.Count > 1; // strict == JobFilterType.Relax
					});
					//was before 7.1: .ClassJobCategoryLookup[categoryId].Count == 1;
			}
			return isEquipable;
		}
		public static CriticalInventoryItem ToInventoryItem(this ItemRow itemEx, InventoryType inventoryType) {
			return new CriticalInventoryItem(inventoryType, 0, itemEx.RowId, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		}

		public static bool ObtainedWithSpecialShopCurrency2(this ItemRow itemEx, uint currencyItemId)
		{
			return itemEx.Sources.Any(s=>s.CostItems.Any(c=>c.ItemRow.RowId == currencyItemId));
			// return Service.ExcelCache.GetSpecialShopSheet().Any(u => u.CostItems.Any(c => c.RowId == currencyItemId) && u.Items.Any(i => i == itemEx));

			// if (Service.ExcelCache.SpecialShopItemRewardCostLookup.TryGetValue(itemEx.RowId, out var specialShop)) {
			// 	return specialShop.Any(c => c.Item2 == currencyItemId);
			// }
			//
			// return false;
		}
		public static ISharedImmediateTexture IconTextureWrap(this ItemRow itemEx) {
			return IconWrapper.Get(itemEx);
		}



		public static void OpenInGarlandTools(this ItemRow item)
			=> $"https://www.garlandtools.org/db/#item/{item.RowId}".OpenBrowser();
		public static void OpenInTeamcraft(this ItemRow item)
			=> $"https://ffxivteamcraft.com/db/en/item/{item.RowId}".OpenBrowser();

		public static void OpenInGamerEscape(this ItemRow item) {

			var enItem = PluginServices.DataManager.GameData.Excel.GetSheet<Item>(Language.English).GetRowOrDefault(item.RowId);
				// .GetSheet<ItemSheet>(Language.English)?.GetRow(item.RowId);

			if (enItem != null)
				$"https://ffxiv.gamerescape.com/w/index.php?search={Uri.EscapeDataString(enItem.Value.Name.ToDalamudString().ToString())}".OpenBrowser();
		}
		public static void OpenInUniversalis(this ItemRow item)
			=> $"https://universalis.app/market/{item.RowId}".OpenBrowser();
		public static void CopyNameToClipboard(this ItemRow item)
			=> item.NameString.ToClipboard();
		public static void LinkInChatHistory(this ItemRow item) {
			if (item.RowId == HardcodedItems.FreeCompanyCreditItemId) {
				return;
			}
			var payloadList = new List<Payload> {
				new UIForegroundPayload((ushort) (0x223 + item.Base.Rarity * 2)),
				new UIGlowPayload((ushort) (0x224 + item.Base.Rarity * 2)),
				new ItemPayload(item.RowId, item.Base.CanBeHq && PluginServices.KeyState[0x11]),
				new UIForegroundPayload(500),
				new UIGlowPayload(501),
				new TextPayload($"{(char) SeIconChar.LinkMarker}"),
				new UIForegroundPayload(0),
				new UIGlowPayload(0),
				new TextPayload(item.NameString + (item.Base.CanBeHq && PluginServices.KeyState[0x11] ? $" {(char)SeIconChar.HighQuality}" : "")),
				new RawPayload(new byte[] {0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03}),
				new RawPayload(new byte[] {0x02, 0x13, 0x02, 0xEC, 0x03})
			};

			var payload = new SeString(payloadList);

			PluginServices.ChatGui.Print(new XivChatEntry {
				Message = payload
			});
		}
		//=> PluginServices.ChatUtilities.LinkItem(item);
		public static void TryOn(this ItemRow item) {
			if (item.CanTryOn && PluginServices.TryOn.CanUseTryOn)
				PluginServices.TryOn.TryOnItem(item);
		}
		//public static void OpenCraftingLog(this ItemRow item) {
		//	if (item.CanOpenCraftLog)
		//		PluginServices.GameInterface.OpenCraftingLog(item.RowId);
		//}

		public static bool IsSoldByAnyVendor(this ItemRow item, IEnumerable<string> vendorNames)
			=> item.GilShops.Any(s => s.ENpcs.Any(n => vendorNames.Any(av => av == n.Resident.Value.Singular)));
				// Service.ExcelCache.GetGilShopItemSheet()
				// .ShopCollection?.GetShops(item.RowId).Any(s => s.ENpcs.Any(n => vendorNames.Any(av => av == n.Resident!.Singular))) ?? false;

		public static Dictionary<EquipSlot,CustomItemId> ToCustomItemId(this ItemRow item, InteropGlamourPlateSlot slot)
			=> Design.FromInventoryItem(item.Base, slot);

		public static bool IsDyeable1(this ItemRow item) {
			return item.Base.DyeCount > 0;
		}
		public static bool IsDyeable2(this ItemRow item) {
			return item.Base.DyeCount > 1;
		}
	}
}
