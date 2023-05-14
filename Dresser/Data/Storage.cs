using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiScene;

using Dalamud.Interface.GameFonts;
using Lumina.Data.Files;
using Lumina.Excel;

using CriticalCommonLib;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;

using Dresser.Data.Excel;
using Dresser.Structs.FFXIV;
using CriticalCommonLib.Enums;
using Dresser.Extensions;
using Dalamud.Logging;

namespace Dresser.Data {
	internal class Storage : IDisposable {

		public static ExcelSheet<Dye>? Dyes = null;
		public static string HighResolutionSufix = "_hr1";

		public const int PlateNumber = 20;
		public static Dictionary<GlamourPlateSlot, MirageItem> SlotMirageItems = new();
		//public static Dictionary<GlamourPlateSlot, InventoryItem> SlotInventoryItems = new();
		public static MiragePage[]? Pages = null;
		public static MiragePage? DisplayPage = null;
		public static Dictionary<byte, Vector4> RarityColors = new();
		public readonly GameFontHandle FontTitle =
			PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic68));
		public readonly GameFontHandle FontRadio =
			PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.Axis36));

		public Storage() {
			Dyes = Sheets.GetSheet<Dye>();
			// ui colors:
			// bad: 14

			RarityColors = new(){
				// items colors:
				{ 0, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 2)) },
				// white: 33/549
				{ 1, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 549)) },
				// green: 42/67/551
				{ 2, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 551)) },
				// blue : 37/38/553
				{ 3, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 553)) },
				// purple: 522/48/555
				{ 4, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 555)) },
				// orange:557
				{ 5, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 557)) },
				// yellow:559
				{ 6, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 559)) },
				// pink: 578/556
				{ 7, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 561)) },
				// gold: 563
				{ 8, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 563)) },
			};

			foreach ((var slot, var part_id) in ImageGuiCrop.EmptyGlamourPlateSlot)
				ImageGuiCrop.GetPart("character", part_id);

			LoadAdditionalItems();
		}
		public void Dispose() {
			Dyes = null;
			Sheets.Cache.Clear();
		}

		public static TexFile GetEmptyEquipTex() {

			var path = $"ui/uld/Character{HighResolutionSufix}.tex";
			return PluginServices.DataManager.GetFile<TexFile>(path)!;
		}

		public static Vector4 RarityColor(ItemEx itemEx) {
			if (!RarityColors.TryGetValue(itemEx.Rarity, out var rarityColor))
				rarityColor = Vector4.One;
			return rarityColor;
		}




		// prepare aditional items data
		public enum AdditionalItem {
			None,
			All,
			Vendor,
			Currency,
		}

		public static Dictionary<AdditionalItem, Dictionary<InventoryType, string>> FilterNames = new() {
			{AdditionalItem.All,new() {
				{ (InventoryType) 99300, "All Items" },
			}},
			{AdditionalItem.Vendor,new() {
				{ (InventoryType) 99400, "Calamity Vendor" },
				{ (InventoryType) 99401, "Relic Vendor" },
			}},
			{AdditionalItem.Currency,new() {
				{(InventoryType) 99520, "Storm Seal" },
				{(InventoryType) 99521, "Serpent Seal" },
				{(InventoryType) 99522, "Flame Seal" },
				{(InventoryType) 99528, "Poetics" },
			}},
		};
		public Dictionary<InventoryType, HashSet<InventoryItem>> AdditionalItems = FilterNames.SelectMany(a => a.Value.Keys).ToDictionary(itn => itn, itn => new HashSet<InventoryItem>());
		//public Dictionary<AdditionalItem, Dictionary<InventoryType, HashSet<InventoryItem>>> AdditionalItems = FilterNames.ToDictionary(fn=>fn.Key,fn=>fn.Value.ToDictionary(itn=>itn.Key,itn=> new HashSet<InventoryItem>()));

		public static InventoryItem NewInventoryItem(InventoryType inventoryType, uint itemId) {

			var invIt =  new InventoryItem(inventoryType, 0, itemId, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
			invIt.SortedContainer = inventoryType;
			return invIt;
		}

		// all items
		public static HashSet<InventoryType> FilterAll = new() { (InventoryType)99300 };
		// vendor
		public static Dictionary<InventoryType, HashSet<string>> FilterVendorAllowedNames = new() {
			{ (InventoryType) 99400 , new(){"Calamity salvager", "journeyman salvager"} },
			{ (InventoryType) 99401 , new(){"restoration node", "Drake"} },
		};
		// currency
		public static Dictionary<InventoryType, uint> FilterCurrencyIds = new() {
			{(InventoryType) 99520, 20 },
			{(InventoryType) 99521, 21 },
			{(InventoryType) 99522, 22 },
			{(InventoryType) 99528, 28 },
		};
		private void LoadAdditional_All() {
			foreach (var inventoryType in FilterAll) {
				// at least filter glam items
				PluginLog.Debug($"================= item numbers all: {Service.ExcelCache.AllItems.Count}");

				AdditionalItems[inventoryType] = Service.ExcelCache.AllItems
					//.DistinctBy(i=>i.Value.GetSharedModels())
					.Where((itemPair) => itemPair.Value.ModelMain != 0)
					.Select(i => NewInventoryItem(inventoryType, i.Key)).ToHashSet();


				var a = FilterNames[AdditionalItem.All][inventoryType];
				var b = AdditionalItems[inventoryType].Count;
				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.All][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}
		private void LoadAdditional_Vendor() {

			foreach ((var inventoryType, var allowedVendorsForType) in FilterVendorAllowedNames) {
				AdditionalItems[inventoryType] = Service.ExcelCache.AllItems.Where((itemPair) => {
					return Service.ExcelCache.ShopCollection.GetShops(itemPair.Key).Any(s => s.ENpcs.Any(n => allowedVendorsForType.Any(av => av == n.Resident!.Singular)));
				}).Select(i => NewInventoryItem(inventoryType, i.Key)).ToHashSet();
				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.Vendor][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}
		private void LoadAdditional_Currency() {
			foreach ((var inventoryType, var currencyId) in FilterCurrencyIds) {
				AdditionalItems[inventoryType] = Service.ExcelCache.AllItems
					.Where((itemPair) => itemPair.Value.ObtainedWithSpecialShopCurrency(currencyId))
					.Select(i => NewInventoryItem(inventoryType, i.Key))
					.ToHashSet();
				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.Currency][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}

		public void LoadAdditionalItems() {

			LoadAdditional_All();
			LoadAdditional_Vendor();
			LoadAdditional_Currency();

		}
	}
}
