using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Logic.Glamourer;
using Dresser.Structs.Dresser;

using Glamourer.Api.Enums;

using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

using System;

namespace Dresser.Services {
	internal class GlamourerService : IDisposable {

		private Glamourer.Api.IpcSubscribers.ApiVersion ApiVersionSubscriber;
		private Glamourer.Api.IpcSubscribers.SetItem SetItemSubscriber;
		private Glamourer.Api.IpcSubscribers.RevertState RevertStateSubscriber;
		private Glamourer.Api.IpcSubscribers.RevertToAutomation RevertToAutomationSubscriber;

		public GlamourerService(DalamudPluginInterface pluginInterface) {

			ApiVersionSubscriber = new global::Glamourer.Api.IpcSubscribers.ApiVersion(pluginInterface);
			SetItemSubscriber = new global::Glamourer.Api.IpcSubscribers.SetItem(pluginInterface);
			RevertStateSubscriber = new global::Glamourer.Api.IpcSubscribers.RevertState(pluginInterface);
			RevertToAutomationSubscriber = new global::Glamourer.Api.IpcSubscribers.RevertToAutomation(pluginInterface);

		}
		public bool IsInitialized() { try { return ApiVersions().Major >= 0; } catch (Exception) { return false; } }
		public (int Major, int Minor) ApiVersions() { try { return ApiVersionSubscriber.Invoke(); } catch (Exception) { return (-1, -1); } }

		public void RevertCharacter(Character? character) { if (character == null) return; try { RevertStateSubscriber.Invoke(character.ObjectIndex); } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertCharacter"); } }
		public bool RevertToAutomationCharacter(Character? character) { if (character == null) return false; try { return RevertToAutomationSubscriber.Invoke(character.ObjectIndex) == GlamourerApiEc.Success; } catch (Exception e) { PluginLog.Error(e, "Failed to contact RevertToAutomation"); return false; } }


		public GlamourerApiEc SetItem(Character character, EquipSlot slot, CustomItemId itemId, byte stainId, uint key) {
			try {
				var iid = itemId.Id;
				if (iid == 0) iid = (ulong)Design.NothingId(slot).Id;
				return (GlamourerApiEc)SetItemSubscriber.Invoke(character.ObjectIndex, (ApiEquipSlot)slot, iid, stainId);
			} catch (Exception e) {
				PluginLog.Error(e, "Failed to contact SetItem"); return (GlamourerApiEc)100;
			}
		}
		public bool SetItem(Character character, InventoryItem item, GlamourPlateSlot slot) {
			return SetItem(character, slot.ToPenumbraEquipSlot(), item.Item.ToCustomItemId(slot), item.Stain, 0) == GlamourerApiEc.Success;
		}

		public void Dispose() {
		}
	}
}
