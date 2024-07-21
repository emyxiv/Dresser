using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;

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
						Textures.Add(uldBundle.GetHashCode(), texture);
						return texture;
					}
				}
			} else {
				// if uld not found, make it
				MakeUld(uldBundle);
			}
			PluginLog.Warning($"Unable to load Uld texture {uldBundle.Handle}");
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

			foreach (var uldBundle in parts) {
				ImGui.Text($"[{uldBundle.Uld}]       [{uldBundle.Tex}]      [{uldBundle.Part}]");
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
		private static string ThemePathPart
			=> "";
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


	}
}
