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


	}
}
