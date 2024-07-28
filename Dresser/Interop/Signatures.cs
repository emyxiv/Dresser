namespace Dresser.Interop {
	internal static class Signatures {

		// PlayerCharacter/Actor/GameObject(?) Appearance
		// Thanks Chirp ♥
		// https://github.com/ktisis-tools/Ktisis/blob/0ee4bf058833e118eaf384728814b64643a85b4b/Ktisis/Interop/Methods.cs#L37
		internal const string ChangeEquip = "E8 ?? ?? ?? ?? B1 01 41 FF C6";
		internal const string ChangeWeapon = "E8 ?? ?? ?? ?? 4C 8B 45 7F";

		// Glamour plates alter methods
		// Thanks Anna ♥
		// https://git.anna.lgbt/ascclemens/Glamaholic/src/branch/main/Glamaholic/GameFunctions.cs#L21
		internal const string SetGlamourPlateSlot = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 46 10 8B 1B";
		internal const string ModifyGlamourPlateSlot = "48 89 74 24 ?? 57 48 83 EC 20 80 79 30 00 49 8B F9";
		internal const string ClearGlamourPlateSlot = "40 57 48 83 EC 20 80 79 30 00 48 8B F9 75 3F 4C";
		internal const string IsInArmoire = "E8 ?? ?? ?? ?? 85 C0 0F 88 BD 01 00 00";
		internal const string ArmoirePointer = "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 84 C0 74 98 44 0F B7";

	}
}
