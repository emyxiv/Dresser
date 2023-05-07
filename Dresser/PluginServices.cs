using Dalamud.IoC;
using Dalamud.Data;
using Dalamud.Plugin;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;

using CriticalCommonLib;
using CriticalCommonLib.Crafting;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Ui;

using Dresser.Data;
using Dresser.Logic;
using Dalamud.Logging;

namespace Dresser {
	internal class PluginServices {
		[PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
		[PluginService] internal static ClientState ClientState { get; private set; } = null!;
		[PluginService] internal static DataManager DataManager { get; private set; } = null!;
		[PluginService] internal static TargetManager TargetManager { get; private set; } = null!;
		[PluginService] internal static SigScanner SigScanner { get; private set; } = null!;
		[PluginService] internal static KeyState KeyState { get; private set; } = null!;

		public static OdrScanner OdrScanner { get; private set; } = null!;
		public static InventoryMonitor InventoryMonitor { get; private set; } = null!;
		public static InventoryScanner InventoryScanner { get; private set; } = null!;
		public static CharacterMonitor CharacterMonitor { get; private set; } = null!;
		public static GameInterface GameInterface { get; private set; } = null!;
		public static GameUiManager GameUi { get; private set; } = null!;
		public static TryOn TryOn { get; private set; } = null!;
		public static CraftMonitor CraftMonitor { get; private set; } = null!;
		//public static MarketCache MarketCache { get; private set; } = null!;
		//public static Universalis Universalis { get; private set; } = null!;
		public static IconStorage IconStorage { get; private set; } = null!;


		internal static Interop.Hooks.AddonManager AddonManager = null!;
		internal static Interop.Hooks.GlamourPlates GlamourPlates = null!;
		internal static Storage Storage = null!;


		public static void Init(DalamudPluginInterface dalamud) {

			dalamud.Create<PluginServices>();
			dalamud.Create<Service>();

			IconStorage = new IconStorage();

			//PluginLog.Debug($"data ready {Service.Data.IsDataReady == true}");

			Service.ExcelCache = new ExcelCache(Service.Data);
			ConfigurationManager.Load();
			GameInterface = new GameInterface();

			//Universalis = new Universalis();
			//MarketCache.Initalise(Service.Interface.ConfigDirectory.FullName + "/universalis.json");

			CharacterMonitor = new CharacterMonitor();
			GameUi = new GameUiManager();
			TryOn = new TryOn();
			OdrScanner = new OdrScanner(CharacterMonitor);
			CraftMonitor = new CraftMonitor(GameUi);
			InventoryScanner = new InventoryScanner(CharacterMonitor, GameUi, GameInterface, OdrScanner);
			InventoryMonitor = new InventoryMonitor(CharacterMonitor, CraftMonitor, InventoryScanner);
			InventoryScanner.Enable();

			GlamourPlates = new();

			//PluginLoaded = true;
			//OnPluginLoaded?.Invoke();

			Storage = new Storage();

		}
		public static void Dispose() {
			ConfigurationManager.ClearQueue();
			ConfigurationManager.Save();

			Storage.Dispose();
			//CommandManager.Dispose();
			//FilterService.Dispose();
			InventoryMonitor.Dispose();
			InventoryScanner.Dispose();
			CraftMonitor.Dispose();
			TryOn.Dispose();
			GameUi.Dispose();
			CharacterMonitor.Dispose();
			PluginLog.Debug("leaving dresserrrrrrrrrrrrrrrrrrrrrrrr");

			Service.ExcelCache.Destroy();
			//MarketCache.SaveCache(true);
			//MarketCache.Dispose();
			//Universalis.Dispose();
			GameInterface.Dispose();
			IconStorage.Dispose();


			InventoryMonitor = null!;
			InventoryScanner = null!;
			CharacterMonitor = null!;
			GameUi = null!;
			TryOn = null!;
			//CommandManager = null!;
			//FilterService = null!;
			CraftMonitor = null!;
			//PluginInterface = null!;
			//MarketCache = null!;
			//Universalis = null!;
			GameInterface = null!;
			IconStorage = null!;
		}
	}

}
