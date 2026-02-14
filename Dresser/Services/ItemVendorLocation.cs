using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Textures;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Utility;

using Dresser.Logic;
using Dresser.Structs.Dresser;
using Dresser.Windows.Components;

using Dalamud.Bindings.ImGui;

using Lumina.Excel.Sheets;
using Lumina.Extensions;

namespace Dresser.Services {
    public class ItemVendorLocation : IDisposable {
        private readonly IDalamudPluginInterface _pluginInterface;

	    private readonly ICallGateSubscriber<uint, bool, HashSet<(uint npcId, uint territory, (float x, float y) coordinates)>?> _getItemInfoProvider;
	    private readonly ICallGateSubscriber<uint, object?> _openUiWithItemId;


		public ItemVendorLocation(IDalamudPluginInterface pluginInterface) {
            _pluginInterface = pluginInterface;

			_getItemInfoProvider = pluginInterface.GetIpcSubscriber<uint, bool, HashSet<(uint npcId, uint territory, (float x, float y) coordinates)>?>("ItemVendorLocation.GetItemVendors");
			_openUiWithItemId    = pluginInterface.GetIpcSubscriber<uint, object?>("ItemVendorLocation.OpenVendorResults");

		}

		public bool IsInitialized() {
            try {
                return _pluginInterface.InstalledPlugins.Any(x => x.Name == "Item Vendor Location" && x.IsLoaded);
                // return _isInitialized.InvokeFunc();
            } catch(Exception e) { PluginLog.Error(e, "Error on IsInitialized"); return false; }
        }
		public IEnumerable<ItemProviderInfo>? GetItemInfoProvider(uint itemId) {

			HashSet<(uint npcId, uint territory, (float x, float y) coordinates)>? zz = null;
			try {
				PluginServices.Framework.RunOnFrameworkThread(() => {
					zz = _getItemInfoProvider.InvokeFunc(itemId, false);
				});
				PluginLog.Information($"Item vendor location found for ({itemId} {itemId:x8}) : {zz?.Count ?? 0}");

			}
			catch (Exception e) {
				PluginLog.Error(e, "Error on GetItemInfoProvider");
				return null;
			}
			return zz?.Select(i => new ItemProviderInfo(i.npcId, i.territory, i.coordinates));
		}
		public bool HasItemInfoProvider(uint itemId) {
			try {
				return _getItemInfoProvider.InvokeFunc(itemId, false)?.Count > 0;
			} catch (Exception e){ PluginLog.Error(e, "Error on HasItemInfoProvider"); return false; }
		}

		public void OpenUiWithItemId(uint itemId) { try { _openUiWithItemId.InvokeFunc(itemId); } catch (Exception e){ PluginLog.Error(e, "Error on OpenUiWithItemId"); }}

		public void Dispose() { }
	}

	public class ItemProviderInfo {
		public uint NpcId;
		public uint TerritoryId;
		public Vector2 Coordinates;
		public ItemProviderInfo(uint npcId, uint territory, (float x, float y) coordinates) {
			NpcId = npcId;
			TerritoryId = territory;
			Coordinates = new Vector2(coordinates.x, coordinates.y);
		}


		public string? GetNpcName() {
			var npc = PluginServices.DataManager.GetExcelSheet<ENpcResident>().FirstOrNull(n=>n.RowId == NpcId);
			if (npc == null) return null;
			return npc?.Singular.ToString();
		}
		public string? GetPlaceName() {
			var territory = PluginServices.DataManager.GetExcelSheet<TerritoryType>().FirstOrNull(t=>t.RowId == TerritoryId);
			return territory?.PlaceName.Value.Name.ToString();
		}
	}
}