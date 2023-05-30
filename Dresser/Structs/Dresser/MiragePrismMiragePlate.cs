using CriticalCommonLib.Models;

using Dresser.Extensions;
using Dresser.Services;

using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace Dresser.Structs.Dresser {

	// Game structs
	[Agent(AgentId.MiragePrismMiragePlate)]
	[StructLayout(LayoutKind.Explicit)]
	public unsafe partial struct MiragePrismMiragePlate {

		[FieldOffset(0)] public AgentInterface AgentInterface;

		//[FieldOffset(40 + 36)] public IntPtr* PlatesPointer;
		//[FieldOffset(40 + 36)] public fixed MiragePage Plates[20]; // This would be ideal, TODO: try to find a way to achieve this

		internal static AgentInterface* MiragePlateAgent() => Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.MiragePrismMiragePlate);

		// this getter exists because we cannot specify a sized array in the variable
		public MiragePage[] Pages {
			get {
				var totalPages = Storage.PlateNumber + 1; // the currently viewing/editing page is added at the end of the array
				var pages = new MiragePage[totalPages];

				if (!AgentInterface.IsAgentActive()) return pages;

				// TODO: find a way to use PlatesPointer instead of calling the agent again
				var agent = MiragePlateAgent();
				var glamPlatePointer = *(IntPtr*)((IntPtr)agent + 40) + 36;

				for (int plateNumber = 0; plateNumber < totalPages; plateNumber++) {
					var offset = 44 * 12 * plateNumber;
					pages[plateNumber] = *(MiragePage*)(glamPlatePointer + offset);

				}
				return pages;
			}
		}
		public bool VerifyItem(ushort plateNumber, GlamourPlateSlot slot, InventoryItem item) {
			if (!AgentInterface.IsAgentActive()) return false;
			var agent = MiragePlateAgent();
			var glamPlatePointer = *(IntPtr*)((IntPtr)agent + 40) + 36;
			var offset = 44 * 12 * plateNumber;
			var plate = *(MiragePage*)(glamPlatePointer + offset);
			var mirageItem = plate[slot];
			return item.IsAppearanceDifferent(item);
		}
	}

	[StructLayout(LayoutKind.Explicit, Size = 0x210)]
	public struct MiragePage {
		[FieldOffset(0x2C * 00)] public MirageItem MainHand;
		[FieldOffset(0x2C * 01)] public MirageItem OffHand;
		[FieldOffset(0x2C * 02)] public MirageItem Head;
		[FieldOffset(0x2C * 03)] public MirageItem Chest;
		[FieldOffset(0x2C * 04)] public MirageItem Hands;
		[FieldOffset(0x2C * 05)] public MirageItem Legs;
		[FieldOffset(0x2C * 06)] public MirageItem Feet;
		[FieldOffset(0x2C * 07)] public MirageItem Earring;
		[FieldOffset(0x2C * 08)] public MirageItem Necklace;
		[FieldOffset(0x2C * 09)] public MirageItem Bracelet;
		[FieldOffset(0x2C * 10)] public MirageItem RingRight;
		[FieldOffset(0x2C * 11)] public MirageItem RingLeft;

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
	[StructLayout(LayoutKind.Explicit, Size = 44)]
	public struct MirageItem {
		[FieldOffset(0)] public uint ItemId;
		//[FieldOffset(4)] public uint Unk1; // > 0 when previewing an item
		//[FieldOffset(8)] public uint Unk2; // = 1 when previwing item
		//[FieldOffset(12)] public uint Unk3;
		//[FieldOffset(16)] public uint Unk4;
		[FieldOffset(20)] public uint ItemType; // not item slot
		[FieldOffset(24)] public byte DyeId;
		[FieldOffset(25)] public byte DyePreviewId;
		//[FieldOffset(26)] public byte Unk5; // = 1 when previwing item
		//[FieldOffset(28)] public uint Unk7; // > 0 when previewing item + dye
		//[FieldOffset(39)] public byte Unk8; // = 1 when previewing item + dye
		//[FieldOffset(42)] public ushort Unk9;

		public static explicit operator InventoryItem?(MirageItem a) {
			return a.ItemId == 0 ? null : InventoryItemExtensions.New(a.ItemId, a.DyeId);
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
