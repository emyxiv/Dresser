using System.Collections.Generic;

using ImGuiScene;

using Lumina.Data.Files;
using Lumina.Excel;

using CriticalCommonLib.Sheets;

using Dresser.Data.Excel;
using Dresser.Structs.FFXIV;

namespace Dresser.Data {
	internal class Storage {

		public static ExcelSheet<Dye>? Dyes = null;
		public static string HighResolutionSufix = "_hr1";
		public static TextureWrap? EmptyEquipTexture = null;

		public const int PlateNumber = 20;
		public static Dictionary<GlamourPlateSlot, MirageItem> SlotMirageItems = new();
		public static Dictionary<GlamourPlateSlot, ItemEx> SlotItemsEx = new();
		public static MiragePage[]? Pages = null;
		public static MiragePage? DisplayPage = null;


		public static void Init() {
			Dyes = Sheets.GetSheet<Dye>();
			EmptyEquipTexture = PluginServices.DataManager.GetImGuiTexture(GetEmptyEquipTex());
		}
		public static void Dispose() {
			Dyes = null;
			Sheets.Cache.Clear();
			EmptyEquipTexture?.Dispose();
		}

		public static TexFile GetEmptyEquipTex() {

			var path = $"ui/uld/Character{HighResolutionSufix}.tex";
			return PluginServices.DataManager.GetFile<TexFile>(path)!;
		}
	}
}
