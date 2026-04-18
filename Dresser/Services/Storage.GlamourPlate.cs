using System.Collections.Generic;
using System.Linq;

using CriticalCommonLib.Enums;

using Dresser.Interop;
using Dresser.Interop.Hooks;
using Dresser.Models;


namespace Dresser.Services {
	internal partial class Storage {

		public const int PlateNumber = Offsets.TotalPlates;
		public Dictionary<GlamourPlateSlot, FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData> SlotMirageItems = new();
		public FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData.GlamourPlate[]? Pages = null;
		public FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlateData.GlamourPlate? DisplayPage = null;

		public static Dictionary<ushort, InventoryItemSet> PagesInv {
			get => PluginServices.Storage.Pages?.Select((value, index) => new { value, index }).ToDictionary(p => (ushort)p.index, p => (InventoryItemSet)p.value) ?? new();
		}
	}
}
