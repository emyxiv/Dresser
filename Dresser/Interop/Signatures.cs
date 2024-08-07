namespace Dresser.Interop {
	internal static class Signatures {

		// PlayerCharacter/Actor/GameObject(?) Appearance
		// Thanks Chirp ♥
		// https://github.com/ktisis-tools/Ktisis/blob/0ee4bf058833e118eaf384728814b64643a85b4b/Ktisis/Interop/Methods.cs#L37
		internal const string ChangeEquip = "E8 ?? ?? ?? ?? B1 01 41 FF C6";
		internal const string ChangeWeapon = "E8 ?? ?? ?? ?? 4C 8B 45 7F";

		// Glamour plates alter methods
		// Thanks Anna and Caitlyn ♥
		// https://github.com/caitlyn-gg/Glamaholic/blob/d6165186644024d4bf62e1531c769cc0e311c4ae/Glamaholic/GameFunctions.cs#L40
		internal const string SetGlamourPlateSlot = "E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 8B 46 10 8B 1B";
		internal const string SetGlamourPlateSlotStains = "48 89 74 24 ?? 57 48 83 EC 20 48 8B F2 48 8B F9 48 8B 51 28";
		internal const string GetCabinetItemId = "E8 ?? ?? ?? ?? 44 8B 0B 44 8B C0";

	}
}
