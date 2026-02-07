using AllaganLib.GameSheets.Service;

using CriticalCommonLib.Services;

using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Dresser.Interop.Addons;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;

namespace Dresser {
	internal class PluginServices {
		[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
		[PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
		[PluginService] internal static IClientState ClientState { get; private set; } = null!;
		[PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
		[PluginService] internal static IDataManager DataManager { get; private set; } = null!;
		[PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
		[PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
		[PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
		[PluginService] internal static IKeyState KeyState { get; private set; } = null!;
		[PluginService] internal static IGameGui DalamudGameGui { get; private set; } = null!;
		[PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
		[PluginService] public static IFramework Framework { get; set; } = null!;
		[PluginService] public static IChatGui ChatGui { get; set; } = null!;
		[PluginService] public static IObjectTable Objects { get; set; } = null!;
        [PluginService] public static IGameConfig GameConfig { get; set; } = null!;
        [PluginService] public static ICondition Condition { get; set; } = null!;
        [PluginService] public static IGameInteropProvider GameInteropProvider { get; set; } = null!;

		public static PenumbraIpc Penumbra { get; private set; } = null!;
		//public static IChatUtilities ChatUtilities { get; private set; } = null!;
		public static HotkeyService HotkeyService { get; private set; } = null!;
		//public static CharacterMonitor CharacterMonitor { get; private set; } = null!;
		//public static GameInterface GameInterface { get; private set; } = null!;
		// public static GameUiManager GameUi { get; private set; } = null!;
		public static SheetManager SheetManager { get; private set; } = null!;
		// public static OverlayService OverlayService { get; private set; } = null!;
		// public static TryOn TryOn { get; private set; } = null!;
		public static ImageGuiCrop ImageGuiCrop { get; private set; } = null!;
		public static ModdedIconStorage ModdedIconStorage { get; private set; } = null!;
		public static AllaganToolsService AllaganTools { get; private set; } = null!;
		public static GlamourerService Glamourer { get; private set; } = null!;
		public static Actions Actions { get; private set; } = null!;
		public static InventoryItemFactory InventoryItemFactory { get; private set; } = null!;


		internal static AddonManager AddonManager = null!;
		internal static GlamourPlates GlamourPlates = null!;
		internal static Storage Storage = null!;
		internal static ApplyGearChange ApplyGearChange = null!;
		internal static Context Context = null!;

		public static bool PluginLoaded { get; private set; } = false;

		public delegate void PluginLoadedDelegate();
		public static event PluginLoadedDelegate? OnPluginLoaded;

		public static void Init(IDalamudPluginInterface dalamud, Plugin plugin)
		{

			dalamud.Create<PluginServices>();

			var gd = DataManager.GameData;
			var smso = new SheetManagerStartupOptions()
			{
				BuildNpcLevels = true,
				BuildNpcShops = true,
				BuildItemInfoCache = true,
				CalculateLookups = true,
                PersistInDataShare = true,
                CacheInDataShare = true
			};
			SheetManager = new SheetManager(dalamud, gd, smso);
			InventoryItemFactory = new InventoryItemFactory(SheetManager, DataManager);

			Context = new Context();

			HotkeyService = new HotkeyService(Framework, KeyState);
			ImageGuiCrop = new ImageGuiCrop();
			Penumbra = new PenumbraIpc();
			Storage = new Storage();
			//ChatUtilities = new ChatUtilities();

			ConfigurationManager.Load();
			//GameInterface = new GameInterface(Service.GameInteropProvider);
			HotkeySetup.Init();

			ModdedIconStorage = new ModdedIconStorage();
			AllaganTools = new AllaganToolsService(dalamud);
			Glamourer = new GlamourerService(dalamud);
			//CharacterMonitor = new CharacterMonitor(Service.Framework, Service.ClientState, Service.ExcelCache);
			// GameUi = new GameUiManager(Framework, DalamudGameGui, PluginLog);
			// OverlayService = new OverlayService(GameUi);
			// TryOn = new TryOn(Framework, PluginLog);
			GlamourPlates = new();
			ApplyGearChange = new ApplyGearChange(plugin);

			Actions = new Actions();

			PluginLoaded = true;
			OnPluginLoaded?.Invoke();

		}
		public static void Dispose() {
			PluginLoaded = false;
			ConfigurationManager.ClearQueue();
			ConfigurationManager.Save();

			Actions.Dispose();
			ModdedIconStorage.Dispose();
			Storage.Dispose();
			// OverlayService.Dispose();
			// TryOn.Dispose();
			// GameUi.Dispose();
			//CharacterMonitor.Dispose();
			AllaganTools.Dispose();
			Glamourer.Dispose();

			Context.Dispose();

			//GameInterface.Dispose();
			HotkeyService.Dispose();
			ImageGuiCrop.Dispose();
			ApplyGearChange.Dispose();
			Penumbra.Dispose();

			GlamourPlates.Dispose();

			Actions = null!;
			AllaganTools = null!;
			Glamourer = null!;
			//CharacterMonitor = null!;
			//ChatUtilities = null!;
			Context = null!;
			// GameUi = null!;
			// TryOn = null!;
			// OverlayService = null!;
			GlamourPlates = null!;
			ModdedIconStorage = null!;
			//GameInterface = null!;
			HotkeyService = null!;
			ImageGuiCrop = null!;
			ApplyGearChange = null!;
			Penumbra = null!;
		}
	}

}
