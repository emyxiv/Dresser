using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using Dresser.Logic;

using FFXIVClientStructs.FFXIV.Client.Game;

using System;
using System.Collections.Generic;
using System.Linq;

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
		private readonly ICallGateSubscriber<ulong, HashSet<ulong[]>> _getCharacterItems;
		private readonly ICallGateSubscriber<bool, HashSet<ulong>> _getCharactersOwnedByActive;
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
			_getCharacterItems       = pluginInterface.GetIpcSubscriber<ulong, HashSet<ulong[]>>("AllaganTools.GetCharacterItems");
			_getCharactersOwnedByActive = pluginInterface.GetIpcSubscriber<bool, HashSet<ulong>>("AllaganTools.GetCharactersOwnedByActive");
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

		public uint InventoryCountByType(uint inventoryType, ulong? characterId)     { try { return _inventoryCountByType.    InvokeFunc(inventoryType, characterId  ) ; } catch(Exception e){return 0;}}
		public uint InventoryCountByTypes(uint[] inventoryTypes, ulong? characterId) { try { return _inventoryCountByTypes.   InvokeFunc(inventoryTypes, characterId ) ; } catch(Exception e){return 0;}}
		public uint ItemCount(uint a1, ulong a2, int a3)                             { try { return _itemCount.               InvokeFunc(a1, a2, a3                  ) ; } catch(Exception e){return 0;}}
		public uint ItemCountHQ(uint a1, ulong a2, int a3)                           { try { return _itemCountHQ.             InvokeFunc(a1, a2, a3                  ) ; } catch(Exception e){return 0;}}
		public uint ItemCountOwned(uint a1, bool a2, uint[] a3)                      { try { return _itemCountOwned.          InvokeFunc(a1, a2, a3                  ) ; } catch(Exception e){return 0;}}
		public bool EnableUiFilter(string a1)                                        { try { return _enableUiFilter.          InvokeFunc(a1                          ) ; } catch(Exception e){return false;}}
		public bool DisableUiFilter()                                                { try { return _disableUiFilter.         InvokeFunc(                            ) ; } catch(Exception e){return false;}}
		public bool ToggleUiFilter(string a1)                                        { try { return _toggleUiFilter.          InvokeFunc(a1                          ) ; } catch(Exception e){return false;}}
		public bool EnableBackgroundFilter(string a1)                                { try { return _enableBackgroundFilter.  InvokeFunc(a1                          ) ; } catch(Exception e){return false;}}
		public bool DisableBackgroundFilter()                                        { try { return _disableBackgroundFilter. InvokeFunc(                            ) ; } catch(Exception e){return false;}}
		public bool ToggleBackgroundFilter(string a1)                                { try { return _toggleBackgroundFilter.  InvokeFunc(a1                          ) ; } catch(Exception e){return false;}}
		public bool EnableCraftList(string a1)                                       { try { return _enableCraftList.         InvokeFunc(a1                          ) ; } catch(Exception e){return false;}}
		public bool DisableCraftList()                                               { try { return _disableCraftList.        InvokeFunc(                            ) ; } catch(Exception e){return false;}}
		public bool ToggleCraftList(string a1)                                       { try { return _toggleCraftList.         InvokeFunc(a1                          ) ; } catch(Exception e){return false;}}
		public bool AddItemToCraftList(string a1, uint a2, uint a3)                  { try { return _addItemToCraftList.      InvokeFunc(a1, a2, a3                  ) ; } catch(Exception e){return false;}}
		public bool RemoveItemFromCraftList(string a1, uint a2, uint a3)             { try { return _removeItemFromCraftList. InvokeFunc(a1,a2,a3                    ) ; } catch(Exception e){return false;}}
		public Dictionary<uint, uint> GetFilterItems(string a1)                      { try { return _getFilterItems.          InvokeFunc(a1                          ) ; } catch(Exception e){return new();}}
		public Dictionary<uint, uint> GetCraftItems(string a1)                       { try { return _getCraftItems.           InvokeFunc(a1                          ) ; } catch(Exception e){return new(); }}
		public HashSet<ulong[]> GetCharacterItemsSerialized(ulong characterId)                { try { return _getCharacterItems.       InvokeFunc(characterId                 ); } catch (Exception e) { PluginLog.Error(e, "Error on GetCharacterItems1"); return new(); }}
		public HashSet<ulong> GetCharactersOwnedByActive(bool includeOwner)          { try { return _getCharactersOwnedByActive.InvokeFunc(includeOwner              ); } catch (Exception e){ PluginLog.Error(e, "Error on GetCharactersOwnedByActive"); return new(); }}
		public Dictionary<string, string> GetCraftLists()                            { try { return _getCraftLists.           InvokeFunc(                            ) ; } catch(Exception e){return new(); }}
		public string AddNewCraftList(string a1, Dictionary<uint, uint> a2)          { try { return _addNewCraftList.         InvokeFunc(a1, a2                      ) ; } catch(Exception e){return "";}}
		public ulong CurrentCharacter()                                              { try { return _currentCharacter.        InvokeFunc(                            ) ; } catch(Exception e){return 0;}}
		public bool IsInitialized()                                                  { try { return _isInitialized.           InvokeFunc(                            ) ; } catch(Exception e) { return false; }}

		public IEnumerable<CriticalCommonLib.Models.InventoryItem> GetCharacterItems(ulong characterId)
			=> GetCharacterItemsSerialized(characterId).Select(CriticalCommonLib.Models.InventoryItem.FromNumeric);
		public IEnumerable<Structs.Dresser.InventoryItem> GetItemsLocalChar()
			=> GetItems(PluginServices.Context.LocalPlayerCharacterId);
		public IEnumerable<Structs.Dresser.InventoryItem> GetItems(ulong characterId)
			=> GetCharacterItems(characterId).Select(Structs.Dresser.InventoryItem.FromCritical);
		public Dictionary<ulong, IEnumerable<Structs.Dresser.InventoryItem>> GetItemsLocalCharsRetainers(bool includeActiveCharacter = false)
			=> GetCharactersOwnedByActive(includeActiveCharacter).ToDictionary(chId => chId, GetItems);

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
