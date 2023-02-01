
using Dresser.Structs.Actor;

namespace Dresser.Interop {
	internal static class Signatures {

		// PlayerCharacter/Actor/GameObject(?) Appearance
		// Thanks Chirp ♥
		// https://github.com/ktisis-tools/Ktisis/blob/0ee4bf058833e118eaf384728814b64643a85b4b/Ktisis/Interop/Methods.cs#L37
		internal const string ChangeEquip ="E8 ?? ?? ?? ?? 41 B5 01 FF C6";
		internal const string ChangeWeapon ="E8 ?? ?? ?? ?? 80 7F 25 00";

		// Glamour plates alter methods
		// Thanks Anna ♥
		// https://git.anna.lgbt/ascclemens/Glamaholic/src/commit/d9b283e7fd4865b0e7b518405f5fcb6b52235d70/Glamaholic/GameFunctions.cs#L21
		internal const string SetGlamourPlateSlot = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 46 10 8B 1B";
		internal const string ModifyGlamourPlateSlot = "48 89 74 24 ?? 57 48 83 EC 20 80 79 30 00";
		internal const string ClearGlamourPlateSlot = "80 79 30 00 4C 8B C1";

	}
}
