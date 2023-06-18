using CsvHelper;

using Dalamud.Game.ClientState.JobGauge.Enums;

using Dresser.Structs.Dresser;
using Dresser.Windows.Components;

using FFXIVClientStructs.FFXIV.Client.UI.Misc;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule;

namespace Dresser.Logic {
	internal class GearSets {

		public static Dictionary<ushort, List<GearsetEntry>> PlateLinks = new();

		//HasLinkedGlamourPlate
		//GetClassJobIconForGearset

		public static unsafe void FetchGearSets() {
			PlateLinks.Clear();
			var gearsets = Instance()->Gearset;
			for (var i = 0; i < 100; i++) {
				var gearset = *gearsets[i];
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
				
				var nameSeString = Dalamud.Game.Text.SeStringHandling.SeString.Parse(g.Name, 47);
				var name = nameSeString.TextValue;
				var id = g.ID + 1;
				var ilvl = g.ItemLevel;
				//{ (char)0xE033}
				return $"{id}. {name} {ilvl}";
			});
		public unsafe static void RelatedGearSetNamesImgui(ushort plateNumber) {
			var gearsets = RelatedGearSets(plateNumber);
			if (gearsets.Count == 0) {
				ImGui.TextDisabled("Not linked to gearset");
				return;
			}
			ImGui.TextDisabled("Gearsets");
			foreach (var g in gearsets) {
				var name = Dalamud.Game.Text.SeStringHandling.SeString.Parse(g.Name, 47).TextValue;
				var id = g.ID + 1;
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
				GuiHelpers.TextWithFont($"{(char)0xE033}",GuiHelpers.Font.Axis_12);
				ImGui.SameLine();
				ImGui.Text($"{ilvl}");
			}
		}

	}
}
