
using Dresser.Structs.Actor;

namespace Dresser.Interop {
	internal static class Offsets {
		// PlayerCharacter/Actor/GameObject(?) Appearance
		// Thanks Chirp ♥
		// https://github.com/ktisis-tools/Ktisis/blob/862a0c41ba4027c981d4d227b721c0090b9ec3d5/Ktisis/Structs/Actor/Actor.cs#LL22C38-L22C38
		internal const int ActorDrawData = 0x6E8;
		// https://github.com/ktisis-tools/Ktisis/blob/862a0c41ba4027c981d4d227b721c0090b9ec3d5/Ktisis/Structs/Actor/ActorDrawData.cs#L5
		internal const int WeaponMainHand = ActorDrawData + 0x010;
		internal const int WeaponOffHand = ActorDrawData + 0x078;
		internal const int Equipment = ActorDrawData + 0x148;
		internal const int Customize = ActorDrawData + 0x170;

		// MiragePrismMiragePlate
		// https://git.anna.lgbt/ascclemens/Glamaholic/src/commit/d9b283e7fd4865b0e7b518405f5fcb6b52235d70/Glamaholic/GameFunctions.cs#L164
		internal const int EditorInfo = 0x28;
		// https://git.anna.lgbt/ascclemens/Glamaholic/src/commit/d9b283e7fd4865b0e7b518405f5fcb6b52235d70/Glamaholic/GameFunctions.cs#L243
		internal const int EditorCurrentPlate = 0x18;

	}
}
