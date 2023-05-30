using System.Runtime.InteropServices;

namespace Dresser.Structs.Dresser {
	[StructLayout(LayoutKind.Explicit, Size = 136)]
	internal readonly struct GlamourPlateItem {
		[FieldOffset(0x70)]
		internal readonly uint Index;

		[FieldOffset(0x74)]
		internal readonly uint ItemId;

		[FieldOffset(0x86)]
		internal readonly byte StainId;
	}
}
