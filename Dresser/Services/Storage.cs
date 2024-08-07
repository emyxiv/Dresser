using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Sheets;

using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures;

using Dresser.Extensions;
using Dresser.Interop;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Structs.Dresser;

using Lumina.Data.Files;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using static Dresser.Windows.Components.GuiHelpers;

using InventoryItem = Dresser.Structs.Dresser.InventoryItem;


namespace Dresser.Services {
	internal class Storage : IDisposable {

		//public ExcelSheet<Dye>? Dyes = null;
		public static string HighResolutionSufix = "_hr1";

		public const int PlateNumber = Offsets.TotalPlates;
		public Dictionary<GlamourPlateSlot, MiragePlateItem> SlotMirageItems = new();
		//public static Dictionary<GlamourPlateSlot, InventoryItem> SlotInventoryItems = new();
		public MiragePlate[]? Pages = null;
		public MiragePlate? DisplayPage = null;
		public Dictionary<byte, Vector4> RarityColors = new();
		public HashSet<byte> RarityAllowed = new() { 1, 2, 3, 4, 7 };

		public Dictionary<Font, (IFontHandle handle, float size)> FontHandles = new();



		public Storage() {

			//Dyes = Sheets.GetSheet<Dye>();
			// ui colors:
			// bad: 14

			var uicolorsheet = PluginServices.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.UIColor>()!;

			RarityColors = new(){
				// items colors:
				{ 0, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 2)) },
				// white: 33/549
				{ 1, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 549)) },
				// green: 42/67/551
				{ 2, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 551)) },
				// blue : 37/38/553
				{ 3, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 553)) },
				// purple: 522/48/555
				{ 4, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 555)) },
				// orange:557
				{ 5, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 557)) },
				// yellow:559
				{ 6, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 559)) },
				// pink: 578/556
				{ 7, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 561)) },
				// gold: 563
				{ 8, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 563)) },
			};


			InitItemTypes();
			PluginServices.OnPluginLoaded += LoadAdditionalItems;
		}
		public void Dispose() {
			//Dyes = null;
			//Sheets.Cache.Clear();

			foreach((var k, (var h, var s)) in FontHandles) h.Dispose();
			FontHandles.Clear();
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
			ObtainedAt = 2,
			Currency = 3,
			Modded = 7,
		}
		// InventoryTypeExtra must match AdditionalItem * 1000000 + (currency item id OR other)
		public enum InventoryTypeExtra {
			AllItems = 1000000,
			AllButSqStore = 1000001,

			CalamityVendors = 2000001,
			RelicVendors = 2000002,
			SquareStore = 2000003,

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
					var itemEx = Service.ExcelCache.GetItemExSheet().First(i => i.RowId == itemId);
					FilterCurrencyItemEx.Add((InventoryType)invTypeExtra, itemEx);
					FilterCurrencyIconTexture.Add((InventoryType)invTypeExtra, itemEx.IconTextureWrap());
				}
			}

			AdditionalItems = FilterNames.SelectMany(a => a.Value.Keys).ToDictionary(itn => itn, itn => new List<InventoryItem>());

		}
		public Dictionary<AdditionalItem, Dictionary<InventoryType, string>> FilterNames;
		public Dictionary<InventoryType, List<InventoryItem>> AdditionalItems;
		//public Dictionary<AdditionalItem, Dictionary<InventoryType, HashSet<InventoryItem>>> AdditionalItems = FilterNames.ToDictionary(fn=>fn.Key,fn=>fn.Value.ToDictionary(itn=>itn.Key,itn=> new HashSet<InventoryItem>()));

		// all items
		public static HashSet<InventoryType> FilterAll = new() { (InventoryType)InventoryTypeExtra.AllItems, (InventoryType)InventoryTypeExtra.AllButSqStore };
		// vendor
		public static Dictionary<InventoryType, Func<ItemEx, bool>> FilterUnobtainedFromCustomSource = new() {
			{ (InventoryType) InventoryTypeExtra.CalamityVendors , (i) => {return i.IsSoldByAnyVendor(new string[] {"Calamity salvager", "journeyman salvager"}); } },
			{ (InventoryType) InventoryTypeExtra.RelicVendors , (i) => {return i.IsSoldByAnyVendor(new string[] {"Drake", "restoration node", "staelhundr", "Regana", "House Manderville vendor"}); } },
			{ (InventoryType) InventoryTypeExtra.SquareStore , i => i.PurchasedSQStore },
		};
		// currency
		public Dictionary<InventoryType, uint> FilterCurrencyIds;
		public Dictionary<InventoryType, ItemEx> FilterCurrencyItemEx;
		public Dictionary<InventoryType, ISharedImmediateTexture> FilterCurrencyIconTexture;
		private void LoadAdditional_All() {
			foreach (var inventoryType in FilterAll) {
				// at least filter glam items
				PluginLog.Debug($"================= item numbers all: {Service.ExcelCache.AllItems.Count}");

				var q = Service.ExcelCache.AllItems
					//.DistinctBy(i=>i.Value.GetSharedModels())
					.Where((itemPair) => itemPair.Value.ModelMain != 0);

				if (inventoryType == (InventoryType)InventoryTypeExtra.AllButSqStore) q = q.Where(p => !p.Value.PurchasedSQStore); // for AllButSqStore

				AdditionalItems[inventoryType] = q.Select(i => new InventoryItem(inventoryType, i.Key)).ToList();

				var a = FilterNames[AdditionalItem.All][inventoryType];
				var b = AdditionalItems[inventoryType].Count;
				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.All][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}
		private void LoadAdditional_Custom() {

			foreach ((var inventoryType, var filterFunction) in FilterUnobtainedFromCustomSource) {
				AdditionalItems[inventoryType] = Service.ExcelCache.AllItems.Where((itemPair) => {
					return itemPair.Value.ModelMain != 0 && filterFunction(itemPair.Value);
				}).Select(i => new InventoryItem(inventoryType, i.Key)).ToList();

				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.ObtainedAt][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}
		private void LoadAdditional_Currency() {
			foreach ((var inventoryType, var currencyId) in FilterCurrencyIds) {
				AdditionalItems[inventoryType] = Service.ExcelCache.AllItems
					.Where((itemPair) => itemPair.Value.ModelMain != 0 && itemPair.Value.ObtainedWithSpecialShopCurrency2(currencyId))
					.Select(i => new InventoryItem(inventoryType, i.Key))
					.ToList();
				PluginLog.Debug($" Loaded {FilterNames[AdditionalItem.Currency][inventoryType]} ({inventoryType}): {AdditionalItems[inventoryType].Count} items");
			}
		}

		private void LoadAdditional_Modded() {
			AdditionalItems[(InventoryType)InventoryTypeExtra.ModdedItems] = ConfigurationManager.Config.PenumbraModdedItems;
		}

		public static string PenumbraCollectionModList = "Dresser Mod List";
		private void RecomputeModdedItemsList() {
			if (PluginServices.Penumbra.GetEnabledState() == false) return;

			IsReloadingMods = true;

			var penumbraVersions = PluginServices.Penumbra.ApiVersions();
			PluginLog.Debug($"--------------- PENUMBRA --- v {penumbraVersions.Breaking} - {penumbraVersions.Features} ----------------");

			// make a list of enabled mods

			List<(string Path, string Name)> DaCollModsSettings;
			if (ConfigurationManager.Config.PenumbraUseModListCollection) DaCollModsSettings = PluginServices.Penumbra.GetEnabledModsForCollection(PenumbraCollectionModList, true);
			else {
				DaCollModsSettings = PluginServices.Penumbra.GetNotBlacklistedMods().ToList();
				ModsReloadingMax = DaCollModsSettings.Count;
			}

			PluginLog.Debug($"Found {DaCollModsSettings.Count} enabled mod in collection {PenumbraCollectionModList}");
			// make list of inventoryItems
			List<InventoryItem> inventoryItems = PluginServices.Penumbra.GetChangedInventoryItemForMods(DaCollModsSettings);

			//PluginLog.Debug($"------------ CHECK  PENUMBRA --------------------------");
			//foreach(var i in tmpItemList) {
			//	PluginLog.Debug($"Checking tmpItemList item {i.ItemId} for mod {i.ModName} || {i.ModDirectory}");
			//}
			ConfigurationManager.Config.PenumbraModdedItems = inventoryItems;
			LoadAdditional_Modded();
			//foreach (var i in AdditionalItems[(InventoryType)InventoryTypeExtra.ModdedItems]) {
			//	PluginLog.Debug($"Checking item {i.ItemId} for mod {i.ModName} || {i.ModDirectory}");
			//}
			PluginLog.Debug($"------------ END - PENUMBRA --------------------------");
			IsReloadingMods = false;

			if(Plugin.GetInstance().GearBrowser.IsOpen) Windows.GearBrowser.RecomputeItems();
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
