using System;
using System.Collections.Generic;
using System.Linq;

using AllaganLib.GameSheets.Caches;
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
using Dresser.Services;
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
		public static bool IsOrnate(this ItemRow item) {
			return item.Base.MateriaSlotCount >= 5;
		}
		public static bool IsCashShop(this ItemRow item) {
			return item.HasSourcesByType(ItemInfoType.CashShop);
		}
		public static bool HasNoSource(this ItemRow item) {
			return item.Sources.Count == 0;
		}
		public static bool IsPartOfGlamourSet(this ItemRow item) {
			return item.Uses.Any(s => s.Type == ItemInfoType.GlamourReadySetItem);
		}

		public static bool CanBeEquipedByPlayedRaceGenderGc(this ItemRow item) {
			var gender = PluginServices.Context.LocalPlayerGender;
			var race = PluginServices.Context.LocalPlayerRace;
			if (gender == null || race == null) return false;
			if (!item.CanBeEquippedByRaceGender((CharacterRace)race, (CharacterSex)gender)) return false; // race/gender doesn't match, not equipable

			if(!item.Base.GrandCompany.IsValid || item.Base.GrandCompany.RowId == 0) return true; // no GC restriction, equipable


			var playerGc = PluginServices.Context.LocalPlayerGrandCompany;
			if (playerGc == null) return false; // player has no GC, but item requires one, not equipable

			var playerGcId = playerGc.Value.RowId;
			var itemGc = item.Base.GrandCompany.RowId;
			if(playerGcId != itemGc) return false; // player's GC doesn't match item's GC, not equipable

			// all checks passed, item is equipable
			return true;
		}
		public static bool CanBeEquipedByPlayedJob(this Item item) {
			var job = PluginServices.Context.LocalPlayerClass;
			if (job == null) return false;
			var jobCategory = item.ClassJobCategory;
			return PluginServices.SheetManager.GetSheet<ClassJobCategorySheet>().IsItemEquippableBy(jobCategory.RowId, job.Value.RowId);

		}
		public static bool CanBeEquipedByFilteredJobs(this ItemRow item)
			=> item.Base.CanBeEquipedByFilteredJobs();
		public static bool CanBeEquipedByFilteredJobs(this Item item) {

			var jobCategory = item.ClassJobCategory;
			//if (jobCategory == null) return false;

			var filterList = ConfigurationManager.Config.FilterClassJobCategories;
			if (filterList == null || filterList.Count == 0) return false;

			var jcrId = jobCategory.RowId;
			var jft = ConfigurationManager.Config.filterCurrentJobFilterType;
			var sheet = PluginServices.SheetManager.GetSheet<ClassJobCategorySheet>();

			var jobCatRow = sheet.FirstOrDefault(jc => jc.RowId == jcrId);
			var classJobIdsCount = jobCatRow?.ClassJobIds?.Count ?? 0;

			foreach (var jcRowId in filterList) {
				if (!sheet.IsItemEquippableBy(jcrId, jcRowId)) continue; // item is not equippable by this job, skip

				if (jft == JobFilterType.All) return true;

				if (jft == JobFilterType.Job) {
					if (classJobIdsCount == 1) return true;
					continue;
				}
				if (jft == JobFilterType.Type) {
					if(classJobIdsCount > 1 && classJobIdsCount < PluginServices.Storage.ClassJobsTotalCount) return true;
					continue;
				}

				if (jft == JobFilterType.NoJob) {
					if (classJobIdsCount > 1) return true;
					continue;
				}

				return true; // if no filter type matched, return true as fallback, should not happen
			}

			return false;
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
		public static void OpenInItemVendorLocation(this ItemRow item) {
			if (!PluginServices.ItemVendorLocation.IsInitialized()) return;
			PluginServices.ItemVendorLocation.OpenUiWithItemId(item.RowId);
		}

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
			// if (item.CanTryOn && PluginServices.TryOn.CanUseTryOn)
			// 	PluginServices.TryOn.TryOnItem(item);
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
