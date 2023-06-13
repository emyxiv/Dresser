using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;

using Dalamud.Interface.GameFonts;
using Dalamud.Logging;

using Dresser.Data;
using Dresser.Data.Excel;
using Dresser.Extensions;
using Dresser.Structs.Dresser;

using ImGuiScene;

using Lumina.Data.Files;
using Lumina.Excel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Dresser.Services {
	internal class Storage : IDisposable {

		public ExcelSheet<Dye>? Dyes = null;
		public static string HighResolutionSufix = "_hr1";

		public const int PlateNumber = 20;
		public Dictionary<GlamourPlateSlot, MirageItem> SlotMirageItems = new();
		//public static Dictionary<GlamourPlateSlot, InventoryItem> SlotInventoryItems = new();
		public MiragePage[]? Pages = null;
		public MiragePage? DisplayPage = null;
		public Dictionary<byte, Vector4> RarityColors = new();
		public readonly GameFontHandle FontTitle =
			PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic68));
		public readonly GameFontHandle FontRadio =
			PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.Axis36));
		public readonly GameFontHandle FontConfigHeaders =
			PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic23));

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


			InitItemTypes();
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
			if (!PluginServices.Storage.RarityColors.TryGetValue(itemEx.Rarity, out var rarityColor))
				rarityColor = Vector4.One;
			return rarityColor;
		}
		public static Dictionary<ushort, InventoryItemSet> PagesInv {
			get => PluginServices.Storage.Pages?.Select((value, index) => new { value, index }).ToDictionary(p => (ushort)p.index, p => (InventoryItemSet)p.value) ?? new();
		}




		// prepare aditional items data
		public enum AdditionalItem {
			None = 0,
			All = 1,
			Vendor = 2,
			Currency = 3,
		}
		// InventoryTypeExtra must match AdditionalItem * 1000000 + (currency item id OR other)
		public enum InventoryTypeExtra {
			AllItems = 1000000,

			CalamityVendor = 2000001,
			RelicVendor = 2000002,

			//StormSeal = 3000020,
			//SerpentSeal = 3000021,
			//FlameSeal = 3000022,
			WolfMarks = 3000025,
			AlliedSeals = 3000027,
			Poetics = 3000028,
			MandervilleGoldsaucerPoints = 3000029,
			AllaganTomestonesOfCausality = 3000044,
			CenturioSeals = 3010307,
			WhiteCraftersScrips = 3025199,
			WhiteGaterersScrips = 3025200,
			SackOfNuts = 3026533,
			PurpleCraftersScripts = 3033913,
			PurpleGatherersScripts = 3033914,
			TrophyCrystal = 3036656,
			SeafarersCowrie = 3037549,

		}

		public void InitItemTypes() {

			var inventoryTypeExtras = Enum.GetValues<InventoryTypeExtra>();


			FilterNames = new();
			FilterCurrencyIds = new();
			FilterCurrencyItemEx = new();
			FilterCurrencyIconTexture = new();

			foreach (var invTypeExtra in inventoryTypeExtras) {
				var additionalItem = (AdditionalItem)((int)invTypeExtra / 1000000);
				var itemId = (uint)invTypeExtra % 100000;

				if (!FilterNames.ContainsKey(additionalItem))
					FilterNames.Add(additionalItem, new());
				FilterNames[additionalItem].Add((InventoryType)invTypeExtra, invTypeExtra.ToString());

				if (additionalItem == AdditionalItem.Currency) {
					FilterCurrencyIds.Add((InventoryType)invTypeExtra, itemId);
					var itemEx = Service.ExcelCache.GetItemExSheet().First(i => i.RowId == itemId);
					FilterCurrencyItemEx.Add((InventoryType)invTypeExtra, itemEx);
					FilterCurrencyIconTexture.Add((InventoryType)invTypeExtra, itemEx.IconTextureWrap());
				}
			}

			AdditionalItems = FilterNames.SelectMany(a => a.Value.Keys).ToDictionary(itn => itn, itn => new HashSet<InventoryItem>());

		}
		public Dictionary<AdditionalItem, Dictionary<InventoryType, string>> FilterNames;
		public Dictionary<InventoryType, HashSet<InventoryItem>> AdditionalItems;
		//public Dictionary<AdditionalItem, Dictionary<InventoryType, HashSet<InventoryItem>>> AdditionalItems = FilterNames.ToDictionary(fn=>fn.Key,fn=>fn.Value.ToDictionary(itn=>itn.Key,itn=> new HashSet<InventoryItem>()));

		public static InventoryItem NewInventoryItem(InventoryType inventoryType, uint itemId) {

			var invIt = new InventoryItem(inventoryType, 0, itemId, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
			invIt.SortedContainer = inventoryType;
			return invIt;
		}

		// all items
		public static HashSet<InventoryType> FilterAll = new() { (InventoryType)InventoryTypeExtra.AllItems };
		// vendor
		public static Dictionary<InventoryType, HashSet<string>> FilterVendorAllowedNames = new() {
			{ (InventoryType) InventoryTypeExtra.CalamityVendor , new(){"Calamity salvager", "journeyman salvager"} },
			{ (InventoryType) InventoryTypeExtra.RelicVendor , new(){"Drake", "restoration node", "staelhundr", "Regana", "House Manderville vendor", } },
		};
		// currency
		public Dictionary<InventoryType, uint> FilterCurrencyIds;
		public Dictionary<InventoryType, ItemEx> FilterCurrencyItemEx;
		public Dictionary<InventoryType, TextureWrap> FilterCurrencyIconTexture;
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
					return itemPair.Value.ModelMain != 0 && Service.ExcelCache.ShopCollection.GetShops(itemPair.Key).Any(s => s.ENpcs.Any(n => allowedVendorsForType.Any(av => av == n.Resident!.Singular)));
				}).Select(i => NewInventoryItem(inventoryType, i.Key)).ToHashSet();
				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.Vendor][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}
		private void LoadAdditional_Currency() {
			foreach ((var inventoryType, var currencyId) in FilterCurrencyIds) {
				AdditionalItems[inventoryType] = Service.ExcelCache.AllItems
					.Where((itemPair) => itemPair.Value.ModelMain != 0 && itemPair.Value.ObtainedWithSpecialShopCurrency2(currencyId))
					.Select(i => NewInventoryItem(inventoryType, i.Key))
					.ToHashSet();
				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.Currency][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}

		public void LoadAdditionalItems() {
			Task.Run(async delegate {
				await Task.Run(() => LoadAdditional_All());
				await Task.Run(() => LoadAdditional_Vendor());
				await Task.Run(() => LoadAdditional_Currency());
			});
		}
	}
}
