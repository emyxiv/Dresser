using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;

using Dalamud.Utility;

using Dresser.Logic;
using Dresser.Structs.Dresser;

using Newtonsoft.Json.Linq;

using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Structs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using static Dresser.Services.Storage;

using CurrentSettings = System.ValueTuple<Penumbra.Api.Enums.PenumbraApiEc, (bool EnabledState, int Priority, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IList<string>> EnabledOptions, bool Inherited)?>;
using PseudoEquipItem = System.ValueTuple<string, ulong, uint, ushort, ushort, byte, uint>;

namespace Dresser.Services;

internal class PenumbraIpc : IDisposable {
	private Penumbra.Api.IpcSubscribers.GetModList                   GetModsSubscriber;
	private Penumbra.Api.IpcSubscribers.GetChangedItemsForCollection GetChangedItemsForCollectionSubscriber;
	private Penumbra.Api.IpcSubscribers.GetCollectionsByIdentifier   GetCollectionsByIdentifierSubscriber;
	private Penumbra.Api.IpcSubscribers.GetCurrentModSettings GetCurrentModSettingsSubscriber;
	private Penumbra.Api.IpcSubscribers.TrySetModPriority     TrySetModPrioritySubscriber;
	private Penumbra.Api.IpcSubscribers.TrySetModSettings     TrySetModSettingsSubscriber;
	private Penumbra.Api.IpcSubscribers.TryInheritMod         TryInheritModSubscriber;
	private Penumbra.Api.IpcSubscribers.ApiVersion                   ApiVersionsSubscriber;
	private Penumbra.Api.IpcSubscribers.GetEnabledState              GetEnabledStateSubscriber;
	private Penumbra.Api.IpcSubscribers.GetCollection         GetCollectionForTypeSubscriber;
	private Penumbra.Api.IpcSubscribers.GetModDirectory              GetModDirectorySubscriber;
	private Penumbra.Api.IpcSubscribers.TrySetMod             TrySetModSubscriber;
	private Penumbra.Api.IpcSubscribers.OpenMainWindow               OpenMainWindowSubscriber;






	private EventSubscriber<string, string>? Test { get; set; }

	internal PenumbraIpc() {
		GetChangedItemsForCollectionSubscriber = new global::Penumbra.Api.IpcSubscribers.GetChangedItemsForCollection(PluginServices.PluginInterface);
		GetCollectionsByIdentifierSubscriber = new global::Penumbra.Api.IpcSubscribers.GetCollectionsByIdentifier(PluginServices.PluginInterface);

		GetModsSubscriber = new global::Penumbra.Api.IpcSubscribers.GetModList(PluginServices.PluginInterface);
		//GetChangedItemsSubscriber = new global::Penumbra.Api.IpcSubscribers.Legacy.GetChangedItems       (PluginServices.PluginInterface);
		GetCurrentModSettingsSubscriber = new global::Penumbra.Api.IpcSubscribers.GetCurrentModSettings (PluginServices.PluginInterface);
		TrySetModPrioritySubscriber = new global::Penumbra.Api.IpcSubscribers.TrySetModPriority     (PluginServices.PluginInterface);
		TrySetModSettingsSubscriber = new global::Penumbra.Api.IpcSubscribers.TrySetModSettings     (PluginServices.PluginInterface);
		TryInheritModSubscriber = new global::Penumbra.Api.IpcSubscribers.TryInheritMod         (PluginServices.PluginInterface);
		ApiVersionsSubscriber = new global::Penumbra.Api.IpcSubscribers.ApiVersion                   (PluginServices.PluginInterface);
		GetEnabledStateSubscriber = new global::Penumbra.Api.IpcSubscribers.GetEnabledState              (PluginServices.PluginInterface);
		GetCollectionForTypeSubscriber = new global::Penumbra.Api.IpcSubscribers.GetCollection  (PluginServices.PluginInterface);
		GetModDirectorySubscriber = new global::Penumbra.Api.IpcSubscribers.GetModDirectory              (PluginServices.PluginInterface);
		TrySetModSubscriber = new global::Penumbra.Api.IpcSubscribers.TrySetMod             (PluginServices.PluginInterface);
		OpenMainWindowSubscriber = new global::Penumbra.Api.IpcSubscribers.OpenMainWindow               (PluginServices.PluginInterface);

		RegisterEvents();
	}

	public void Dispose() {
	}

	private void RegisterEvents() {
	}

	internal IEnumerable<(string Path, string Name)> GetMods() {
		try {
			return GetModsSubscriber!.Invoke().Select(p=>(p.Key,p.Value));
		} catch (Exception) {
			return new List<(string, string)>();
		}
	}
	internal IList<(string Path, string Name)> GetNotBlacklistedMods()
		=> GetMods().Where(m => !ConfigurationManager.Config.PenumbraModsBlacklist.Contains(m)).ToList();

	/// <returns>A dictionary of affected items in <paramref name="collectionName"/> via name and known objects or null.</returns>
	internal IReadOnlyDictionary<string, dynamic?> GetChangedItemsForCollection(string collectionName) {
		try {
			return GetChangedItemsForCollectionSubscriber.Invoke(CollectionNameToGuid(collectionName));
			//return GetChangedItemsSubscriber.Invoke(collectionName);
		} catch (Exception) {
			return new Dictionary<string, dynamic?>();
		}
	}
	internal IEnumerable<(uint ItemId, string ModModelPath)> GetChangedItemIdsForMod(string modPath, string modName) {

		List<(uint ItemId, string ModModelPath)> items = new();

		var res5 = PluginServices.Penumbra.TrySetMod(ConfigurationManager.Config.PenumbraCollectionTmp, modPath, true);
		//PluginLog.Warning($"Enable mod (path:){modPath}: {res5}");

		//var tempCollection = $"DaCol_{modCount}";
		//PluginLog.Debug($"Mod enabled, creating temp collection {tempCollection}");
		//var statusCreatedTmpCol = PluginServices.Penumbra.CreateNamedTemporaryCollection(tempCollection);
		//Task.Run(async delegate { await Task.Delay(500); }).Wait();
		//if (statusCreatedTmpCol == PenumbraApiEc.Success) {

		//foreach ((var optionGroup, var options) in modSettings.Item2.Value.EnabledOptions) {
		//	PluginServices.Penumbra.TrySetModSettings(tempCollection, mod.Path, mod.Name, optionGroup, options.ToList());
		//}

		Task.Run(async delegate { await Task.Delay(50); }).Wait();
		var changedItems = PluginServices.Penumbra.GetChangedEquipItemsForCollection(ConfigurationManager.Config.PenumbraCollectionTmp);


		foreach (var i in changedItems) {
			items.Add((i.ItemId.Id, i.ModelString));
		}

		//var ddd = PluginServices.Penumbra.RemoveTemporaryCollectionByName(tempCollection);
		//PluginLog.Debug($"{tempCollection} {(ddd == PenumbraApiEc.Success ? "removed" : "NOT REMOVED")}");
		//}
		var res6 = PluginServices.Penumbra.TrySetMod(ConfigurationManager.Config.PenumbraCollectionTmp, modPath, false);
		//PluginLog.Debug($"disable mod {modPath}: {res6}");

		return items;
	}

	// carefull here, the ignoreInheritance seems reversed?
	internal List<(string Path, string Name)> GetEnabledModsForCollection(string collection, bool ignoreInheritance) {
		return new();
		// todo fix it;
		List<(string Path, string Name)> DaCollModsSettings = new();
		foreach (var mod in PluginServices.Penumbra.GetNotBlacklistedMods()) {
			if (!PluginServices.Penumbra.IsModAppliedInCollection(collection, mod.Path, mod.Name, ignoreInheritance)) continue;

			PluginServices.Storage.ModsReloadingMax++;
			//PluginLog.Debug($"Found ACTIVE mod {mod.Name} || {mod.Path}");

			DaCollModsSettings.Add(mod);
		}
		return DaCollModsSettings;
	}
	internal List<InventoryItem> GetChangedInventoryItemForMods(List<(string Path, string Name)> mods) {
		List<InventoryItem> tmpItemList = new();
		foreach (var mod3 in mods) {

			var meta = GetModMeta(mod3.Path);
			var configTexts = GetConfigFileTexts(mod3.Path);
			var items = PluginServices.Penumbra.GetChangedItemIdsForMod(mod3.Path, mod3.Name);
			foreach (var i in items) {
				var item = new InventoryItem((InventoryType)InventoryTypeExtra.ModdedItems, i.ItemId.Copy(), mod3.Name.Copy()!, mod3.Path.Copy()!, i.ModModelPath.Copy()!);
				// todo: add icon path

				if (meta != null && meta.HasValues) {
					item.ModAuthor = meta.GetValue("Author")?.ToString();
					item.ModVersion = meta.GetValue("Version")?.ToString();
					item.ModWebsite = meta.GetValue("Website")?.ToString();
					item.ModIconPath = FindIconForItem(item.Icon, configTexts)?.ModdedIconFilePath;
				}

				tmpItemList.Add(item);
				//PluginLog.Debug($"Added item {item.ItemId} [{item.FormattedName}] for mod {item.ModName} || {item.ModDirectory}");
			}
			PluginServices.Storage.ModsReloadingCur++;
		}
		return tmpItemList;
	}
	internal static (string FoundIconGamePath, string ModdedIconFilePath)? FindIconForItem(uint iconId, Dictionary<string, string> configTexts) {
		List<string> possibleIconPaths = ModdedIconStorage.PossibleIconPaths(iconId);

		foreach ((var configFile, var configContents) in configTexts) {
			foreach (var possibleIconPath in possibleIconPaths) {
				if (configContents.Contains(possibleIconPath)) {

					var moddedIconPath = JObject.Parse(configContents) // parse the contents
						.SelectTokens($"Options[*].Files.['{possibleIconPath}']") // find the possible values
						.Where(v => !v.ToString().IsNullOrEmpty())
						.FirstOrDefault() // get the best result
						?.ToString(); // as a string or null

					if (moddedIconPath != null)
						return (possibleIconPath, moddedIconPath);
				}
			}
		}
		return null;
	}
	internal JObject? GetModMeta(string modDirectory) {
		string? bp = GetModDirectoryCached();
		if (bp != null) {
			var metaPath = Path.Combine(bp, modDirectory, "meta.json");
			if (File.Exists(metaPath)) {
				//PluginLog.Debug($"metaPath: {metaPath}");
				var metaJson = File.ReadAllText(metaPath);
				if (metaJson != null) {
					return JObject.Parse(metaJson);
				}
			}
		}
		return null;
	}
	internal Dictionary<string, string> GetConfigFileTexts(string modDirectory) {
		string? bp = GetModDirectoryCached();
		if (bp != null) {

			var jsonsPath = Path.Combine(bp, modDirectory);

			string[] files = Directory.GetFiles(jsonsPath, "*.json");
			return files.ToDictionary(n => n, File.ReadAllText);
		}
		return new();
	}


	internal IEnumerable<EquipItem> GetChangedEquipItemsForCollection(string collectionName) {
		return this.GetChangedItemsForCollection(collectionName).Where(o => {
			return o.Value?.GetType().ToString() == typeof(EquipItem).ToString();
			try {
				PluginLog.Debug($"change type: {o.Value?.GetType()??"null"}");
				EquipItem ddd = (PseudoEquipItem)o.Value;
				return true;
			} catch (Exception e) {
				PluginLog.Debug(e, $"failed to load item from penumbra");
			}

			return false;
		}).Select(i => {
			var qsd = (EquipItem)(PseudoEquipItem)i.Value;

			PluginLog.Debug($"changed equip item: {qsd.Name} || {qsd.Id} || {i.Key}");
			return qsd;
		});
	}

	internal string? GetModDirectory() {
		try {
			return GetModDirectorySubscriber.Invoke();
		} catch (Exception) {
			return null;
		}
	}

	private string? ModDirectoryCache = null;
	internal string? GetModDirectoryCached()
		=> ModDirectoryCache ??= GetModDirectory();
	



	/// <summary> Try to set the enabled state of a mod in a collection. </summary>
	internal bool TrySetMod(string collection, string directory, bool enabled) {
		try {
			return TrySetModSubscriber.Invoke(CollectionNameToGuid(collection), directory, enabled) == PenumbraApiEc.Success;
		} catch (Exception) {
			return false;
		}
	}

	/// <summary> Try to set the inheritance state of a mod in a collection. </summary>
	/// <returns>ModMissing, CollectionMissing, NothingChanged or Success.</returns>
	internal PenumbraApiEc TryInheritMod(string collectionName, string modDirectory, string modName, bool inherit) {
		try {
			return TryInheritModSubscriber.Invoke(CollectionNameToGuid(collectionName), modDirectory, inherit, modName);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
		}
	}


	/// <summary>
	/// Obtain the enabled state, the priority, the settings of a mod given by its <paramref name="modDirectory" /> name or <paramref name="modName" /> in the specified collection.
	/// </summary>
	/// <param name="collectionName">Specify the collection.</param>
	/// <param name="modDirectory">Specify the mod via its directory name.</param>
	/// <param name="modName">Specify the mod via its (non-unique) display name.</param>
	/// <param name="ignoreInheritance">Whether the settings need to be from the given collection or can be inherited from any other by it.</param>
	/// <returns>ModMissing, CollectionMissing or Success. <para />
	/// On Success, a tuple of Enabled State, Priority, a dictionary of option group names and lists of enabled option names and a bool whether the settings are inherited or not.</returns>
	internal CurrentSettings GetCurrentModSettings(string collectionName, string modDirectory, string modName, bool ignoreInheritance) {
		try {
			var response = GetCurrentModSettingsSubscriber.Invoke(CollectionNameToGuid(collectionName), modDirectory, modName, ignoreInheritance);
			var status = response.Item1;
			var info = response.Item2;
			var state = info?.Item4;
			return (CurrentSettings)response;
		} catch (Exception) {
			return (PenumbraApiEc.UnknownError, null);
		}
	}
	internal bool IsModAppliedInCollection(string collectionName, string modDirectory, string modName, bool ignoreInheritance = true) {
		return false;
		// todo fix GetCurrentModSettingsSubscriber
		try {
			var response = GetCurrentModSettingsSubscriber.Invoke(CollectionNameToGuid(collectionName), modDirectory, modName, ignoreInheritance);
			var status = response.Item1;
			var info = response.Item2;
			var state = info?.Item4 ?? false;
			return state;
		} catch (Exception) {
			return false;
		}
	}


	/// <inheritdoc cref="Penumbra.Api.IPenumbraApi.TrySetModPriority"/>
	internal PenumbraApiEc TrySetModPriority(string collectionName, string modDirectory, string modName, int priority) {
		try {
			return TrySetModPrioritySubscriber.Invoke(CollectionNameToGuid(collectionName), modDirectory, priority, modName);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
		}
	}

	internal PenumbraApiEc TrySetModSettings(string collectionName, string modDirectory, string modName, string optionGroupName, IReadOnlyList<string> options) {
		try {
			return TrySetModSettingsSubscriber.Invoke(CollectionNameToGuid(collectionName), modDirectory, optionGroupName, options, modName);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
		}
	}

	/// <returns>The name of the collection assigned to the given <paramref name="type"/> or an empty string if none is assigned or type is invalid.</returns>
	internal string GetCollectionForType(ApiCollectionType type) {
		try {
			return GetCollectionForTypeSubscriber.Invoke(type)?.Name ?? "";
		} catch (Exception) {
			return "";
		}
	}
	internal string GetCollectionForLocalPlayerCharacter() {
		return GetCollectionForType(ApiCollectionType.Yourself);
	}







	/// <inheritdoc cref="Penumbra.Api.IPenumbraApiBase.ApiVersion"/>
	internal (int Breaking, int Features) ApiVersions() {
		try {
			return ApiVersionsSubscriber.Invoke();
		} catch (Exception) {
			return (0, 0);
		}
	}
	/// <inheritdoc cref="Penumbra.Api.IPenumbraApi.GetEnabledState"/>
	internal bool GetEnabledState() {
		try {
			return GetEnabledStateSubscriber.Invoke();
		} catch (Exception) {
			return false;
		}
	}	/// <inheritdoc cref="Penumbra.Api.IPenumbraApi.OpenMainWindow(TabType, string, string)"/>
	internal PenumbraApiEc OpenMainWindow(TabType tab, string modDirectory, string modName) {
		try {
			return OpenMainWindowSubscriber.Invoke(tab, modDirectory, modName);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
		}
	}
	internal bool OpenModWindow((string modDirectory, string modName) mod) {
		return OpenMainWindow(TabType.Mods, mod.modDirectory, mod.modName) == PenumbraApiEc.Success;
	}

	internal int CountModsDresserApplyCollection() {
		return -1;
		// todo fix it
		return GetEnabledModsForCollection(ConfigurationManager.Config.PenumbraCollectionApply, true).Count();
	}
	internal void CleanDresserApplyCollection() {

		foreach(var mod in GetEnabledModsForCollection(ConfigurationManager.Config.PenumbraCollectionApply, true)){
			CleanDresserApplyMod(mod);
		}

	}
	internal void CleanDresserApplyMod((string Path, string Name) mod) {
		PluginLog.Debug($"reset apply state of mod {mod.Path},{mod.Name}");
		TryInheritMod(ConfigurationManager.Config.PenumbraCollectionApply, mod.Path, mod.Name, true);
	}
	internal Guid CollectionNameToGuid(string collectionName) {
		return this.GetCollectionsByIdentifierSubscriber.Invoke(collectionName).First().Id;
	}
}
