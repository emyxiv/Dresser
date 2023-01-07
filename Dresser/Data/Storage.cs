using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiScene;

using Lumina.Data.Files;
using Lumina.Excel;

using CriticalCommonLib;
using CriticalCommonLib.Models;
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
		public static Dictionary<GlamourPlateSlot, InventoryItem> SlotInventoryItems = new();
		public static MiragePage[]? Pages = null;
		public static MiragePage? DisplayPage = null;
		public static Dictionary<byte, Vector4> RarityColors = new();


		public static void Init() {
			Dyes = Sheets.GetSheet<Dye>();
			EmptyEquipTexture = PluginServices.DataManager.GetImGuiTexture(GetEmptyEquipTex());
			// ui colors:
			// bad: 14

			RarityColors = new(){
				// items colors:
				{ 0, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 2)) },
				// white: 33/549
				{ 1, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 549)) },
				// green: 42/67/551
				{ 2, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 551)) },
				// blue : 37/38/553
				{ 3, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 553)) },
				// purple: 522/48/555
				{ 4, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 555)) },
				// orange:557
				{ 5, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 557)) },
				// yellow:559
				{ 6, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 559)) },
				// pink: 578/556
				{ 7, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 561)) },
				// gold: 563
				{ 8, Utils.ConvertUIColorToColor(Service.ExcelCache.GetUIColorSheet().First(c => c.RowId == 563)) },
			};
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

		public static Vector4 RarityColor(ItemEx itemEx) {
			if (!RarityColors.TryGetValue(itemEx.Rarity, out var rarityColor))
				rarityColor = Vector4.One;
			return rarityColor;
		}
	}
}
