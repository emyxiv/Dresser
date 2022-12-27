using System.IO;

using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;

using FabulousDresser.Windows;

namespace FabulousDresser {
	public sealed class Plugin : IDalamudPlugin {
		public string Name => "Fabulous Dresser";
		private const string CommandName = "/dresser";

		public Configuration Configuration { get; init; }
		public WindowSystem WindowSystem = new("FabulousDresser");

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager) {
			Services.Init(pluginInterface);

			this.Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			this.Configuration.Initialize(pluginInterface);

			Interop.Hooks.Addons.Init();

			// you might normally want to embed resources and load them from the manifest stream
			var imagePath = Path.Combine(pluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
			var goatImage = pluginInterface.UiBuilder.LoadImage(imagePath);

			WindowSystem.AddWindow(new ConfigWindow(this));
			WindowSystem.AddWindow(new MainWindow(this, goatImage));

			Services.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "A useful message to display in /xlhelp"
			});

			pluginInterface.UiBuilder.Draw += DrawUI;
			pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
		}

		public void Dispose() {
			this.WindowSystem.RemoveAllWindows();
			Services.CommandManager.RemoveHandler(CommandName);

			Interop.Hooks.Addons.Dispose();
		}

		private void OnCommand(string command, string args) {
			// in response to the slash command, just display our main ui
			WindowSystem.GetWindow("Fabulous Dresser")!.IsOpen = true;
		}

		private void DrawUI() {
			this.WindowSystem.Draw();
		}

		public void DrawConfigUI() {
			WindowSystem.GetWindow("Fabulous Dresser Settings")!.IsOpen = true;
		}
	}
}
