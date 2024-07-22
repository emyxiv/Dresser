using Dresser.Extensions;
using Dresser.Interop;
using Dresser.Interop.Hooks;
using Dresser.Services;

using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Dresser.Structs.Dresser {

	// Game structs
	[Agent(AgentId.MiragePrismMiragePlate)]
	[StructLayout(LayoutKind.Explicit)]
	public unsafe partial struct MiragePrismMiragePlate {

		[FieldOffset(0)] public AgentInterface AgentInterface;

		//[FieldOffset(Offsets.HeadSize + Offsets.HeadPostOffset)] public IntPtr* PlatesPointer;
		//[FieldOffset(Offsets.HeadSize + Offsets.HeadPostOffset)] public fixed MiragePage Plates[Offsets.TotalPlates]; // This would be ideal, TODO: try to find a way to achieve this

		internal static AgentInterface* MiragePlateAgent() => Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismMiragePlate);

		// this getter exists because we cannot specify a sized array in the variable
		public MiragePage[] Pages {
			get {
				var totalPages = Storage.PlateNumber + 1; // the currently viewing/editing page is added at the end of the array
				var totalSlots = GlamourPlates.CountSlots;
				var pages = new MiragePage[totalPages];

				if (!AgentInterface.IsAgentActive()) return pages;

				// TODO: find a way to use PlatesPointer instead of calling the agent again
				var agent = MiragePlateAgent();
				var glamPlatePointer = *(IntPtr*)((IntPtr)agent + Offsets.HeadSize) + Offsets.HeadPostOffset;

				for (int plateNumber = 0; plateNumber < totalPages; plateNumber++) {
					var offset = Offsets.SlotSize * totalSlots * plateNumber;
					pages[plateNumber] = *(MiragePage*)(glamPlatePointer + offset);

				}
				return pages;
			}
		}
		public bool VerifyItem(ushort plateNumber, GlamourPlateSlot slot, InventoryItem item) {
			if (!AgentInterface.IsAgentActive()) return false;
			var agent = MiragePlateAgent();
			var glamPlatePointer = *(IntPtr*)((IntPtr)agent + Offsets.HeadSize) + Offsets.HeadPostOffset;
			var offset = Offsets.SlotSize * GlamourPlates.CountSlots * plateNumber;
			var plate = *(MiragePage*)(glamPlatePointer + offset);
			var mirageItem = plate[slot];
			return item.IsAppearanceDifferent(item);
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 0x210)]
	public struct MiragePage {
		[FieldOffset(Offsets.SlotSize * 00)] public MirageItem MainHand;
		[FieldOffset(Offsets.SlotSize * 01)] public MirageItem OffHand;
		[FieldOffset(Offsets.SlotSize * 02)] public MirageItem Head;
		[FieldOffset(Offsets.SlotSize * 03)] public MirageItem Chest;
		[FieldOffset(Offsets.SlotSize * 04)] public MirageItem Hands;
		[FieldOffset(Offsets.SlotSize * 05)] public MirageItem Legs;
		[FieldOffset(Offsets.SlotSize * 06)] public MirageItem Feet;
		[FieldOffset(Offsets.SlotSize * 07)] public MirageItem Earring;
		[FieldOffset(Offsets.SlotSize * 08)] public MirageItem Necklace;
		[FieldOffset(Offsets.SlotSize * 09)] public MirageItem Bracelet;
		[FieldOffset(Offsets.SlotSize * 10)] public MirageItem RingRight;
		[FieldOffset(Offsets.SlotSize * 11)] public MirageItem RingLeft;

		public Dictionary<GlamourPlateSlot, MirageItem> ToDictionary() {
			Dictionary<GlamourPlateSlot, MirageItem> dic = new();
			var fields = typeof(MiragePage).GetFields();
			for (int slot = 0; slot < fields.Length; slot++)
				dic.Add((GlamourPlateSlot)slot, (MirageItem)fields[slot].GetValue(this)!);
			return dic;
		}
		//public static explicit operator InventoryItemSet(MiragePage page) {
		//	var items = page.ToDictionary().ToDictionary(i=>i.Key,i=>(InventoryItem?)i.Value);
		//	return new InventoryItemSet(items);
		//}
		public MirageItem this[GlamourPlateSlot slot] {
			get {
				return slot switch {
					GlamourPlateSlot.MainHand => MainHand,
					GlamourPlateSlot.OffHand => OffHand,
					GlamourPlateSlot.Head => Head,
					GlamourPlateSlot.Body => Chest,
					GlamourPlateSlot.Hands => Hands,
					GlamourPlateSlot.Legs => Legs,
					GlamourPlateSlot.Feet => Feet,
					GlamourPlateSlot.Ears => Earring,
					GlamourPlateSlot.Neck => Necklace,
					GlamourPlateSlot.Wrists => Bracelet,
					GlamourPlateSlot.RightRing => RingRight,
					GlamourPlateSlot.LeftRing => RingLeft,
					_ => throw new NotImplementedException()
				};
			}
		}
	}

	// Thanks to Anna's Glamaholic code
	// for showing the logic behind the Glamour Plates <3
	[StructLayout(LayoutKind.Explicit, Size = Offsets.SlotSize)]
	public struct MirageItem {
		[FieldOffset(0)] public uint ItemId;
		//[FieldOffset(4)] public uint Unk1; // > 0 when previewing an item
		//[FieldOffset(8)] public uint Unk2; // = 1 when previwing item
		//[FieldOffset(12)] public uint Unk3;
		//[FieldOffset(16)] public uint Unk4;
		[FieldOffset(20)] public uint ItemType; // not item slot
		[FieldOffset(Offsets.SlotOffsetStain1)] public byte DyeId;
		[FieldOffset(Offsets.SlotOffsetStain2)] public byte DyeId2;
		[FieldOffset(Offsets.SlotOffsetStain1Preview)] public byte DyePreviewId;
		[FieldOffset(Offsets.SlotOffsetStain2Preview)] public byte DyePreviewId2;
		//[FieldOffset(26)] public byte Unk5; // = 1 when previwing item
		//[FieldOffset(28)] public uint Unk7; // > 0 when previewing item + dye
		//[FieldOffset(39)] public byte Unk8; // = 1 when previewing item + dye
		//[FieldOffset(42)] public ushort Unk9;

		public static explicit operator InventoryItem?(MirageItem a) {
			return a.ItemId == 0 ? null : InventoryItemExtensions.New(a.ItemId, a.DyeId, a.DyeId2);
		}
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
}
