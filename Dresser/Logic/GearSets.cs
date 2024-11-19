using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using CriticalCommonLib;

using Dalamud.Interface.Textures;

using Dresser.Structs;
using Dresser.Windows.Components;

using ImGuiNET;

using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule;

namespace Dresser.Logic {
	internal class GearSets {

		public static Dictionary<ushort, List<GearsetEntry>> PlateLinks = new();

		//HasLinkedGlamourPlate
		//GetClassJobIconForGearset

		public static unsafe void FetchGearSets() {
			PlateLinks.Clear();
			for (var i = 0; i < 100; i++) {
				var gearset = *(Instance()->GetGearset(i));
				if (!gearset.Flags.HasFlag(GearsetFlag.Exists)) continue;

				var glamLink = gearset.GlamourSetLink;
				if (glamLink == 0) continue;
				glamLink--;
				if (!PlateLinks.TryGetValue(glamLink, out var plateLink)) {
					PlateLinks[glamLink] = new();
					plateLink = PlateLinks[glamLink];
				}
				plateLink.Add(gearset);
			}
		}
		private static List<GearsetEntry> RelatedGearSets(ushort plateNumber) {
			if (PlateLinks.TryGetValue(plateNumber, out var plateLink))
				return plateLink;
			else
				return new();
		}

		public unsafe static IEnumerable<string> RelatedGearSetNames(ushort plateNumber)
			=> RelatedGearSets(plateNumber).Select(g => {
				
				var name = g.NameString;
				var id = g.Id + 1;
				var ilvl = g.ItemLevel;
				//{ (char)0xE033}
				return $"{id}. {name} {ilvl}";
			});
		public unsafe static IEnumerable<byte> RelatedGearSetClassJob(ushort plateNumber)
			=> RelatedGearSets(plateNumber).Select(g => g.ClassJob);
		public unsafe static uint? GetClassJobIconForPlate(ushort plateNumber) {
			var gearsets = RelatedGearSets(plateNumber);
			if(gearsets.Count == 0) return null;
			var gearset = gearsets.First();
			return (uint)Instance()->GetClassJobIconForGearset(gearset.Id);
		}
		public static ClassJobRole? RelatedGearSetRole(ushort plateNumber) {
			var gearsets = RelatedGearSetClassJob(plateNumber);
			if (gearsets.Count() == 0) return null;
			var classjobByte = gearsets.First();
			var classJob = Service.ExcelCache.GetClassJobSheet().GetRow(classjobByte);
			if (classJob == null) return null;
			return (ClassJobRole)classJob.Base.Role;
		}
		public static Vector4? RelatedGearSetClassJobCategoryColor(ushort plateNumber)
			=> RelatedGearSetRole(plateNumber) switch {
				ClassJobRole.Tank => new(new Vector3(42, 93, 149) / 255, 1),
				ClassJobRole.Healer => new(new Vector3(67, 149, 84) / 255, 1),
				ClassJobRole.RangeDps => new(new Vector3(135,30,35) / 255, 1),
				ClassJobRole.MeleeDps => new(new Vector3(135,30,35) / 255, 1),
				_ => null
			};

		public static ISharedImmediateTexture GetClassJobIconTextureForPlate(ushort plateNumber) {
			var iconId = GetClassJobIconForPlate(plateNumber);
			if(!iconId.HasValue) return null;
			return IconWrapper.Get(iconId.Value);
		}
		public static bool TryGetClassJobIconTextureForPlate(ushort plateNumber, out ISharedImmediateTexture icon) {
			icon = GetClassJobIconTextureForPlate(plateNumber);
			return icon != null;
		}


		public unsafe static void RelatedGearSetNamesImgui(ushort plateNumber) {
			var gearsets = RelatedGearSets(plateNumber);
			if (gearsets.Count == 0) {
				ImGui.TextDisabled("Not linked to gearset");
				return;
			}
			ImGui.TextDisabled("Gearsets");
			foreach (var g in gearsets) {
				var name = g.NameString;
				var id = g.Id + 1;
				var ilvl = g.ItemLevel;
				//ImGui.Bullet();
				ImGui.Text(" • ");
				ImGui.SameLine();
				//ImGui.AlignTextToFramePadding();
				//GuiHelpers.TextWithFont($"{id}",GuiHelpers.Font.TrumpGothic_184);
				ImGui.Text($"{id}  ");
				ImGui.SameLine();
				ImGui.Text($"{name}  ");
				ImGui.SameLine();
				GuiHelpers.TextWithFont($"{(char)0xE033}",GuiHelpers.Font.BubblePlateNumber);
				ImGui.SameLine();
				ImGui.Text($"{ilvl}");
			}
		}

		public unsafe static ushort? CurrentGearsetToPlateNumber() {
			var ddd = Instance()->CurrentGearsetIndex;
			if (ddd >= 0) return null;
			if (!Instance()->IsValidGearset(ddd)) return null;
			var currentGearset = Instance()->GetGearset(ddd);

			var glamourPlateNumber = currentGearset->GlamourSetLink;

			return glamourPlateNumber;
		}

	}
}
