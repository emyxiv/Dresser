
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

namespace Dresser {
	public sealed class Plugin : IDalamudPlugin {
		public string Name => "Dresser";
		private const string CommandName = "/dresser";

		public Configuration Configuration { get; init; }

		public WindowSystem WindowSystem = new("Dresser");

		public Plugin(
			[RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
			[RequiredVersion("1.0")] CommandManager commandManager) {
			PluginServices.Init(pluginInterface);


			PluginServices.InventoryMonitor.OnInventoryChanged += InventoryMonitorOnOnInventoryChanged;
			PluginServices.CharacterMonitor.OnActiveRetainerChanged += CharacterMonitorOnOnActiveCharacterChanged;
			PluginServices.CharacterMonitor.OnActiveRetainerLoaded += CharacterMonitorOnOnActiveCharacterChanged;
			PluginServices.CharacterMonitor.OnCharacterUpdated += CharacterMonitorOnOnCharacterUpdated;
			this.Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
			this.Configuration.Initialize(pluginInterface);

			GameInterface.AcquiredItemsUpdated += GameInterfaceOnAcquiredItemsUpdated;


			Service.Framework.Update += FrameworkOnUpdate;

			ImageGuiCrop.Init();
			Gathering.Init();

			Methods.Init();
			Interop.Hooks.AddonListeners.Init();



			WindowSystem.AddWindow(new ConfigWindow(this));
			WindowSystem.AddWindow(new GearBrowser());
			WindowSystem.AddWindow(new CurrentGear());


			PluginServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {
				HelpMessage = "A useful message to display in /xlhelp"
			});

			pluginInterface.UiBuilder.Draw += DrawUI;
			pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;


			EventManager.GearSelectionOpen += OpenDresser;
			EventManager.GearSelectionClose += CloseDresser;

			if (GlamourPlates.IsGlamingAtDresser())
				EventManager.GearSelectionOpen?.Invoke();
		}

		public void Dispose() {
			EventManager.GearSelectionClose?.Invoke();

			this.WindowSystem.RemoveAllWindows();
			PluginServices.CommandManager.RemoveHandler(CommandName);

			GameInterface.AcquiredItemsUpdated -= GameInterfaceOnAcquiredItemsUpdated;

			Service.Framework.Update -= FrameworkOnUpdate;
			PluginServices.InventoryMonitor.OnInventoryChanged -= InventoryMonitorOnOnInventoryChanged;
			PluginServices.CharacterMonitor.OnActiveRetainerChanged -= CharacterMonitorOnOnActiveCharacterChanged;
			PluginServices.CharacterMonitor.OnActiveRetainerLoaded -= CharacterMonitorOnOnActiveCharacterChanged;
			PluginServices.CharacterMonitor.OnCharacterUpdated -= CharacterMonitorOnOnCharacterUpdated;

			EventManager.GearSelectionOpen -= OpenDresser;
			EventManager.GearSelectionClose -= CloseDresser;


			Interop.Hooks.AddonListeners.Dispose();
			ImageGuiCrop.Dispose();
			PluginServices.Dispose();
		}

		private void OnCommand(string command, string args) {
			// in response to the slash command, just display our main ui
			WindowSystem.GetWindow("Gear Browser")!.IsOpen = true;
			WindowSystem.GetWindow("Current Gear")!.IsOpen = true;
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
		public bool IsDresserVisible()
			=> WindowSystem.GetWindow("Current Gear")!.IsOpen;








		// Inventory tools save inventories
		private DateTime? _nextSaveTime = null;
		public void ClearAutoSave() {
			_nextSaveTime = null;
		}
		public DateTime? NextSaveTime => _nextSaveTime;

		private void FrameworkOnUpdate(Framework framework) {
			if (Configuration.AutoSave) {
				if (NextSaveTime == null && Configuration.AutoSaveMinutes != 0) {
					_nextSaveTime = DateTime.Now.AddMinutes(Configuration.AutoSaveMinutes);
				} else {
					if (DateTime.Now >= NextSaveTime) {
						_nextSaveTime = null;
						ConfigurationManager.Save();
					}
				}
			}

		}
		private void ConfigOnConfigurationChanged() {
			ConfigurationManager.Save();
		}


		private Dictionary<ulong, List<Payload>> _cachedTooltipLines = new();
		private bool _clearCachedLines = false;


		private Dictionary<int, InventoryMonitor.ItemChangesItem> _recentlyAddedSeen = new();
		private void InventoryMonitorOnOnInventoryChanged(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> inventories, InventoryMonitor.ItemChanges itemChanges) {
			PluginLog.Verbose("PluginLogic: Inventory changed, saving to config.");
			_clearCachedLines = true;
			Configuration.SavedInventories = inventories;

			foreach (var item in itemChanges.NewItems) {
				if (_recentlyAddedSeen.ContainsKey(item.ItemId)) {
					_recentlyAddedSeen.Remove(item.ItemId);
				}
				_recentlyAddedSeen.Add(item.ItemId, item);
			}
		}

		private void CharacterMonitorOnOnCharacterUpdated(Character? character) {
			if (character != null) {
				if (Configuration.AcquiredItems.ContainsKey(character.CharacterId)) {
					GameInterface.AcquiredItems = Configuration.AcquiredItems[character.CharacterId];
				}
			} else {
				GameInterface.AcquiredItems = new HashSet<uint>();
			}
		}
		private void GameInterfaceOnAcquiredItemsUpdated() {
			var activeCharacter = PluginServices.CharacterMonitor.ActiveCharacter;
			if (activeCharacter != 0) {
				Configuration.AcquiredItems[activeCharacter] = GameInterface.AcquiredItems;
			}
		}

		private ulong _currentRetainerId;
		private void CharacterMonitorOnOnActiveCharacterChanged(ulong retainerId) {
			PluginLog.Debug("Retainer changed.");
			PluginLog.Debug("Retainer ID: " + retainerId);
			_currentRetainerId = retainerId;
		}


	}
}
