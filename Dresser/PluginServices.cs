using CriticalCommonLib;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Ui;

using DalaMock.Shared.Classes;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
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
		[PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;

		public static IChatUtilities ChatUtilities { get; private set; } = null!;
		public static HotkeyService HotkeyService { get; private set; } = null!;
		public static CharacterMonitor CharacterMonitor { get; private set; } = null!;
		public static GameInterface GameInterface { get; private set; } = null!;
		public static GameUiManager GameUi { get; private set; } = null!;
		public static OverlayService OverlayService { get; private set; } = null!;
		public static TryOn TryOn { get; private set; } = null!;
		public static ImageGuiCrop ImageGuiCrop { get; private set; } = null!;
		public static AllaganToolsService AllaganTools { get; private set; } = null!;


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

			AllaganTools = new AllaganToolsService(dalamud);
			CharacterMonitor = new CharacterMonitor(Service.Framework, Service.ClientState, Service.ExcelCache);
			GameUi = new GameUiManager(Service.GameInteropProvider);
			OverlayService = new OverlayService(GameUi);
			TryOn = new TryOn();
			GlamourPlates = new();
			ApplyGearChange = new ApplyGearChange(plugin);

			PluginLoaded = true;
			OnPluginLoaded?.Invoke();

		}
		public static void Dispose() {
			PluginLoaded = false;
			ConfigurationManager.ClearQueue();
			ConfigurationManager.Save();

			Storage.Dispose();
			OverlayService.Dispose();
			TryOn.Dispose();
			GameUi.Dispose();
			CharacterMonitor.Dispose();
			AllaganTools.Dispose();

			Context.Dispose();

			Service.ExcelCache.Destroy();
			GameInterface.Dispose();
			HotkeyService.Dispose();
			ImageGuiCrop.Dispose();
			ApplyGearChange.Dispose();

			GlamourPlates.Dispose();

			AllaganTools = null!;
			CharacterMonitor = null!;
			ChatUtilities = null!;
			Context = null!;
			GameUi = null!;
			TryOn = null!;
			OverlayService = null!;
			GlamourPlates = null!;

			GameInterface = null!;
			HotkeyService = null!;
			ImageGuiCrop = null!;
			ApplyGearChange = null!;
		}
	}

}
