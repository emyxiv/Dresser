using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;

using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Structs.Dresser;
using Dresser.Windows.Components;

using ImGuiNET;

using System;
using System.Collections.Generic;

namespace Dresser.Services {
	internal class ImageGuiCrop : IDisposable {
		public Dictionary<int, IDalamudTextureWrap> Textures = new();
		public Dictionary<string, UldWrapper> Ulds = new();

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

			if (Textures.TryGetValue(uldBundle.GetHashCode(), out var texture)) {
				return texture;
			}

			if (Ulds.TryGetValue(uldBundle.Uld, out var uld)) {

				if (uld.Valid) {
					texture = uld.LoadTexturePart(uldBundle.Tex, uldBundle.Part);
					if (texture != null) {
						// PluginLog.Debug($"ULD tex {uldBundle.Uld} loaded but unable to LoadTexturePart");
						Textures.Add(uldBundle.GetHashCode(), texture);
						return texture;
					}
				}
			} else {
				// if uld not found, make it
				MakeUld(uldBundle);
			}
			//PluginLog.Warning($"Unable to load Uld texture {uldBundle.Handle}");
			return null;
		}


		public IDalamudTextureWrap? GetPart(GlamourPlateSlot slot)
			=> GetPart(EmptyGlamourPlateSlot[slot]);

		public ImageGuiCrop() { }
		private void MakeUld(UldBundle uldBundle) {
			PluginLog.Warning($"creating {uldBundle.Tex} {uldBundle.Uld} {uldBundle.Part} {uldBundle.Handle}");

			if (Ulds.ContainsKey(uldBundle.Uld)) return;
			if (!PluginServices.DataManager.FileExists(uldBundle.Uld)) return;
			var uld = PluginServices.PluginInterface.UiBuilder.LoadUld(uldBundle.Uld);
			if (uld == null) {
				PluginLog.Debug("created uld but NULL");
				return;
			}

			//var assets = uld.Uld?.AssetData;
			//if (assets != null) {

			//	foreach ( var asset in assets) {
			//		var path = new string(asset.Path);
			//		PluginLog.Debug($" [{uldBundle.Uld}] => {path}");
			//	}
			//}

			Ulds.Add(uldBundle.Uld, uld);
			if (uld.Uld == null) {
				PluginLog.Debug("created uld but uldfile NULL");
			}
			if (!uld.Valid) {
				PluginLog.Debug("created uld but not valid");
			}
		}
		public static void TestParts() {

			Type uldBundleType = typeof(UldBundle);
			var prepertiesInfo = uldBundleType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

			var parts = new List<UldBundle>();
			foreach (var pi in prepertiesInfo) if(pi.PropertyType == uldBundleType) parts.Add((UldBundle)pi.GetValue(null)!);

			string? lastUld = null;
			string? lastTex = null;
			foreach (var uldBundle in parts) {

				if(uldBundle.Uld != lastUld) {
					ImGui.Spacing();
					var uldFileExists = PluginServices.DataManager.FileExists(uldBundle.Uld);
					ImGui.TextColored(uldFileExists ? ItemIcon.ColorGood : ItemIcon.ColorBad, $"[{uldBundle.Uld}]");
					if (PluginServices.ImageGuiCrop.Ulds.TryGetValue(uldBundle.Uld, out var uld)) {

						ImGui.Text($"uld valid: {uld.Valid}");

						if(uld.Uld != null) {
							foreach (var z in uld.Uld.AssetData) {
								ImGui.Text($"{z.Id:D2} {z.Unk1} {z.Unk2} path: {new string(z.Path)}");
							}
						}
					} else {
						ImGui.Text("UldWrapper not found");
					}
					ImGui.Spacing();
				}
				if(uldBundle.Tex != lastTex) {
					var texFileExists = PluginServices.DataManager.FileExists(uldBundle.Tex);
					ImGui.TextColored(texFileExists ? ItemIcon.ColorGood : ItemIcon.ColorBad, $"[{uldBundle.Tex}]");
				}


				var texture = PluginServices.ImageGuiCrop.GetPart(uldBundle);
				if (texture?.Size.X > ImGui.GetContentRegionAvail().X) ImGui.NewLine();
				if (texture == null) {
					ImGui.TextColored(ItemIcon.ColorBad, "Error");
				} else {
					ImGui.Image(texture.ImGuiHandle, texture.Size);
				}
				ImGui.SameLine();
				ImGui.Text($"[{uldBundle.Part}]");
				ImGui.SameLine();
				ImGui.TextWrapped(uldBundle.Handle);

				lastUld = uldBundle.Uld;
				lastTex = uldBundle.Tex;

			}
		}
		public void Dispose() {
			foreach ((var k, var t) in Textures) t.Dispose();
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

		// this is blocked by UldWrapper.GetTexture(string texturePath)  only allowing tex in the list of Uld.AssetData
		// TODO maybe offer PR dalamud with (modified for regex)
		// near: texturePath = texturePath.Replace("_hr1", string.Empty);
		// add:  texturePath = texturePath.Replace("fourth/", string.Empty); // fix alt themes
		private static string ThemePathPart
			=> "";
		//=> "dark/";
		//=> "light/";
		//=> "third/";
		//=> "fourth/";


		public static UldBundle ItemCapNormal         => new($"ui/uld/{ThemePathPart}IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 0, "ItemCapNormal"); // OK
		public static UldBundle ItemCapCrossBlue      => new($"ui/uld/{ThemePathPart}IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 2, "ItemCapCrossBlue");
		public static UldBundle ItemCapCrossRed       => new($"ui/uld/{ThemePathPart}IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 3, "ItemCapCrossRed");
		public static UldBundle ItemSlot              => new($"ui/uld/{ThemePathPart}IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 4, "ItemSlot");
		public static UldBundle SlotHighlightInner    => new($"ui/uld/{ThemePathPart}IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 5, "SlotHighlightInner");
		public static UldBundle SlotHighlight         => new($"ui/uld/{ThemePathPart}IconA_Frame{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 16, "SlotHighlight");


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
		public static UldBundle CircleSmallMagnifyLense      => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 0,"CircleSmallMagnifyLense");
		public static UldBundle CircleSmallEdit              => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 1,"CircleSmallEdit");
		public static UldBundle CircleSmallViewport          => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 2,"CircleSmallViewport");
		public static UldBundle CircleSmallHat               => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 3,"CircleSmallHat");
		public static UldBundle CircleSmallWeapon            => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 4,"CircleSmallWeapon");
		public static UldBundle CircleSmallVisor             => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 5,"CircleSmallVisor");
		public static UldBundle CircleSmallDisplayGear       => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 6,"CircleSmallDisplayGear");
		public static UldBundle CircleSmallDyePot            => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 7,"CircleSmallDyePot");
		public static UldBundle CircleSmallEye               => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 8,"CircleSmallEye");
		public static UldBundle CircleSmallRollback          => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 9,"CircleSmallRollback");
		public static UldBundle CircleSmallSavePin           => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 10,"CircleSmallSavePin");
		public static UldBundle CircleLargeCog               => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 11,"CircleLargeCog");
		public static UldBundle CircleLargeFilter            => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 12,"CircleLargeFilter");
		public static UldBundle CircleLargeSort              => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 13,"CircleLargeSort");
		public static UldBundle CircleLargeQuestionMark      => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 14,"CircleLargeQuestionMark");
		public static UldBundle CircleLargeRefresh           => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 15,"CircleLargeRefresh");
		public static UldBundle CircleLargeBubble            => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 16,"CircleLargeBubble");
		public static UldBundle CircleLargeNote              => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 17,"CircleLargeNote");
		public static UldBundle CircleLargeEdit              => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 18,"CircleLargeEdit");
		public static UldBundle CircleLargePlus              => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 19,"CircleLargePlus");
		public static UldBundle CircleLargeRight             => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 20, "CircleLargeRight");
		public static UldBundle CircleLargeMusicNote         => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 21, "CircleLargeMusicNote");
		public static UldBundle CircleLargeSprout            => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 22,"CircleLargeSprout");
		public static UldBundle CircleLargeEye               => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 23,"CircleLargeEye");
		public static UldBundle CircleLargeLetterReceived    => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 24,"CircleLargeLetterReceived");
		public static UldBundle CircleLargeSoundOn           => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 25,"CircleLargeSoundOn");
		public static UldBundle CircleLargeSoundMute         => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 26,"CircleLargeSoundMute");
		public static UldBundle CircleLargeHeartbeat         => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 27,"CircleLargeHeartbeat");
		public static UldBundle CircleLargeCheckbox          => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 28, "CircleLargeCheckbox");
		public static UldBundle CircleLargeHighlightedCog    => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 29,"CircleLargeHighlightedCog");
		public static UldBundle CircleLargeHighlightedFilter => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 30,"CircleLargeHighlightedFilter");
		public static UldBundle CircleLargeRefresh2          => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 31,"CircleLargeRefresh2");
		public static UldBundle CircleLargeHighlight         => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 32,"CircleLargeHighlight");
		public static UldBundle CircleLargeExclamationMark   => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 33,"CircleLargeExclamationMark");
		public static UldBundle CircleLargeHighlightedNote   => new($"ui/uld/{ThemePathPart}CircleButtons{Storage.HighResolutionSufix}.tex", "ui/uld/Character.uld", 34,"CircleLargeHighlightedNote");

		// ULD: ui/uld/MiragePrismPlate.uld

		// handle: mirage_prism_box
		public static UldBundle MirageSlotItemCrossBlue  => new($"ui/uld/MiragePrismBoxIcon{Storage.HighResolutionSufix}.tex", "ui/uld/MiragePrismPlate.uld", 0,"MirageSlotItemCrossBlue"); //
		public static UldBundle MirageSlotItemCrossRed   => new($"ui/uld/MiragePrismBoxIcon{Storage.HighResolutionSufix}.tex", "ui/uld/MiragePrismPlate.uld", 1,"MirageSlotItemCrossRed"); //
		public static UldBundle MirageSlotNormal         => new($"ui/uld/MiragePrismBoxIcon{Storage.HighResolutionSufix}.tex", "ui/uld/MiragePrismPlate.uld", 2,"MirageSlotNormal"); // OK

		// handle: mirage_prism_plate2
		public static UldBundle MiragePlateRadio         => new($"ui/uld/MiragePrismPlate2{Storage.HighResolutionSufix}.tex", "ui/uld/MiragePrismPlate.uld", 4,"MiragePlateRadio"); // OK
		public static UldBundle MiragePlateRadioSelected => new($"ui/uld/MiragePrismPlate2{Storage.HighResolutionSufix}.tex", "ui/uld/MiragePrismPlate.uld", 5,"MiragePlateRadioSelected"); // 




		public static UldBundle RecipeNoteBook_1 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 1, "RecipeNoteBook_1");
		public static UldBundle RecipeNoteBook_2 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 2, "RecipeNoteBook_2");
		public static UldBundle RecipeNoteBook_3 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 3, "RecipeNoteBook_3");
		public static UldBundle RecipeNoteBook_4 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 4, "RecipeNoteBook_4");
		public static UldBundle RecipeNoteBook_5 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 5, "RecipeNoteBook_5");
		public static UldBundle RecipeNoteBook_6 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 6, "RecipeNoteBook_6");
		public static UldBundle RecipeNoteBook_7 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 7, "RecipeNoteBook_7");
		public static UldBundle RecipeNoteBook_8 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 8, "RecipeNoteBook_8");
		public static UldBundle RecipeNoteBook_9 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 9, "RecipeNoteBook_9");
		public static UldBundle RecipeNoteBook_10 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 10, "RecipeNoteBook_10");
		public static UldBundle RecipeNoteBook_11 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 11, "RecipeNoteBook_11");
		public static UldBundle RecipeNoteBook_ObtainedCheckMark => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 12, "RecipeNoteBook_ObtainedCheckMark");
		public static UldBundle RecipeNoteBook_13 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 13, "RecipeNoteBook_13");
		public static UldBundle RecipeNoteBook_14 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 14, "RecipeNoteBook_14");
		public static UldBundle RecipeNoteBook_15 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 15, "RecipeNoteBook_15");
		public static UldBundle RecipeNoteBook_16 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 16, "RecipeNoteBook_16");
		public static UldBundle RecipeNoteBook_17 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 17, "RecipeNoteBook_17");
		public static UldBundle RecipeNoteBook_18 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 18, "RecipeNoteBook_18");
		public static UldBundle RecipeNoteBook_19 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 19, "RecipeNoteBook_19");
		public static UldBundle RecipeNoteBook_20 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 20, "RecipeNoteBook_20");
		public static UldBundle RecipeNoteBook_21 => new($"ui/uld/{ThemePathPart}RecipeNoteBook{Storage.HighResolutionSufix}.tex", "ui/uld/ItemDetail.uld", 21, "RecipeNoteBook_21");


	}
}
