using System;
using System.Threading.Tasks;

using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;

using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Dresser.Data;
using Dresser.Extensions;
using Dresser.Structs.FFXIV;

namespace Dresser.Interop.Hooks {
	internal class GlamourPlates : IDisposable {

		private delegate void SetGlamourPlateSlotDelegate(IntPtr agent, MirageSource mirageSource, int glamId, uint itemId, byte stainId);
		private delegate void ModifyGlamourPlateSlotDelegate(IntPtr agent, GlamourPlateSlot slot, byte stainId, IntPtr numbers, int stainItemId);
		private delegate void ClearGlamourPlateSlotDelegate(IntPtr agent, GlamourPlateSlot slot);

		[Signature(Signatures.SetGlamourPlateSlot)] private readonly SetGlamourPlateSlotDelegate _setGlamourPlateSlot = null!;
		[Signature(Signatures.ModifyGlamourPlateSlot)] private readonly ModifyGlamourPlateSlotDelegate _modifyGlamourPlateSlot = null!;
		[Signature(Signatures.ClearGlamourPlateSlot)] private readonly ClearGlamourPlateSlotDelegate _clearGlamourPlateSlot = null!;

		internal unsafe static AgentInterface* MiragePrismMiragePlateAgent => Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismMiragePlate);
		internal unsafe static AgentInterface* MiragePrismPrismBoxAgent => Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismPrismBox);
		internal unsafe static AgentInterface* CabinetAgent => Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.Cabinet);

		internal unsafe static bool IsGlaming() {


			var isLocked = Service.Condition[ConditionFlag.OccupiedInQuestEvent];
			
			bool isActiveMiragePrismMiragePlate = false;
			bool isActiveMiragePrismPrismBox = false;
			bool isActiveCabinet = false;

			if (MiragePrismMiragePlateAgent != null)
				isActiveMiragePrismMiragePlate = MiragePrismMiragePlateAgent->IsAgentActive();
			if (MiragePrismPrismBoxAgent != null)
				isActiveMiragePrismPrismBox = MiragePrismPrismBoxAgent->IsAgentActive();
			if (CabinetAgent != null)
				isActiveCabinet = CabinetAgent->IsAgentActive();

			return isLocked && isActiveMiragePrismMiragePlate && (isActiveMiragePrismPrismBox || isActiveCabinet);
		}

		internal unsafe void SetGlamourPlateSlot(MirageSource source, int glamId, uint itemId, byte stainId) {
			this._setGlamourPlateSlot(
				(IntPtr)MiragePrismMiragePlateAgent,
				source,
				glamId,
				itemId,
				stainId);
		}
		internal void ModifyGlamourPlateSlot(InventoryItem item, Action<InventoryItem>? doAfter = null) {
			ModifyGlamourPlateSlot(item);
			if (!Gathering.IsApplied(item))
				Task.Run(async delegate {
					await Task.Delay(100);
					ModifyGlamourPlateSlot(item);
					if (!Gathering.IsApplied(item))
						PluginLog.Warning($"Unable to apply item after a retry {item.ItemId}");
					else
						doAfter?.Invoke(item);
				});
		}
		internal unsafe void ModifyGlamourPlateSlot(GlamourPlateSlot slot, byte stainId, IntPtr numbers, int stainItemId) {
			this._modifyGlamourPlateSlot((IntPtr)MiragePrismMiragePlateAgent, slot, stainId, numbers, stainItemId);
		}
		internal unsafe void ClearGlamourPlateSlot(GlamourPlateSlot slot) {
			this._clearGlamourPlateSlot((IntPtr)MiragePrismMiragePlateAgent, slot);
		}
		internal unsafe void ModifyGlamourPlateSlot(InventoryItem item) {
			var agent = MiragePrismMiragePlateAgent;
			if (agent == null) {
				return;
			}

			// Updated: 6.11 C98BC0
			var editorInfo = *(IntPtr*)((IntPtr)agent + Offsets.EditorInfo);
			if (editorInfo == IntPtr.Zero) {
				return;
			}

			// Updated: 6.11 C984CF
			// current plate 6.11 C9AC9F
			var slotPtr = (GlamourPlateSlot*)(editorInfo + Offsets.EditorCurrentPlate);
			var initialSlot = *slotPtr;


			// get source
			MirageSource? source = item.Container switch {
				InventoryType.GlamourChest => source = MirageSource.GlamourDresser,
				InventoryType.Armoire => source = MirageSource.Armoire,
				_ => null,
			};

			if (source == null) return;
			

			// Get slot from item
			var slot = item.Item.GlamourPlateSlot();
			if (slot == null) return;

			// change slot to the item's
			*slotPtr = (GlamourPlateSlot)slot;


			SetGlamourPlateSlot((MirageSource)source, item.GlamourIndex, item.ItemId, item.Stain);

			// restore initial slot, since changing this does not update the ui
			*slotPtr = initialSlot;
		}

		internal GlamourPlates() {

			SignatureHelper.Initialise(this);

		}

		public void Dispose() {
		}

	}
}
