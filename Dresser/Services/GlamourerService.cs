using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Structs.Dresser;

using Glamourer.Designs;

using Newtonsoft.Json.Linq;

using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

using System;

namespace Dresser.Services {
	internal class GlamourerService : IDisposable {

		private readonly ICallGateSubscriber<int>                               _apiVersionSubscriber;
		private readonly ICallGateSubscriber<(int Major, int Minor)>            _apiVersionsSubscriber;

		private readonly ICallGateSubscriber<string, string?>                   _getAllCustomizationSubscriber;
		private readonly ICallGateSubscriber<Character?, string?>               _getAllCustomizationFromCharacterSubscriber;

		private readonly ICallGateSubscriber<string, string, object?>           _applyAllSubscriber;
		private readonly ICallGateSubscriber<string, Character?, object?>       _applyAllToCharacterSubscriber;
		private readonly ICallGateSubscriber<string, string, object?>           _applyOnlyEquipmentSubscriber;
		private readonly ICallGateSubscriber<string, Character?, object?>       _applyOnlyEquipmentToCharacterSubscriber;
		private readonly ICallGateSubscriber<string, string, object?>           _applyOnlyCustomizationSubscriber;
		private readonly ICallGateSubscriber<string, Character?, object?>       _applyOnlyCustomizationToCharacterSubscriber;

		private readonly ICallGateSubscriber<string, Character?, uint, object?> _applyOnlyEquipmentToCharacterSubscriberLock;
		private readonly ICallGateSubscriber<string, string, uint, object?>     _applyAllSubscriberLock;
		private readonly ICallGateSubscriber<string, Character?, uint, object?> _applyAllToCharacterSubscriberLock;
		private readonly ICallGateSubscriber<string, string, uint, object?>     _applyOnlyEquipmentSubscriberLock;
		private readonly ICallGateSubscriber<string, string, uint, object?>     _applyOnlyCustomizationSubscriberLock;
		private readonly ICallGateSubscriber<string, Character?, uint, object?> _applyOnlyCustomizationToCharacterSubscriberLock;


		private readonly ICallGateSubscriber<string, object?>                   _revertSubscriber;
		private readonly ICallGateSubscriber<Character?, object?>               _revertCharacterSubscriber;

		private readonly ICallGateSubscriber<string, uint, object?>             _revertSubscriberLock;
		private readonly ICallGateSubscriber<Character?, uint, object?>         _revertCharacterSubscriberLock;

		private readonly ICallGateSubscriber<string, uint, bool>                _revertToAutomationSubscriber;
		private readonly ICallGateSubscriber<Character?, uint, bool>            _revertToAutomationCharacterSubscriber;

		private readonly ICallGateSubscriber<Character?, uint, bool>            _unlockSubscriber;
		private readonly ICallGateSubscriber<string, uint, bool>                _unlockNameSubscriber;

		private readonly ICallGateSubscriber<Character?, byte, ulong, uint, int> _setItemSubscriber;
		private readonly ICallGateSubscriber<string, byte, ulong, uint, int>     _setItemByActorNameSubscriber;


		public GlamourerService(DalamudPluginInterface pluginInterface) {
			_apiVersionSubscriber  = pluginInterface.GetIpcSubscriber<int>                   ("Glamourer.ApiVersion");
			_apiVersionsSubscriber = pluginInterface.GetIpcSubscriber<(int Major, int Minor)>("Glamourer.ApiVersions");

			_getAllCustomizationSubscriber              = pluginInterface.GetIpcSubscriber<string, string?>    ("Glamourer.GetAllCustomization");
			_getAllCustomizationFromCharacterSubscriber = pluginInterface.GetIpcSubscriber<Character?, string?>("Glamourer.GetAllCustomizationFromCharacter");

			_applyAllSubscriber                          = pluginInterface.GetIpcSubscriber<string, string, object?>    ("Glamourer.ApplyAll");
			_applyAllToCharacterSubscriber               = pluginInterface.GetIpcSubscriber<string, Character?, object?>("Glamourer.ApplyAllToCharacter");
			_applyOnlyEquipmentSubscriber                = pluginInterface.GetIpcSubscriber<string, string, object?>    ("Glamourer.ApplyOnlyEquipment");
			_applyOnlyEquipmentToCharacterSubscriber     = pluginInterface.GetIpcSubscriber<string, Character?, object?>("Glamourer.ApplyOnlyEquipmentToCharacter");
			_applyOnlyCustomizationSubscriber            = pluginInterface.GetIpcSubscriber<string, string, object?>    ("Glamourer.ApplyOnlyCustomization");
			_applyOnlyCustomizationToCharacterSubscriber = pluginInterface.GetIpcSubscriber<string, Character?, object?>("Glamourer.ApplyOnlyCustomizationToCharacter");


			_applyAllSubscriberLock                          = pluginInterface.GetIpcSubscriber<string, string, uint, object?>    ("Glamourer.ApplyAllLock");
			_applyAllToCharacterSubscriberLock               = pluginInterface.GetIpcSubscriber<string, Character?, uint, object?>("Glamourer.ApplyAllToCharacterLock");
			_applyOnlyEquipmentSubscriberLock                = pluginInterface.GetIpcSubscriber<string, string, uint, object?>    ("Glamourer.ApplyOnlyEquipmentLock");
			_applyOnlyEquipmentToCharacterSubscriberLock     = pluginInterface.GetIpcSubscriber<string, Character?, uint, object?>("Glamourer.ApplyOnlyEquipmentToCharacterLock");
			_applyOnlyCustomizationSubscriberLock            = pluginInterface.GetIpcSubscriber<string, string, uint, object?>    ("Glamourer.ApplyOnlyCustomizationLock");
			_applyOnlyCustomizationToCharacterSubscriberLock = pluginInterface.GetIpcSubscriber<string, Character?, uint, object?>("Glamourer.ApplyOnlyCustomizationToCharacterLock");

			_revertSubscriber                      = pluginInterface.GetIpcSubscriber<string, object?>          ("Glamourer.Revert");
			_revertCharacterSubscriber             = pluginInterface.GetIpcSubscriber<Character?, object?>      ("Glamourer.RevertCharacter");
			_revertSubscriberLock                  = pluginInterface.GetIpcSubscriber<string, uint, object?>    ("Glamourer.RevertLock");
			_revertCharacterSubscriberLock         = pluginInterface.GetIpcSubscriber<Character?, uint, object?>("Glamourer.RevertCharacterLock");
			_revertToAutomationSubscriber          = pluginInterface.GetIpcSubscriber<string, uint, bool>       ("Glamourer.RevertToAutomation");
			_revertToAutomationCharacterSubscriber = pluginInterface.GetIpcSubscriber<Character?, uint, bool>   ("Glamourer.RevertToAutomationCharacter");
			_unlockSubscriber                      = pluginInterface.GetIpcSubscriber<Character?, uint, bool>   ("Glamourer.Unlock");
			_unlockNameSubscriber                  = pluginInterface.GetIpcSubscriber<string, uint, bool>       ("Glamourer.UnlockName");

			_setItemSubscriber            = pluginInterface.GetIpcSubscriber<Character?, byte, ulong, uint, int>("Glamourer.SetItem");
			_setItemByActorNameSubscriber = pluginInterface.GetIpcSubscriber<string, byte, ulong, uint, int>    ("Glamourer.SetItemByActorName");

		}
		public bool IsInitialized()                                                                   { try { return _apiVersionSubscriber.                                 InvokeFunc() >= 0;                         } catch (Exception) { return false; } }

		public int ApiVersion()                                                                       { try { return _apiVersionSubscriber.                                 InvokeFunc( );                             } catch (Exception e) { PluginLog.Error(e, "Failed to contact Glamourer"); return -1; } }
		public (int Major, int Minor) ApiVersions()                                                   { try { return _apiVersionsSubscriber.                                InvokeFunc();                              } catch (Exception) { return (-1, -1); } }

		public string? GetAllCustomization(string a1)                                                 { try { return _getAllCustomizationSubscriber.                InvokeFunc(a1);                            } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyOnlyEquipmentToCharacter"); return null; } }
		public string? GetAllCustomizationFromCharacter(Character? a1)                                { try { return _getAllCustomizationFromCharacterSubscriber.   InvokeFunc(a1);                            } catch (Exception e) { PluginLog.Error(e, "Failed to contact GetAllCustomizationFromCharacter"); return null; } }
		public JObject? GetAllCustomizationDesign(string a1)                                          { var b64 = GetAllCustomization(a1);              var json = Design.FromBase64(b64??""); return json;}
		public JObject? GetAllCustomizationDesignFromCharacter(Character? a1)                         { var b64 = GetAllCustomizationFromCharacter(a1); var json = Design.FromBase64(b64??""); return json;}

		public void ApplyAllToCharacter(string a1, Character? character)                              { try { _applyAllToCharacterSubscriber.                       InvokeAction(a1, character);               } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyAllToCharacter"); } }
		public void ApplyOnlyEquipment(string a1, string characterName)                               { try { _applyOnlyEquipmentSubscriber.                        InvokeAction(a1, characterName);           } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyOnlyEquipment"); } }
		public void ApplyOnlyEquipmentToCharacter(string a1, Character? character)                    { try { _applyOnlyEquipmentToCharacterSubscriber.             InvokeAction(a1, character);               } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyOnlyEquipmentToCharacter"); } }

		public void ApplyOnlyEquipmentToCharacterLock(string a1, Character? character, uint lockCode) { try { _applyOnlyEquipmentToCharacterSubscriberLock.         InvokeAction(a1, character, lockCode);     } catch (Exception e) { PluginLog.Error(e, "Failed to contact ApplyOnlyEquipmentToCharacterLock"); } }


		public void RevertCharacter(Character? character)                                             { try { _revertCharacterSubscriber.                           InvokeAction(character);                   } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertCharacter"); } }
		public void RevertCharacterLock(Character? character, uint lockCode)                          { try { _revertCharacterSubscriberLock.                       InvokeAction(character, lockCode);         } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertCharacterLock"); } }
		public bool RevertToAutomationCharacter(Character? character)                                 { try { return _revertToAutomationCharacterSubscriber.        InvokeFunc(character,0);                   } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertToAutomationCharacter");  return false; } }
		public bool RevertToAutomationCharacterLock(Character? character,uint lockCode)               { try { return _revertToAutomationCharacterSubscriber.        InvokeFunc(character,lockCode);            } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertToAutomationCharacterLock");  return false; } }


		public bool Unlock(Character? character,uint lockCode)                                        { try { return _unlockSubscriber.                             InvokeFunc(character,lockCode);            } catch (Exception e) { PluginLog.Error(e, "Failed to contact Unlock");  return false; } }


		public GlamourerErrorCode SetItem(Character? character, EquipSlot slot, CustomItemId itemId, uint key) {
			try {
				return (GlamourerErrorCode)_setItemSubscriber.InvokeFunc(character, (byte)slot, itemId.Id, key);
			} catch (Exception e) {
				PluginLog.Error(e, "Failed to contact SetItem"); return (GlamourerErrorCode)100;
			}
		}
		public bool SetItem(Character? character, InventoryItem item, GlamourPlateSlot slot) {
			return SetItem(character, slot.ToPenumbraEquipSlot(), item.Item.ToCustomItemId(slot), 0) == GlamourerErrorCode.Success;
		}

		public enum GlamourerErrorCode {
			Success,
			ActorNotFound,
			ActorNotHuman,
			ItemInvalid,
		}

		public void Dispose() {
		}
	}
}
