using Dalamud.IoC;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Plugin;
using Dalamud.Game.Gui;
using Dalamud.Game.Command;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;


namespace FabulousDresser {
	internal class Services {
		[PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
		[PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
		[PluginService] internal static ClientState ClientState { get; private set; } = null!;


		public static void Init(DalamudPluginInterface dalamud) {
			dalamud.Create<Services>();
		}
	}
}
