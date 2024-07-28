using CriticalCommonLib;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility.Signatures;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Dresser;

using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Lumina.Excel.GeneratedSheets;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

using InventoryItem = Dresser.Structs.Dresser.InventoryItem;
using UsedStains = System.Collections.Generic.Dictionary<(uint, uint), uint>;


namespace Dresser.Interop.Hooks {
	internal class GlamourPlates : IDisposable {

		// https://git.anna.lgbt/ascclemens/Glamaholic/src/branch/main/Glamaholic/GameFunctions.cs

		private delegate void SetGlamourPlateSlotDelegate(IntPtr agent, MirageSource mirageSource, int glamId, uint itemId, byte stainId, byte stainId2);
		private delegate void ModifyGlamourPlateSlotDelegate(IntPtr agent, GlamourPlateSlot slot, byte stainId, IntPtr numbers, int stainItemId, int stainSlot);
		private delegate void ClearGlamourPlateSlotDelegate(IntPtr agent, GlamourPlateSlot slot);
		private delegate byte IsInArmoireDelegate(IntPtr armoire, int index);

		[Signature(Signatures.SetGlamourPlateSlot)] private readonly SetGlamourPlateSlotDelegate _setGlamourPlateSlot = null!;
		[Signature(Signatures.ModifyGlamourPlateSlot)] private readonly ModifyGlamourPlateSlotDelegate _modifyGlamourPlateSlot = null!;
		[Signature(Signatures.ClearGlamourPlateSlot)] private readonly ClearGlamourPlateSlotDelegate _clearGlamourPlateSlot = null!;
		[Signature(Signatures.IsInArmoire)] private readonly IsInArmoireDelegate _isInArmoire = null!;
		[Signature(Signatures.ArmoirePointer, ScanType = ScanType.StaticAddress)] private readonly IntPtr _armoirePtr;


		internal unsafe static AgentInterface* MiragePrismMiragePlateAgent => Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismMiragePlate);
		internal unsafe static AgentInterface* MiragePrismPrismBoxAgent => Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismPrismBox);
		internal unsafe static AgentInterface* CabinetAgent => Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.Cabinet);

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
		internal unsafe static bool IsAnyPlateSelectionOpen() {
			return MiragePrismMiragePlateAgent->IsAgentActive();
		}

		internal unsafe ushort? CurrentPlateIndex() {

			var agent = MiragePrismMiragePlateAgent;
			if (agent == null) {
				return null;
			}

			var editorInfo = *(IntPtr*)((IntPtr)agent + Offsets.HeadSize);
			if (editorInfo == IntPtr.Zero) {
				return null;
			}

			var currentPlate = *(ushort*)(editorInfo + Offsets.EditorCurrentPlate);
			if (currentPlate >= Offsets.TotalPlates && currentPlate < 0) return null;
			return currentPlate;

		}
		internal IEnumerable<GlamourPlateSlot> SetGlamourPlateSlot(InventoryItemSet set) {
			PluginLog.Error($"About to put info in glamour plate");

			UsedStains usedStain = new();
			HashSet<GlamourPlateSlot> successfullyApplied = new();
			foreach ( (var slot, var item) in set.Items) {
				InventoryItem item2;
				if (item == null || item.ItemId == 0) {
					item2 = InventoryItem.Zero;
				} else {
					item2 = item;
				}
				if (SetGlamourPlateSlot(item2, ref usedStain, slot)) {
					successfullyApplied.Add(slot);
					PluginLog.Debug($"apply into dresser ({slot}) SUCCESS");
				} else {
					PluginLog.Warning($"apply into dresser ({slot}) FAILLURE {item?.FormattedName}");
				}
				System.Threading.Tasks.Task.Run(async delegate {
					await System.Threading.Tasks.Task.Delay(50);
				});
			}
			return successfullyApplied;
		}

		private static readonly Stopwatch DresserTimer = new();
		private static List<GlamourPlateItem>? _dresserContents;

		internal static unsafe List<GlamourPlateItem> DresserContents {
			get {
				if (_dresserContents != null && DresserTimer.Elapsed < TimeSpan.FromSeconds(1)) {
					return _dresserContents;
				}

				var list = new List<GlamourPlateItem>();

				var agents = Framework.Instance()->GetUIModule()->GetAgentModule();
				var dresserAgent = agents->GetAgentByInternalId(AgentId.MiragePrismPrismBox);

				// these offsets in 6.3-HF1: AD2BEB
				var itemsStart = *(IntPtr*)((IntPtr)dresserAgent + Offsets.HeadSize);
				if (itemsStart == IntPtr.Zero) {
					return _dresserContents ?? list;
				}

				for (var i = 0; i < Offsets.TotalBoxSlot; i++) {
					var glamItem = *(GlamourPlateItem*)(itemsStart + i * Offsets.BoxSlotSize);
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


		internal unsafe void ModifyGlamourPlateSlot(GlamourPlateSlot slot, byte stainId, int stainSlot, ref UsedStains usedStains) {
			if(CurrentPlate?.TryGetValue(slot, out var glamItem) != null && glamItem != null) {
				glamItem.StainId = stainId;
				this.ApplyStain(MiragePrismMiragePlateAgent, slot, glamItem, usedStains, stainSlot);
			}
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
		internal unsafe bool SetGlamourPlateSlot(InventoryItem applyItem, ref UsedStains usedStains, GlamourPlateSlot? applyItemSlot = null) {
			var agent = MiragePrismMiragePlateAgent;
			if (agent == null) {
				return false;
			}

			var editorInfo = *(IntPtr*)((IntPtr)agent + Offsets.HeadSize);
			if (editorInfo == IntPtr.Zero) {
				PluginLog.Error($"Glamour dresser not opened");
				return false;
			}


			// use the provided slot in priority, for the ring, try get glamour plate from item if none provided
			applyItemSlot ??= applyItem.Item.GlamourPlateSlot();
			if (applyItemSlot == null || (uint)applyItemSlot >= CountSlots) {
				PluginLog.Error($"Invalid slot {applyItemSlot}");
				return false;
			}

			// clean up the slot if item is 0
			if (applyItem.ItemId == 0) {
				this._clearGlamourPlateSlot((IntPtr)agent, applyItemSlot.Value);
				PluginLog.Debug($"Is supposed to have emptied {applyItemSlot.Value}");
				return true;
			}

			// skip if already correct
			var currentPlate = CurrentPlate;
			if (currentPlate != null && currentPlate.TryGetValue(applyItemSlot.Value, out var currentItem)) {
				if (currentItem.ItemId == applyItem.ItemId && currentItem.StainId == applyItem.Stain && currentItem.StainId2 == applyItem.Stain2) {
					PluginLog.Verbose($"Item alread correct, ignoring");
					return true;
				}
			}



			// prepare the item to feed to the game
			var dresser = DresserContents;
			(MirageSource container, int index, uint id, byte stain1, byte stain2) info = (0, 0, 0, 0, 0);

			// check for items in glamour dresser, we want dyed ones in priority
			var matchingIds = dresser.FindAll(mirage => mirage.ItemId % HqItemOffset == applyItem.ItemId);
			if (matchingIds.Count != 0) {

				var idx = matchingIds.FindIndex(mirage => mirage.StainId == applyItem.Stain && mirage.StainId2 == applyItem.Stain2);
				if (idx == -1) idx = matchingIds.FindIndex(mirage => mirage.StainId == applyItem.Stain);
				if (idx == -1) idx = matchingIds.FindIndex(mirage => mirage.StainId2 == applyItem.Stain2);
				if (idx == -1) idx = 0;

				var mirage = matchingIds[idx];
				info = (MirageSource.GlamourDresser, (int)mirage.Index, mirage.ItemId, mirage.StainId, mirage.StainId2);
			}

			// if nothing found in glamour dresser, check for the armoire
			if (info.container == 0) {
				var armoireIndex = this.ArmoireIndexIfPresent(applyItem.ItemId);
				if (armoireIndex != null && armoireIndex != 0) {
					info = (MirageSource.Armoire, (int)armoireIndex, applyItem.ItemId, 0, 0);
				}
			}

			// nothing found after all
			if (info.container == 0 || info.id == 0) {
				PluginLog.Warning($"Item seems not owned");
				return false;
			}

			// get MiragePlate current slot pointer
			var slotPtr = (GlamourPlateSlot*)(editorInfo + Offsets.EditorCurrentSlot);

			// save MiragePlate previous slot
			var initialSlot = *slotPtr;

			// set MiragePlate current slot to desired one
			*slotPtr = (GlamourPlateSlot)applyItemSlot;

			// apply the item
			this._setGlamourPlateSlot((IntPtr)agent, info.container, info.index, info.id, info.stain1, info.stain2);

			// put the MiragePlate current slot to previous slot
			*slotPtr = initialSlot;


			if (applyItem.Stain != info.stain1)
				if (currentPlate?.TryGetValue(applyItemSlot.Value, out var glamItem) != null && glamItem != null) {
					glamItem.StainId = applyItem.Stain;
					this.ApplyStain(MiragePrismMiragePlateAgent, applyItemSlot.Value, glamItem, usedStains, 0);
				}
			if (applyItem.Stain2 != info.stain2)
				if (currentPlate?.TryGetValue(applyItemSlot.Value, out var glamItem) != null && glamItem != null) {
					glamItem.StainId = applyItem.Stain2;
					this.ApplyStain(MiragePrismMiragePlateAgent, applyItemSlot.Value, glamItem, usedStains, 1);
				}
			return true;
		}
		internal unsafe IEnumerable<GlamourPlateSlot> LoadPlate(SavedPlate plate) {
			HashSet<GlamourPlateSlot> successSlots = new();
			var agent = MiragePrismMiragePlateAgent;
			if (agent == null) {
				return successSlots;
			}

			// Updated: 6.11 C98BC0
			var editorInfo = *(IntPtr*)((IntPtr)agent + Offsets.HeadSize);
			if (editorInfo == IntPtr.Zero) {
				return successSlots;
			}

			var dresser = DresserContents;
			var current = CurrentPlate;
			var usedStains = new UsedStains();

			// Updated: 6.11 C984CF
			// current plate 6.11 C9AC9F
			var slotPtr = (GlamourPlateSlot*)(editorInfo + Offsets.EditorCurrentSlot);
			var initialSlot = *slotPtr;
			var fakeEmptyItem = new SavedGlamourItem { ItemId = 0, StainId = 0 , StainId2 = 0 };
			foreach (var (slot, item) in plate.Items) {
				if (current != null && current.TryGetValue(slot, out var currentItem)) {
					if (currentItem.ItemId == item.ItemId && currentItem.StainId == item.StainId && currentItem.StainId2 == item.StainId2) {
						// ignore already-correct items
						continue;
					}
				}
				if (Gathering.VerifyItem(Storage.PlateNumber, slot, (InventoryItem)fakeEmptyItem)) {
					continue;
				}


				*slotPtr = slot;
				if (item.ItemId == 0) {
					this._clearGlamourPlateSlot((IntPtr)agent, slot);
					if (!Gathering.VerifyItem(Storage.PlateNumber, slot, (InventoryItem)item))
						successSlots.Add(slot);
					continue;
				}

				var source = MirageSource.GlamourDresser;
				var info = (0, 0u, (byte)0, (byte)0);
				// find an item in the dresser that matches
				var matchingIds = dresser.FindAll(mirage => mirage.ItemId % HqItemOffset == item.ItemId);
				if (matchingIds.Count == 0) {
					// if not in the glamour dresser, look in the armoire
					if (this.ArmoireIndexIfPresent(item.ItemId) is { } armoireIdx) {
						source = MirageSource.Armoire;
						info = ((int)armoireIdx, item.ItemId, 0,0);
					}
				} else {
					// try to find an item with a matching stain
					var idx = matchingIds.FindIndex(mirage => mirage.StainId == item.StainId);
					if (idx == -1) {
						idx = 0;
					}

					var mirage = matchingIds[idx];
					info = ((int)mirage.Index, mirage.ItemId, mirage.StainId, mirage.StainId2);
				}

				if (info.Item1 == 0) {
					continue;
				}

				this._setGlamourPlateSlot(
					(IntPtr)agent,
					source,
					info.Item1,
					info.Item2,
					info.Item3,
					info.Item4
				);

				if (item.StainId != info.Item3) {
					// mirage in dresser did not have stain for this item, so apply it
					this.ApplyStain(agent, slot, item, usedStains, 0);
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

		private unsafe void ApplyStain(AgentInterface* editorAgent, GlamourPlateSlot slot, SavedGlamourItem item, UsedStains usedStains, int stainSlot) {
			// find the dye for this stain in the player's inventory
			var inventory = FFXIVClientStructs.FFXIV.Client.Game.InventoryManager.Instance();
			var stainId = stainSlot switch { 1 => item.StainId2, _ => item.StainId };
			var transient = PluginServices.DataManager.GetExcelSheet<StainTransient>()!.GetRow(stainId);

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
						if (invItem.ItemId != dyeItem.RowId) {
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
				dyeInfo.Item1,
				stainSlot
			);

			if (mem != IntPtr.Zero) {
				Marshal.FreeHGlobal(mem);
			}
		}

		public static int CountSlots => Enum.GetValues(typeof(GlamourPlateSlot)).Length;
		internal static unsafe Dictionary<GlamourPlateSlot, SavedGlamourItem>? CurrentPlate {
			get {
				var agent = MiragePrismMiragePlateAgent;
				if (agent == null) {
					return null;
				}

					var editorInfo = *(IntPtr*)((IntPtr)agent + Offsets.HeadSize);
				if (editorInfo == IntPtr.Zero) {
					return null;
				}

				var plate = new Dictionary<GlamourPlateSlot, SavedGlamourItem>();
				foreach (var slot in (GlamourPlateSlot[])Enum.GetValues(typeof(GlamourPlateSlot))) {
					// Updated: 6.1
					// from SetGlamourPlateSlot
					var item = editorInfo + Offsets.SlotSize * (int)slot + (Offsets.HeadPostOffset + (Offsets.SlotSize * CountSlots * Storage.PlateNumber));

					var itemId = *(uint*)item;
					var stainId = *(byte*)(item + Offsets.SlotOffsetStain1);
					var stainId2 = *(byte*)(item + Offsets.SlotOffsetStain2);
					var stainPreviewId = *(byte*)(item + Offsets.SlotOffsetStain1Preview);
					var stainPreviewId2 = *(byte*)(item + Offsets.SlotOffsetStain2Preview);
					var actualStainId = stainPreviewId == 0 ? stainId : stainPreviewId;
					var actualStainId2 = stainPreviewId2 == 0 ? stainId2 : stainPreviewId2;

					if (itemId == 0) {
						continue;
					}

					plate[slot] = new SavedGlamourItem {
						ItemId = itemId,
						StainId = actualStainId,
						StainId2 = actualStainId2,
					};
				}

				return plate;
			}
		}


		private static void Wait(int ms) {
			System.Threading.Tasks.Task.Run(async delegate { await System.Threading.Tasks.Task.Delay(ms); }).Wait();
		}


		internal GlamourPlates() {

			Service.GameInteropProvider.InitializeFromAttributes(this);

		}

		public void Dispose() {
		}

	}

	internal enum MirageSource {
		GlamourDresser = 1,
		Armoire = 2,
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
		public byte StainId2 { get; set; }

		internal SavedGlamourItem Clone() {
			return new SavedGlamourItem() {
				ItemId = this.ItemId,
				StainId = this.StainId,
				StainId2 = this.StainId2,
			};
		}
		public static explicit operator InventoryItem(SavedGlamourItem item)
			=> Extensions.InventoryItemExtensions.New(item.ItemId, item.StainId, item.StainId2);

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
					diffLeft.Items.Add(slot, item1?.Clone() ?? new() { ItemId = 0, StainId = 0 });
					diffRight.Items.Add(slot, item2?.Clone() ?? new() { ItemId = 0, StainId = 0 });
				}

			}

			return diffLeft.Items.Any() || diffRight.Items.Any();
		}

	}
}
