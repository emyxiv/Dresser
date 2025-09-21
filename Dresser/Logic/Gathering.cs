using CriticalCommonLib.Enums;

using Dresser.Extensions;
using Dresser.Interop;
using Dresser.Interop.Hooks;
using Dresser.Services;
using Dresser.Structs.Dresser;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using System.Linq;
using System.Threading.Tasks;

using AgentMiragePrismMiragePlateData = FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData;

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
			ConfigurationManager.Config.DisplayPlateItems = (InventoryItemSet)(FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData.GlamourPlate)PluginServices.Storage.DisplayPage;
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
		private unsafe static AgentMiragePrismMiragePlateData.GlamourPlate[]? GetDataFromDresser() {
			var agent = AgentMiragePrismMiragePlate.Instance();
			if (agent == null) return null;
			if (!agent->IsAgentActive()) return null;

			var data = *(AgentMiragePrismMiragePlateData**)((nint)agent + Offsets.HeadSize);
			if (data == null) return null;


			return data->GlamourPlates.ToArray();
		}
		public static bool IsApplied(InventoryItem item) {

			// Todo: avoid getting everything each time for performance purposes
			ParseGlamourPlates();

			var slot = item.Item.GlamourPlateSlot();
			if (slot == null) return false;
			var storedItem = ConfigurationManager.Config.DisplayPlateItems.GetSlot((GlamourPlateSlot)slot);
			//if ((storedItem?.ItemId ?? 0) == (item?.ItemId ?? 0) && (storedItem?.Stain ?? 0) == (item?.Stain ?? 0))
			if ((storedItem?.ItemId ?? 0) == (item?.ItemId ?? 0) && (storedItem?.Stain ?? 0) == (item?.Stain ?? 0) && (storedItem?.Stain2 ?? 0) == (item?.Stain2 ?? 0))
				return true;
			return false;
		}
	}
}
