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
using System.Runtime.InteropServices;
using CriticalCommonLib.GameStructs;

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

		internal unsafe bool ModifyGlamourPlateSlot(InventoryItem? item, GlamourPlateSlot glamPlateSlotIfEmpty) {
			PluginLog.Verbose($"start applying glam ({item?.ItemId ?? 0}) to plate");

			var agent = MiragePrismMiragePlateAgent;
			if (agent == null) {
				return false;
			}

			// Updated: 6.11 C98BC0
			var editorInfo = *(IntPtr*)((IntPtr)agent + Offsets.EditorInfo);
			if (editorInfo == IntPtr.Zero) {
				return false;
			}

			// Updated: 6.11 C984CF
			// current plate 6.11 C9AC9F
			var slotPtr = (GlamourPlateSlot*)(editorInfo + Offsets.EditorCurrentPlate);
			var initialSlot = *slotPtr;

			if(item == null || item.ItemId == 0) {
				ClearGlamourPlateSlot(glamPlateSlotIfEmpty);
				return true;
			}



			// get source
			InventoryItem? itemTmp = null;
			var matchingItemsInGlamourCest = DresserContents.FindAll(i => i.ItemId % HqItemOffset == i.ItemId);
			// If the item is found in GlamourChest
			if (matchingItemsInGlamourCest.Count != 0) {
				var index = matchingItemsInGlamourCest.FindIndex(i => i.StainId == item.Stain);

				itemTmp = item.Copy();
				if(itemTmp != null) {
					itemTmp.GlamourIndex = index == -1 ? 0 : index;
					itemTmp.Container = InventoryType.GlamourChest;
				}


			}
			// If not, search ins Armoire
			else if (this.ArmoireIndexIfPresent(item.ItemId) is { } armoireIndex) {

				itemTmp = item.Copy();
				if (itemTmp != null) {
					itemTmp.GlamourIndex = (int)armoireIndex;
					itemTmp.Container = InventoryType.Armoire;
				}

			}

			if (itemTmp == null) return false;

			MirageSource? source = itemTmp.Container switch {
				InventoryType.GlamourChest => source = MirageSource.GlamourDresser,
				InventoryType.Armoire => source = MirageSource.Armoire,
				_ => null,
			};

			if (source == null) return false;


			// Get slot from itemTmp
			var slot = itemTmp.Item.GlamourPlateSlot();
			if (slot == null) return false;

			// change slot to the item's
			*slotPtr = (GlamourPlateSlot)slot;

			SetGlamourPlateSlot((MirageSource)source, itemTmp.GlamourIndex, itemTmp.ItemId, itemTmp.Stain);

			// todo: sometimes, it fails to apply the glamour, maybe it's because of ClearGlamourPlateSlot?
			bool isApplied = false;
			int maxTries = 3;
			for( int i = 0; i < maxTries; i++) {
				isApplied = Gathering.IsApplied(item);
				if (isApplied) break;
				if (!isApplied) PluginLog.Warning($"Glamour place change did not take effect ({i+1} try)");
				Wait(500);
				SetGlamourPlateSlot((MirageSource)source, itemTmp.GlamourIndex, itemTmp.ItemId, itemTmp.Stain);
			}
			if (!isApplied) PluginLog.Error($"Error while applying item ({itemTmp.ItemId}) in glamour plate after {maxTries} tries");

			// restore initial slot, since changing this does not update the ui
			//*slotPtr = initialSlot;

			return isApplied;
		}


		internal unsafe IEnumerable<GlamourPlateSlot> LoadPlate(SavedPlate plate) {
			HashSet<GlamourPlateSlot> successSlots = new();
			var agent = MiragePrismMiragePlateAgent;
			if (agent == null) {
				return successSlots;
			}

			// Updated: 6.11 C98BC0
			var editorInfo = *(IntPtr*)((IntPtr)agent + 0x28);
			if (editorInfo == IntPtr.Zero) {
				return successSlots;
			}

			var dresser = DresserContents;
			var current = CurrentPlate;
			var usedStains = new Dictionary<(uint, uint), uint>();

			// Updated: 6.11 C984CF
			// current plate 6.11 C9AC9F
			var slotPtr = (GlamourPlateSlot*)(editorInfo + 0x18);
			var initialSlot = *slotPtr;
			var fakeEmptyItem = new SavedGlamourItem { ItemId = 0, StainId = 0 };
			foreach (var (slot, item) in plate.Items) {
				if (current != null && current.TryGetValue(slot, out var currentItem)) {
					if (currentItem.ItemId == item.ItemId && currentItem.StainId == item.StainId) {
						// ignore already-correct items
						continue;
					}
				}
				if(Gathering.VerifyItem(Storage.PlateNumber, slot, (InventoryItem)fakeEmptyItem)) {
					continue;
				}


				*slotPtr = slot;
				if (item.ItemId == 0) {
					this._clearGlamourPlateSlot((IntPtr)agent, slot);
					if(!Gathering.VerifyItem(Storage.PlateNumber, slot, (InventoryItem)item))
						successSlots.Add(slot);
					continue;
				}

				var source = MirageSource.GlamourDresser;
				var info = (0, 0u, (byte)0);
				// find an item in the dresser that matches
				var matchingIds = dresser.FindAll(mirage => mirage.ItemId % HqItemOffset == item.ItemId);
				if (matchingIds.Count == 0) {
					// if not in the glamour dresser, look in the armoire
					if (this.ArmoireIndexIfPresent(item.ItemId) is { } armoireIdx) {
						source = MirageSource.Armoire;
						info = ((int)armoireIdx, item.ItemId, 0);
					}
				} else {
					// try to find an item with a matching stain
					var idx = matchingIds.FindIndex(mirage => mirage.StainId == item.StainId);
					if (idx == -1) {
						idx = 0;
					}

					var mirage = matchingIds[idx];
					info = ((int)mirage.Index, mirage.ItemId, mirage.StainId);
				}

				if (info.Item1 == 0) {
					continue;
				}

				this._setGlamourPlateSlot(
					(IntPtr)agent,
					source,
					info.Item1,
					info.Item2,
					info.Item3
				);

				if (item.StainId != info.Item3) {
					// mirage in dresser did not have stain for this item, so apply it
					this.ApplyStain(agent, slot, item, usedStains);
				}
				if (!Gathering.VerifyItem(Storage.PlateNumber, slot, (InventoryItem)item))
					successSlots.Add(slot);

			}

			// restore initial slot, since changing this does not update the ui
			*slotPtr = initialSlot;

			return successSlots;
		}
		private static readonly FFXIVClientStructs.FFXIV.Client.Game.InventoryType[] PlayerInventories = {
			FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory1,
			FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory2,
			FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory3,
			FFXIVClientStructs.FFXIV.Client.Game.InventoryType.Inventory4,
		};

		private unsafe void ApplyStain(AgentInterface* editorAgent, GlamourPlateSlot slot, SavedGlamourItem item, Dictionary<(uint, uint), uint> usedStains) {
			// find the dye for this stain in the player's inventory
			var inventory = FFXIVClientStructs.FFXIV.Client.Game.InventoryManager.Instance();
			var transient = PluginServices.DataManager.GetExcelSheet<StainTransient>()!.GetRow(item.StainId);
			(int itemId, int qty, int inv, int slot) dyeInfo = (0, 0, -1, 0);
			var items = new[] { transient?.Item1?.Value, transient?.Item2?.Value };
			foreach (var dyeItem in items) {
				if (dyeItem == null || dyeItem.RowId == 0) {
					continue;
				}

				if (dyeInfo.itemId == 0) {
					// use the first one (free one) as placeholder
					dyeInfo.itemId = (int)dyeItem.RowId;
				}

				foreach (var type in PlayerInventories) {
					var inv = inventory->GetInventoryContainer(type);
					if (inv == null) {
						continue;
					}

					for (var i = 0; i < inv->Size; i++) {
						var address = ((uint)type, (uint)i);
						var invItem = inv->Items[i];
						if (invItem.ItemID != dyeItem.RowId) {
							continue;
						}

						if (usedStains.TryGetValue(address, out var numUsed) && numUsed >= invItem.Quantity) {
							continue;
						}

						// first one that we find in the inventory is the one we'll use
						dyeInfo = ((int)dyeItem.RowId, (int)inv->Items[i].Quantity, (int)type, i);
						if (usedStains.ContainsKey(address)) {
							usedStains[address] += 1;
						} else {
							usedStains[address] = 1;
						}

						goto NoBreakLabels;
					}
				}

			NoBreakLabels:
				{
				}
			}

			// do nothing if there is no dye item found
			if (dyeInfo.itemId == 0) {
				return;
			}

			var info = new ColorantInfo((uint)dyeInfo.inv, (ushort)dyeInfo.slot, (uint)dyeInfo.itemId, (uint)dyeInfo.qty);

			// allocate 24 bytes to store dye info if we have the dye
			var mem = dyeInfo.inv == -1
				? IntPtr.Zero
				: Marshal.AllocHGlobal(24);

			if (mem != IntPtr.Zero) {
				*(ColorantInfo*)mem = info;
			}

			this._modifyGlamourPlateSlot(
				(IntPtr)editorAgent,
				slot,
				item.StainId,
				mem,
				dyeInfo.Item1
			);

			if (mem != IntPtr.Zero) {
				Marshal.FreeHGlobal(mem);
			}
		}

		internal static unsafe Dictionary<GlamourPlateSlot, SavedGlamourItem>? CurrentPlate {
			get {
				var agent = MiragePrismMiragePlateAgent;
				if (agent == null) {
					return null;
				}

				var editorInfo = *(IntPtr*)((IntPtr)agent + 0x28);
				if (editorInfo == IntPtr.Zero) {
					return null;
				}

				var plate = new Dictionary<GlamourPlateSlot, SavedGlamourItem>();
				foreach (var slot in (GlamourPlateSlot[])Enum.GetValues(typeof(GlamourPlateSlot))) {
					// Updated: 6.1
					// from SetGlamourPlateSlot
					var item = editorInfo + 44 * (int)slot + 10596;

					var itemId = *(uint*)item;
					var stainId = *(byte*)(item + 24);
					var stainPreviewId = *(byte*)(item + 25);
					var actualStainId = stainPreviewId == 0 ? stainId : stainPreviewId;

					if (itemId == 0) {
						continue;
					}

					plate[slot] = new SavedGlamourItem {
						ItemId = itemId,
						StainId = actualStainId,
					};
				}

				return plate;
			}
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


	[StructLayout(LayoutKind.Sequential)]
	internal readonly struct ColorantInfo {
		private readonly uint InventoryId;
		private readonly ushort InventorySlot;
		private readonly byte Unk3;
		private readonly byte Unk4;
		private readonly uint StainItemId;
		private readonly uint StainItemCount;
		private readonly ulong Unk7;

		internal ColorantInfo(uint inventoryId, ushort inventorySlot, uint stainItemId, uint stainItemCount) {
			this.InventoryId = inventoryId;
			this.InventorySlot = inventorySlot;
			this.StainItemId = stainItemId;
			this.StainItemCount = stainItemCount;

			this.Unk3 = 0;
			this.Unk4 = 0;
			this.Unk7 = 0;
		}
	}
	public class SavedGlamourItem {
		public uint ItemId { get; set; }
		public byte StainId { get; set; }

		internal SavedGlamourItem Clone() {
			return new SavedGlamourItem() {
				ItemId = this.ItemId,
				StainId = this.StainId,
			};
		}
		public static explicit operator InventoryItem(SavedGlamourItem item)
			=> Extensions.InventoryItemExtensions.New(item.ItemId, item.StainId);
		
	}
	public class SavedPlate {
		public Dictionary<GlamourPlateSlot, SavedGlamourItem> Items { get; init; } = new();
		public List<string> Tags { get; } = new();

		internal SavedPlate Clone() {
			return new SavedPlate() {
				Items = this.Items.ToDictionary(entry => entry.Key, entry => entry.Value.Clone()),
			};
		}
		public bool IsDifferent(SavedPlate set2, out SavedPlate diffLeft, out SavedPlate diffRight) {
			var set1Items = this.Items;
			var set2Items = set2.Items;

			// diffLeft = items from set1 when there is a difference;
			diffLeft = new();
			diffRight = new();

			var slots = Enum.GetValues<GlamourPlateSlot>().ToList();

			foreach (var slot in slots) {
				set1Items.TryGetValue(slot, out var item1);
				set2Items.TryGetValue(slot, out var item2);

				if ((item1?.ItemId ?? 0) != (item2?.ItemId ?? 0) || (item1?.StainId ?? 0) != (item2?.StainId ?? 0)) {
					diffLeft.Items.Add(slot, item1?.Clone() ?? new() { ItemId = 0, StainId = 0});
					diffRight.Items.Add(slot, item2?.Clone() ?? new() { ItemId = 0, StainId = 0 });
				}

			}

			return diffLeft.Items.Any() || diffRight.Items.Any();
		}

	}
}
