using Dalamud.Game.ClientState.Objects.SubKinds;

using Dresser.Structs.Actor;

using System;
using System.Runtime.InteropServices;

namespace Dresser.Interop {
	internal class Methods {

		// Change actor equipment
		// a1 = Actor + 0x6D0, a2 = EquipIndex, a3 = EquipItem

		internal unsafe delegate void ChangeEquipDelegate(IntPtr writeTo, EquipIndex index, ItemEquip* item, bool force);
		internal static ChangeEquipDelegate? ActorChangeEquip;
		internal unsafe static void ChangeEquip(IntPtr writeTo, EquipIndex index, ItemEquip item)
			=> ActorChangeEquip?.Invoke(writeTo, index, &item, true);

		internal delegate void ChangeWeaponDelegate(IntPtr writeTo, WeaponIndex slot, WeaponEquip weapon, byte a4, byte a5, byte a6, byte a7); // a4-a7 is always 0,1,0,0.
		internal static ChangeWeaponDelegate? ActorChangeWeapon;

		internal unsafe static void ChangeWeapon(IntPtr writeTo, WeaponIndex slot, WeaponEquip weapon) {
			ActorChangeWeapon?.Invoke(writeTo, slot, default, 0, 1, 0, 0);
			ActorChangeWeapon?.Invoke(writeTo, slot, weapon, 0, 1, 0, 0);

		}


		// Init & Dispose

		private static TDelegate Retrieve<TDelegate>(string sig)
			=> Marshal.GetDelegateForFunctionPointer<TDelegate>(PluginServices.SigScanner.ScanText(sig));

		internal static void Init() {
			ActorChangeEquip = Retrieve<ChangeEquipDelegate>(Signatures.ChangeEquip);
			ActorChangeWeapon = Retrieve<ChangeWeaponDelegate>(Signatures.ChangeWeapon);
		}
	}
}
