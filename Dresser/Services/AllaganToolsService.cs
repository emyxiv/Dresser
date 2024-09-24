using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using Dresser.Logic;
using Dresser.Windows;

using FFXIVClientStructs.FFXIV.Client.Game;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Dresser.Services {
	internal class AllaganToolsService : IDisposable {

		private readonly ICallGateSubscriber<ulong, HashSet<ulong[]>> _getCharacterItems;
		private readonly ICallGateSubscriber<bool, HashSet<ulong>> _getCharactersOwnedByActive;
		private readonly ICallGateSubscriber<bool> _isInitialized;
		private readonly ICallGateSubscriber<ulong?, bool> _retainerChanged;
		private readonly ICallGateSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool> _itemAdded;
		private readonly ICallGateSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool> _itemRemoved;

        private readonly IDalamudPluginInterface _pluginInterface;


		public AllaganToolsService(IDalamudPluginInterface pluginInterface) {
            _pluginInterface = pluginInterface;

			_getCharacterItems       = pluginInterface.GetIpcSubscriber<ulong, HashSet<ulong[]>>("AllaganTools.GetCharacterItems");
			_getCharactersOwnedByActive = pluginInterface.GetIpcSubscriber<bool, HashSet<ulong>>("AllaganTools.GetCharactersOwnedByActive");
			_isInitialized           = pluginInterface.GetIpcSubscriber<bool>("AllaganTools.IsInitialized");

			//Events
			_retainerChanged = pluginInterface.GetIpcSubscriber<ulong?, bool>("AllaganTools.RetainerChanged");
			_retainerChanged.Subscribe(RetainerChanged);
			_itemAdded       = pluginInterface.GetIpcSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>("AllaganTools.ItemAdded");
			_itemAdded.Subscribe(ItemAdded);
			_itemRemoved     = pluginInterface.GetIpcSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>("AllaganTools.ItemRemoved");
			_itemRemoved.Subscribe(ItemRemoved);

		}

		public HashSet<ulong[]> GetCharacterItemsSerialized(ulong characterId)       { try { return _getCharacterItems.       InvokeFunc(characterId                 ); } catch (Exception e){ PluginLog.Error(e, "Error on GetCharacterItems1"); return new(); }}
		public HashSet<ulong> GetCharactersOwnedByActive(bool includeOwner)          { try { return _getCharactersOwnedByActive.InvokeFunc(includeOwner              ); } catch (Exception e){ PluginLog.Error(e, "Error on GetCharactersOwnedByActive"); return new(); }}
		public bool IsInitialized() {
            try {
                return _pluginInterface.InstalledPlugins.Any(x => x.Name == "AllaganTools" && x.IsLoaded);
                // return _isInitialized.InvokeFunc();
            } catch(Exception e) { PluginLog.Error(e, "Error on IsInitialized"); return false; }
        }

		public IEnumerable<CriticalCommonLib.Models.InventoryItem> GetCharacterItems(ulong characterId)
			=> GetCharacterItemsSerialized(characterId).Select(CriticalCommonLib.Models.InventoryItem.FromNumeric);
		public IEnumerable<Structs.Dresser.InventoryItem> GetItems(ulong characterId)
			=> GetCharacterItems(characterId).Select(Structs.Dresser.InventoryItem.FromCritical);
		public Dictionary<ulong, IEnumerable<Structs.Dresser.InventoryItem>> GetItemsLocalCharsRetainers(bool includeActiveCharacter = false)
			=> GetCharactersOwnedByActive(includeActiveCharacter).ToDictionary(chId => chId, GetItems);

		private void RetainerChanged(ulong? a1) {
			if(Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
		}
		private void ItemRemoved((uint, InventoryItem.ItemFlags, ulong, uint) a1) {
			if (Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
		}
		private void ItemAdded((uint, InventoryItem.ItemFlags, ulong, uint) a1) {
			if (Plugin.GetInstance().GearBrowser.IsOpen) GearBrowser.RecomputeItems();
		}

		public void Dispose() {
			_retainerChanged.Unsubscribe(RetainerChanged);
			_itemAdded.Unsubscribe(ItemAdded);
			_itemRemoved.Unsubscribe(ItemRemoved);
		}
	}
}
