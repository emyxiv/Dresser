using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiScene;

using Dalamud.Interface.GameFonts;
using Lumina.Data.Files;
using Lumina.Excel;

using CriticalCommonLib;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;

using Dresser.Data.Excel;
using Dresser.Structs.FFXIV;

namespace Dresser.Data {
	internal class Storage : IDisposable {

		public static ExcelSheet<Dye>? Dyes = null;
		public static string HighResolutionSufix = "_hr1";

		public const int PlateNumber = 20;
		public static Dictionary<GlamourPlateSlot, MirageItem> SlotMirageItems = new();
		//public static Dictionary<GlamourPlateSlot, InventoryItem> SlotInventoryItems = new();
		public static MiragePage[]? Pages = null;
		public static MiragePage? DisplayPage = null;
		public static Dictionary<byte, Vector4> RarityColors = new();
		public readonly GameFontHandle FontTitle =
			PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.TrumpGothic68));
		public readonly GameFontHandle FontRadio =
			PluginServices.PluginInterface.UiBuilder.GetGameFontHandle(new GameFontStyle(GameFontFamilyAndSize.Axis36));

		public Storage() {
			Dyes = Sheets.GetSheet<Dye>();
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

			foreach ((var slot, var part_id) in ImageGuiCrop.EmptyGlamourPlateSlot)
				ImageGuiCrop.GetPart("character", part_id);
		}
		public void Dispose() {
			Dyes = null;
			Sheets.Cache.Clear();
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


	internal class ImageGuiCrop {
		public static Dictionary<string,TextureWrap> Textures = new ();
		public static Dictionary<(string, int), (IntPtr, Vector2, Vector2, Vector2)> Cache = new();

		public static Dictionary<string, Dictionary<int, (Vector2,Vector2)>> TexturesParts = new() {
			{ "character", new(){

				// main weapon: 17
				{ 17, (new(0, 3*48), new(64, 64) ) },
				// off weapon: 18
				{ 18, (new(64, 3*48), new(64, 64) ) },
				// head: 19
				{ 19, (new(2*64, 3*48), new(64, 64) ) },
				// body: 20
				{ 20, (new(3*64, 3*48), new(64, 64) ) },
				// hands: 21
				{ 21, (new(4*64, 3*48), new(64, 64) ) },
				// legs: 23
				{ 23, (new(6*64, 3*48), new(64, 64) ) },
				// feet: 24
				{ 24, (new(0, 3*48 + 64), new(64, 64) ) },
				// earring: 25
				{ 25, (new(64, 3*48 + 64), new(64, 64) ) },
				// necklace: 26
				{ 26, (new(2*64, 3*48 + 64), new(64, 64) ) },
				// bracer: 27
				{ 27, (new(3*64, 3*48 + 64), new(64, 64) ) },
				// ring: 28
				{ 28, (new(4*64, 3*48 + 64), new(64, 64) ) },


			}},
			{ "icon_a_frame", new(){

				// item cap - normal
				{ 1, (new(0, 0), new(96, 96) ) },
				// item cap - blue cross
				{ 2, (new(0, 96), new(96, 96) ) },
				// item cap - red cross
				{ 4, (new(96, 96), new(96, 96) ) },
				// highlight
				{ 16, (new(96*5, 0), new(144, 144) ) },
				// normal slot
				{ 11, (new(96*6+144, 0), new(96, 96) ) },
				// hovered slot
				{ 12, (new(0, 96*2), new(96, 96) ) },
			}},
			{ "mirage_prism_box", new(){

				// item slot - blue cross
				{ 1, (new(0, 0), new(96, 96) ) },
				// item slot - red cross
				{ 2, (new(96, 0), new(96, 96) ) },
				// item slot - normal
				{ 3, (new(0, 96), new(96, 96) ) },
			}},

			{ "mirage_prism_plate2", new(){
				// button not selected plate (radio)
				{ 5, (new(0, 56), new(56*2, 48) ) },
				// button currently selected plate (radio)
				{ 6, (new(56*2, 56), new(56*2, 48) ) },
			}},

		};

		public static Dictionary<GlamourPlateSlot, int> EmptyGlamourPlateSlot = new() {
				{ GlamourPlateSlot.MainHand, 17 }, // main weapon: 17
				{ GlamourPlateSlot.OffHand, 18 }, // off weapon: 18
				{ GlamourPlateSlot.Head, 19 }, // head: 19
				{ GlamourPlateSlot.Body, 20 }, // body: 20
				{ GlamourPlateSlot.Hands, 21 }, // hands: 21
				{ GlamourPlateSlot.Legs, 23 }, // legs: 23
				{ GlamourPlateSlot.Feet, 24 }, // feet: 24
				{ GlamourPlateSlot.Ears, 25 }, // earring: 25
				{ GlamourPlateSlot.Neck, 26 }, // necklace: 26
				{ GlamourPlateSlot.Wrists, 27 }, // bracer: 27
				{ GlamourPlateSlot.RightRing, 28 }, // ring: 28
				{ GlamourPlateSlot.LeftRing, 28 }, // ring: 28
			};

		public static (IntPtr, Vector2, Vector2, Vector2) GetPart(string type, int part_id) {
			if (Cache.TryGetValue((type, part_id), out var cachedInfo))
				return cachedInfo;
			if (Textures.TryGetValue(type, out var texture))
				if (texture != null && TexturesParts.TryGetValue(type, out var parts))
					if (parts != null && parts.Count > 0 && parts.TryGetValue(part_id, out var posSize)) {

						var textureSize = new Vector2(texture.Width, texture.Height);
						var pos = posSize.Item1;
						var size = posSize.Item2;
						Vector2 uv0 = new(
							pos.X / textureSize.X,
							pos.Y / textureSize.Y
							);
						Vector2 uv1 = new(
							(pos.X + size.X) / textureSize.X,
							(pos.Y + size.Y) / textureSize.Y
							);
						var partInfo = (texture.ImGuiHandle, uv0, uv1, size);
						Cache.Add((type, part_id), partInfo);
						return partInfo;
					}

			return (default, default, default, default);
		}

		public static (IntPtr, Vector2, Vector2, Vector2) GetPart(GlamourPlateSlot slot)
			=> GetPart("character", EmptyGlamourPlateSlot[slot]);

		public static void Init() {
			Dictionary<string, string> paths = new() {
				{"character", $"ui/uld/Character{Storage.HighResolutionSufix}.tex"},
				{"icon_a_frame", $"ui/uld/IconA_Frame{Storage.HighResolutionSufix}.tex"},
				{"mirage_prism_box", $"ui/uld/MiragePrismBoxIcon{Storage.HighResolutionSufix}.tex"},
				{"mirage_prism_plate2", $"ui/uld/MiragePrismPlate2{Storage.HighResolutionSufix}.tex"}, // plate number tabs
			};
			foreach((var handle, var path) in paths) {

				var image = PluginServices.DataManager.GetFile<TexFile>(path);
				if (image == null) continue;
				var tex = PluginServices.DataManager.GetImGuiTexture(path);
				if (tex == null) continue;

				Textures.Add(handle, tex);
			}
		}
		public static void Dispose() {
			Cache.Clear();
			foreach ((var handle, var texture) in Textures)
				texture.Dispose();
			Textures.Clear();
		}
	}
}
