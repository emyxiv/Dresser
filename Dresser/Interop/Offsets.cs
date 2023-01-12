
using Dresser.Structs.Actor;

namespace Dresser.Interop {
	internal static class Offsets {
		// PlayerCharacter/Actor/GameObject(?) Appearance
		// Thanks Chirp ♥
		// https://github.com/ktisis-tools/Ktisis/blob/0ee4bf058833e118eaf384728814b64643a85b4b/Ktisis/Structs/Actor/Actor.cs#L22
		internal const int WeaponMainHand = 0x6E0;
		internal const int WeaponOffHand = 0x748;
		internal const int Equipment = 0x818;
		internal const int Customize = 0x840;

		// PlayerCharacter/Actor/GameObject(?) Change equip
		// Thanks Chirp ♥
		// https://github.com/ktisis-tools/Ktisis/blob/0ee4bf058833e118eaf384728814b64643a85b4b/Ktisis/Interop/Methods.cs#L16
		// https://github.com/ktisis-tools/Ktisis/blob/0ee4bf058833e118eaf384728814b64643a85b4b/Ktisis/Structs/Actor/Actor.cs#L56
		internal const int EquipChangeToWriteTo = 0x6D0;

		// MiragePrismMiragePlate
		internal const int EditorInfo = 0x28;
		internal const int EditorCurrentPlate = 0x18;

	}
}
