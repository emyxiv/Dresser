using CriticalCommonLib;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Ui;

using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;

using Dresser.Interop.Addons;
using Dresser.Services;

namespace Dresser {
	internal class PluginServices {
		[PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
		[PluginService] internal static ClientState ClientState { get; private set; } = null!;
		[PluginService] internal static DataManager DataManager { get; private set; } = null!;
		[PluginService] internal static TargetManager TargetManager { get; private set; } = null!;
		[PluginService] internal static SigScanner SigScanner { get; private set; } = null!;
		[PluginService] internal static KeyState KeyState { get; private set; } = null!;

		public static FrameworkService FrameworkService { get; private set; } = null!;
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
		public static IconStorage IconStorage { get; private set; } = null!;
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

			Service.ExcelCache = new ExcelCache(Service.Data);
			FrameworkService = new FrameworkService(Service.Framework);
			IconStorage = new IconStorage();
			ImageGuiCrop = new ImageGuiCrop();
			Storage = new Storage();

			ConfigurationManager.Load();
			GameInterface = new GameInterface();

			//Universalis = new Universalis();
			//MarketCache.Initalise(Service.Interface.ConfigDirectory.FullName + "/universalis.json");

			CharacterMonitor = new CharacterMonitor();
			GameUi = new GameUiManager();
			OverlayService = new OverlayService(GameUi);

			TryOn = new TryOn();
			OdrScanner = new OdrScanner(CharacterMonitor);
			CraftMonitor = new CraftMonitor(GameUi);
			InventoryScanner = new InventoryScanner(CharacterMonitor, GameUi, GameInterface, OdrScanner);
			InventoryMonitor = new InventoryMonitor(CharacterMonitor, CraftMonitor, InventoryScanner, FrameworkService);
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
			PluginLog.Debug("leaving dresserrrrrrrrrrrrrrrrrrrrrrrr");

			Context.Dispose();

			Service.ExcelCache.Destroy();
			//MarketCache.SaveCache(true);
			//MarketCache.Dispose();
			//Universalis.Dispose();
			GameInterface.Dispose();
			IconStorage.Dispose();
			ImageGuiCrop.Dispose();
			ApplyGearChange.Dispose();
			FrameworkService.Dispose();


			FrameworkService = null!;
			InventoryMonitor = null!;
			InventoryScanner = null!;
			CharacterMonitor = null!;
			Context = null!;
			GameUi = null!;
			TryOn = null!;
			//CommandManager = null!;
			//FilterService = null!;
			OverlayService = null!;
			CraftMonitor = null!;
			//PluginInterface = null!;
			//MarketCache = null!;
			//Universalis = null!;
			GameInterface = null!;
			IconStorage = null!;
			ImageGuiCrop = null!;
			ApplyGearChange = null!;
		}
	}

}
