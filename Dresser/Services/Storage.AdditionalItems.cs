using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AllaganLib.GameSheets.Caches;
using AllaganLib.GameSheets.Sheets;
using AllaganLib.GameSheets.Sheets.Rows;

using CriticalCommonLib.Enums;

using Dalamud.Interface.Textures;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Models;
using Dresser.Gui;

using InventoryItem = Dresser.Models.InventoryItem;


namespace Dresser.Services {
	internal partial class Storage {

		// prepare aditional items data
		public enum AdditionalItem {
			None = 0,
			All = 1,
			ObtainedAt = 2,
			Currency = 3,
			Modded = 7,
		}
		// InventoryTypeExtra must match AdditionalItem * 1000000 + (currency item id OR other)
		public enum InventoryTypeExtra {
			AllItems = 1000000,

			CalamityVendors = 2000001,
			RelicVendors = 2000022,
			SquareStore = 2000033,

			//StormSeal = 3000020,
			//SerpentSeal = 3000021,
			//FlameSeal = 3000022,
			WolfMarks = 3000025,
			AlliedSeals = 3000027,
			Poetics = 3000028,
			MandervilleGoldsaucerPoints = 3000029,
			AllaganTomestonesPreviousTier = 3000048,
			AllaganTomestonesCurrentTier = 3000049,
			CenturioSeals = 3010307,
			SackOfNuts = 3026533,
			// BicolorGemstones = 3026807,
			CraftersScriptsPreviousTier = 3033913,
			GatherersScriptsPreviousTier = 3033914,
			CraftersScriptsCurrentTier = 3041784,
			GatherersScriptsCurrentTier = 3041785,
			TrophyCrystal = 3036656,
			SeafarersCowrie = 3037549,
			// IslanderCowrie = 3037550,

			ModdedItems = 7000000,

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
					var itemEx = PluginServices.SheetManager.GetSheet<ItemSheet>().First(i => i.RowId == itemId);
					FilterCurrencyItemEx.Add((InventoryType)invTypeExtra, itemEx);
					FilterCurrencyIconTexture.Add((InventoryType)invTypeExtra, itemEx.IconTextureWrap());
				}
			}

			AdditionalItems = FilterNames.SelectMany(a => a.Value.Keys).ToDictionary(itn => itn, itn => new List<InventoryItem>());

		}
		public Dictionary<AdditionalItem, Dictionary<InventoryType, string>> FilterNames;
		public Dictionary<InventoryType, List<InventoryItem>> AdditionalItems;

		// all items
		public static HashSet<InventoryType> FilterAll = new() { (InventoryType)InventoryTypeExtra.AllItems, };
		// vendor
		public static Dictionary<InventoryType, Func<ItemRow, bool>> FilterUnobtainedFromCustomSource = new() {
			{ (InventoryType) InventoryTypeExtra.CalamityVendors , (i) => {return i.HasSourcesByType(ItemInfoType.CalamitySalvagerShop); } },
			{ (InventoryType) InventoryTypeExtra.RelicVendors , (i) => {return i.IsSoldByAnyVendor(new string[] {"Drake", "restoration node", "staelhundr", "Regana", "House Manderville vendor"}); } },
			{ (InventoryType) InventoryTypeExtra.SquareStore , i => i.HasSourcesByCategory(ItemInfoCategory.Shop) },
		};
		// currency
		public Dictionary<InventoryType, uint> FilterCurrencyIds;
		public Dictionary<InventoryType, ItemRow> FilterCurrencyItemEx;
		public Dictionary<InventoryType, ISharedImmediateTexture> FilterCurrencyIconTexture;
		private void LoadAdditional_All() {
			foreach (var inventoryType in FilterAll) {
				// at least filter glam items
				PluginLog.Debug($"================= item numbers all: {PluginServices.SheetManager.GetSheet<ItemSheet>().Count}");

				var q = PluginServices.SheetManager.GetSheet<ItemSheet>()
					//.DistinctBy(i=>i.Value.GetSharedModels())
					.Where((itemPair) => itemPair.Base.ModelMain != 0);

				AdditionalItems[inventoryType] = q.Select(i => new InventoryItem(inventoryType, i.RowId)).ToList();

				var a = FilterNames[AdditionalItem.All][inventoryType];
				var b = AdditionalItems[inventoryType].Count;
				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.All][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}
		private void LoadAdditional_Custom() {

			foreach ((var inventoryType, var filterFunction) in FilterUnobtainedFromCustomSource) {
				AdditionalItems[inventoryType] = PluginServices.SheetManager.GetSheet<ItemSheet>().Where((itemPair) => {
					return itemPair.Base.ModelMain != 0 && filterFunction(itemPair);
				}).Select(i => new InventoryItem(inventoryType, i.RowId)).ToList();

				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.ObtainedAt][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}
		private void LoadAdditional_Currency() {
			foreach ((var inventoryType, var currencyId) in FilterCurrencyIds) {
				AdditionalItems[inventoryType] = PluginServices.SheetManager.GetSheet<ItemSheet>()
					.Where((itemPair) => itemPair.Base.ModelMain != 0 && itemPair.ObtainedWithSpecialShopCurrency2(currencyId))
					.Select(i => new InventoryItem(inventoryType, i.RowId))
					.ToList();
				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.Currency][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}

		private void LoadAdditional_Modded() {
			AdditionalItems[(InventoryType)InventoryTypeExtra.ModdedItems] = ConfigurationManager.Config.PenumbraModdedItems;
		}

		private void RecomputeModdedItemsList() {
			if (PluginServices.Penumbra.GetEnabledState() == false) return;

			IsReloadingMods = true;

			var penumbraVersions = PluginServices.Penumbra.ApiVersions();
			PluginLog.Debug($"--------------- PENUMBRA --- v {penumbraVersions.Breaking} - {penumbraVersions.Features} ----------------");
			ConfigurationManager.Config.PenumbraModdedItems = PluginServices.Penumbra.GetModdedInventoryItems();
			LoadAdditional_Modded();
			PluginLog.Debug($"------------ END - PENUMBRA --------------------------");
			IsReloadingMods = false;

			if(Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
		}


		private bool _isReloadingMods = false;
		public bool IsReloadingMods {
			get {
				return _isReloadingMods;
			}
			private set {
				_isReloadingMods = value;
				ModsReloadingCur = 0;
				ModsReloadingMax = 0;

			}
		}
		public int ModsReloadingCur = 0;
		public int ModsReloadingMax = 0;

		public void ClearMods() {
			ConfigurationManager.Config.PenumbraModdedItems.Clear();
			AdditionalItems[(InventoryType)InventoryTypeExtra.ModdedItems].Clear();
		}
		public void ReloadMods() {
			Task.Run(async delegate {
				await Task.Run(() => {
					this.RecomputeModdedItemsList();
				});
			});

		}
		public void LoadAdditionalItems() {
			PluginServices.OnPluginLoaded -= LoadAdditionalItems;
			Task.Run(async delegate {
				await Task.Run(() => LoadAdditional_All());
				await Task.Run(() => LoadAdditional_Custom());
				await Task.Run(() => LoadAdditional_Currency());
				await Task.Run(() => LoadAdditional_Modded());
			});
		}
	}
}
