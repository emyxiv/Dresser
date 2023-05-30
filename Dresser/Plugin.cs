
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;

using Dresser.Data;
using Dresser.Windows;
using Dresser.Interop.Hooks;
using Dresser.Interop;
using CriticalCommonLib;
using Dresser.Logic;
using System;
using Dalamud.Game;
using CriticalCommonLib.Models;
using CriticalCommonLib.Services;
using Dalamud.Logging;
using System.Collections.Generic;
using Dalamud.Game.Text.SeStringHandling;
using System.Linq;
using ImGuiNET;
using Dresser.Windows.Components;
using CriticalCommonLib.Services.Ui;

namespace Dresser {
	public sealed class Plugin : IDalamudPlugin {
		public string Name => "Dresser";
		private const string CommandName = "/dresser";


		public WindowSystem WindowSystem = new("Dresser");

		private static Plugin? PluginInstance = null;
		public static Plugin GetInstance() => PluginInstance!;

		internal ConfigWindow? ConfigWindow = null;
		internal GearBrowser? GearBrowser = null;
		internal CurrentGear? CurrentGear = null;
		internal Dialogs? Dialogs = null;

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager) {
			PluginInstance = this;
			PluginServices.Init(pluginInterface, this);


			ConfigurationManager.Config.ConfigurationChanged += ConfigOnConfigurationChanged;
			PluginServices.InventoryMonitor.OnInventoryChanged += InventoryMonitorOnOnInventoryChanged;
			PluginServices.CharacterMonitor.OnCharacterUpdated += CharacterMonitorOnOnCharacterUpdated;
			Service.Framework.Update += FrameworkOnUpdate;

			PluginServices.InventoryMonitor.LoadExistingData(ConfigurationManager.Config.GetSavedInventory());
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
			Interop.Hooks.AddonListeners.Init();


			ConfigWindow = new ConfigWindow(this);
			WindowSystem.AddWindow(ConfigWindow);
			GearBrowser = new GearBrowser(this);
			WindowSystem.AddWindow(GearBrowser);
			CurrentGear = new CurrentGear(this);
			WindowSystem.AddWindow(CurrentGear);
			Dialogs = new Dialogs(this);
			WindowSystem.AddWindow(Dialogs);



			PluginServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "Open dresser."
			});

			pluginInterface.UiBuilder.Draw += DrawUI;
			pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;


		}

		public void Dispose() {
			PluginServices.ApplyGearChange.RestoreAppearance();


			this.WindowSystem.RemoveAllWindows();
			PluginServices.CommandManager.RemoveHandler(CommandName);

			PluginServices.GameInterface.AcquiredItemsUpdated -= GameInterfaceOnAcquiredItemsUpdated;
			ConfigurationManager.Config.SavedCharacters = PluginServices.CharacterMonitor.Characters;
			Service.Framework.Update -= FrameworkOnUpdate;
			PluginServices.InventoryMonitor.OnInventoryChanged -= InventoryMonitorOnOnInventoryChanged;
			PluginServices.CharacterMonitor.OnCharacterUpdated -= CharacterMonitorOnOnCharacterUpdated;
			ConfigurationManager.Config.ConfigurationChanged -= ConfigOnConfigurationChanged;




			Interop.Hooks.AddonListeners.Dispose();
			PluginServices.Dispose();
		}

		private void OnCommand(string command, string args) {
			PluginLog.Debug($"{command} {args}");
			switch (args) {
				case "config": DrawConfigUI(); break;
				default:
					// in response to the slash command, just display our main ui
					WindowSystem.GetWindow("Gear Browser")!.IsOpen = true;
					WindowSystem.GetWindow("Current Gear")!.IsOpen = true;
					break;
			}
		}

		private void DrawUI() {
			this.WindowSystem.Draw();
		}

		public void DrawConfigUI() {
			WindowSystem.GetWindow("Dresser Settings")!.IsOpen = true;
		}
		public void ToggleDresser() {
			WindowSystem.GetWindow("Current Gear")!.IsOpen = !IsDresserVisible();
			WindowSystem.GetWindow("Gear Browser")!.IsOpen = !IsDresserVisible();
		}
		public void OpenDresser() {
			//PluginLog.Debug($"OpenDresser");
			WindowSystem.GetWindow("Current Gear")!.IsOpen = true;
			WindowSystem.GetWindow("Gear Browser")!.IsOpen = true;
		}
		public void CloseDresser() {
			//PluginLog.Debug($"CloseDresser");
			WindowSystem.GetWindow("Current Gear")!.IsOpen = false;
			WindowSystem.GetWindow("Gear Browser")!.IsOpen = false;
		}
		public void CloseBrowser() {
			WindowSystem.GetWindow("Gear Browser")!.IsOpen = false;
		}
		public void OpenGearBrowserIfClosed() {
			if(!WindowSystem.GetWindow("Gear Browser")!.IsOpen) {
				WindowSystem.GetWindow("Gear Browser")!.IsOpen = true;
			}
		}
		public bool IsDresserVisible()
			=> WindowSystem?.GetWindow("Current Gear")?.IsOpen ?? false;
		public bool IsBrowserVisible()
			=> WindowSystem?.GetWindow("Gear Browser")?.IsOpen ?? false;
		public void OpenDialog(DialogInfo dialogInfo) {
			Dialogs!.DialogInfo = dialogInfo;
			WindowSystem.GetWindow("Dialogs")!.IsOpen = true;
		}






		// Inventory tools save inventories
		private DateTime? _nextSaveTime = null;
		public void ClearAutoSave() {
			_nextSaveTime = null;
		}
		public DateTime? NextSaveTime => _nextSaveTime;

		private void FrameworkOnUpdate(Framework framework) {
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

		}
		private void ConfigOnConfigurationChanged() {
			ConfigurationManager.Save();
		}


		private Dictionary<ulong, List<Payload>> _cachedTooltipLines = new();
		private bool _clearCachedLines = false;


		private Dictionary<uint, InventoryMonitor.ItemChangesItem> _recentlyAddedSeen = new();
		private void InventoryMonitorOnOnInventoryChanged(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> inventories, InventoryMonitor.ItemChanges itemChanges) {
			//PluginLog.Verbose($"PluginLogic: Inventory changed, saving to config.");
			//PluginLog.Debug($"====== RECORD UPDATE {inventories.Count + itemChanges.NewItems.Count + itemChanges.RemovedItems.Count}");
			_clearCachedLines = true;
			//ConfigurationManager.Config.SavedInventories = inventories;
			//PluginConfiguration.SavedInventories = inventories;
			//PluginLog.Debug($"====== inv updated {ConfigurationManager.Config.SavedInventories.Select(t=>t.Value.Count).Sum()}");

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
