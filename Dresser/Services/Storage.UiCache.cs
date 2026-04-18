using System.Collections.Generic;
using System.Numerics;

using AllaganLib.GameSheets.Sheets.Rows;

using Dalamud.Interface.ManagedFontAtlas;

using Lumina.Data.Files;

using static Dresser.Gui.Components.GuiHelpers;


namespace Dresser.Services {
	internal partial class Storage {

		public static string HighResolutionSufix = "_hr1";

		public Dictionary<byte, Vector4> RarityColors = new();
		public HashSet<byte> RarityAllowed = new() { 1, 2, 3, 4 };

		public Dictionary<Font, (IFontHandle handle, float size)> FontHandles = new();
		public uint ChangePoseIconId;
		public int ClassJobsTotalCount;
		public ushort MaxEquipLevel;
		public uint MaxItemLevel;

		public static TexFile GetEmptyEquipTex() {
			var path = $"ui/uld/Character{HighResolutionSufix}.tex";
			return PluginServices.DataManager.GetFile<TexFile>(path)!;
		}

		public static Vector4 RarityColor(ItemRow itemEx) {
			if (!PluginServices.Storage.RarityColors.TryGetValue(itemEx.Base.Rarity, out var rarityColor))
				rarityColor = Vector4.One;
			return rarityColor;
		}
	}
}
