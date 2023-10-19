
using CriticalCommonLib;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services;
using CriticalCommonLib.Services.Ui;

using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using Dresser.Interop;
using Dresser.Interop.Addons;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Windows;

using System;
using System.Collections.Generic;

using InventoryItem = Dresser.Structs.Dresser.InventoryItem;
using CriticalInventoryItem = CriticalCommonLib.Models.InventoryItem;


namespace Dresser {
	public sealed class Plugin : IDalamudPlugin {
		public string Name => "Dresser";
		private const string CommandName = "/dresser";


		public WindowSystem WindowSystem = new("Dresser");

		private static Plugin? PluginInstance = null;
		public static Plugin GetInstance() => PluginInstance!;

		internal ConfigWindow ConfigWindow { get; init; }
		internal GearBrowser GearBrowser { get; init; }
		internal CurrentGear CurrentGear { get; init; }
		internal DyePicker DyePicker { get; init; }
		internal Dialogs? Dialogs = null;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] ICommandManager commandManager) {
			PluginInstance = this;
			PluginServices.Init(pluginInterface, this);


			ConfigurationManager.Config.ConfigurationChanged += ConfigOnConfigurationChanged;
			PluginServices.InventoryMonitor.OnInventoryChanged += InventoryMonitorOnOnInventoryChanged;
			PluginServices.CharacterMonitor.OnCharacterUpdated += CharacterMonitorOnOnCharacterUpdated;
			Service.Framework.Update += FrameworkOnUpdate;

			PluginServices.InventoryMonitor.LoadExistingData(ConfigurationManager.LoadInventory());
			PluginServices.CharacterMonitor.LoadExistingRetainers(ConfigurationManager.Config.GetSavedRetainers());


			PluginServices.GameUi.WatchWindowState(WindowName.RetainerGrid0);
			PluginServices.GameUi.WatchWindowState(WindowName.InventoryGrid0E);
			PluginServices.GameUi.WatchWindowState(WindowName.RetainerList);
			PluginServices.GameUi.WatchWindowState(WindowName.Inventory);
			PluginServices.GameUi.WatchWindowState(WindowName.InventoryLarge);
			PluginServices.GameUi.WatchWindowState(WindowName.InventoryRetainerLarge);
			PluginServices.GameUi.WatchWindowState(WindowName.InventoryRetainer);
			PluginServices.GameUi.WatchWindowState(WindowName.InventoryBuddy);
			PluginServices.GameUi.WatchWindowState(WindowName.InventoryBuddy2);

			PluginServices.GameInterface.AcquiredItemsUpdated += GameInterfaceOnAcquiredItemsUpdated;




			Gathering.Init();

			Methods.Init();
			AddonListeners.Init();


			ConfigWindow = new ConfigWindow(this);
			GearBrowser = new GearBrowser(this);
			CurrentGear = new CurrentGear(this);
			DyePicker = new DyePicker(this);
			Dialogs = new Dialogs(this);
			WindowSystem.AddWindow(ConfigWindow);
			WindowSystem.AddWindow(GearBrowser);
			WindowSystem.AddWindow(CurrentGear);
			WindowSystem.AddWindow(DyePicker);
			WindowSystem.AddWindow(Dialogs);



			PluginServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "Open dresser."
			});

			pluginInterface.UiBuilder.Draw += DrawUI;
			pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
			pluginInterface.UiBuilder.OpenMainUi += DrawMainUI;


		}

		public void Dispose() {
			PluginServices.ApplyGearChange.RestoreAppearance();
			PluginServices.ApplyGearChange.ClearApplyDresser();



			ConfigWindow.Dispose();
			GearBrowser.Dispose();
			CurrentGear.Dispose();
			Dialogs?.Dispose();
			DyePicker.Dispose();

			this.WindowSystem.RemoveAllWindows();
			PluginServices.CommandManager.RemoveHandler(CommandName);

			PluginServices.GameInterface.AcquiredItemsUpdated -= GameInterfaceOnAcquiredItemsUpdated;
			ConfigurationManager.Config.SavedCharacters = PluginServices.CharacterMonitor.Characters;
			Service.Framework.Update -= FrameworkOnUpdate;
			PluginServices.InventoryMonitor.OnInventoryChanged -= InventoryMonitorOnOnInventoryChanged;
			PluginServices.CharacterMonitor.OnCharacterUpdated -= CharacterMonitorOnOnCharacterUpdated;
			ConfigurationManager.Config.ConfigurationChanged -= ConfigOnConfigurationChanged;




			AddonListeners.Dispose();
			PluginServices.Dispose();
		}

		private void OnCommand(string command, string args) {
			PluginLog.Debug($"{command} {args}");
			switch (args) {
				case "config": DrawConfigUI(); break;
				default:
					// in response to the slash command, just display our main ui
					GearBrowser.IsOpen = true;
					CurrentGear.IsOpen = true;
					break;
			}
		}

		private void DrawUI() {
			this.WindowSystem.Draw();
		}
		public void ToggleConfigUI() {
			ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
		}
		public void DrawMainUI() {
			CurrentGear.IsOpen = true;
		}
		public void DrawConfigUI() {
			ConfigWindow.IsOpen = true;
		}
		public void ToggleDresser() {
			CurrentGear.IsOpen = !IsDresserVisible();
			GearBrowser.IsOpen = !IsDresserVisible();
		}
		public void OpenDresser() {
			//PluginLog.Debug($"OpenDresser");
			CurrentGear.IsOpen = true;
			GearBrowser.IsOpen = true;
		}
		public void CloseDresser() {
			//PluginLog.Debug($"CloseDresser");
			CurrentGear.IsOpen = false;
			GearBrowser.IsOpen = false;
		}
		public void CloseBrowser() {
			GearBrowser.IsOpen = false;
		}
		public void OpenGearBrowserIfClosed() {
			if(!GearBrowser.IsOpen) {
				GearBrowser.IsOpen = true;
			}
		}
		public void UncollapseGearBrowserIfCollapsed() {
			if(GearBrowser.Collapsed == null)
				GearBrowser.Collapsed = false;
		}
		public bool IsDresserVisible()
			=> CurrentGear.IsOpen;
		public bool IsBrowserVisible()
			=> GearBrowser.IsOpen;
		public void OpenDialog(DialogInfo dialogInfo) {
			Dialogs!.DialogInfo = dialogInfo;
			Dialogs!.IsOpen = true;
		}






		// Inventory tools save inventories
		private DateTime? _nextSaveTime = null;
		public void ClearAutoSave() {
			_nextSaveTime = null;
		}
		public DateTime? NextSaveTime => _nextSaveTime;

		private void FrameworkOnUpdate(IFramework framework) {
			if (ConfigurationManager.Config.AutoSave) {
				if (NextSaveTime == null && ConfigurationManager.Config.AutoSaveMinutes != 0) {
					_nextSaveTime = DateTime.Now.AddMinutes(ConfigurationManager.Config.AutoSaveMinutes);
				} else {
					if (DateTime.Now >= NextSaveTime) {
						//PluginLog.Debug("===============SAVING INV NOW==============");
						_nextSaveTime = null;
						ConfigurationManager.SaveAsync();
					}
				}
			}
			PluginServices.Context.Refresh();
			PluginServices.ApplyGearChange.FrameworkUpdate();

		}
		private void ConfigOnConfigurationChanged() {
			ConfigurationManager.Save();
		}


		private Dictionary<ulong, List<Payload>> _cachedTooltipLines = new();
		private bool _clearCachedLines = false;


		private Dictionary<uint, InventoryMonitor.ItemChangesItem> _recentlyAddedSeen = new();
		private void InventoryMonitorOnOnInventoryChanged(List<InventoryChange> inventoryChanges, InventoryMonitor.ItemChanges? itemChanges) {
			//PluginLog.Verbose($"PluginLogic: Inventory changed, saving to config.");
			//PluginLog.Debug($"====== RECORD UPDATE {inventories.Count + itemChanges.NewItems.Count + itemChanges.RemovedItems.Count}");
			_clearCachedLines = true;
			//ConfigurationManager.Config.SavedInventories = inventories;
			//PluginConfiguration.SavedInventories = inventories;
			//PluginLog.Debug($"====== inv updated {ConfigurationManager.Config.SavedInventories.Select(t=>t.Value.Count).Sum()}");
			if (itemChanges != null)
			foreach (var item in itemChanges.NewItems) {
				if (_recentlyAddedSeen.ContainsKey(item.ItemId)) {
					_recentlyAddedSeen.Remove(item.ItemId);
				}
				_recentlyAddedSeen.Add(item.ItemId, item);
			}
			ConfigurationManager.SaveAsync();
			PluginLog.Debug($"PluginServices.Context.IsCurrentGearWindowOpen {PluginServices.Context.IsCurrentGearWindowOpen}");
			if (PluginServices.Context.IsCurrentGearWindowOpen) PluginServices.ApplyGearChange?.ReApplyAppearanceAfterEquipUpdate();
		}

		private void CharacterMonitorOnOnCharacterUpdated(Character? character) {
			if (character != null) {
				if (ConfigurationManager.Config.AcquiredItems.ContainsKey(character.CharacterId)) {
					PluginServices.GameInterface.AcquiredItems = ConfigurationManager.Config.AcquiredItems[character.CharacterId];
				}
				ConfigurationManager.SaveAsync();
				if (PluginServices.Context.IsCurrentGearWindowOpen) PluginServices.ApplyGearChange.ReApplyAppearanceAfterEquipUpdate();
			} else {
				PluginServices.GameInterface.AcquiredItems = new HashSet<uint>();
			}
		}
		private void GameInterfaceOnAcquiredItemsUpdated() {
			var activeCharacter = PluginServices.CharacterMonitor.ActiveCharacterId;
			if (activeCharacter != 0) {
				ConfigurationManager.Config.AcquiredItems[activeCharacter] = PluginServices.GameInterface.AcquiredItems;
				//if (PluginServices.Context.IsCurrentGearWindowOpen) PluginServices.ApplyGearChange.ReApplyAppearanceAfterEquipUpdate();
				ConfigurationManager.SaveAsync();
			}
		}

	}
}
