using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Structs.Dresser;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Lumina.Excel.Sheets;
using Lumina.Extensions;

using Cabinet = Lumina.Excel.Sheets.Cabinet;
using InventoryItem = FFXIVClientStructs.FFXIV.Client.Game.InventoryItem;
using InventoryItemDr = Dresser.Structs.Dresser.InventoryItem;
using UsedStains = System.Collections.Generic.Dictionary<(uint, uint), uint>;


namespace Dresser.Interop.Hooks {
	internal class GlamourPlates : IDisposable {

		// https://github.com/caitlyn-gg/Glamaholic/blob/main/Glamaholic/GameFunctions.cs

		private unsafe delegate int GetCabinetItemIdDelegate(FFXIVClientStructs.FFXIV.Client.Game.UI.Cabinet* armoire, uint baseItemId);

		[Signature(Signatures.GetCabinetItemId)] private readonly GetCabinetItemIdDelegate GetCabinetItemId = null!;


		internal unsafe static AgentMiragePrismMiragePlate* MiragePlateAgent => AgentMiragePrismMiragePlate.Instance();
		internal unsafe FFXIVClientStructs.FFXIV.Client.Game.UI.Cabinet* Armoire => &UIState.Instance()->Cabinet;
		internal unsafe bool ArmoireLoaded => this.Armoire->IsCabinetLoaded();
		internal unsafe static AgentMiragePrismPrismBox* PrismBoxAgent => AgentMiragePrismPrismBox.Instance();
		internal unsafe static AgentCabinet* CabinetAgent => AgentCabinet.Instance();
		private Throttler<Task<SetGlamourPlateSlotReturn>> _glamourDresserApplyThrottler;


		internal unsafe static bool IsGlamingAtDresser() {


			var isLocked = PluginServices.Condition[ConditionFlag.OccupiedInQuestEvent];

			bool isActiveMiragePrismMiragePlate = false;
			bool isActiveMiragePrismPrismBox = false;
			bool isActiveCabinet = false;

			if (MiragePlateAgent != null)
				isActiveMiragePrismMiragePlate = MiragePlateAgent->IsAgentActive();
			if (PrismBoxAgent != null)
				isActiveMiragePrismPrismBox = PrismBoxAgent->IsAgentActive();
			if (CabinetAgent != null)
				isActiveCabinet = CabinetAgent->IsAgentActive();

			return isLocked && isActiveMiragePrismMiragePlate && (isActiveMiragePrismPrismBox || isActiveCabinet);
		}
		internal unsafe static bool IsAnyPlateSelectionOpen() {
			return MiragePlateAgent->IsAgentActive();
		}

		internal unsafe ushort? CurrentPlateIndex() {

			var agent = MiragePlateAgent;
			if (agent == null) {
				return null;
			}

			var data = *(FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData**)((nint)agent + Offsets.HeadSize);
			if (data == null) {
				return null;
			}

			var currentPlate = data->SelectedMiragePlateIndex;
			if (currentPlate >= Offsets.TotalPlates && currentPlate < 0) return null;
			return (ushort)currentPlate;

		}
		internal unsafe IEnumerable<GlamourPlateSlot> SetGlamourPlateSlot(InventoryItemSet set) {



			// PluginLog.Error($"About to put info in glamour plate");

			UsedStains usedStain = new();
			HashSet<GlamourPlateSlot> successfullyApplied = new();


			var agent = MiragePlateAgent;
			if (agent == null) {
				return successfullyApplied;
			}

			var data = *(FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData**)((nint)agent + Offsets.HeadSize);
			if (data == null) {
				PluginLog.Error($"Glamour dresser not opened");
				return successfullyApplied;
			}
			var initialSlot = data->SelectedItemIndex;

			int changedCount = 0;
			foreach ( (var slot, var item) in set.Items) {
				InventoryItemDr item2;
				if (item == null || item.ItemId == 0) {
					item2 = InventoryItemDr.Zero;
				} else {
					item2 = item;
				}
				var zzz = SetGlamourPlateSlot(item2, ref usedStain, slot);
				if (zzz == SetGlamourPlateSlotReturn.success) changedCount++;
				if (zzz == SetGlamourPlateSlotReturn.success || zzz == SetGlamourPlateSlotReturn.same) {
					successfullyApplied.Add(slot);
					PluginLog.Debug($"apply into dresser ({slot}) SUCCESS{(zzz == SetGlamourPlateSlotReturn.same? " (Same)": "")}");
				} else {
					PluginLog.Warning($"apply into dresser ({slot}) FAILURE {item?.FormattedName}");
				}
				//System.Threading.Tasks.Task.Run(async delegate {
				//	await System.Threading.Tasks.Task.Delay(50);
				//});
			}

			data->SelectedItemIndex = initialSlot;
			if(changedCount > 0) {
				data->HasChanges = true;
			}
			return successfullyApplied;
		}

		private static readonly Stopwatch DresserTimer = new();

		internal static unsafe List<PrismBoxCachedItem> DresserContents {
			get => _cachedDresserItems;
		}



		internal unsafe void ClearGlamourPlateSlot(GlamourPlateSlot slot) {
			//this._clearGlamourPlateSlot((IntPtr)MiragePrismMiragePlateAgent, slot);
		}
		internal unsafe bool IsInArmoire(uint itemId) {
			var row = PluginServices.DataManager.GetExcelSheet<Cabinet>().FirstOrNull(row => row.Item.RowId == itemId);
			if (row == null) {
				return false;
			}

			return this.Armoire->IsItemInCabinet((int)row.Value.RowId);
		}

		internal unsafe uint? ArmoireIndexIfPresent(uint itemId) {
			var row = PluginServices.DataManager.GetExcelSheet<Cabinet>().FirstOrNull(row => row.Item.RowId == itemId);
			if (row == null) {
				return null;
			}

			var isInArmoire = this.Armoire->IsItemInCabinet((int)row.Value.RowId);
			return isInArmoire
				? row.Value.RowId
				: null;
		}



		internal enum SetGlamourPlateSlotReturn {
			failed,
			success,
			same
		}
		internal SetGlamourPlateSlotReturn SetGlamourPlateSlot(InventoryItemDr applyItem, ref UsedStains usedStains, GlamourPlateSlot? applyItemSlot = null)
		{
			var vvv = usedStains;
			Task<SetGlamourPlateSlotReturn> task = _glamourDresserApplyThrottler.Throttle(() =>
			{
				task = PluginServices.Framework.RunOnFrameworkThread(() => SetGlamourPlateSlotFramework(applyItem, ref vvv, applyItemSlot));
				task.Wait();

				return task;

			});
			usedStains = vvv;
			return task.Result;
		}
		private unsafe SetGlamourPlateSlotReturn SetGlamourPlateSlotFramework(InventoryItemDr applyItem, ref UsedStains usedStains, GlamourPlateSlot? applyItemSlot = null) {
			var agent = MiragePlateAgent;
			if (agent == null) {
				return SetGlamourPlateSlotReturn.failed;
			}

			var data = *(FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData**)((nint)agent + Offsets.HeadSize);
			if (data == null) {
				PluginLog.Error($"Glamour dresser not opened");
				return SetGlamourPlateSlotReturn.failed;
			}


			// use the provided slot in priority, for the ring, try get glamour plate from item if none provided
			applyItemSlot ??= applyItem.Item.GlamourPlateSlot();
			if (applyItemSlot == null || (uint)applyItemSlot >= CountSlots) {
				PluginLog.Error($"Invalid slot {applyItemSlot}");
				return SetGlamourPlateSlotReturn.failed;
			}

			// skip if already correct
			var currentPlate = CurrentPlate;
			if (currentPlate != null && currentPlate.TryGetValue(applyItemSlot.Value, out var currentItem)) {
				if (currentItem.ItemId == applyItem.ItemId
					&& currentItem.Stain1 == applyItem.Stain
					&& currentItem.Stain2 == applyItem.Stain2) {
					// PluginLog.Verbose($"Item alread correct, ignoring");
					//data->SelectedItemIndex = initialSlot;
					return SetGlamourPlateSlotReturn.same;
				}
			}

			// save MiragePlate previous slot
			//var initialSlot = data->SelectedItemIndex;

			// set MiragePlate current slot to desired one
			data->SelectedItemIndex = (uint)applyItemSlot;

			// clean up the slot if item is 0
			if (applyItem.ItemId == 0) {
				uint previousContextSlot = data->ContextMenuItemIndex;
				data->ContextMenuItemIndex = (uint)applyItemSlot;

				AtkValue rv;
				agent->ReceiveEvent(&rv, null, 0, 1); // "Remove Item Image from Plate"

				data->ContextMenuItemIndex = previousContextSlot;
				//data->SelectedItemIndex = initialSlot;
				//data->HasChanges = true;

				//PluginLog.Debug($"Is supposed to have emptied {applyItemSlot.Value}");
				return SetGlamourPlateSlotReturn.success;
			}

			// prepare the item to feed to the game
			var dresser = DresserContents;
			(FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData.ItemSource? container, int index, uint id, byte stain1, byte stain2) info = (null, 0, 0, 0, 0);

			// check for items in glamour dresser
			//PluginLog.Verbose($"Searching for {applyItemSlot} {applyItem.ItemId} ({applyItem.Stain}, {applyItem.Stain2})");
			var matchingIds = dresser.FindAll(mirage => (mirage.ItemId % Offsets.ItemModifierMod) == applyItem.ItemId);

			//PluginLog.Verbose($"Dresser has {matchingIds.Count} items matching {applyItem.ItemId}");

			if (matchingIds.Count != 0) {
				//PluginLog.Debug($"zzz {dresser.Count} {matchingIds.Count } ");

				// we want dyed ones in priority
				var idx = matchingIds.FindIndex(mirage => mirage.Stain1 == applyItem.Stain && mirage.Stain2 == applyItem.Stain2);
				if (idx == -1) idx = matchingIds.FindIndex(mirage => mirage.Stain1 == applyItem.Stain);
				if (idx == -1) idx = matchingIds.FindIndex(mirage => mirage.Stain2 == applyItem.Stain2);
				if (idx == -1) idx = 0;

				if(idx > -1) {
					var mirage = matchingIds[idx];
					//PluginLog.Debug($" >> {mirage.ItemId} {idx}");
					info = (FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData.ItemSource.PrismBox, (int)mirage.Slot, mirage.ItemId, mirage.Stain1, mirage.Stain2);
				}
			}

			// if nothing found in glamour dresser, check for the armoire
			if (info.container == null) {

				var cabinetId = GetCabinetItemId(&UIState.Instance()->Cabinet, applyItem.ItemId);
				//var cabinetId = GetCabinetItemId(&UIState.Instance()->Cabinet, applyItem.ItemId);
				if (cabinetId != -1 && this.Armoire->IsItemInCabinet(cabinetId)) {
					info = (FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData.ItemSource.Cabinet, (int)cabinetId, applyItem.ItemId, 0, 0);
					// PluginLog.Verbose($"Item Found in {FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData.ItemSource.Cabinet} slot {cabinetId}");
				}
			}

			// nothing found after all
			if (info.container == null || info.id == 0) {
				PluginLog.Warning($"Item seems not owned");
				return SetGlamourPlateSlotReturn.failed;
			}


			//PluginLog.Debug($"=>>> send item info {info.container.Value} | {info.index} | {info.id} | {info.stain1} | {info.stain2}");

			// apply the item
			AgentMiragePrismMiragePlate.Instance()->SetSelectedItemData(
                info.container.Value,
                (uint) info.index, // slot or cabinet id
                info.id, // item id
                info.stain1, // stain 1
                info.stain2  // stain 2
            );
			if (applyItem.Stain != info.stain1 || applyItem.Stain2 != info.stain2) {


				if (currentPlate?.TryGetValue(applyItemSlot.Value, out var glamItem1) != null && glamItem1 != null) {
					glamItem1.Stain1 = applyItem.Stain;
				}
				if (currentPlate?.TryGetValue(applyItemSlot.Value, out var glamItem2) != null && glamItem2 != null) {
					glamItem2.Stain1 = applyItem.Stain2;
				}


				uint previousContextSlot = data->ContextMenuItemIndex;
				data->ContextMenuItemIndex = (uint)applyItemSlot.Value;
				PluginLog.Verbose($"Applying stains to {applyItemSlot.Value}: {applyItem.Stain}, {applyItem.Stain2}");

				// item loading for plates is deferred as of patch 7.1
				// so we must set the flags ourselves in order to activate the second dye slot immediately
				if (applyItem.Stain2 != 0)
					data->CurrentItems[0].Flags = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData.ItemFlag.HasStain1;

				this.ApplyStains(applyItemSlot.Value, applyItem, ref usedStains);
				data->ContextMenuItemIndex = previousContextSlot;


			}



			// PluginLog.Debug("zzz vvv");
			var zzz = data->GlamourPlates;

			var fff = data->SelectedMiragePlateIndex;
			var plat = zzz[(int)fff];
			// PluginLog.Debug("zzz");
			var it = plat.Items[(int)applyItemSlot.Value];
			// if(it.ItemId != info.id) PluginLog.Error($"failed to apply {info.id}=>{it.ItemId} on {applyItemSlot.Value}");



			return SetGlamourPlateSlotReturn.success;
		}


		private unsafe void ApplyStains(GlamourPlateSlot slot, InventoryItemDr item, ref UsedStains usedStains) {
			var stain1Item = SelectStainItem(item.Stain,  ref usedStains, out var stain1ItemId);
			var stain2Item = SelectStainItem(item.Stain2, ref usedStains, out var stain2ItemId);

			PluginLog.Verbose($"SetGlamourPlateSlotStains({(stain1Item != null ? stain1Item->Slot : 0)}, {item.Stain}, {stain1ItemId}, {(stain2Item != null ? stain2Item->Slot : 0)}, {item.Stain2}, {stain2ItemId})");
		    AgentMiragePrismMiragePlate.Instance()->SetSelectedItemStains(stain1Item, item.Stain, stain1ItemId,
                                                                          stain2Item, item.Stain2, stain2ItemId);

		}

		private static readonly InventoryType[] PlayerInventories = {
			InventoryType.Inventory1,
			InventoryType.Inventory2,
			InventoryType.Inventory3,
			InventoryType.Inventory4,
		};

		private unsafe InventoryItem* SelectStainItem(byte stainId, ref UsedStains usedStains, out uint bestItemId) {
			var inventory = InventoryManager.Instance();

			var transient = PluginServices.DataManager.GetExcelSheet<StainTransient>()!.GetRowOrDefault(stainId);

			InventoryItem* item = null;

			bestItemId = transient?.Item1.ValueNullable?.RowId ?? (transient?.Item2.ValueNullable?.RowId ?? 0);

			var items = new[] { transient?.Item1.ValueNullable, transient?.Item2.ValueNullable };
			foreach (var dyeItem in items) {
				if (dyeItem == null || dyeItem.Value.RowId == 0) {
					continue;
				}

				foreach (var type in PlayerInventories) {
					var inv = inventory->GetInventoryContainer(type);
					if (inv == null) {
						continue;
					}

					for (var i = 0; i < inv->Size; i++) {
						var address = ((uint)type, (uint)i);
						var invItem = inv->Items[i];

						if (invItem.ItemId != dyeItem.Value.RowId) {
							continue;
						}

						if (usedStains.TryGetValue(address, out var numUsed) && numUsed >= invItem.Quantity) {
							continue;
						}

						// first one that we find in the inventory is the one we'll use
						item = &inv->Items[i];
						bestItemId = invItem.ItemId;

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

			return item;
		}

		public static int CountSlots => Enum.GetValues(typeof(GlamourPlateSlot)).Length;
		internal static InventoryItemSet CurrentSet() {
			return new InventoryItemSet(CurrentSetItems());
		}
		private static Dictionary<GlamourPlateSlot, InventoryItemDr?> CurrentSetItems() {
			return CurrentPlate?.ToDictionary(kvp => kvp.Key, kvp => (InventoryItemDr?)InventoryItemDr.FromSavedGlamourItem(kvp.Value))
					?? Enum.GetValues<GlamourPlateSlot>().ToDictionary(s=>s, s => (InventoryItemDr?)null);
		}
		private static unsafe Dictionary<GlamourPlateSlot, SavedGlamourItem>? CurrentPlate {
			get {

				var agent = MiragePlateAgent;
				if (agent == null) {
					return null;
				}

				var data = *(FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData**)((nint)agent + Offsets.HeadSize);
				if (data == null)
					return null;

				var plate = new Dictionary<GlamourPlateSlot, SavedGlamourItem>();
				foreach (var slot in Enum.GetValues<GlamourPlateSlot>()) {
					ref var item = ref data->CurrentItems[(int)slot];

					if (item.ItemId == 0)
						continue;

					var stain1 =
						item.PendingStainIds[0] != 0
							? item.PendingStainIds[0]
							: item.StainIds[0];

					var stain2 =
						item.PendingStainIds[1] != 0
							? item.PendingStainIds[1]
							: item.StainIds[1];

					plate[slot] = new SavedGlamourItem {
						ItemId = item.ItemId,
						Stain1 = stain1,
						Stain2 = stain2,
					};
				}

				return plate;
			}
		}


		private static void Wait(int ms) {
			Task.Run(async delegate { await Task.Delay(ms); }).Wait();
		}


		internal GlamourPlates() {
			_glamourDresserApplyThrottler = new Throttler<Task<SetGlamourPlateSlotReturn>>(0);

			PluginServices.GameInteropProvider.InitializeFromAttributes(this);
			PluginServices.Framework.Update += OnFrameworkUpdate;

		}

		public void Dispose() {
			PluginServices.Framework.Update -= OnFrameworkUpdate;

		}

		private static List<PrismBoxCachedItem> _cachedDresserItems = new();
		private static int _dresserItemSlotsUsed = 0;

		private unsafe void OnFrameworkUpdate(IFramework framework) {
			GetPrismBoxContents();
		}
		private unsafe List<PrismBoxCachedItem> GetPrismBoxContents() {
			var agent = AgentMiragePrismPrismBox.Instance();
			if (agent == null)
				return new();

			if (!agent->IsAddonReady() || agent->Data == null)
				return new();

			// if (agent->Data->UsedSlots == _dresserItemSlotsUsed)
			// 	return new();
            ushort* usedSlots = (ushort*) ((nint) agent->Data + 0x10B460);
            if (*usedSlots == _dresserItemSlotsUsed)
                return new();

			List<PrismBoxCachedItem> items = new();
			//PluginLog.Verbose($"refreshing dresser contents");

			_cachedDresserItems.Clear();
			foreach (var item in agent->Data->PrismBoxItems) {
				if (item.ItemId == 0 || item.Slot >= Offsets.TotalBoxSlot)
					continue;

				//PluginLog.Verbose($"PrismBox item: {item.ItemId} {item.Slot}");
				items.Add(new PrismBoxCachedItem {
					Name = item.Name.ToString(),
					Slot = item.Slot,
					ItemId = item.ItemId,
					IconId = item.IconId,
					Stain1 = item.Stains[0],
					Stain2 = item.Stains[1],
				});
			}

			_cachedDresserItems = items;
			_dresserItemSlotsUsed = agent->Data->UsedSlots;

			return items;
		}
	}

	internal struct PrismBoxCachedItem {
		public string Name { get; set; }
		public uint Slot { get; set; }
		public uint ItemId { get; set; }
		public uint IconId { get; set; }
		public byte Stain1 { get; set; }
		public byte Stain2 { get; set; }
	}


	public class SavedGlamourItem {
		public uint ItemId { get; set; }
		public byte Stain1 { get; set; }
		public byte Stain2 { get; set; }

		internal SavedGlamourItem Clone() {
			return new SavedGlamourItem() {
				ItemId = this.ItemId,
				Stain1 = this.Stain1,
				Stain2 = this.Stain2,
			};
		}
		public static explicit operator InventoryItemDr(SavedGlamourItem item)
			=> InventoryItemDr.New(item.ItemId, item.Stain1, item.Stain2);

	}
	public enum GlamourPlateSlot : uint {
		MainHand = 0,
		OffHand = 1,
		Head = 2,
		Body = 3,
		Hands = 4,
		Legs = 5,
		Feet = 6,
		Ears = 7,
		Neck = 8,
		Wrists = 9,
		RightRing = 10,
		LeftRing = 11,
	}

	// from glamaholic (thanks caitlyn)
	// size may be incorrect
	[StructLayout(LayoutKind.Explicit, Size = 0x3B38)]
	public struct AgentMiragePrismMiragePlateData {
		[FieldOffset(0)]
		private bool Unk0;

		[FieldOffset(1)]
		public bool HasChanges;

		[FieldOffset(20)]
		private uint _SelectedMiragePlateIndex;

		// The index of the item selected in the current Mirage Plate
		[FieldOffset(24)]
		private uint _SelectedItemIndex;

		// The index of the item the context menu is associated with
		[FieldOffset(28)]
		private uint _ContextMenuItemIndex;

		// If anyone feels like figuring out what the hell is in here..
		// Please, be my guest.

		[FieldOffset(36)]
		private FixedSizeArray20<MiragePlate> _Plates;

		[FieldOffset(14436)]
		private FixedSizeArray12<MiragePlateItem> _Items;

		public uint SelectedMiragePlateIndex {
			get => _SelectedMiragePlateIndex;
			set => _SelectedMiragePlateIndex = Math.Clamp(value, 0, 19);
		}

		public uint SelectedItemIndex {
			get => _SelectedItemIndex;
			set => _SelectedItemIndex = Math.Clamp(value, 0, (uint)GlamourPlateSlot.LeftRing);
		}

		public uint ContextMenuItemIndex {
			get => _ContextMenuItemIndex;
			set => _ContextMenuItemIndex = Math.Clamp(value, 0, (uint)GlamourPlateSlot.LeftRing);
		}

		public unsafe Span<MiragePlate> Plates =>
			MemoryMarshal.CreateSpan(ref Unsafe.As<FixedSizeArray20<MiragePlate>, MiragePlate>(ref _Plates), Offsets.TotalPlates + 1);

		public unsafe Span<MiragePlateItem> Items =>
			MemoryMarshal.CreateSpan(ref Unsafe.As<FixedSizeArray12<MiragePlateItem>, MiragePlateItem>(ref _Items), 12);
	}

	[StructLayout(LayoutKind.Explicit, Size = (0x3C * 12))]
	public struct MiragePlate {
		[FieldOffset(0x0)]
		private FixedSizeArray12<MiragePlateItem> _Items;
		public unsafe Span<MiragePlateItem> Items =>
			MemoryMarshal.CreateSpan(ref Unsafe.As<FixedSizeArray12<MiragePlateItem>, MiragePlateItem>(ref _Items), 12);

	}
	[StructLayout(LayoutKind.Explicit, Size = 0x3C)]
	public struct MiragePlateItem {
		[FieldOffset(0)]
		public uint ItemId;

		[FieldOffset(4)]
		public uint SlotOrCabinetId;

		[FieldOffset(8)]
		public MirageSource Source;

		[FieldOffset(16)]
		public byte Flags;

		[FieldOffset(24)]
		public byte Stain1;

		[FieldOffset(25)]
		public byte Stain2;

		[FieldOffset(26)]
		public byte PreviewStain1;

		[FieldOffset(27)]
		public byte PreviewStain2;

		[FieldOffset(28)]
		public bool HasChanged;

		// After this seem to be 3 ints, not sure what they are yet

		public static explicit operator InventoryItemDr?(MiragePlateItem a) {
			return a.ItemId == 0 ? null : InventoryItemDr.New(a.ItemId, a.Stain1, a.Stain2);
		}

	}

	public enum MirageSource : uint {
		GlamourDresser = 1,
		Armoire = 2,
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	[InlineArray(12)]
	internal struct FixedSizeArray12<T> where T : unmanaged {
		private T _element0;
	}
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	[InlineArray(Offsets.TotalPlates)]
	internal struct FixedSizeArray20<T> where T : unmanaged {
		private T _element0;
	}
}
