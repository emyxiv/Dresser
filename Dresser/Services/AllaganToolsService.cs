using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using Dresser.Logic;

using FFXIVClientStructs.FFXIV.Client.Game;

using System;
using System.Collections.Generic;

namespace Dresser.Services {
	internal class AllaganToolsService : IDisposable {

		private readonly ICallGateSubscriber<uint, ulong?, uint> _inventoryCountByType;
		private readonly ICallGateSubscriber<uint[], ulong?, uint> _inventoryCountByTypes;
		private readonly ICallGateSubscriber<uint, ulong, int, uint> _itemCount;
		private readonly ICallGateSubscriber<uint, ulong, int, uint> _itemCountHQ;
		private readonly ICallGateSubscriber<uint, bool, uint[], uint> _itemCountOwned;
		private readonly ICallGateSubscriber<string, bool> _enableUiFilter;
		private readonly ICallGateSubscriber<bool> _disableUiFilter;
		private readonly ICallGateSubscriber<string, bool> _toggleUiFilter;
		private readonly ICallGateSubscriber<string, bool> _enableBackgroundFilter;
		private readonly ICallGateSubscriber<bool> _disableBackgroundFilter;
		private readonly ICallGateSubscriber<string, bool> _toggleBackgroundFilter;
		private readonly ICallGateSubscriber<string, bool> _enableCraftList;
		private readonly ICallGateSubscriber<bool> _disableCraftList;
		private readonly ICallGateSubscriber<string, bool> _toggleCraftList;
		private readonly ICallGateSubscriber<string, uint, uint, bool> _addItemToCraftList;
		private readonly ICallGateSubscriber<string, uint, uint, bool> _removeItemFromCraftList;
		private readonly ICallGateSubscriber<string, Dictionary<uint, uint>> _getFilterItems;
		private readonly ICallGateSubscriber<string, Dictionary<uint, uint>> _getCraftItems;
		private readonly ICallGateSubscriber<Dictionary<string, string>> _getCraftLists;
		private readonly ICallGateSubscriber<string, Dictionary<uint, uint>, string> _addNewCraftList;
		private readonly ICallGateSubscriber<ulong> _currentCharacter;
		private readonly ICallGateSubscriber<bool> _isInitialized;
		private readonly ICallGateSubscriber<ulong?, bool> _retainerChanged;
		private readonly ICallGateSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool> _itemAdded;
		private readonly ICallGateSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool> _itemRemoved;
		private readonly ICallGateSubscriber<bool, bool> _initialized;

		public AllaganToolsService(DalamudPluginInterface pluginInterface) {
			_inventoryCountByType    = pluginInterface.GetIpcSubscriber<uint, ulong?, uint>("AllaganTools.InventoryCountByType");
			_inventoryCountByTypes   = pluginInterface.GetIpcSubscriber<uint[], ulong?, uint>("AllaganTools.InventoryCountByTypes");
			_itemCount               = pluginInterface.GetIpcSubscriber<uint, ulong, int, uint>("AllaganTools.ItemCount");
			_itemCountHQ             = pluginInterface.GetIpcSubscriber<uint, ulong, int, uint>("AllaganTools.ItemCountHQ");
			_itemCountOwned          = pluginInterface.GetIpcSubscriber<uint, bool, uint[], uint>("AllaganTools.ItemCountOwned");
			_enableUiFilter          = pluginInterface.GetIpcSubscriber<string, bool>("AllaganTools.EnableUiFilter");
			_disableUiFilter         = pluginInterface.GetIpcSubscriber<bool>("AllaganTools.DisableUiFilter");
			_toggleUiFilter          = pluginInterface.GetIpcSubscriber<string, bool>("AllaganTools.ToggleUiFilter");
			_enableBackgroundFilter  = pluginInterface.GetIpcSubscriber<string, bool>("AllaganTools.EnableBackgroundFilter");
			_disableBackgroundFilter = pluginInterface.GetIpcSubscriber<bool>("AllaganTools.DisableBackgroundFilter");
			_toggleBackgroundFilter  = pluginInterface.GetIpcSubscriber<string, bool>("AllaganTools.ToggleBackgroundFilter");
			_enableCraftList         = pluginInterface.GetIpcSubscriber<string, bool>("AllaganTools.EnableCraftList");
			_disableCraftList        = pluginInterface.GetIpcSubscriber<bool>("AllaganTools.DisableCraftList");
			_toggleCraftList         = pluginInterface.GetIpcSubscriber<string, bool>("AllaganTools.ToggleCraftList");
			_addItemToCraftList      = pluginInterface.GetIpcSubscriber<string, uint, uint, bool>("AllaganTools.AddItemToCraftList");
			_removeItemFromCraftList = pluginInterface.GetIpcSubscriber<string, uint, uint, bool>("AllaganTools.RemoveItemFromCraftList");
			_getFilterItems          = pluginInterface.GetIpcSubscriber<string, Dictionary<uint, uint>>("AllaganTools.GetFilterItems");
			_getCraftItems           = pluginInterface.GetIpcSubscriber<string, Dictionary<uint, uint>>("AllaganTools.GetCraftItems");
			_getCraftLists           = pluginInterface.GetIpcSubscriber<Dictionary<string, string>>("AllaganTools.GetCraftLists");
			_addNewCraftList         = pluginInterface.GetIpcSubscriber<string, Dictionary<uint, uint>, string>("AllaganTools.AddNewCraftList");
			_currentCharacter        = pluginInterface.GetIpcSubscriber<ulong>("AllaganTools.CurrentCharacter");
			_isInitialized           = pluginInterface.GetIpcSubscriber<bool>("AllaganTools.IsInitialized");

			//Events
			_retainerChanged = pluginInterface.GetIpcSubscriber<ulong?, bool>("AllaganTools.RetainerChanged");
			_retainerChanged.Subscribe(RetainerChanged);
			_itemAdded       = pluginInterface.GetIpcSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>("AllaganTools.ItemAdded");
			_itemAdded.Subscribe(ItemAdded);
			_itemRemoved     = pluginInterface.GetIpcSubscriber<(uint, InventoryItem.ItemFlags, ulong, uint), bool>("AllaganTools.ItemRemoved");
			_itemRemoved.Subscribe(ItemRemoved);
			_initialized     = pluginInterface.GetIpcSubscriber<bool, bool>("AllaganTools.Initialized");
			_initialized.Subscribe(Initialized);


		}


		//private readonly EventSubscriber<ModSettingChange, string, string, bool> _modSettingChanged;
		//private ActionSubscriber<int, RedrawType> _redrawSubscriber;
		//private FuncSubscriber<nint, (nint, string)> _drawObjectInfo;

		public uint InventoryCountByType(uint inventoryType, ulong? characterId) => _inventoryCountByType.InvokeFunc(inventoryType, characterId);
		public uint InventoryCountByTypes(uint[] inventoryTypes, ulong? characterId) => _inventoryCountByTypes.InvokeFunc(inventoryTypes, characterId);

		public uint ItemCount(uint a1, ulong a2, int a3) => _itemCount.InvokeFunc(a1, a2, a3);
		public uint ItemCountHQ(uint a1, ulong a2, int a3) => _itemCountHQ.InvokeFunc(a1, a2, a3);
		public uint ItemCountOwned(uint a1, bool a2, uint[] a3) => _itemCountOwned.InvokeFunc(a1, a2, a3);
		public bool EnableUiFilter(string a1) => _enableUiFilter.InvokeFunc(a1);
		public bool DisableUiFilter() => _disableUiFilter.InvokeFunc();
		public bool ToggleUiFilter(string a1) => _toggleUiFilter.InvokeFunc(a1);
		public bool EnableBackgroundFilter(string a1) => _enableBackgroundFilter.InvokeFunc(a1);
		public bool DisableBackgroundFilter() => _disableBackgroundFilter.InvokeFunc();
		public bool ToggleBackgroundFilter(string a1) => _toggleBackgroundFilter.InvokeFunc(a1);
		public bool EnableCraftList(string a1) => _enableCraftList.InvokeFunc(a1);
		public bool DisableCraftList() => _disableCraftList.InvokeFunc();
		public bool ToggleCraftList(string a1) => _toggleCraftList.InvokeFunc(a1);
		public bool AddItemToCraftList(string a1, uint a2, uint a3) => _addItemToCraftList.InvokeFunc(a1, a2, a3);
		public bool RemoveItemFromCraftList(string a1, uint a2, uint a3) => _removeItemFromCraftList.InvokeFunc(a1,a2,a3);
		public Dictionary<uint, uint> GetFilterItems(string a1) => _getFilterItems.InvokeFunc(a1);
		public Dictionary<uint, uint> GetCraftItems(string a1) => _getCraftItems.InvokeFunc(a1);
		public Dictionary<string, string> GetCraftLists() => _getCraftLists.InvokeFunc();
		public string AddNewCraftList(string a1, Dictionary<uint, uint> a2) => _addNewCraftList.InvokeFunc(a1, a2);
		public ulong CurrentCharacter() => _currentCharacter.InvokeFunc();
		public bool IsInitialized() => _isInitialized.InvokeFunc();


		private void RetainerChanged(ulong? a1) {
			PluginLog.Debug($"AllaganTools.RetainerChanged\n{a1}");
			
		}
		private void ItemRemoved((uint, InventoryItem.ItemFlags, ulong, uint) a1) {
			PluginLog.Debug($"AllaganTools.ItemRemoved\n{a1}");

		}
		private void ItemAdded((uint, InventoryItem.ItemFlags, ulong, uint) a1) {
			PluginLog.Debug($"AllaganTools.ItemAdded\n{a1}");

		}
		private void Initialized(bool a1) {
			PluginLog.Debug($"AllaganTools.Initialized\n{a1}");

		}

		public void Dispose() {
			_retainerChanged.Unsubscribe(RetainerChanged);
			_itemAdded.Unsubscribe(ItemAdded);
			_itemRemoved.Unsubscribe(ItemRemoved);
			_initialized.Unsubscribe(Initialized);
		}
	}
}
