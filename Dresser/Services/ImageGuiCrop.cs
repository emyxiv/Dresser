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

		public IDalamudTextureWrap GetPartOrEmpty(UldBundle uldBundle) => (IDalamudTextureWrap) GetPart(uldBundle);
		public IDalamudTextureWrap? GetPart(UldBundle uldBundle) {

			if (Textures.TryGetValue(uldBundle.GetHashCode(), out var texture)) {
				return texture;
			}

			if (Ulds.TryGetValue(uldBundle.Uld, out var uld)) {

				if (uld.Valid) {
					texture = uld.LoadTexturePart(uldBundle.Tex, uldBundle.Index);
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
			PluginLog.Warning($"creating {uldBundle.Tex} {uldBundle.Uld} {uldBundle.Index} {uldBundle.Handle}");

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
				ImGui.Text($"[{uldBundle.Index}]");
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
		public int Index; // index
		public string Handle;


		public UldBundle(string tex, string uld, int index, string handle) {
			Tex = tex;
			Uld = uld;
			Index = index;
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
			return (Tex, Uld, Part: Index).GetHashCode();
		}

		// this is blocked by UldWrapper.GetTexture(string texturePath)  only allowing tex in the list of Uld.AssetData
		// TODO maybe offer PR dalamud with (modified for regex)
		// near: texturePath = texturePath.Replace("_hr1", string.Empty);
		// add:  texturePath = texturePath.Replace("fourth/", string.Empty); // fix alt themes
		// private static string ThemePathPart
			// => "";
		//=> "dark/";
		//=> "light/";
		//=> "third/";
		//=> "fourth/";
		private static string BuildTexPath(string textureSubPath)
			=> "ui/uld/"
				// + ThemePathPart // todo this needs Dalamud UldWrapper update
				+ textureSubPath
				+ Storage.HighResolutionSufix
				+ ".tex";

		public static UldBundle ItemCapNormal         => new(BuildTexPath("IconA_Frame"), "ui/uld/Character.uld", 0, "ItemCapNormal"); // OK
		public static UldBundle ItemCapCrossBlue      => new(BuildTexPath("IconA_Frame"), "ui/uld/Character.uld", 2, "ItemCapCrossBlue");
		public static UldBundle ItemCapCrossRed       => new(BuildTexPath("IconA_Frame"), "ui/uld/Character.uld", 3, "ItemCapCrossRed");
		public static UldBundle ItemSlot              => new(BuildTexPath("IconA_Frame"), "ui/uld/Character.uld", 4, "ItemSlot");
		public static UldBundle SlotHighlightInner    => new(BuildTexPath("IconA_Frame"), "ui/uld/Character.uld", 5, "SlotHighlightInner");
		public static UldBundle SlotHighlight         => new(BuildTexPath("IconA_Frame"), "ui/uld/Character.uld", 16, "SlotHighlight");


		// handle: character
		// empty slot icons
		public static UldBundle MainHand  => new(BuildTexPath("Character"), "ui/uld/Character.uld", 17, "MainHand");
		public static UldBundle OffHand   => new(BuildTexPath("Character"), "ui/uld/Character.uld", 18, "OffHand");
		public static UldBundle Head      => new(BuildTexPath("Character"), "ui/uld/Character.uld", 19, "Head");
		public static UldBundle Body      => new(BuildTexPath("Character"), "ui/uld/Character.uld", 20, "Body");
		public static UldBundle Hands     => new(BuildTexPath("Character"), "ui/uld/Character.uld", 21, "Hands");
		public static UldBundle Legs      => new(BuildTexPath("Character"), "ui/uld/Character.uld", 23, "Legs");
		public static UldBundle Feet      => new(BuildTexPath("Character"), "ui/uld/Character.uld", 24, "Feet");
		public static UldBundle Ears      => new(BuildTexPath("Character"), "ui/uld/Character.uld", 25, "Ears");
		public static UldBundle Neck      => new(BuildTexPath("Character"), "ui/uld/Character.uld", 26, "Neck");
		public static UldBundle Wrists    => new(BuildTexPath("Character"), "ui/uld/Character.uld", 27, "Wrists");
		public static UldBundle RightRing => new(BuildTexPath("Character"), "ui/uld/Character.uld", 28, "RightRing");
		public static UldBundle LeftRing  => new(BuildTexPath("Character"), "ui/uld/Character.uld", 28, "LeftRing");
		public static UldBundle Character_Person  => new(BuildTexPath("Character"), "ui/uld/Character.uld", 7, "Character_Person");
		public static UldBundle ArmouryBoard_ChestPiece  => new(BuildTexPath("ArmouryBoard"), "ui/uld/ArmouryBoard.uld", 2, "ArmouryBoard_ChestPiece");


		// handle: circle_buttons_4
		public static UldBundle CircleSmallMagnifyLense      => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 0,"CircleSmallMagnifyLense");
		public static UldBundle CircleSmallEdit              => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 1,"CircleSmallEdit");
		public static UldBundle CircleSmallViewport          => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 2,"CircleSmallViewport");
		public static UldBundle CircleSmallHat               => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 3,"CircleSmallHat");
		public static UldBundle CircleSmallWeapon            => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 4,"CircleSmallWeapon");
		public static UldBundle CircleSmallVisor             => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 5,"CircleSmallVisor");
		public static UldBundle CircleSmallDisplayGear       => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 6,"CircleSmallDisplayGear");
		public static UldBundle CircleSmallDyePot            => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 7,"CircleSmallDyePot");
		public static UldBundle CircleSmallEye               => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 8,"CircleSmallEye");
		public static UldBundle CircleSmallRollback          => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 9,"CircleSmallRollback");
		public static UldBundle CircleSmallSavePin           => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 10,"CircleSmallSavePin");
		public static UldBundle CircleLargeCog               => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 11,"CircleLargeCog");
		public static UldBundle CircleLargeFilter            => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 12,"CircleLargeFilter");
		public static UldBundle CircleLargeSort              => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 13,"CircleLargeSort");
		public static UldBundle CircleLargeQuestionMark      => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 14,"CircleLargeQuestionMark");
		public static UldBundle CircleLargeRefresh           => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 15,"CircleLargeRefresh");
		public static UldBundle CircleLargeBubble            => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 16,"CircleLargeBubble");
		public static UldBundle CircleLargeNote              => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 17,"CircleLargeNote");
		public static UldBundle CircleLargeEdit              => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 18,"CircleLargeEdit");
		public static UldBundle CircleLargePlus              => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 19,"CircleLargePlus");
		public static UldBundle CircleLargeRight             => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 20, "CircleLargeRight");
		public static UldBundle CircleLargeMusicNote         => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 21, "CircleLargeMusicNote");
		public static UldBundle CircleLargeSprout            => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 22,"CircleLargeSprout");
		public static UldBundle CircleLargeEye               => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 23,"CircleLargeEye");
		public static UldBundle CircleLargeLetterReceived    => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 24,"CircleLargeLetterReceived");
		public static UldBundle CircleLargeSoundOn           => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 25,"CircleLargeSoundOn");
		public static UldBundle CircleLargeSoundMute         => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 26,"CircleLargeSoundMute");
		public static UldBundle CircleLargeHeartbeat         => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 27,"CircleLargeHeartbeat");
		public static UldBundle CircleLargeCheckbox          => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 28, "CircleLargeCheckbox");
		public static UldBundle CircleLargeHighlightedCog    => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 29,"CircleLargeHighlightedCog");
		public static UldBundle CircleLargeHighlightedFilter => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 30,"CircleLargeHighlightedFilter");
		public static UldBundle CircleLargeRefresh2          => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 31,"CircleLargeRefresh2");
		public static UldBundle CircleLargeHighlight         => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 32,"CircleLargeHighlight");
		public static UldBundle CircleLargeExclamationMark   => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 33,"CircleLargeExclamationMark");
		public static UldBundle CircleLargeHighlightedNote   => new(BuildTexPath("CircleButtons"), "ui/uld/Character.uld", 34,"CircleLargeHighlightedNote");

		// ULD: ui/uld/MiragePrismPlate.uld

		// handle: mirage_prism_box
		public static UldBundle MirageSlotItemCrossBlue  => new(BuildTexPath("MiragePrismBoxIcon"), "ui/uld/MiragePrismPlate.uld", 0,"MirageSlotItemCrossBlue"); //
		public static UldBundle MirageSlotItemCrossRed   => new(BuildTexPath("MiragePrismBoxIcon"), "ui/uld/MiragePrismPlate.uld", 1,"MirageSlotItemCrossRed"); //
		public static UldBundle MirageSlotNormal         => new(BuildTexPath("MiragePrismBoxIcon"), "ui/uld/MiragePrismPlate.uld", 2,"MirageSlotNormal"); // OK

		// handle: mirage_prism_plate2
		public static UldBundle MiragePlateRadio         => new(BuildTexPath("MiragePrismPlate2"), "ui/uld/MiragePrismPlate.uld", 4,"MiragePlateRadio"); // OK
		public static UldBundle MiragePlateRadioSelected => new(BuildTexPath("MiragePrismPlate2"), "ui/uld/MiragePrismPlate.uld", 5,"MiragePlateRadioSelected"); // 




		public static UldBundle RecipeNoteBook_ObtainedCheckMark => new(BuildTexPath("RecipeNoteBook"), "ui/uld/ItemDetail.uld", 0, "RecipeNoteBook_ObtainedCheckMark");


		public static UldBundle ColorChooser_ShadeColor => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 0, "ColorChooser_ShadeColor");
		public static UldBundle ColorChooser_ShadeOutline => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 1, "ColorChooser_ShadeOutline");
		public static UldBundle ColorChooser_ShadeHover => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 2, "ColorChooser_ShadeHover");
		public static UldBundle ColorChooser_StainColor => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 3, "ColorChooser_StainColor");
		public static UldBundle ColorChooser_StainOutline => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 4, "ColorChooser_StainOutline");
		public static UldBundle ColorChooser_StainHover => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 5, "ColorChooser_StainHover");
		public static UldBundle ColorChooser_NotOwned => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 6, "ColorChooser_NotOwned");
		public static UldBundle ColorChooser_Active => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 7, "ColorChooser_Active");
		public static UldBundle ColorChooser_ShadeMulticolor => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 8, "ColorChooser_ShadeMulticolor");
		public static UldBundle ColorChooser_StainMetallic => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 9, "ColorChooser_StainMetallic");
		public static UldBundle ColorChooser_StainActive => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 10, "ColorChooser_StainActive");
		public static UldBundle ColorChooser_StainNotStained => new(BuildTexPath("ListColorChooser"), "ui/uld/ColorantColoringSelector.uld", 11, "ColorChooser_StainNotStained");

		public static UldBundle ColorantButton_Switch1 => new(BuildTexPath("ColorantButton"), "ui/uld/ColorantColoringSelector.uld", 0, "ColorantButton_Switch1");
		public static UldBundle ColorantButton_Switch2 => new(BuildTexPath("ColorantButton"), "ui/uld/ColorantColoringSelector.uld", 1, "ColorantButton_Switch2");
		public static UldBundle ColorantButton_Swap => new(BuildTexPath("ColorantButton"), "ui/uld/ColorantColoringSelector.uld", 2, "ColorantButton_Swap");
		// public static UldBundle ColorantButton_Swap => new(BuildTexPath("ColorantButton")}", "uuld/ColorantColoringSelector.uld", 2, "ColorantButton_Swap");
		public static UldBundle ColorantButton_Redo => new(BuildTexPath("ColorantButton"), "ui/uld/ColorantColoringSelector.uld", 3, "ColorantButton_Redo");
		public static UldBundle ColorantButton_Undo => new(BuildTexPath("ColorantButton"), "ui/uld/ColorantColoringSelector.uld", 4, "ColorantButton_Undo");

		public static UldBundle ColorantToggleButton_DyeIndicatorInactive => new(BuildTexPath("ToggleButton"), "ui/uld/ColorantColoringSelector.uld", 16, "ColorantToggleButton_DyeIndicatorInactive");
		public static UldBundle ColorantToggleButton_DyeIndicatorActive => new(BuildTexPath("ToggleButton"), "ui/uld/ColorantColoringSelector.uld", 17, "ColorantToggleButton_DyeIndicatorActive");
		public static UldBundle ColorantToggleButton_IntoDye => new(BuildTexPath("ToggleButton"), "ui/uld/ColorantColoringSelector.uld", 18, "ColorantToggleButton_IntoDye");
		public static UldBundle MiragePrismBox_Heart => new(BuildTexPath("ItemSearch"), "ui/uld/MiragePrismBox.uld", 10, "MiragePrismBox_Heart");

		public static UldBundle Buddy_HighlightSmall => new(BuildTexPath("BgParts"), "ui/uld/Buddy.uld", 0, "Buddy_Highlight");
		public static UldBundle Buddy_CapSmall => new(BuildTexPath("BgParts"), "ui/uld/Buddy.uld", 1, "Buddy_Highlight");
		public static UldBundle Buddy_Highlight => new(BuildTexPath("BgParts"), "ui/uld/Buddy.uld", 2, "Buddy_Highlight");
		public static UldBundle Buddy_CapGloss => new(BuildTexPath("BgParts"), "ui/uld/Buddy.uld", 3, "Buddy_CapGloss");
		public static UldBundle Buddy_Cap => new(BuildTexPath("BgParts"), "ui/uld/Buddy.uld", 8, "Buddy_Cap");
		public static UldBundle Buddy_Slot => new(BuildTexPath("BgParts"), "ui/uld/Buddy.uld", 5, "Buddy_Slot");
	}
}
