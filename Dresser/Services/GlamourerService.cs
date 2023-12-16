using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using Dresser.Logic;

using FFXIVClientStructs.FFXIV.Client.Game;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Dresser.Services {
	internal class GlamourerService : IDisposable {

		private readonly ICallGateSubscriber<int> _apiVersion;
		private readonly ICallGateSubscriber<(int Major, int Minor)> _apiVersions;
		private readonly ICallGateSubscriber<string, Character?, object?> _applyOnlyEquipmentToCharacterProvider;


		public GlamourerService(DalamudPluginInterface pluginInterface) {
			_apiVersion    = pluginInterface.GetIpcSubscriber<int>("Glamourer.ApiVersion");
			_apiVersions   = pluginInterface.GetIpcSubscriber<(int Major, int Minor)>("Glamourer.ApiVersions");
			_applyOnlyEquipmentToCharacterProvider = pluginInterface.GetIpcSubscriber<string, Character?, object?>("Glamourer.ApplyOnlyEquipmentToCharacter");

		}
		public int ApiVersion()                     { try { return _apiVersion.InvokeFunc( ); } catch (Exception e) { PluginLog.Error(e, "Failed to contact Glamourer"); return -1; } }
		public (int Major, int Minor) ApiVersions() { try { return _apiVersions.InvokeFunc(); } catch (Exception) { return (-1, -1); } }
		public bool IsInitialized()                 { return ApiVersion() >= 0; }
		public object? ApplyOnlyEquipmentToCharacter(string a1, Character? character) { try { return _applyOnlyEquipmentToCharacterProvider.InvokeFunc(a1,character); } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyOnlyEquipmentToCharacter"); return null; } }



		public void Dispose() {
		}
	}
}
