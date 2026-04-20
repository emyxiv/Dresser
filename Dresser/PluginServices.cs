using AllaganLib.GameSheets.Service;

using CriticalCommonLib.Services;

using Dalamud.Game;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;

using Dresser.Interop.Addons;
using Dresser.Interop.Agents;
using Dresser.Interop.Overlays;
using Dresser.Logic;
using Dresser.Core;
using Dresser.Services;
using Dresser.Services.Ipc;
using Dresser.Models;

using Microsoft.Extensions.DependencyInjection;

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
        [PluginService] public static IAddonLifecycle AddonLifecycle { get; set; } = null!;
        [PluginService] public static INotificationManager NotificationManager { get; set; } = null!;

		private static ServiceProvider _serviceProvider = null!;

		public static PenumbraIpc Penumbra { get; private set; } = null!;
		public static HotkeyService HotkeyService { get; private set; } = null!;
		public static SheetManager SheetManager { get; private set; } = null!;
		public static ImageGuiCrop ImageGuiCrop { get; private set; } = null!;
		public static ModdedIconStorage ModdedIconStorage { get; private set; } = null!;
		public static ItemVendorLocation ItemVendorLocation { get; private set; } = null!;
		public static AllaganToolsService AllaganTools { get; private set; } = null!;
		public static GlamourerService Glamourer { get; private set; } = null!;
		public static Actions Actions { get; private set; } = null!;
		public static InventoryItemFactory InventoryItemFactory { get; private set; } = null!;


		internal static AddonManager AddonManager = null!;
		internal static MiragePlateAgent MiragePlateAgent = null!;
		internal static MiragePlateOverlayController MiragePlateOverlay = null!;
		internal static Storage Storage = null!;
		internal static ApplyGearChange ApplyGearChange = null!;
		internal static Context Context = null!;

		public static bool PluginLoaded { get; private set; } = false;

		public delegate void PluginLoadedDelegate();
		public static event PluginLoadedDelegate? OnPluginLoaded;

		public static void Init(IDalamudPluginInterface dalamud, Plugin plugin)
		{

			dalamud.Create<PluginServices>();

			_serviceProvider = ServiceRegistration.ConfigureServices(dalamud, plugin);

			// Resolve in order to control initialization sequence
			SheetManager = _serviceProvider.GetRequiredService<SheetManager>();
			InventoryItemFactory = _serviceProvider.GetRequiredService<InventoryItemFactory>();

			Context = _serviceProvider.GetRequiredService<Context>();

			HotkeyService = _serviceProvider.GetRequiredService<HotkeyService>();
			ImageGuiCrop = _serviceProvider.GetRequiredService<ImageGuiCrop>();
			Penumbra = _serviceProvider.GetRequiredService<PenumbraIpc>();
			Storage = _serviceProvider.GetRequiredService<Storage>();

			ConfigurationManager.Load();
			TagStore.LoadLinks();
			HotkeySetup.Init();

			ModdedIconStorage = _serviceProvider.GetRequiredService<ModdedIconStorage>();
			ItemVendorLocation = _serviceProvider.GetRequiredService<ItemVendorLocation>();
			AllaganTools = _serviceProvider.GetRequiredService<AllaganToolsService>();
			Glamourer = _serviceProvider.GetRequiredService<GlamourerService>();
			MiragePlateAgent = _serviceProvider.GetRequiredService<MiragePlateAgent>();
			ApplyGearChange = _serviceProvider.GetRequiredService<ApplyGearChange>();
			MiragePlateOverlay = new MiragePlateOverlayController();

			Actions = _serviceProvider.GetRequiredService<Actions>();

			PluginLoaded = true;
			OnPluginLoaded?.Invoke();

		}
		public static void Dispose() {
			PluginLoaded = false;
			ConfigurationManager.ClearQueue();
			ConfigurationManager.Save();

			MiragePlateOverlay?.Dispose();

			// ServiceProvider.Dispose() disposes all IDisposable singletons
			// in reverse creation order
			_serviceProvider.Dispose();
			_serviceProvider = null!;
		}
	}

}
