using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;

using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Internal;
using Dresser.Logic;

using Dresser.Data;
using Dresser.Data.Excel;
using Dresser.Extensions;
using Dresser.Structs.Dresser;



using Lumina.Data.Files;
using Lumina.Excel;

using Penumbra.Api.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

using InventoryItem = Dresser.Structs.Dresser.InventoryItem;


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
		public HashSet<byte> RarityAllowed = new() { 1, 2, 3, 4, 7 };
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
			ObtainedAt = 2,
			Currency = 3,
			Modded = 7,
		}
		// InventoryTypeExtra must match AdditionalItem * 1000000 + (currency item id OR other)
		public enum InventoryTypeExtra {
			AllItems = 1000000,

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
		public static HashSet<InventoryType> FilterAll = new() { (InventoryType)InventoryTypeExtra.AllItems };
		// vendor
		public static Dictionary<InventoryType, Func<ItemEx, bool>> FilterUnobtainedFromCustomSource = new() {
			{ (InventoryType) InventoryTypeExtra.CalamityVendors , (i) => {return i.IsSoldByAnyVendor(new string[] {"Calamity salvager", "journeyman salvager"}); } },
			{ (InventoryType) InventoryTypeExtra.RelicVendors , (i) => {return i.IsSoldByAnyVendor(new string[] {"Drake", "restoration node", "staelhundr", "Regana", "House Manderville vendor"}); } },
			{ (InventoryType) InventoryTypeExtra.SquareStore , i => i.PurchasedSQStore },
		};
		// currency
		public Dictionary<InventoryType, uint> FilterCurrencyIds;
		public Dictionary<InventoryType, ItemEx> FilterCurrencyItemEx;
		public Dictionary<InventoryType, IDalamudTextureWrap> FilterCurrencyIconTexture;
		private void LoadAdditional_All() {
			foreach (var inventoryType in FilterAll) {
				// at least filter glam items
				PluginLog.Debug($"================= item numbers all: {Service.ExcelCache.AllItems.Count}");

				AdditionalItems[inventoryType] = Service.ExcelCache.AllItems
					//.DistinctBy(i=>i.Value.GetSharedModels())
					.Where((itemPair) => itemPair.Value.ModelMain != 0)
					.Select(i => new InventoryItem(inventoryType, i.Key)).ToList();


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

		public static string PenumbraCollectionModList = "Dresser Mod List";
		private void LoadAdditional_Modded() {
			if (PluginServices.Penumbra.GetEnabledState() == false) return;

			_isReloadingMods = true;

			var penumbraVersions = PluginServices.Penumbra.ApiVersions();
			PluginLog.Debug($"--------------- PENUMBRA --- v {penumbraVersions.Breaking} - {penumbraVersions.Features} ----------------");


			List<InventoryItem> tmpItemList = new();
			Dictionary<(string Path, string Name),(bool EnabledState, int Priority, IDictionary<string, IList<string>> EnabledOptions, bool Inherited)> DaCollModsSettings = new();

			PluginLog.Debug($"Penumbra mods:");
			foreach (var mod1 in PluginServices.Penumbra.GetMods()) {
				PluginLog.Debug($"Checking {mod1.Name}||{mod1.Path}");
				var modSettings = PluginServices.Penumbra.GetCurrentModSettings(PenumbraCollectionModList, mod1.Path, mod1.Name, true);
				if(modSettings.Item1 == PenumbraApiEc.Success && modSettings.Item2.HasValue && modSettings.Item2.Value.EnabledState) {
					ModsReloadingMax++;
					PluginLog.Debug($"Found ACTIVE mod {mod1.Name} || {mod1.Path}");

					DaCollModsSettings.Add(mod1, modSettings.Item2.Value);
				}
			}

			PluginLog.Debug($"Found {DaCollModsSettings.Count} enabled mod in collection {PenumbraCollectionModList}");


			foreach ((var mod3, var modSettings) in DaCollModsSettings) {

				foreach(var i in PluginServices.Penumbra.GetChangedItemIdsForMod(mod3.Path, mod3.Name)) {
					var item = new InventoryItem((InventoryType)InventoryTypeExtra.ModdedItems, i.ItemId.Copy(), mod3.Name.Copy()!, mod3.Path.Copy()!, i.ModModelPath.Copy()!);
					// todo: add icon path
					tmpItemList.Add(item);
					PluginLog.Debug($"Added item {item.ItemId} [{item.FormattedName}] for mod {item.ModName} || {item.ModDirectory}");
				}
				ModsReloadingCur++;
			}



			PluginLog.Debug($"------------ CHECK  PENUMBRA --------------------------");
			foreach(var i in tmpItemList) {
				PluginLog.Debug($"Checking tmpItemList item {i.ItemId} for mod {i.ModName} || {i.ModDirectory}");
			}
			AdditionalItems[(InventoryType)InventoryTypeExtra.ModdedItems] = tmpItemList;
			foreach (var i in AdditionalItems[(InventoryType)InventoryTypeExtra.ModdedItems]) {
				PluginLog.Debug($"Checking item {i.ItemId} for mod {i.ModName} || {i.ModDirectory}");
			}
			PluginLog.Debug($"------------ END - PENUMBRA --------------------------");
			_isReloadingMods = false;
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

		public void ReloadMods() {
			Task.Run(async delegate {
				await Task.Run(() => {
					this.LoadAdditional_Modded();
				});
			});

		}
		public void LoadAdditionalItems() {
			Task.Run(async delegate {
				await Task.Run(() => LoadAdditional_All());
				await Task.Run(() => LoadAdditional_Custom());
				await Task.Run(() => LoadAdditional_Currency());
				await Task.Run(() => LoadAdditional_Modded());
			});
		}
	}
}
