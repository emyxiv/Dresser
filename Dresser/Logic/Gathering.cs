using CriticalCommonLib.Enums;

using Dresser.Extensions;
using Dresser.Services;
using Dresser.Structs.Dresser;

using System.Linq;
using System.Threading.Tasks;

namespace Dresser.Logic {

	internal static class Gathering {
		public static void Init() {
			ParseGlamourPlates();
		}
		public static void ParseGlamourPlates() {
			var tempPages = GetDataFromDresser();
			if (tempPages == null) return;
			PluginServices.Storage.Pages = GetDataFromDresser();
			PluginServices.Storage.DisplayPage = PluginServices.Storage.Pages?.Last();
			if (PluginServices.Storage.DisplayPage == null) return;
			ConfigurationManager.Config.DisplayPlateItems = (InventoryItemSet)(MiragePage)PluginServices.Storage.DisplayPage;
		}
		public static InventoryItemSet EmptyGlamourPlate() {
			return new() {
				Items = PluginServices.Storage.SlotMirageItems.ToDictionary(p => p.Key, p =>
				(InventoryItem?)EmptyItemSlot()
			)
			};
		}
		public static InventoryItem EmptyItemSlot() => new InventoryItem(InventoryType.GlamourChest, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		public static void DelayParseGlamPlates()
			=> Task.Run(async delegate {
				await Task.Delay(250);
				ParseGlamourPlates();
			});
		public static void DelayParseGlamPlatesAndComparePending()
			=> Task.Run(async delegate {
				await Task.Delay(250);
				ParseGlamourPlates();
				PluginServices.ApplyGearChange.CheckModificationsOnPendingPlates();
			});
		private unsafe static MiragePage[]? GetDataFromDresser() {
			var agent = MiragePrismMiragePlate.MiragePlateAgent();
			if (agent == null) return null;
			var miragePlates = (MiragePrismMiragePlate*)agent;
			if (!miragePlates->AgentInterface.IsAgentActive()) return null;

			return miragePlates->Pages;
		}
		public unsafe static bool VerifyItem(ushort plateNumber, GlamourPlateSlot slot, InventoryItem item) {
			var agent = MiragePrismMiragePlate.MiragePlateAgent();
			if (agent == null) return false;
			var miragePlates = (MiragePrismMiragePlate*)agent;
			if (!miragePlates->AgentInterface.IsAgentActive()) return false;

			return miragePlates->VerifyItem(plateNumber, slot, item);
		}
		public static bool IsApplied(InventoryItem item) {

			// Todo: avoid getting everything each time for performance purposes
			ParseGlamourPlates();

			var slot = item.Item.GlamourPlateSlot();
			if (slot == null) return false;
			var storedItem = ConfigurationManager.Config.DisplayPlateItems.GetSlot((GlamourPlateSlot)slot);
			if ((storedItem?.ItemId ?? 0) == (item?.ItemId ?? 0) && (storedItem?.Stain ?? 0) == (item?.Stain ?? 0))
				return true;
			return false;
		}
	}
}
