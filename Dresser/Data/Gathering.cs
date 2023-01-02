using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;

using Dalamud.Logging;

using Dresser.Structs.FFXIV;
using Dresser.Windows.Components;


using ImGuiNET;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Reflection.Metadata.BlobBuilder;


namespace Dresser.Data {

	internal class Gathering {
		public static void Init() {
			ParseGlamourPlates();


			//ParseGlamourChest();
		}
		public static void ParseGlamourPlates() {
			Storage.Pages = GetDataFromDresser();
			Storage.DisplayPage = Storage.Pages?.Last();
			if (Storage.DisplayPage == null) return;
			Storage.SlotMirageItems = Storage.DisplayPage.Value.ToDictionary();
			Storage.SlotInventoryItems = Storage.SlotMirageItems.ToDictionary(p => p.Key, p => 
				new InventoryItem(InventoryType.GlamourChest, (short)p.Key, p.Value.ItemId, 1, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, p.Value.DyeId, 0)
			)!;
		}
		private unsafe static MiragePage[]? GetDataFromDresser() {
			var agent = MiragePrismMiragePlate.MiragePlateAgent();
			if (agent == null) return null;
			var miragePlates = (MiragePrismMiragePlate*)agent;
			if (!miragePlates->AgentInterface.IsAgentActive()) return null;

			return miragePlates->Pages;
		}
	}
}
