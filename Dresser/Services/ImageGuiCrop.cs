using Dresser.Structs.Dresser;

using Dalamud.Interface.Internal;

using Lumina.Data.Files;

using System;
using System.Collections.Generic;
using System.Numerics;
//using Dalamud.Interface;
using Dalamud.Logging;
using System.IO;
using static System.Runtime.InteropServices.JavaScript.JSType;

using Dresser.Logic;
using Dresser.Data.Excel;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using static System.Net.WebRequestMethods;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using ImGuiNET;
using Dresser.Windows.Components;
using System.Linq;

namespace Dresser.Services {
	internal class ImageGuiCrop : IDisposable {
		public Dictionary<int, IDalamudTextureWrap> Textures = new();
		public Dictionary<string, UldWrapper> Ulds = new();
		public HashSet<int> Blacklist = new();
		//public Dictionary<(string, int), (IntPtr, Vector2, Vector2, Vector2)> Cache = new();

		// ui/uld/ArmouryBoard.uld
		// ui/uld/ArmouryBoard_hr1.tex

		public Dictionary<string, (string TexPath, string UldPath)> HandlesPaths = new Dictionary<string, (string, string)>() {
			{"character",           ($"ui/uld/Character{Storage.HighResolutionSufix}.tex"           , "ui/uld/Character.uld" )},
			{"icon_a_frame",        ($"ui/uld/IconA_Frame{Storage.HighResolutionSufix}.tex"         , "ui/uld/Character.uld")},
			{"mirage_prism_box",    ($"ui/uld/MiragePrismBoxIcon{Storage.HighResolutionSufix}.tex"  , "ui/uld/MiragePrismBox.uld")},
			{"mirage_prism_plate2", ($"ui/uld/MiragePrismPlate2{Storage.HighResolutionSufix}.tex"   , "ui/uld/MiragePrismPlate.uld")}, // plate number tabs
			{"circle_buttons_4",    ($"ui/uld/CircleButtons{Storage.HighResolutionSufix}.tex"       , "ui/uld/Character.uld")}, 
			//{"circle_buttons_4",    ($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/fourth/Character.uld")},
		};


		public Dictionary<GlamourPlateSlot, UldBundle> EmptyGlamourPlateSlot = new() {
				{ GlamourPlateSlot.MainHand, UldBundle.MainHand }, // main weapon: 17
				{ GlamourPlateSlot.OffHand, UldBundle.OffHand }, // off weapon: 18
				{ GlamourPlateSlot.Head, UldBundle.Head }, // head: 19
				{ GlamourPlateSlot.Body, UldBundle.Body }, // body: 20
				{ GlamourPlateSlot.Hands, UldBundle.Hands }, // hands: 21
				{ GlamourPlateSlot.Legs, UldBundle.Legs }, // legs: 23
				{ GlamourPlateSlot.Feet, UldBundle.Feet }, // feet: 24
				{ GlamourPlateSlot.Ears, UldBundle.Ears }, // earring: 25
				{ GlamourPlateSlot.Neck, UldBundle.Neck }, // necklace: 26
				{ GlamourPlateSlot.Wrists, UldBundle.Wrists }, // bracer: 27
				{ GlamourPlateSlot.RightRing, UldBundle.RightRing }, // ring: 28
				{ GlamourPlateSlot.LeftRing, UldBundle.LeftRing }, // ring: 28
			};

		public IDalamudTextureWrap? GetPart(UldBundle uldBundle) {
			//return null;
			if (Blacklist.Contains(uldBundle.GetHashCode())) return null;

			if (Textures.TryGetValue(uldBundle.GetHashCode(), out var texture)) {
				return texture;
			}

			if (Ulds.TryGetValue(uldBundle.Uld, out var uld)) {
				texture =  uld.LoadTexturePart(uldBundle.Tex, uldBundle.Part);
				if(texture != null) {
					Textures.Add(uldBundle.GetHashCode(), texture);
					return texture;
				}
			}
			PluginLog.Warning($"Unable to load Uld {uldBundle.Handle}");
			Blacklist.Add(uldBundle.GetHashCode());
			//PluginLog.Warning($"Some stats: Blacklist:{Blacklist.Count} Textures:{Textures.Count} Ulds:{Ulds.Count}");
			return null;
		}



		/*
		public (IntPtr ImGuiHandle, Vector2 uv0, Vector2 uv1, Vector2 size) GetPart(string type, int part_id) {
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
		*/

		public IDalamudTextureWrap? GetPart(GlamourPlateSlot slot)
			=> GetPart(EmptyGlamourPlateSlot[slot]);

		public ImageGuiCrop() {
			PluginLog.Error("LOADING ULDS");


			var uldsPaths = new List<string>() {
				"ui/uld/Character.uld",
				"ui/uld/MiragePrismBox.uld",
				"ui/uld/MiragePrismPlate.uld",
			};
			foreach (var uldFile in uldsPaths) {
				//PluginLog.Debug($"TEX: {(PluginServices.DataManager.FileExists(texFile) ? "  EXIST  " : "not found")} file: {texFile}");

				if (Ulds.ContainsKey(uldFile)) continue;
				if (!PluginServices.DataManager.FileExists(uldFile)) continue;
				var uld = new UldWrapper(uldFile);
				if (uld == null || !uld.Valid || uld.Uld == null) continue;

				PluginLog.Debug($"ULD: {uldFile} compatible with assets:");
				var tli = new string(uld.Uld.TimelineList.Identifier).Replace("\0", string.Empty);
				PluginLog.Debug($"  timeline identifier: {tli}");
				

				foreach ( var textureEntry in uld.Uld.AssetData) {
					var assetPath = new string(textureEntry.Path).Replace("\0", string.Empty);
					PluginLog.Debug($"    {assetPath}");
				}

				Ulds.Add(uldFile, uld);
				
				//var uld = new UldWrapper(PluginServices.PluginInterface.UiBuilder, "");

				//	var image = PluginServices.DataManager.GetFile<TexFile>(path);
				//	if (image == null) continue;
				//	var tex = PluginServices.TextureProvider.GetTextureFromGame(path,true);
				//	if (tex == null) continue;

				//	Textures.Add(handle, tex);
			}

			//foreach ((var slot, var part_id) in EmptyGlamourPlateSlot)
			//	GetPart("character", part_id);

			//TestParts();
		}
		public static void TestParts() {

			Type uldBundleType = typeof(UldBundle);
			var prepertiesInfo = uldBundleType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

			var parts = new List<UldBundle>();
			foreach (var pi in prepertiesInfo) if(pi.PropertyType == uldBundleType) parts.Add((UldBundle)pi.GetValue(null)!);

			foreach (var uldBundle in parts) {
				var texture = PluginServices.ImageGuiCrop.GetPart(uldBundle);
				if (texture?.Size.X > ImGui.GetContentRegionAvail().X) ImGui.NewLine();
				if (texture == null) {
					ImGui.TextColored(ItemIcon.ColorBad, "Error");
				} else {
					ImGui.Image(texture.ImGuiHandle, texture.Size);
				}
				ImGui.SameLine();
				ImGui.TextWrapped(uldBundle.Handle);

			}
		}
		public void Dispose() {
			foreach ((var handle, var uld) in Ulds) uld.Dispose();
			Ulds.Clear();
		}
	}

	internal class UldBundle {
		public string Tex;
		public string Uld;
		public int Part;
		public string Handle;


		public UldBundle(string tex, string uld, int part, string handle) {
			Tex = tex;
			Uld = uld;
			Part = part;
			Handle = handle;
		}

		//public static bool operator ==(UldBundle left, UldBundle? right) {
		//	return
		//		left.Tex == right?.Tex && left.Uld == right.Uld && left.Part == right.Part;
		//}
		//public static bool operator !=(UldBundle left, UldBundle? right) {
		//	return
		//		left.Tex != right?.Tex
		//		|| left.Uld != right?.Uld
		//		|| left.Part != right?.Part;
		//}

		//public override string ToString() {
		//	return (Tex, Uld, Part).GetHashCode().ToString();
		//}
		public override int GetHashCode() {
			return (Tex, Uld, Part).GetHashCode();
		}


		public static UldBundle ItemCapNormal         => new($"ui/uld/fourth/IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 0, "ItemCapNormal"); // OK
		public static UldBundle ItemCapCrossBlue      => new($"ui/uld/fourth/IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 2, "ItemCapCrossBlue");
		public static UldBundle ItemCapCrossRed       => new($"ui/uld/fourth/IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 3, "ItemCapCrossRed");
		public static UldBundle ItemSlot              => new($"ui/uld/fourth/IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 4, "ItemSlot");
		public static UldBundle SlotHighlightInner    => new($"ui/uld/fourth/IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 5, "SlotHighlightInner");
		public static UldBundle SlotHighlight         => new($"ui/uld/fourth/IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 16, "SlotHighlight");


		// handle: character
		// empty slot icons
		public static UldBundle MainHand  => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 17, "MainHand");
		public static UldBundle OffHand   => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 18, "OffHand");
		public static UldBundle Head      => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 19, "Head");
		public static UldBundle Body      => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 20, "Body");
		public static UldBundle Hands     => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 21, "Hands");
		public static UldBundle Legs      => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 23, "Legs");
		public static UldBundle Feet      => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 24, "Feet");
		public static UldBundle Ears      => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 25, "Ears");
		public static UldBundle Neck      => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 26, "Neck");
		public static UldBundle Wrists    => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 27, "Wrists");
		public static UldBundle RightRing => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 28, "RightRing");
		public static UldBundle LeftRing  => new($"ui/uld/Character{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 28, "LeftRing");


		// handle: circle_buttons_4
		public static UldBundle CircleSmallMagnifyLense      => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 0,"CircleSmallMagnifyLense");
		public static UldBundle CircleSmallEdit              => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 1,"CircleSmallEdit");
		public static UldBundle CircleSmallViewport          => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 2,"CircleSmallViewport");
		public static UldBundle CircleSmallHat               => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 3,"CircleSmallHat");
		public static UldBundle CircleSmallWeapon            => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 4,"CircleSmallWeapon");
		public static UldBundle CircleSmallVisor             => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 5,"CircleSmallVisor");
		public static UldBundle CircleSmallDisplayGear       => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 6,"CircleSmallDisplayGear");
		public static UldBundle CircleSmallDyePot            => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 7,"CircleSmallDyePot");
		public static UldBundle CircleSmallEye               => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 8,"CircleSmallEye");
		public static UldBundle CircleSmallRollback          => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 9,"CircleSmallRollback");
		public static UldBundle CircleSmallSavePin           => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 10,"CircleSmallSavePin");
		public static UldBundle CircleLargeCog               => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 11,"CircleLargeCog");
		public static UldBundle CircleLargeFilter            => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 12,"CircleLargeFilter");
		public static UldBundle CircleLargeSort              => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 13,"CircleLargeSort");
		public static UldBundle CircleLargeQuestionMark      => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 14,"CircleLargeQuestionMark");
		public static UldBundle CircleLargeRefresh           => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 15,"CircleLargeRefresh");
		public static UldBundle CircleLargeBubble            => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 16,"CircleLargeBubble");
		public static UldBundle CircleLargeNote              => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 17,"CircleLargeNote");
		public static UldBundle CircleLargeEdit              => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 18,"CircleLargeEdit");
		public static UldBundle CircleLargePlus              => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 19,"CircleLargePlus");
		public static UldBundle CircleLargeRight             => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 20, "CircleLargeRight");
		public static UldBundle CircleLargeMusicNote         => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 21, "CircleLargeMusicNote");
		public static UldBundle CircleLargeSprout            => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 22,"CircleLargeSprout");
		public static UldBundle CircleLargeEye               => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 23,"CircleLargeEye");
		public static UldBundle CircleLargeLetterReceived    => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 24,"CircleLargeLetterReceived");
		public static UldBundle CircleLargeSoundOn           => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 25,"CircleLargeSoundOn");
		public static UldBundle CircleLargeSoundMute         => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 26,"CircleLargeSoundMute");
		public static UldBundle CircleLargeHeartbeat         => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 27,"CircleLargeHeartbeat");
		public static UldBundle CircleLargeCheckbox          => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 28, "CircleLargeCheckbox");
		public static UldBundle CircleLargeHighlightedCog    => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 29,"CircleLargeHighlightedCog");
		public static UldBundle CircleLargeHighlightedFilter => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 30,"CircleLargeHighlightedFilter");
		public static UldBundle CircleLargeRefresh2          => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 31,"CircleLargeRefresh2");
		public static UldBundle CircleLargeHighlight         => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 32,"CircleLargeHighlight");
		public static UldBundle CircleLargeExclamationMark   => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 33,"CircleLargeExclamationMark");
		public static UldBundle CircleLargeHighlightedNote   => new($"ui/uld/fourth/CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 34,"CircleLargeHighlightedNote");

		// ULD: ui/uld/MiragePrismPlate.uld

		// handle: mirage_prism_box
		public static UldBundle MirageSlotItemCrossBlue  => new($"ui/uld/MiragePrismBoxIcon{Storage.HighResolutionSufix}.tex", "ui/uld/MiragePrismPlate.uld", 0,"MirageSlotItemCrossBlue"); //
		public static UldBundle MirageSlotItemCrossRed   => new($"ui/uld/MiragePrismBoxIcon{Storage.HighResolutionSufix}.tex", "ui/uld/MiragePrismPlate.uld", 1,"MirageSlotItemCrossRed"); //
		public static UldBundle MirageSlotNormal         => new($"ui/uld/MiragePrismBoxIcon{Storage.HighResolutionSufix}.tex", "ui/uld/MiragePrismPlate.uld", 2,"MirageSlotNormal"); // OK

		// handle: mirage_prism_plate2
		public static UldBundle MiragePlateRadio         => new($"ui/uld/MiragePrismPlate2{Storage.HighResolutionSufix}.tex", "ui/uld/MiragePrismPlate.uld", 4,"MiragePlateRadio"); // OK
		public static UldBundle MiragePlateRadioSelected => new($"ui/uld/MiragePrismPlate2{Storage.HighResolutionSufix}.tex", "ui/uld/MiragePrismPlate.uld", 5,"MiragePlateRadioSelected"); // 


	}
}
