using CriticalCommonLib;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Ui;

using DalaMock.Shared.Classes;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Dresser.Interop.Addons;
using Dresser.Logic;
using Dresser.Services;

namespace Dresser {
	internal class PluginServices {
		[PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
		[PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
		[PluginService] internal static IClientState ClientState { get; private set; } = null!;
		[PluginService] internal static IDataManager DataManager { get; private set; } = null!;
		[PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
		[PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
		[PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
		[PluginService] internal static IKeyState KeyState { get; private set; } = null!;
		[PluginService] internal static IGameGui DalamudGameGui { get; private set; } = null!;

		public static IChatUtilities ChatUtilities { get; private set; } = null!;
		public static HotkeyService HotkeyService { get; private set; } = null!;
		public static OdrScanner OdrScanner { get; private set; } = null!;
		public static InventoryMonitor InventoryMonitor { get; private set; } = null!;
		public static InventoryScanner InventoryScanner { get; private set; } = null!;
		public static CharacterMonitor CharacterMonitor { get; private set; } = null!;
		public static GameInterface GameInterface { get; private set; } = null!;
		public static GameUiManager GameUi { get; private set; } = null!;
		public static OverlayService OverlayService { get; private set; } = null!;
		public static TryOn TryOn { get; private set; } = null!;
		public static CraftMonitor CraftMonitor { get; private set; } = null!;
		//public static MarketCache MarketCache { get; private set; } = null!;
		//public static Universalis Universalis { get; private set; } = null!;
		public static ImageGuiCrop ImageGuiCrop { get; private set; } = null!;


		internal static AddonManager AddonManager = null!;
		internal static Interop.Hooks.GlamourPlates GlamourPlates = null!;
		internal static Storage Storage = null!;
		internal static ApplyGearChange ApplyGearChange = null!;
		internal static Context Context = null!;

		public static bool PluginLoaded { get; private set; } = false;

		public delegate void PluginLoadedDelegate();
		public static event PluginLoadedDelegate? OnPluginLoaded;

		public static void Init(DalamudPluginInterface dalamud, Plugin plugin) {

			dalamud.Create<PluginServices>();
			dalamud.Create<Service>();

			Context = new Context();

			//PluginLog.Debug($"data ready {Service.Data.IsDataReady == true}");

			Service.Interface = new PluginInterfaceService(dalamud);
			Service.ExcelCache = new ExcelCache(Service.Data);
			Service.ExcelCache.PreCacheItemData();
			HotkeyService = new HotkeyService(Service.Framework, KeyState);
			ImageGuiCrop = new ImageGuiCrop();
			Storage = new Storage();
			ChatUtilities = new ChatUtilities();

			ConfigurationManager.Load();
			GameInterface = new GameInterface(Service.GameInteropProvider);
			HotkeySetup.Init();


			//Universalis = new Universalis();
			//MarketCache.Initalise(Service.Interface.ConfigDirectory.FullName + "/universalis.json");

			CharacterMonitor = new CharacterMonitor(Service.Framework, Service.ClientState, Service.ExcelCache);
			GameUi = new GameUiManager(Service.GameInteropProvider);
			OverlayService = new OverlayService(GameUi);

			TryOn = new TryOn();
			OdrScanner = new OdrScanner(CharacterMonitor);
			CraftMonitor = new CraftMonitor(GameUi);
			InventoryScanner = new InventoryScanner(CharacterMonitor, GameUi, GameInterface, OdrScanner, Service.GameInteropProvider);
			InventoryMonitor = new InventoryMonitor(CharacterMonitor, CraftMonitor, InventoryScanner, Service.Framework);
			InventoryScanner.Enable();

			GlamourPlates = new();

			//PluginLoaded = true;
			//OnPluginLoaded?.Invoke();

			ApplyGearChange = new ApplyGearChange(plugin);

			PluginLoaded = true;
			OnPluginLoaded?.Invoke();

		}
		public static void Dispose() {
			PluginLoaded = false;
			ConfigurationManager.ClearQueue();
			ConfigurationManager.Save();

			Storage.Dispose();
			//CommandManager.Dispose();
			//FilterService.Dispose();
			OverlayService.Dispose();
			InventoryMonitor.Dispose();
			InventoryScanner.Dispose();
			CraftMonitor.Dispose();
			TryOn.Dispose();
			GameUi.Dispose();
			CharacterMonitor.Dispose();

			Context.Dispose();

			Service.ExcelCache.Destroy();
			//MarketCache.SaveCache(true);
			//MarketCache.Dispose();
			//Universalis.Dispose();
			GameInterface.Dispose();
			HotkeyService.Dispose();
			ImageGuiCrop.Dispose();
			ApplyGearChange.Dispose();

			GlamourPlates.Dispose();

			InventoryMonitor = null!;
			InventoryScanner = null!;
			CharacterMonitor = null!;
			ChatUtilities = null!;
			Context = null!;
			GameUi = null!;
			TryOn = null!;
			//CommandManager = null!;
			//FilterService = null!;
			OverlayService = null!;
			CraftMonitor = null!;
			GlamourPlates = null!;
			//PluginInterface = null!;
			//MarketCache = null!;
			//Universalis = null!;
			GameInterface = null!;
			HotkeyService = null!;
			ImageGuiCrop = null!;
			ApplyGearChange = null!;
		}
	}

}
