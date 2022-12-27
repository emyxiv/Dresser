using Dalamud.Logging;

using FabulousDresser.Structs.FFXIV;

using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FabulousDresser.Windows.Components {
	internal class Plates {
		public const int PlateNumber = 20;

		public static void Draw() {
			PluginLog.Debug($"sqdqsd");
			var plates = GetDataFromDresser();
			if(plates == null) return;

			var showingPlate = plates.Last();
			DrawDisplay(showingPlate);
		}
		private static void DrawDisplay(MiragePage plate) {
			var fields = typeof(MiragePage).GetFields();
			for (int slot = 0; slot < fields.Length; slot++) {
				MirageItem item = (MirageItem)fields[slot].GetValue(plate)!;

				var glamSlot = (GlamourPlateSlot)slot;
				ImGui.Text($"{glamSlot} {item.ItemType} {item.ItemId}");

			}
		}
		internal unsafe static MiragePage[]? GetDataFromDresser() {
			var agent = MiragePrismMiragePlate.MiragePlateAgent();
			if (agent == null) return null;
			var miragePlates = (MiragePrismMiragePlate*)agent;
			if (!miragePlates->AgentInterface.IsAgentActive()) return null;

			return miragePlates->Pages;
		}
	}
}
