
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
		// https://git.anna.lgbt/ascclemens/Glamaholic/src/commit/d9b283e7fd4865b0e7b518405f5fcb6b52235d70/Glamaholic/GameFunctions.cs#L164
		internal const int EditorInfo = 0x28;
		// https://git.anna.lgbt/ascclemens/Glamaholic/src/commit/d9b283e7fd4865b0e7b518405f5fcb6b52235d70/Glamaholic/GameFunctions.cs#L243
		internal const int EditorCurrentPlate = 0x18;

	}
}
