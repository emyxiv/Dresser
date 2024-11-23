
using CriticalCommonLib;

using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Dresser.Interop;
using Dresser.Interop.Addons;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Windows;

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
		internal DyePicker DyePicker { get; init; }
		internal Dialogs? Dialogs = null;

		public Plugin(
			IDalamudPluginInterface pluginInterface,
			ICommandManager commandManager) {
			PluginInstance = this;
			PluginServices.Init(pluginInterface, this);


			ConfigurationManager.Config.ConfigurationChanged += ConfigOnConfigurationChanged;
			PluginServices.Framework.Update += FrameworkOnUpdate;

			Gathering.Init();

			Methods.Init();
			AddonListeners.Init();


			ConfigWindow = new ConfigWindow(this);
			GearBrowser = new GearBrowser(this);
			CurrentGear = new CurrentGear(this);
			// DyePicker = new DyePicker(this);
			Dialogs = new Dialogs(this);
			WindowSystem.AddWindow(ConfigWindow);
			WindowSystem.AddWindow(GearBrowser);
			WindowSystem.AddWindow(CurrentGear);
			// WindowSystem.AddWindow(DyePicker);
			WindowSystem.AddWindow(Dialogs);



			PluginServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "Open dresser."
			});

			pluginInterface.UiBuilder.Draw += DrawUI;
			pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			pluginInterface.UiBuilder.OpenMainUi += DrawMainUI;


		}

		public void Dispose() {
			PluginServices.ApplyGearChange.RestoreAppearance();
			PluginServices.ApplyGearChange.ClearApplyDresser();

			ConfigWindow.Dispose();
			GearBrowser.Dispose();
			CurrentGear.Dispose();
			Dialogs?.Dispose();
			// DyePicker.Dispose();

			this.WindowSystem.RemoveAllWindows();
			PluginServices.CommandManager.RemoveHandler(CommandName);

			PluginServices.Framework.Update -= FrameworkOnUpdate;
			ConfigurationManager.Config.ConfigurationChanged -= ConfigOnConfigurationChanged;


			AddonListeners.Dispose();
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
			CurrentGear.IsOpen = true;
		}
		public void DrawConfigUI() {
			ConfigWindow.IsOpen = true;
		}
		public void ToggleDresser() {
			CurrentGear.IsOpen = !IsDresserVisible();
			GearBrowser.IsOpen = !IsDresserVisible();
		}
		public void OpenDresser() {
			//PluginLog.Debug($"OpenDresser");
			CurrentGear.IsOpen = true;
			GearBrowser.IsOpen = true;
		}
		public void CloseDresser() {
			//PluginLog.Debug($"CloseDresser");
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
			=> CurrentGear.IsOpen;
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
			PluginServices.ApplyGearChange.FrameworkUpdate();

		}
		private void ConfigOnConfigurationChanged() {
			ConfigurationManager.Save();
		}
	}
}
