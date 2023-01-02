using Dalamud.IoC;
using Dalamud.Data;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;

using CriticalCommonLib.Crafting;
using CriticalCommonLib.Services.Ui;
using CriticalCommonLib;
using CriticalCommonLib.MarketBoard;
using MarketCache = CriticalCommonLib.MarketBoard.Cache;
using CriticalCommonLib.Services;
using Dalamud.Logging;
using Dalamud.Game.Gui;
using Dalamud.Interface.ImGuiFileDialog;
using Dresser.Data;
using Dalamud.Game.ClientState.Objects;

namespace Dresser {
	internal class PluginServices {
		[PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
		[PluginService] internal static ClientState ClientState { get; private set; } = null!;
		[PluginService] internal static DataManager DataManager { get; private set; } = null!;
		[PluginService] internal static TargetManager TargetManager { get; private set; } = null!;


		public static OdrScanner OdrScanner { get; private set; } = null!;
		public static InventoryMonitor InventoryMonitor { get; private set; } = null!;
		public static CharacterMonitor CharacterMonitor { get; private set; } = null!;
		//public static GameInterface GameInterface { get; private set; } = null!;
		public static GameUiManager GameUi { get; private set; } = null!;
		public static TryOn TryOn { get; private set; } = null!;
		public static CraftMonitor CraftMonitor { get; private set; } = null!;
		//public static MarketCache MarketCache { get; private set; } = null!;
		//public static Universalis Universalis { get; private set; } = null!;
		public static IconStorage IconStorage { get; private set; } = null!;


		internal static Interop.Hooks.AddonManager AddonManager = null!;


		public static void Init(DalamudPluginInterface dalamud) {

			dalamud.Create<PluginServices>();
			dalamud.Create<Service>();

			IconStorage = new IconStorage();

			//PluginLog.Debug($"data ready {Service.Data.IsDataReady == true}");

			Service.ExcelCache = new ExcelCache(Service.Data);
			//ConfigurationManager.Load();
			//Universalis = new Universalis();
			//MarketCache.Initalise(Service.Interface.ConfigDirectory.FullName + "/universalis.json");

			CharacterMonitor = new CharacterMonitor();
			GameUi = new GameUiManager();
			TryOn = new TryOn();
			OdrScanner = new OdrScanner(CharacterMonitor);
			CraftMonitor = new CraftMonitor(GameUi);
			//InventoryScanner = new InventoryScanner(CharacterMonitor, GameUi, GameInterface);
			InventoryMonitor = new InventoryMonitor(OdrScanner, CharacterMonitor, GameUi, CraftMonitor);



			//PluginLoaded = true;
			//OnPluginLoaded?.Invoke();

		}
		public static void Dispose() {
			//CommandManager.Dispose();
			//FilterService.Dispose();
			InventoryMonitor.Dispose();
			OdrScanner.Dispose();
			CraftMonitor.Dispose();
			TryOn.Dispose();
			GameUi.Dispose();
			CharacterMonitor.Dispose();
			Service.ExcelCache.Destroy();
			//MarketCache.SaveCache(true);
			//MarketCache.Dispose();
			//Universalis.Dispose();
			//GameInterface.Dispose();
			IconStorage.Dispose();


			InventoryMonitor = null!;
			OdrScanner = null!;
			CharacterMonitor = null!;
			GameUi = null!;
			TryOn = null!;
			//CommandManager = null!;
			//FilterService = null!;
			CraftMonitor = null!;
			//PluginInterface = null!;
			//MarketCache = null!;
			//Universalis = null!;
			//GameInterface = null!;
			IconStorage = null!;
		}
	}

}
