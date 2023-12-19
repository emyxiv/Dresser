using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

using Penumbra.GameData.Enums;

using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Dresser.Structs.Actor {
	[StructLayout(LayoutKind.Explicit)]
	public struct Weapon {
		[FieldOffset(0x00)] public WeaponEquip Equip;
		[FieldOffset(0x08)] public unsafe WeaponModel* Model;
		[FieldOffset(0x40)] public bool IsSheathed;
		[FieldOffset(0x60)] public WeaponFlags Flags;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct WeaponEquip {
		[FieldOffset(0x00)] public ushort Set;
		[FieldOffset(0x02)] public ushort Base;
		[FieldOffset(0x04)] public ushort Variant;
		[FieldOffset(0x06)] public byte Dye;

		public static WeaponEquip Empty => new() { Base = 0, Dye = 0, Set = 0, Variant = 0 };
		public readonly ulong ToModelId() {
			return Set | ((ulong)Base << 16) | ((ulong)Variant << 32);
		}

		public readonly FullEquipType ToFullEquipType() {
			return Penumbra.GameData.Data.ItemData.ConvertWeaponId(Set);
		}
		public readonly bool IsMainModelOnOffhand()
			=> ToFullEquipType().ToSlot() == EquipSlot.OffHand;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct WeaponModel {
		[FieldOffset(0x50)] public hkQsTransformf Transform;
		[FieldOffset(0x50)] public Vector3 Position;
		[FieldOffset(0x60)] public Quaternion Rotation;
		[FieldOffset(0x70)] public Vector3 Scale;

		[FieldOffset(0x88)] public byte Flags;

		[FieldOffset(0xA0)] public unsafe Skeleton* Skeleton;

		[FieldOffset(0x8F0)] public WeaponEquip Equip;
	}
	public enum WeaponIndex : int {
		MainHand,
		OffHand,
		Prop
	}
	[Flags]
	public enum WeaponFlags : byte {
		None = 0,
		Hidden = 2
	}
}
