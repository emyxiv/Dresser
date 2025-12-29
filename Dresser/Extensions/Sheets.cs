using Dalamud.Bindings.ImGui;

using Lumina.Excel.Sheets;

using System.Numerics;

namespace Dresser.Extensions {
	public static class Sheets {

		public static Vector4 ColorVector4(this Stain stain) {
			if (stain.Color == 0) return new(0, 0, 0, 0);
			var c = ImGui.ColorConvertU32ToFloat4(stain.Color);
			return new Vector4(c.Z, c.Y, c.X, 1);
		}
		public static bool IsValid(this Stain stain)
			 => stain.Shade != 0;

		public static bool IsTypeTank(this ClassJob classJob)
			=> classJob.JobType == 1 || classJob.Role == 1; // Role 1 will include the GLA and MRD classes
		public static bool IsTypeHealer(this ClassJob classJob)
			=> classJob.IsTypePureHealer() || classJob.IsTypeBarrierHealer();
		public static bool IsTypePureHealer(this ClassJob classJob)
			=> classJob.JobType == 2; // this also includes the CNJ class somehow
		public static bool IsTypeBarrierHealer(this ClassJob classJob)
			=> classJob.JobType == 6;
		public static bool IsTypeDpsMelee(this ClassJob classJob)
			=> classJob.JobType == 3 || classJob.Role == 2; // Role 2 will include the PGL LNC ROG classes
		public static bool IsTypeDpsPhysicalRanged(this ClassJob classJob)
			=> classJob.JobType == 4 || classJob.Abbreviation == "ARC"; // ARC = archer (BRD class)
		public static bool IsTypeDpsMagicalRanged(this ClassJob classJob)
			=> classJob.JobType == 5 || classJob.Abbreviation == "THM" || classJob.Abbreviation == "ACN"; // THM = thaumaturgist (BLM class), ACN = arcanist (SMN class)
		public static bool IsTypeDisciplesOfTheHand(this ClassJob classJob)
			=> classJob.DohDolJobIndex >= 0 && !classJob.IsTypeDisciplesOfTheLand();
		public static bool IsTypeDisciplesOfTheLand(this ClassJob classJob)
			=> classJob.DohDolJobIndex >= 0 && (classJob.Abbreviation == "MIN" || classJob.Abbreviation == "BTN" || classJob.Abbreviation == "FSH");



	}
}
