namespace Dresser.Interop {
	internal static class Offsets {
		// PlayerCharacter/Actor/GameObject(?) Appearance
		// Thanks Chirp ♥
		// https://github.com/ktisis-tools/Ktisis/blob/862a0c41ba4027c981d4d227b721c0090b9ec3d5/Ktisis/Structs/Actor/Actor.cs#LL22C38-L22C38
		internal const int ActorDrawData = 0x708;
		// https://github.com/ktisis-tools/Ktisis/blob/862a0c41ba4027c981d4d227b721c0090b9ec3d5/Ktisis/Structs/Actor/ActorDrawData.cs#L5
		internal const int WeaponMainHand = ActorDrawData + 0x010;
		internal const int WeaponOffHand = ActorDrawData + 0x080;
		internal const int Equipment = ActorDrawData + 0x1D0;
		internal const int Customize = ActorDrawData + 0x220;

		// MiragePrism
		// https://git.anna.lgbt/ascclemens/Glamaholic/src/commit/d9b283e7fd4865b0e7b518405f5fcb6b52235d70/Glamaholic/GameFunctions.cs#L164
		// https://git.anna.lgbt/ascclemens/Glamaholic/src/commit/d9b283e7fd4865b0e7b518405f5fcb6b52235d70/Glamaholic/GameFunctions.cs#L243
		//  box
		internal const int TotalBoxSlot = 800;
		//internal const int BoxSlotSize = 136;

		// plate
		internal const int TotalPlates = 20;
		//internal const int EditorCurrentPlate = 20;
		//internal const int EditorCurrentSlot = 0x18;

		internal const int HeadSize = 40;
		internal const int HeadPostOffset = 36;

		internal const int SlotSize = 60;
		internal const int SlotOffsetStain1 = 24;
		internal const int SlotOffsetStain2 = 25;
		internal const int SlotOffsetStain1Preview = 26;
		internal const int SlotOffsetStain2Preview = 27;
		internal const uint ItemModifierMod = 500_000;
		internal const uint ItemModifierHQ = 1_000_000;

	}
}
