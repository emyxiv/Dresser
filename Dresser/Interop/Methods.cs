using System;
using System.Runtime.InteropServices;

using Dresser.Structs.Actor;

namespace Dresser.Interop {
	internal class Methods {

		// Change actor equipment
		// a1 = Actor + 0x6D0, a2 = EquipIndex, a3 = EquipItem

		internal delegate IntPtr ChangeEquipDelegate(IntPtr writeTo, EquipIndex index, ItemEquip item);
		internal static ChangeEquipDelegate? ActorChangeEquip;

		internal delegate void ChangeWeaponDelegate(IntPtr writeTo, int slot, WeaponEquip weapon, byte a4, byte a5, byte a6, byte a7); // a4-a7 is always 0,1,0,0.
		internal static ChangeWeaponDelegate? ActorChangeWeapon;

		// Init & Dispose

		private static TDelegate Retrieve<TDelegate>(string sig)
			=> Marshal.GetDelegateForFunctionPointer<TDelegate>(PluginServices.SigScanner.ScanText(sig));

		internal static void Init() {
			ActorChangeEquip = Retrieve<ChangeEquipDelegate>("E8 ?? ?? ?? ?? 41 B5 01 FF C3");
			ActorChangeWeapon = Retrieve<ChangeWeaponDelegate>("E8 ?? ?? ?? ?? 80 7F 25 00");
		}
	}
}
