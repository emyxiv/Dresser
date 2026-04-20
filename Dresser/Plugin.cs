
using CriticalCommonLib;

using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;

using Dresser.Interop.Addons;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Core;
using Dresser.Gui;
using Dresser.UI.Ktk;

using KamiToolKit;

using System;


namespace Dresser {
	public sealed class Plugin : IDalamudPlugin {
		public string Name => "Dresser";
		private const string CommandName = "/dresser";


		public WindowSystem WindowSystem = new("Dresser");

		private static Plugin? PluginInstance = null;
		public static Plugin GetInstance() => PluginInstance!;

		internal ConfigWindow ConfigWindow { get; init; }
		internal GearBrowser GearBrowser { get; init; }
		internal CurrentGear CurrentGear { get; init; }
		internal TagManager TagManager { get; init; }
		internal Dialogs? Dialogs = null;
		internal KtkCurrentGear? KtkCurrentGear = null;
		private bool _ktkCurrentGearCrashed = false;

		public Plugin(
			IDalamudPluginInterface pluginInterface,
			ICommandManager commandManager) {
			PluginInstance = this;
			KamiToolKitLibrary.Initialize(pluginInterface);
			PluginServices.Init(pluginInterface, this);


			ConfigurationManager.Config.ConfigurationChanged += ConfigOnConfigurationChanged;
			PluginServices.Framework.Update += FrameworkOnUpdate;

			Gathering.Init();

			AddonListeners.Init();


			ConfigWindow = new ConfigWindow(this);
			GearBrowser = new GearBrowser(this);
			CurrentGear = new CurrentGear(this);
			TagManager = new TagManager(this);
			Dialogs = new Dialogs(this);
			WindowSystem.AddWindow(ConfigWindow);
			WindowSystem.AddWindow(GearBrowser);
			WindowSystem.AddWindow(CurrentGear);
			WindowSystem.AddWindow(TagManager);
			WindowSystem.AddWindow(Dialogs);

			if (ConfigurationManager.Config.EnableKtk) {
				InitKtkCurrentGear();
			}

			PluginServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "Open dresser."
			});

			pluginInterface.UiBuilder.Draw += DrawUI;
			pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			pluginInterface.UiBuilder.OpenMainUi += DrawMainUI;


		}

		public void InitKtkCurrentGear() {
			PluginLog.Debug("Plugin.InitKtkCurrentGear: begin");
			try {
				KtkCurrentGear = new KtkCurrentGear {
					InternalName = "DresserCurrentGear",
					Title = "Plate Creation",
					Size = new System.Numerics.Vector2(220, 420),
				};
				PluginLog.Debug($"Plugin.InitKtkCurrentGear: instance created (InternalName={KtkCurrentGear.InternalName})");
				KtkCurrentGear.OnCrashFallback = () => {
					PluginLog.Error("KTK crashed — falling back to ImGui CurrentGear");
					_ktkCurrentGearCrashed = true;
					KtkCurrentGear?.Dispose();
					KtkCurrentGear = null;
					CurrentGear.IsOpen = true;
					PluginServices.NotificationManager.AddNotification(new Notification {
						Title = "Dresser",
						Content = "Native CurrentGear UI crashed. Fell back to ImGui. The native UI will stay disabled until the plugin is reloaded.",
						Type = NotificationType.Error,
						Minimized = false,
						InitialDuration = TimeSpan.FromSeconds(8),
					});
				};
				PluginLog.Debug("Plugin.InitKtkCurrentGear: complete");
			} catch (Exception e) {
				PluginLog.Error(e, "Failed to init KtkCurrentGear — falling back to ImGui");
				_ktkCurrentGearCrashed = true;
				KtkCurrentGear = null;
				PluginServices.NotificationManager.AddNotification(new Notification {
					Title = "Dresser",
					Content = "Failed to initialize native CurrentGear UI. Using ImGui instead.",
					Type = NotificationType.Error,
					Minimized = false,
					InitialDuration = TimeSpan.FromSeconds(8),
				});
			}
		}

		public void Dispose() {
			PluginServices.ApplyGearChange.RestoreAppearance();
			PluginServices.ApplyGearChange.ClearApplyDresser();

			KtkCurrentGear?.Dispose();
			ConfigWindow.Dispose();
			GearBrowser.Dispose();
			CurrentGear.Dispose();
			TagManager.Dispose();
			Dialogs?.Dispose();
			// DyePicker.Dispose();

			this.WindowSystem.RemoveAllWindows();
			PluginServices.CommandManager.RemoveHandler(CommandName);

			PluginServices.Framework.Update -= FrameworkOnUpdate;
			ConfigurationManager.Config.ConfigurationChanged -= ConfigOnConfigurationChanged;


			AddonListeners.Dispose();
			KamiToolKitLibrary.Dispose();
			PluginServices.Dispose();
		}

		private void OnCommand(string command, string args) {
			PluginLog.Debug($"{command} {args}");
			switch (args) {
				case "config": DrawConfigUI(); break;
				default:
					// in response to the slash command, just display our main ui
					GearBrowser.IsOpen = true;
					CurrentGear.IsOpen = true;
					break;
			}
		}

		private void DrawUI() {
			this.WindowSystem.Draw();
		}
		public void ToggleConfigUI() {
			ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
		}
		public void DrawMainUI() {
			var cfg = ConfigurationManager.Config;
			var ktkReady = cfg.EnableKtk && cfg.UseNativeCurrentGear && !_ktkCurrentGearCrashed && KtkCurrentGear != null;
			if (ktkReady) {
				KtkCurrentGear!.Open();
				CurrentGear.IsOpen = cfg.KtkDebugDualView;
			} else {
				CurrentGear.IsOpen = true;
			}
		}
		public void DrawConfigUI() {
			ConfigWindow.IsOpen = true;
		}
		public void ToggleDresser() {
			if (IsDresserVisible()) {
				CloseDresser();
			} else {
				OpenDresser();
			}
		}
		public void OpenDresser() {
			var cfg = ConfigurationManager.Config;
			var ktkReady = cfg.EnableKtk && cfg.UseNativeCurrentGear && !_ktkCurrentGearCrashed && KtkCurrentGear != null;
			PluginLog.Debug($"OpenDresser: ktkReady={ktkReady} EnableKtk={cfg.EnableKtk} UseNative={cfg.UseNativeCurrentGear} crashed={_ktkCurrentGearCrashed} KtkCurrentGear={(KtkCurrentGear == null ? "null" : "ready")}");

			if (ktkReady) {
				KtkCurrentGear!.Open();
				// In dual-view debug mode, also keep ImGui open for comparison
				CurrentGear.IsOpen = cfg.KtkDebugDualView;
			} else {
				CurrentGear.IsOpen = true;
			}
			GearBrowser.IsOpen = true;
		}
		public void CloseDresser() {
			//PluginLog.Debug($"CloseDresser");
			KtkCurrentGear?.Close();
			CurrentGear.IsOpen = false;
			GearBrowser.IsOpen = false;
		}
		public void CloseBrowser() {
			GearBrowser.IsOpen = false;
		}
		public void OpenGearBrowserIfClosed() {
			if(!GearBrowser.IsOpen) {
				GearBrowser.IsOpen = true;
			}
		}
		public void UncollapseGearBrowserIfCollapsed() {
			if(GearBrowser.Collapsed == null)
				GearBrowser.Collapsed = false;
		}
		public bool IsDresserVisible()
			=> CurrentGear.IsOpen || (KtkCurrentGear?.IsOpen ?? false);
		public bool IsBrowserVisible()
			=> GearBrowser.IsOpen;
		public void OpenDialog(DialogInfo dialogInfo) {
			Dialogs!.DialogInfo = dialogInfo;
			Dialogs!.IsOpen = true;
		}

		// Inventory tools save config
		private DateTime? _nextSaveTime = null;
		public DateTime? NextSaveTime => _nextSaveTime;

		private void FrameworkOnUpdate(IFramework framework) {
			if (ConfigurationManager.Config.AutoSave) {
				if (NextSaveTime == null && ConfigurationManager.Config.AutoSaveMinutes != 0) {
					_nextSaveTime = DateTime.Now.AddMinutes(ConfigurationManager.Config.AutoSaveMinutes);
				} else {
					if (DateTime.Now >= NextSaveTime) {
						_nextSaveTime = null;
						ConfigurationManager.SaveAsync();
					}
				}
			}
			PluginServices.Context.Refresh();

		}
		private void ConfigOnConfigurationChanged() {
			ConfigurationManager.Save();
		}
	}
}
