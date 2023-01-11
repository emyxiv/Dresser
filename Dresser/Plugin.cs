
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;

using Dresser.Data;
using Dresser.Windows;
using Dresser.Interop.Hooks;
using Dresser.Interop;

namespace Dresser {
	public sealed class Plugin : IDalamudPlugin {
		public string Name => "Dresser";
		private const string CommandName = "/dresser";

		public Configuration Configuration { get; init; }
		public WindowSystem WindowSystem = new("Dresser");

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager) {
			PluginServices.Init(pluginInterface);


			this.Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			this.Configuration.Initialize(pluginInterface);

			Storage.Init();
			Gathering.Init();

			Methods.Init();
			Interop.Hooks.AddonListeners.Init();



			WindowSystem.AddWindow(new ConfigWindow(this));
			WindowSystem.AddWindow(new GearBrowser());
			WindowSystem.AddWindow(new CurrentGear());


			PluginServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "A useful message to display in /xlhelp"
			});

			pluginInterface.UiBuilder.Draw += DrawUI;
			pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;


			EventManager.GearSelectionOpen += OpenDresser;
			EventManager.GearSelectionClose += CloseDresser;

			if (GlamourPlates.IsGlaming())
				EventManager.GearSelectionOpen?.Invoke();
		}

		public void Dispose() {
			EventManager.GearSelectionClose?.Invoke();

			this.WindowSystem.RemoveAllWindows();
			PluginServices.CommandManager.RemoveHandler(CommandName);


			EventManager.GearSelectionOpen -= OpenDresser;
			EventManager.GearSelectionClose -= CloseDresser;


			Interop.Hooks.AddonListeners.Dispose();
			Storage.Dispose();
			PluginServices.Dispose();
		}

		private void OnCommand(string command, string args) {
			// in response to the slash command, just display our main ui
			WindowSystem.GetWindow("Gear Browser")!.IsOpen = true;
			WindowSystem.GetWindow("Current Gear")!.IsOpen = true;
		}

		private void DrawUI() {
			this.WindowSystem.Draw();
		}

		public void DrawConfigUI() {
			WindowSystem.GetWindow("Dresser Settings")!.IsOpen = true;
		}
		public void ToggleDresser() {
			WindowSystem.GetWindow("Current Gear")!.IsOpen = !IsDresserVisible();
			WindowSystem.GetWindow("Gear Browser")!.IsOpen = !IsDresserVisible();
		}
		public void OpenDresser() {
			//PluginLog.Debug($"OpenDresser");
			WindowSystem.GetWindow("Current Gear")!.IsOpen = true;
			WindowSystem.GetWindow("Gear Browser")!.IsOpen = true;
		}
		public void CloseDresser() {
			//PluginLog.Debug($"CloseDresser");
			WindowSystem.GetWindow("Current Gear")!.IsOpen = false;
			WindowSystem.GetWindow("Gear Browser")!.IsOpen = false;
		}
		public bool IsDresserVisible()
			=> WindowSystem.GetWindow("Current Gear")!.IsOpen;
	}
}
