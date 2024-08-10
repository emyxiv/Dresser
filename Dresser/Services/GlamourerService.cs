using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Logic.Glamourer;
using Dresser.Structs.Dresser;

using Glamourer.Api.Enums;
using Newtonsoft.Json.Linq;

using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

using System;
using System.Collections.Generic;

namespace Dresser.Services {
	internal class GlamourerService : IDisposable {

		private Glamourer.Api.IpcSubscribers.ApiVersion ApiVersionSubscriber;
		private Glamourer.Api.IpcSubscribers.SetItem SetItemSubscriber;
		private Glamourer.Api.IpcSubscribers.RevertState RevertStateSubscriber;
		private Glamourer.Api.IpcSubscribers.RevertToAutomation RevertToAutomationSubscriber;

		private Glamourer.Api.IpcSubscribers.GetState GetStateSubscriber;
		private Glamourer.Api.IpcSubscribers.ApplyState ApplyStateSubscriber;

		public GlamourerService(IDalamudPluginInterface pluginInterface) {

			ApiVersionSubscriber = new global::Glamourer.Api.IpcSubscribers.ApiVersion(pluginInterface);
			SetItemSubscriber = new global::Glamourer.Api.IpcSubscribers.SetItem(pluginInterface);
			RevertStateSubscriber = new global::Glamourer.Api.IpcSubscribers.RevertState(pluginInterface);
			RevertToAutomationSubscriber = new global::Glamourer.Api.IpcSubscribers.RevertToAutomation(pluginInterface);
			GetStateSubscriber = new global::Glamourer.Api.IpcSubscribers.GetState(pluginInterface);
			ApplyStateSubscriber = new global::Glamourer.Api.IpcSubscribers.ApplyState(pluginInterface);

		}
		public bool IsInitialized() { try { return ApiVersions().Major >= 0; } catch (Exception) { return false; } }
		public (int Major, int Minor) ApiVersions() { try { return ApiVersionSubscriber.Invoke(); } catch (Exception) { return (-1, -1); } }

		public void RevertCharacter(ICharacter? character) { if (character == null) return; try { RevertStateSubscriber.Invoke(character.ObjectIndex); } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertCharacter"); } }
		public bool RevertToAutomationCharacter(ICharacter? character) { if (character == null) return false; try { return RevertToAutomationSubscriber.Invoke(character.ObjectIndex) == GlamourerApiEc.Success; } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertToAutomation"); return false; } }


		public GlamourerApiEc SetItem(ICharacter character, EquipSlot slot, CustomItemId itemId, byte stainId, byte stainId2) {
			try {
				var iid = itemId.Id;
				if (iid == 0) iid = (ulong)Design.NothingId(slot).Id;
				return (GlamourerApiEc)SetItemSubscriber.Invoke(character.ObjectIndex, (ApiEquipSlot)slot, iid, new List<byte>() { stainId, stainId2 }); ;
			} catch (Exception e) {
				PluginLog.Error(e, "Failed to contact SetItem"); return (GlamourerApiEc)100;
			}
		}
		public bool SetItem(ICharacter character, InventoryItem item, GlamourPlateSlot slot) {
			return SetItem(character, slot.ToPenumbraEquipSlot(), item.Item.ToCustomItemId(slot), item.Stain, item.Stain2) == GlamourerApiEc.Success;
		}
		public Newtonsoft.Json.Linq.JObject? GetState() {
			var index = PluginServices.Context.LocalPlayer?.ObjectIndex;
			if(index == null) return null;

			(GlamourerApiEc response, Newtonsoft.Json.Linq.JObject? charaState) result = GetStateSubscriber.Invoke((int)index);
			if(result.response != GlamourerApiEc.Success) return null;

			return result.charaState;
		}

		public enum MetaData {
			Hat,
			Visor,
			Weapon,
		}
		public bool SetMetaData(ICharacter character, MetaData metaData, bool? forceState = null) {
			return SetMetaData(character, new() { { metaData, forceState } });
		}
		public bool SetMetaData(ICharacter character, Dictionary<MetaData, bool?> metaDataStates) {
			var index = character.ObjectIndex;

			var originalState = GetState();
			if (originalState == null) return false;

			RecursivelyModifyParam(originalState!, "Apply", false);

			foreach ((var metaData, var forceState) in metaDataStates) {
				var showFieldName = metaData == MetaData.Visor ? "IsToggled" : "Show";
				var newValue = forceState ?? !(((bool?)originalState?["Equipment"]?[metaData.ToString()]?[showFieldName]) ?? false);

				originalState!["Equipment"]![metaData.ToString()]![showFieldName] = newValue;
				originalState!["Equipment"]![metaData.ToString()]!["Apply"] = true;
			}

			var ret = ApplyStateSubscriber.Invoke(originalState, index);

			return ret == GlamourerApiEc.Success;
		}


		public void RecursivelyModifyParam(JToken token, string key, bool newValue) {
			if (token.HasValues) {
				foreach (JToken child in token.Children()) {
					RecursivelyModifyParam(child, key, newValue);
				}
			} else if (token.Parent != null && token.ToString() == key) {
				token.Parent[key] = newValue;
			}
		}

		public void Dispose() {
		}
	}
}
