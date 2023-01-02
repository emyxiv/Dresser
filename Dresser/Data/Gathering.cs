using CriticalCommonLib;

using Dalamud.Logging;

using Dresser.Structs.FFXIV;
using Dresser.Windows.Components;

using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

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
			Storage.SlotItemsEx = Storage.SlotMirageItems.ToDictionary(p => p.Key, p => Service.ExcelCache.GetItemExSheet().FirstOrDefault(i => i.RowId == p.Value.ItemId))!;
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
