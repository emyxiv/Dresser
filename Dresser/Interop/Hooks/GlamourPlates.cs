using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;

using Lumina.Excel.GeneratedSheets;

using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Dresser.Data;
using Dresser.Extensions;
using Dresser.Structs;
using Dresser.Structs.FFXIV;

namespace Dresser.Interop.Hooks {
	internal class GlamourPlates : IDisposable {

		// https://git.anna.lgbt/ascclemens/Glamaholic/src/branch/main/Glamaholic/GameFunctions.cs

		private delegate void SetGlamourPlateSlotDelegate(IntPtr agent, MirageSource mirageSource, int glamId, uint itemId, byte stainId);
		private delegate void ModifyGlamourPlateSlotDelegate(IntPtr agent, GlamourPlateSlot slot, byte stainId, IntPtr numbers, int stainItemId);
		private delegate void ClearGlamourPlateSlotDelegate(IntPtr agent, GlamourPlateSlot slot);
		private delegate byte IsInArmoireDelegate(IntPtr armoire, int index);

		[Signature(Signatures.SetGlamourPlateSlot)] private readonly SetGlamourPlateSlotDelegate _setGlamourPlateSlot = null!;
		[Signature(Signatures.ModifyGlamourPlateSlot)] private readonly ModifyGlamourPlateSlotDelegate _modifyGlamourPlateSlot = null!;
		[Signature(Signatures.ClearGlamourPlateSlot)] private readonly ClearGlamourPlateSlotDelegate _clearGlamourPlateSlot = null!;
		[Signature(Signatures.IsInArmoire)] private readonly IsInArmoireDelegate _isInArmoire = null!;
		[Signature(Signatures.ArmoirePointer, ScanType = ScanType.StaticAddress)] private readonly IntPtr _armoirePtr;


		internal unsafe static AgentInterface* MiragePrismMiragePlateAgent => Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismMiragePlate);
		internal unsafe static AgentInterface* MiragePrismPrismBoxAgent => Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismPrismBox);
		internal unsafe static AgentInterface* CabinetAgent => Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.Cabinet);

		internal unsafe static bool IsGlamingAtDresser() {


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

		internal unsafe void SetGlamourPlateSlot(MirageSource container, int containerIndex, uint itemId, byte stainId) {
			this._setGlamourPlateSlot(
				(IntPtr)MiragePrismMiragePlateAgent,
				container,
				containerIndex,
				itemId,
				stainId);
		}

		private static readonly Stopwatch DresserTimer = new();
		private static List<GlamourPlateItem>? _dresserContents;

		internal static unsafe List<GlamourPlateItem> DresserContents {
			get {
				if (_dresserContents != null && DresserTimer.Elapsed < TimeSpan.FromSeconds(1)) {
					return _dresserContents;
				}

				var list = new List<GlamourPlateItem>();

				var agents = Framework.Instance()->GetUiModule()->GetAgentModule();
				var dresserAgent = agents->GetAgentByInternalId(AgentId.MiragePrismPrismBox);

				// these offsets in 6.3-HF1: AD2BEB
				var itemsStart = *(IntPtr*)((IntPtr)dresserAgent + 0x28);
				if (itemsStart == IntPtr.Zero) {
					return _dresserContents ?? list;
				}

				for (var i = 0; i < 800; i++) {
					var glamItem = *(GlamourPlateItem*)(itemsStart + i * 136);
					if (glamItem.ItemId == 0) {
						continue;
					}

					list.Add(glamItem);
				}

				_dresserContents = list;
				DresserTimer.Restart();

				return list;
			}
		}


		internal unsafe void ModifyGlamourPlateSlot(GlamourPlateSlot slot, byte stainId, IntPtr numbers, int stainItemId) {
			this._modifyGlamourPlateSlot((IntPtr)MiragePrismMiragePlateAgent, slot, stainId, numbers, stainItemId);
		}
		internal unsafe void ClearGlamourPlateSlot(GlamourPlateSlot slot) {
			this._clearGlamourPlateSlot((IntPtr)MiragePrismMiragePlateAgent, slot);
		}
		internal bool IsInArmoire(uint itemId) {
			var row = PluginServices.DataManager.GetExcelSheet<Cabinet>()!.FirstOrDefault(row => row.Item.Row == itemId);
			if (row == null) {
				return false;
			}

			return this._isInArmoire(this._armoirePtr, (int)row.RowId) != 0;
		}

		internal uint? ArmoireIndexIfPresent(uint itemId) {
			var row = PluginServices.DataManager.GetExcelSheet<Cabinet>()!.FirstOrDefault(row => row.Item.Row == itemId);
			if (row == null) {
				return null;
			}

			var isInArmoire = this._isInArmoire(this._armoirePtr, (int)row.RowId) != 0;
			return isInArmoire
				? row.RowId
				: null;
		}



		internal const uint HqItemOffset = 1_000_000;

		internal unsafe void ModifyGlamourPlateSlot(InventoryItem? item, GlamourPlateSlot glamPlateSlotIfEmpty) {
			PluginLog.Verbose($"start applying glam ({item?.ItemId ?? 0}) to plate");

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

			if(item == null || item.ItemId == 0) {
				ClearGlamourPlateSlot(glamPlateSlotIfEmpty);
				return;
			}



			// get source
			InventoryItem? itemTmp = null;
			var matchingItemsInGlamourCest = DresserContents.FindAll(i => i.ItemId % HqItemOffset == i.ItemId);
			if (matchingItemsInGlamourCest.Count != 0) {
				var index = matchingItemsInGlamourCest.FindIndex(i => i.StainId == item.Stain);

				itemTmp = item.Copy();
				if(itemTmp != null) {
					itemTmp.GlamourIndex = index == -1 ? 0 : index;
					itemTmp.Container = InventoryType.GlamourChest;
				}


			} else if (this.ArmoireIndexIfPresent(item.ItemId) is { } armoireIndex) {

				itemTmp = item.Copy();
				if (itemTmp != null) {
					itemTmp.GlamourIndex = (int)armoireIndex;
					itemTmp.Container = InventoryType.Armoire;
				}

			}

			if (itemTmp == null) return;

			MirageSource? source = itemTmp.Container switch {
				InventoryType.GlamourChest => source = MirageSource.GlamourDresser,
				InventoryType.Armoire => source = MirageSource.Armoire,
				_ => null,
			};

			if (source == null) return;


			// Get slot from itemTmp
			var slot = itemTmp.Item.GlamourPlateSlot();
			if (slot == null) return;

			// change slot to the item's
			*slotPtr = (GlamourPlateSlot)slot;

			SetGlamourPlateSlot((MirageSource)source, itemTmp.GlamourIndex, itemTmp.ItemId, itemTmp.Stain);

			// todo: sometimes, it fails to apply the glamour, maybe it's because of ClearGlamourPlateSlot?
			bool isApplied = false;
			int maxTries = 10;
			for( int i = 0; i < maxTries; i++) {
				isApplied = Gathering.IsApplied(item);
				if (isApplied) break;
				if (!isApplied) PluginLog.Warning($"Glamour place change did not take effect ({i+1} try)");
				Wait(500);
				SetGlamourPlateSlot((MirageSource)source, itemTmp.GlamourIndex, itemTmp.ItemId, itemTmp.Stain);
			}
			if (!isApplied) PluginLog.Error($"Error while applying item in glamour plate after {maxTries} tries");

			// restore initial slot, since changing this does not update the ui
			* slotPtr = initialSlot;
		}


		private static void Wait(int ms) {
			Task.Run(async delegate { await Task.Delay(ms); }).Wait();
		}


		internal GlamourPlates() {

			SignatureHelper.Initialise(this);

		}

		public void Dispose() {
		}

	}
}
