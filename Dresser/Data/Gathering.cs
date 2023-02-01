using System.Linq;
using System.Threading.Tasks;

using CriticalCommonLib.Enums;
using CriticalCommonLib.Models;

using Dresser.Extensions;
using Dresser.Structs.FFXIV;

namespace Dresser.Data {

	internal class Gathering {
		public static void Init() {
			ParseGlamourPlates();
		}
		public static void ParseGlamourPlates() {
			Storage.Pages = GetDataFromDresser();
			Storage.DisplayPage = Storage.Pages?.Last();
			if (Storage.DisplayPage == null) return;
			Storage.SlotMirageItems = Storage.DisplayPage.Value.ToDictionary();
			Configuration.SlotInventoryItems = Storage.SlotMirageItems.ToDictionary(p => p.Key, p => 
				new InventoryItem(InventoryType.GlamourChest, (short)p.Key, p.Value.ItemId, 1, 0, 0,
					0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, p.Value.DyeId, 0)
			)!;
		}
		public static void DelayParseGlamPlates()
			=> Task.Run(async delegate {
				await Task.Delay(250);
				ParseGlamourPlates();
			});
		private unsafe static MiragePage[]? GetDataFromDresser() {
			var agent = MiragePrismMiragePlate.MiragePlateAgent();
			if (agent == null) return null;
			var miragePlates = (MiragePrismMiragePlate*)agent;
			if (!miragePlates->AgentInterface.IsAgentActive()) return null;

			return miragePlates->Pages;
		}
		public static bool IsApplied(InventoryItem item) {
			ParseGlamourPlates();

			var slot = item.Item.GlamourPlateSlot();
			if (slot == null) return false;
			if (Configuration.SlotInventoryItems.TryGetValue((GlamourPlateSlot)slot, out var storedItem)) {
				if (storedItem.ItemId == item.ItemId && storedItem.Stain == item.Stain)
					return true;
			}
			return false;
		}
	}
}
