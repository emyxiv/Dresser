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
		private readonly ICallGateSubscriber<string, string, object?> _applyOnlyEquipmentProvider;
		private readonly ICallGateSubscriber<string, Character?, object?> _applyOnlyEquipmentToCharacterProvider;
		private readonly ICallGateSubscriber<string, string?> _getAllCustomizationProvider;
		private readonly ICallGateSubscriber<string, Character?, object?> _applyAllToCharacterProvider;


		private readonly ICallGateSubscriber<Character?, object?> _revertCharacterProvider;
		private readonly ICallGateSubscriber<Character?, uint, object?> _revertCharacterProviderLock;
		private readonly ICallGateSubscriber<string, Character?, uint, object?> _applyOnlyEquipmentToCharacterProviderLock;
		private readonly ICallGateSubscriber<Character?, uint, bool> _revertToAutomationCharacterProvider;


		public GlamourerService(DalamudPluginInterface pluginInterface) {
			_apiVersion    = pluginInterface.GetIpcSubscriber<int>("Glamourer.ApiVersion");
			_apiVersions   = pluginInterface.GetIpcSubscriber<(int Major, int Minor)>("Glamourer.ApiVersions");
			_applyOnlyEquipmentProvider = pluginInterface.GetIpcSubscriber<string, string, object?>("Glamourer.ApplyOnlyEquipment");
			_applyOnlyEquipmentToCharacterProvider = pluginInterface.GetIpcSubscriber<string, Character?, object?>("Glamourer.ApplyOnlyEquipmentToCharacter");
			_applyOnlyEquipmentToCharacterProviderLock = pluginInterface.GetIpcSubscriber<string, Character?, uint, object?>("Glamourer.ApplyOnlyEquipmentToCharacterLock");
			_getAllCustomizationProvider = pluginInterface.GetIpcSubscriber<string, string?>("Glamourer.GetAllCustomization");
			_applyAllToCharacterProvider = pluginInterface.GetIpcSubscriber<string, Character?, object?>("Glamourer.ApplyAllToCharacter");
			//_applyOnlyEquipmentToCharacterProvider = pluginInterface.GetIpcSubscriber<string, Character?, object?>("Glamourer.ApplyAllToCharacter");
			_apiVersions = pluginInterface.GetIpcSubscriber<(int Major, int Minor)>("Glamourer.ApiVersions");


			_revertCharacterProvider = pluginInterface.GetIpcSubscriber<Character?, object?>("Glamourer.RevertCharacter");
			_revertCharacterProviderLock = pluginInterface.GetIpcSubscriber<Character?, uint, object?>("Glamourer.RevertCharacterLock");
			_revertToAutomationCharacterProvider = pluginInterface.GetIpcSubscriber<Character?, uint, bool>("Glamourer.RevertToAutomationCharacter");

	}
	public int ApiVersion()                                                    { try { return _apiVersion.                    InvokeFunc( );                   } catch (Exception e) { PluginLog.Error(e, "Failed to contact Glamourer"); return -1; } }
		public (int Major, int Minor) ApiVersions()                                { try { return _apiVersions.                   InvokeFunc();                    } catch (Exception) { return (-1, -1); } }
		public bool IsInitialized()                                                { try { return _apiVersion.InvokeFunc() >= 0; } catch (Exception) { return false; } }
		public void ApplyOnlyEquipment(string a1, string characterName)            { try { _applyOnlyEquipmentProvider.           InvokeAction(a1, characterName); } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyOnlyEquipment"); } }
		public void ApplyOnlyEquipmentToCharacter(string a1, Character? character) { try { _applyOnlyEquipmentToCharacterProvider.InvokeAction(a1, character);     } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyOnlyEquipmentToCharacter"); } }
		public void ApplyOnlyEquipmentToCharacterLock(string a1, Character? character, uint lockCode) { try { _applyOnlyEquipmentToCharacterProviderLock.InvokeAction(a1, character, lockCode);     } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyOnlyEquipmentToCharacterLock"); } }
		public void ApplyAllToCharacter(string a1, Character? character)           { try { _applyAllToCharacterProvider.          InvokeAction(a1, character);     } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyAllToCharacter"); } }
		public void RevertCharacter(Character? character)                          { try { _revertCharacterProvider.              InvokeAction(character);         } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertCharacter"); } }
		public void RevertCharacterLock(Character? character, uint lockCode)                          { try { _revertCharacterProviderLock.              InvokeAction(character, lockCode);         } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertCharacterLock"); } }
		public bool RevertToAutomationCharacterLock(Character? character,uint lockCode){ try { return _revertToAutomationCharacterProvider.  InvokeFunc(character,lockCode);} catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertToAutomationCharacterLock");  return false; } }
		public bool RevertToAutomationCharacter(Character? character){ try { return _revertToAutomationCharacterProvider.  InvokeFunc(character,0);} catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertToAutomationCharacter");  return false; } }
		public string? GetAllCustomization(string a1)                              { try { return _getAllCustomizationProvider.   InvokeFunc(a1);                  } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyOnlyEquipmentToCharacter"); return null; } }
		


		public void Dispose() {
		}
	}
}
