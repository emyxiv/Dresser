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
using System.Collections;
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
	//private Penumbra.Api.IpcSubscribers.TrySetModPriority     TrySetModPrioritySubscriber;
	//private Penumbra.Api.IpcSubscribers.TrySetModSettings     TrySetModSettingsSubscriber;
	//private Penumbra.Api.IpcSubscribers.TryInheritMod         TryInheritModSubscriber;
	private Penumbra.Api.IpcSubscribers.ApiVersion                   ApiVersionsSubscriber;
	private Penumbra.Api.IpcSubscribers.GetEnabledState              GetEnabledStateSubscriber;
	private Penumbra.Api.IpcSubscribers.GetCollection         GetCollectionForTypeSubscriber;
	private Penumbra.Api.IpcSubscribers.GetModDirectory              GetModDirectorySubscriber;
	//private Penumbra.Api.IpcSubscribers.TrySetMod             TrySetModSubscriber;
	private Penumbra.Api.IpcSubscribers.OpenMainWindow               OpenMainWindowSubscriber;

	private Penumbra.Api.IpcSubscribers.RemoveTemporaryModSettingsPlayer RemoveTemporaryModSettingsPlayerSubscriber;
	private Penumbra.Api.IpcSubscribers.SetTemporaryModSettingsPlayer SetTemporaryModSettingsPlayerSubscriber;
	private Penumbra.Api.IpcSubscribers.GetChangedItemAdapterDictionary GetChangedItemAdapterDictionarySubscriber;
	private Penumbra.Api.IpcSubscribers.GetChangedItemAdapterList GetChangedItemAdapterListSubscriber;
	private Penumbra.Api.IpcSubscribers.GetAllModSettings GetAllModSettingsSubscriber;

	private Penumbra.Api.IpcSubscribers.RemoveAllTemporaryModSettingsPlayer RemoveAllTemporaryModSettingsPlayerSubscriber;
	private Penumbra.Api.IpcSubscribers.QueryTemporaryModSettingsPlayer QueryTemporaryModSettingsPlayerSubscriber;

	private Penumbra.Api.IpcSubscribers.GetModPath GetModPathSubscriber;

	private Penumbra.Api.IpcSubscribers.GetCurrentModSettingsWithTemp GetCurrentModSettingsWithTempSubscriber;




	private EventSubscriber<string, string>? Test { get; set; }

	internal PenumbraIpc() {
		GetChangedItemsForCollectionSubscriber = new global::Penumbra.Api.IpcSubscribers.GetChangedItemsForCollection(PluginServices.PluginInterface);
		GetCollectionsByIdentifierSubscriber = new global::Penumbra.Api.IpcSubscribers.GetCollectionsByIdentifier(PluginServices.PluginInterface);

		GetModsSubscriber = new global::Penumbra.Api.IpcSubscribers.GetModList(PluginServices.PluginInterface);
		//GetChangedItemsSubscriber = new global::Penumbra.Api.IpcSubscribers.Legacy.GetChangedItems       (PluginServices.PluginInterface);
		GetCurrentModSettingsSubscriber = new global::Penumbra.Api.IpcSubscribers.GetCurrentModSettings (PluginServices.PluginInterface);
		//TrySetModPrioritySubscriber = new global::Penumbra.Api.IpcSubscribers.TrySetModPriority     (PluginServices.PluginInterface);
		//TrySetModSettingsSubscriber = new global::Penumbra.Api.IpcSubscribers.TrySetModSettings     (PluginServices.PluginInterface);
		//TryInheritModSubscriber = new global::Penumbra.Api.IpcSubscribers.TryInheritMod         (PluginServices.PluginInterface);
		ApiVersionsSubscriber = new global::Penumbra.Api.IpcSubscribers.ApiVersion                   (PluginServices.PluginInterface);
		GetEnabledStateSubscriber = new global::Penumbra.Api.IpcSubscribers.GetEnabledState              (PluginServices.PluginInterface);
		GetCollectionForTypeSubscriber = new global::Penumbra.Api.IpcSubscribers.GetCollection  (PluginServices.PluginInterface);
		GetModDirectorySubscriber = new global::Penumbra.Api.IpcSubscribers.GetModDirectory              (PluginServices.PluginInterface);
		//TrySetModSubscriber = new global::Penumbra.Api.IpcSubscribers.TrySetMod             (PluginServices.PluginInterface);
		OpenMainWindowSubscriber = new global::Penumbra.Api.IpcSubscribers.OpenMainWindow               (PluginServices.PluginInterface);

		RemoveTemporaryModSettingsPlayerSubscriber = new global::Penumbra.Api.IpcSubscribers.RemoveTemporaryModSettingsPlayer(PluginServices.PluginInterface);
		SetTemporaryModSettingsPlayerSubscriber = new global::Penumbra.Api.IpcSubscribers.SetTemporaryModSettingsPlayer(PluginServices.PluginInterface);

		GetChangedItemAdapterDictionarySubscriber = new global::Penumbra.Api.IpcSubscribers.GetChangedItemAdapterDictionary(PluginServices.PluginInterface);
		GetChangedItemAdapterListSubscriber = new global::Penumbra.Api.IpcSubscribers.GetChangedItemAdapterList(PluginServices.PluginInterface);

		GetAllModSettingsSubscriber = new global::Penumbra.Api.IpcSubscribers.GetAllModSettings(PluginServices.PluginInterface);

		RemoveAllTemporaryModSettingsPlayerSubscriber = new global::Penumbra.Api.IpcSubscribers.RemoveAllTemporaryModSettingsPlayer(PluginServices.PluginInterface);
		QueryTemporaryModSettingsPlayerSubscriber = new global::Penumbra.Api.IpcSubscribers.QueryTemporaryModSettingsPlayer(PluginServices.PluginInterface);

		GetModPathSubscriber = new global::Penumbra.Api.IpcSubscribers.GetModPath(PluginServices.PluginInterface);

		GetCurrentModSettingsWithTempSubscriber = new global::Penumbra.Api.IpcSubscribers.GetCurrentModSettingsWithTemp(PluginServices.PluginInterface);

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
		=> GetMods().Where(m => 
			!ConfigurationManager.Config.PenumbraModsBlacklist.Contains(m) 
			&& !IsModPathBlacklisted(m.Path)
		).ToList();

	internal IEnumerable<(string ModDir, EquipItem Item)> GetModdedEquipItemsNotBlacklisted() {
		try {
			var it = GetChangedItemAdapterListSubscriber.Invoke() // get the list of changed items per mod and adapter
				.Where(r => !ConfigurationManager.Config.PenumbraModsBlacklist.Any(m => m.Path == r.ModDirectory) // filter out blacklisted mod directories
					&& !IsModPathBlacklisted(r.ModDirectory)) // filter out blacklisted mod paths
				.SelectMany((p, l) => p.ChangedItems.Select((v, k) => (p.ModDirectory, TryExtractEquipItemData(v.Value)))) // extract equip item data and keep the item id for filtering
				.Where(u => u.Item2 != null 
					&& !ConfigurationManager.Config.PenumbraModsBlacklistByItemId.Any(b => b.Path == u.ModDirectory && b.ItemId == u.Item2.Value.Id.Item.Id)) // remove nullability and filter blacklisted mod+item combinations
				.Select(u => (u.ModDirectory, u.Item2!.Value)) // remove nullability after filtering out nulls
				;
			if (ConfigurationManager.Config.PenumbraUseModListCollection) {
				return it.Where(m => {
					return IsModAppliedInCollectionCached(m.ModDirectory);
				});
			} else {
				PluginLog.Debug($"Not filtering modded items by collection. Total count: {it.Count()}");
			}
			return it;
		} catch (Exception) {
			return [];
		}
	}



	internal List<InventoryItem> GetModdedInventoryItems() {

		ResetCache();

		var moddedItems = PluginServices.Penumbra.GetModdedEquipItemsNotBlacklisted();
		PluginLog.Debug($"Found {moddedItems.Count()} modded items from Penumbra after filtering blacklisted mods.");

		PluginServices.Storage.ModsReloadingMax = moddedItems.Count();

		List<InventoryItem> inventoryItems = moddedItems.Select((p) => {
			var name = GetModNameCache(p.ModDir);
			var meta = GetModMetaCached(p.ModDir);
			var configTexts = GetModConfigFileTextCached(p.ModDir);


			var item = new InventoryItem((InventoryType)InventoryTypeExtra.ModdedItems, p.Item.Id.Item.Id) {
				ModDirectory = p.ModDir,
				ModName = name,
				ModModelPath = p.Item.ModelString,
			};


			if (meta != null && meta.HasValues) {
				item.ModAuthor = meta.GetValue("Author")?.ToString();
				item.ModVersion = meta.GetValue("Version")?.ToString();
				item.ModWebsite = meta.GetValue("Website")?.ToString();
				item.ModIconPath = FindIconForItem(item.Icon, configTexts)?.ModdedIconFilePath;
			}

			PluginServices.Storage.ModsReloadingCur++;

			return item;
		}).ToList();

		ResetCache();

		return inventoryItems;
	}

	internal void GetAllModSettings() {
		var collection = ConfigurationManager.Config.PenumbraCollectionModList;
		var collectionId = GetCurrentCollectionGuid();
		if (collectionId == null) {
			PluginLog.Debug(collection == null
				? $"No collection specified for mod list filtering. Cannot get all mod settings. Collection name from config: {ConfigurationManager.Config.PenumbraCollectionModList}"
				: $"Failed to get current collection ID for collection {collection}. Cannot get all mod settings for collection {collection}.");
			return;
		}
		var response = GetAllModSettingsSubscriber.Invoke(collectionId.Value, false, false);
		if (response.Item1 != PenumbraApiEc.Success || response.Item2 == null) {
			PluginLog.Warning($"Failed to get all mod settings for collection {collection}. Status: {response.Item1}");
			return;
		}
		var allSettings = response.Item2;
		foreach (var setting in allSettings) {
			var modDir = setting.Key;
			var enabledState = setting.Value.Item1;
			var priority = setting.Value.Item2;
			var options = setting.Value.Item3;
			var inherited = setting.Value.Item4;
			PluginLog.Debug($"Mod: {modDir}, Enabled: {enabledState}, Priority: {priority}, Inherited: {inherited}");
			foreach (var optionGroup in options) {
				var groupName = optionGroup.Key;
				var enabledOptions = optionGroup.Value;
				PluginLog.Debug($"  Option Group: {groupName}, Enabled Options: {string.Join(", ", enabledOptions)}");
			}
		}
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
		//PluginLog.Debug($"Getting config file texts for mod {modDirectory} with base path {bp}");
		if (bp != null) {

			var jsonsPath = Path.Combine(bp, modDirectory);

			//PluginLog.Debug($"Looking for config files in {jsonsPath}");

			// Verify the path exists before attempting enumeration
			if (!Directory.Exists(jsonsPath)) {
				PluginLog.Warning($"Config directory does not exist: {jsonsPath}");
				return new();
			}

			try {
				string[] files = Directory.GetFiles(jsonsPath, "*.json", SearchOption.TopDirectoryOnly);
				//PluginLog.Debug($"Found {files.Length} config files for mod {modDirectory}: {string.Join(", ", files)}");
				return files.ToDictionary(n => n, File.ReadAllText);
			} catch (UnauthorizedAccessException ex) {
				PluginLog.Warning($"Access denied reading config files from {jsonsPath}: {ex.Message}");
				return new();
			} catch (Exception ex) {
				PluginLog.Error(ex, $"Failed to read config files from {jsonsPath}");
				return new();
			}
		}
		return new();
	}

	internal (string FullPath, bool FullDefault, bool NameDefault)? GetModPath(string modDirectory) {
		try {
			var response = GetModPathSubscriber.Invoke(modDirectory);
			if (response.Item1 != PenumbraApiEc.Success) {
				PluginLog.Warning($"Failed to get mod path for mod {modDirectory}. Status: {response.Item1}");
				return null;
			}
			return (response.Item2, response.Item3, response.Item4);
		} catch (Exception) {
			return null;
		}
	}
	internal string GetModPathString(string modDirectory) {
		return GetModPath(modDirectory)?.FullPath ?? string.Empty;
	}





	internal string? GetModDirectory() {
		try {
			return GetModDirectorySubscriber.Invoke();
		} catch (Exception) {
			return null;
		}
	}



	///////////// cache
	private void ResetCache() {
		ModDirectoryCache = null;
		ModNameCache = null;
		ModMetaCache = new();
		ModConfigFileTextCache = new();
		ModCollectionStateCache = null;
	}

	private string? ModDirectoryCache = null;
	internal string? GetModDirectoryCached()
		=> ModDirectoryCache ??= GetModDirectory();

	private Dictionary<string, string>? ModNameCache = new();
	internal string? GetModNameCache(string modDirectory) {
		if(ModNameCache == null) {
			ModNameCache = GetMods().ToDictionary(p => p.Path, p => p.Name);
		}
		if (ModNameCache != null && ModNameCache.ContainsKey(modDirectory))
			return ModNameCache[modDirectory];
		return null;
	}
	private Dictionary<string, JObject?> ModMetaCache = new();
	internal JObject? GetModMetaCached(string modDirectory) {
		if (!ModMetaCache.ContainsKey(modDirectory)) {
			var meta = GetModMeta(modDirectory);
			ModMetaCache[modDirectory] = meta;
		}
		return ModMetaCache[modDirectory];
	}
	private Dictionary<string, Dictionary<string, string>> ModConfigFileTextCache = new();
	internal Dictionary<string, string> GetModConfigFileTextCached(string modDirectory) {
		if (!ModConfigFileTextCache.ContainsKey(modDirectory)) {
			ModConfigFileTextCache[modDirectory] = GetConfigFileTexts(modDirectory);
		}
		return ModConfigFileTextCache[modDirectory];
	}
	private Dictionary<string, bool>? ModCollectionStateCache = null;
	internal bool IsModAppliedInCollectionCached(string modDirectory) {
		var collection = ConfigurationManager.Config.PenumbraCollectionModList;

		var collectionId = CollectionNameToGuid(collection);

		if (ModCollectionStateCache == null) {

			var response = GetAllModSettingsSubscriber.Invoke(collectionId, false, false);

			if (response.Item1 != PenumbraApiEc.Success || response.Item2 == null) {
				//PluginLog.Warning($"Failed to get all mod settings for collection {collection} when caching collection state. Status: {response.Item1}");
				return false;
			}
			var allSettings = response.Item2;

			ModCollectionStateCache = allSettings.Select(s => {
				var state = s.Value.Item1;
				//PluginLog.Debug($"Caching collection [{collection}] state for mod {s.Key} in collection {collection}: {state}");
				return (s.Key, state);
			}).ToDictionary(p => p.Key, p => p.state);
		}
		if(ModCollectionStateCache != null && ModCollectionStateCache.TryGetValue(modDirectory, out var state)) {
			return state;
		}
		return false;
	}

	private Dictionary<string, string> ModPathCache = [];
	internal string? GetModPathCacheCached(string modDirectory) {
		if (!ModPathCache.ContainsKey(modDirectory)) {
			ModPathCache[modDirectory] = GetModPathString(modDirectory);
		}
		return ModPathCache[modDirectory];
	}

	internal bool IsModPathBlacklisted(string modDirectory) {
		var modPath = GetModPathCacheCached(modDirectory);
		if (string.IsNullOrEmpty(modPath)) return false;

		// If whitelist has items, whitelist takes precedence
		if (ConfigurationManager.Config.PenumbraModsWhitelistByPath.Count > 0) {
			return !ConfigurationManager.Config.PenumbraModsWhitelistByPath.Any(whitelistedPattern => 
				PathMatchesBlacklistPattern(modPath, whitelistedPattern)
			);
		}

		// Otherwise use blacklist
		return ConfigurationManager.Config.PenumbraModsBlacklistByPath.Any(blacklistedPattern => 
			PathMatchesBlacklistPattern(modPath, blacklistedPattern)
		);
	}

	internal bool PathMatchesBlacklistPattern(string modPath, string pattern) {
		// Normalize path separators to forward slash
		modPath = modPath.Replace('\\', '/');
		pattern = pattern.Replace('\\', '/').TrimEnd('/');

		// Exact match
		if (modPath.Equals(pattern, StringComparison.OrdinalIgnoreCase)) 
			return true;

		// Prefix match: pattern matches if modPath starts with pattern followed by /
		// Example: "main1" blacklist matches "main1/sub1", "main1/sub1/mod1", etc.
		if (modPath.StartsWith(pattern + "/", StringComparison.OrdinalIgnoreCase)) 
			return true;

		return false;
	}

	internal bool SetTemporaryModSettings(InventoryItem item) {
		if (item.ModDirectory == null || item.ModName == null) return false;
		var player = PluginServices.Context.LocalPlayer?.ObjectIndex;
		if (player == null) return false;

		try {

			var colGuid = GetCollectionGuidForLocalPlayerCharacter();
			if(colGuid == null) return false;
			var getSettingsRes = GetCurrentModSettingsWithTempSubscriber.Invoke(
				colGuid.Value,
				item.ModDirectory,
				item.ModName,
				false,
				false,
				0);
			var tuple = getSettingsRes.Item2;
			var options = tuple?.Item3;
			var enabled = tuple?.Item1;
			var priority = tuple?.Item2;
			var inherited = tuple?.Item4;
			var unknown = tuple?.Item5;

			Dictionary<string, IReadOnlyList<string>> optionsSafe = [];
			if(options != null) {
				optionsSafe = options.ToDictionary(
					g => g.Key,
					g => (IReadOnlyList<string>)g.Value.ToList()
				);
			}


			PluginLog.Debug($"Setting temporary mod settings for player {player.Value} and mod {item.ModDirectory},{item.ModName}");
			var returned = SetTemporaryModSettingsPlayerSubscriber.Invoke(
				player.Value,
				item.ModDirectory,
				false,
				true,
				priority ?? 0,
				optionsSafe,
				"Dresser",
				0,
				item.ModName);
			return returned == PenumbraApiEc.Success;
		} catch (Exception) {
			return false;
		}
	}
	internal bool RemoveTemporaryModSettings(InventoryItem item) {
		if(item.ModDirectory == null || item.ModName == null) return false;
		var player = PluginServices.Context.LocalPlayer?.ObjectIndex;
		if (player == null) return false;

		try {
			var returned = RemoveTemporaryModSettingsPlayerSubscriber.Invoke(
				player.Value,
				item.ModDirectory,
				0,
				item.ModName);
			return returned == PenumbraApiEc.Success;
		} catch (Exception) {
			return false;
		}
	}
	internal bool RemoveAllTemporaryModSettings() {
		var player = PluginServices.Context.LocalPlayer?.ObjectIndex;
		if (player == null) return false;

		try {
			var returned = RemoveAllTemporaryModSettingsPlayerSubscriber.Invoke(player.Value);
			return returned == PenumbraApiEc.Success;
		} catch (Exception) {
			return false;
		}
	}

	/// <returns>The name of the collection assigned to the given <paramref name="type"/> or an empty string if none is assigned or type is invalid.</returns>
	internal Guid? GetCollectionForType(ApiCollectionType type) {
		try {
			return GetCollectionForTypeSubscriber.Invoke(type)?.Id;
		} catch (Exception) {
			return null;
		}
	}
	internal Guid? GetCollectionGuidForLocalPlayerCharacter() {
		return GetCollectionForType(ApiCollectionType.Yourself);
	}
	internal Guid? GetCurrentCollectionGuid() {
		return GetCollectionForType(ApiCollectionType.Current);
	}
	internal string? GetCurrentCollectionName() {
		try {
			return GetCollectionForTypeSubscriber.Invoke(ApiCollectionType.Current)?.Name;
		} catch (Exception) {
			return null;
		}
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
	}

	internal void CleanDresserApplyMod(InventoryItem item) {
		PluginLog.Debug($"reset apply state of mod {item.ModDirectory},{item.ModName}");
		RemoveTemporaryModSettings(item);
	}
	internal Guid CollectionNameToGuid(string collectionName) {
		return this.GetCollectionsByIdentifierSubscriber.Invoke(collectionName).First().Id;
	}









	////////////////////////////////////////
	/// Debug / extract EquipItem data 
	////////////////////////////////////////

	private static EquipItem? TryExtractEquipItemData(object? item) {
		if (item == null)
			return null;

		try {
			var itemType = item.GetType();
			if (itemType.Name != nameof(EquipItem))
				return null;

			// Try to invoke the remote EquipItem's explicit operator to PseudoEquipItem
			var opExplicitMethod = itemType.GetMethod(
				"op_Explicit",
				System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
				null,
				new[] { itemType },
				null
			);

			if (opExplicitMethod != null && opExplicitMethod.ReturnType == typeof(PseudoEquipItem)) {
				// Invoke the operator to get PseudoEquipItem
				var pseudo = (PseudoEquipItem)opExplicitMethod.Invoke(null, new object[] { item })!;

				// Now use the implicit operator to convert to our local EquipItem
				return (EquipItem)pseudo;
			}

			return null;
		} catch (Exception ex) {
			PluginLog.Debug(ex, "Failed to extract EquipItem via operator");
			return null;
		}
	}
	private static string DumpEquipItemFields(object? item) {
		if (item == null)
			return "Item is null";

		try {
			var type = item.GetType();
			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"=== {type.FullName} ===");
			sb.AppendLine($"AssemblyVersion: {type.Assembly.GetName().Version}");

			var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

			foreach (var field in fields) {
				var value = field.GetValue(item);

				// If it's a struct/class with an Id property, try to extract it
				if (value != null) {
					var idProp = value.GetType().GetProperty("Id");
					if (idProp != null) {
						var idValue = idProp.GetValue(value);
						sb.AppendLine($"{field.Name}: {value?.GetType().Name} => Id: {idValue}");
					} else {
						sb.AppendLine($"{field.Name}: {value}");
					}
				} else {
					sb.AppendLine($"{field.Name}: null");
				}
			}

			return sb.ToString();
		} catch (Exception ex) {
			return $"Error dumping fields: {ex}";
		}
	}

	internal bool GetChangedItemAdapterDictionaryPrintToLog() {
		try {
			var ddd = GetChangedItemAdapterDictionarySubscriber.Invoke();

			foreach ((var i, var j) in ddd) {
				foreach ((var k, var l) in j) {
					var data = TryExtractEquipItemData(l);
					if (data == null) continue;
					// Dump all fields for debugging
					PluginLog.Debug(DumpEquipItemFields(l));

					PluginLog.Debug($"mod: {i} || adapter: {k} || type: {l?.GetType()} || id: {data.Value.Id} || name: {data.Value.Name}");
				}
			}

			return true;
		} catch (Exception) {
			return false;
		}
	}

	internal bool GetChangedItemAdapterListPrintToLog() {
		try {
			var ddd = GetChangedItemAdapterListSubscriber.Invoke();

			foreach ((var i, var j) in ddd) {
				foreach ((var k, var l) in j) {
					var data = TryExtractEquipItemData(l);
					if (data == null) continue;

					PluginLog.Debug($"mod: {i} || adapter: {k} || type: {l?.GetType()} || id: {data.Value.Id} || name: {data.Value.Name}");
				}
			}

			return true;
		} catch (Exception) {
			return false;
		}
	}
}
