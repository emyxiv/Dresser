using Dalamud.Logging;

using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

using System;
using System.Collections.Generic;
using System.Linq;

using PseudoEquipItem = System.ValueTuple<string, ulong, ushort, ushort, ushort, byte, byte>;
using CurrentSettings = System.ValueTuple<Penumbra.Api.Enums.PenumbraApiEc, (bool EnabledState, int Priority, System.Collections.Generic.IDictionary<string, System.Collections.Generic.IList<string>> EnabledOptions, bool Inherited)?>;
using Lumina.Excel.GeneratedSheets;
using System.Collections;
using CriticalCommonLib.Extensions;
using ImGuizmoNET;
using System.Threading.Tasks;
using Dresser.Structs.Dresser;
using static Dresser.Services.Storage;
using CriticalCommonLib.Enums;

namespace Dresser.Services;

internal class PenumbraIpc : IDisposable {
	private FuncSubscriber<IList<(string, string)>> GetModsSubscriber { get; }
	private FuncSubscriber<string, IReadOnlyDictionary<string, dynamic?>> GetChangedItemsSubscriber { get; }
	private FuncSubscriber<string, PenumbraApiEc> CreateNamedTemporaryCollectionSubscriber { get; }
	private FuncSubscriber<string, PenumbraApiEc> RemoveTemporaryCollectionByNameSubscriber { get; }
	private FuncSubscriber<string, string, Dictionary<string, string>, string, int, PenumbraApiEc> AddTemporaryModSubscriber { get; }
	private FuncSubscriber<string, string, int, PenumbraApiEc> RemoveTemporaryModSubscriber { get; }


	private FuncSubscriber<string, string, string, bool, (PenumbraApiEc, (bool, int, IDictionary<string, IList<string>>, bool)?)> GetCurrentModSettingsSubscriber { get; }
	private FuncSubscriber<string, string, string, string, string, PenumbraApiEc> TrySetModSettingSubscriber { get; }
	private FuncSubscriber<string, string, string, string, IReadOnlyList<string>, PenumbraApiEc> TrySetModSettingsSubscriber { get; }
	private FuncSubscriber<string, string, string, bool, PenumbraApiEc> TryInheritModSubscriber { get; }
	
	private FuncSubscriber<(int Breaking, int Features)> ApiVersionsSubscriber { get; }
	private FuncSubscriber<bool> GetEnabledStateSubscriber { get; }


	private FuncSubscriber<string> GetModDirectorySubscriber { get; }
	private FuncSubscriber<string, PenumbraApiEc> AddModSubscriber { get; }
	private FuncSubscriber<string, string, PenumbraApiEc> ReloadModSubscriber { get; }
	private FuncSubscriber<string, string, string, PenumbraApiEc> SetModPathSubscriber { get; }
	private FuncSubscriber<string, string, PenumbraApiEc> DeleteModSubscriber { get; }
	private FuncSubscriber<string, string, string, PenumbraApiEc> CopyModSettingsSubscriber { get; }
	private FuncSubscriber<IList<string>> GetCollectionsSubscriber { get; }
	private FuncSubscriber<string, string, string, bool, PenumbraApiEc> TrySetModSubscriber { get; }
	private FuncSubscriber<string, string, (PenumbraApiEc, string, bool)> GetModPathSubscriber { get; }

	private EventSubscriber<string, string>? Test { get; set; }

	internal PenumbraIpc() {
		GetModsSubscriber = Penumbra.Api.Ipc.GetMods.Subscriber(PluginServices.PluginInterface);
		GetChangedItemsSubscriber = Penumbra.Api.Ipc.GetChangedItems.Subscriber(PluginServices.PluginInterface);
		CreateNamedTemporaryCollectionSubscriber = Penumbra.Api.Ipc.CreateNamedTemporaryCollection.Subscriber(PluginServices.PluginInterface);
		RemoveTemporaryCollectionByNameSubscriber = Penumbra.Api.Ipc.RemoveTemporaryCollectionByName.Subscriber(PluginServices.PluginInterface);
		AddTemporaryModSubscriber = Penumbra.Api.Ipc.AddTemporaryMod.Subscriber(PluginServices.PluginInterface);
		RemoveTemporaryModSubscriber = Penumbra.Api.Ipc.RemoveTemporaryMod.Subscriber(PluginServices.PluginInterface);

		GetCurrentModSettingsSubscriber = Penumbra.Api.Ipc.GetCurrentModSettings.Subscriber(PluginServices.PluginInterface);
		TrySetModSettingSubscriber = Penumbra.Api.Ipc.TrySetModSetting.Subscriber(PluginServices.PluginInterface);
		TrySetModSettingsSubscriber = Penumbra.Api.Ipc.TrySetModSettings.Subscriber(PluginServices.PluginInterface);
		TryInheritModSubscriber = Penumbra.Api.Ipc.TryInheritMod.Subscriber(PluginServices.PluginInterface);

		ApiVersionsSubscriber = Penumbra.Api.Ipc.ApiVersions.Subscriber(PluginServices.PluginInterface);
		GetEnabledStateSubscriber = Penumbra.Api.Ipc.GetEnabledState.Subscriber(PluginServices.PluginInterface);

		GetModDirectorySubscriber = Penumbra.Api.Ipc.GetModDirectory.Subscriber(PluginServices.PluginInterface);
		AddModSubscriber = Penumbra.Api.Ipc.AddMod.Subscriber(PluginServices.PluginInterface);
		ReloadModSubscriber = Penumbra.Api.Ipc.ReloadMod.Subscriber(PluginServices.PluginInterface);
		SetModPathSubscriber = Penumbra.Api.Ipc.SetModPath.Subscriber(PluginServices.PluginInterface);
		DeleteModSubscriber = Penumbra.Api.Ipc.DeleteMod.Subscriber(PluginServices.PluginInterface);
		CopyModSettingsSubscriber = Penumbra.Api.Ipc.CopyModSettings.Subscriber(PluginServices.PluginInterface);
		GetCollectionsSubscriber = Penumbra.Api.Ipc.GetCollections.Subscriber(PluginServices.PluginInterface);
		TrySetModSubscriber = Penumbra.Api.Ipc.TrySetMod.Subscriber(PluginServices.PluginInterface);
		GetModPathSubscriber = Penumbra.Api.Ipc.GetModPath.Subscriber(PluginServices.PluginInterface);

		RegisterEvents();
	}

	public void Dispose() {
		Test?.Dispose();
	}

	private void RegisterEvents() {
		this.Test = Penumbra.Api.Ipc.ModMoved.Subscriber(PluginServices.PluginInterface, (_, _) => {
		});
	}

	internal IList<(string Path, string Name)> GetMods() {
		try {
			return GetModsSubscriber.Invoke();
		} catch (Exception) {
			return new List<(string, string)>();
		}
	}
	internal IList<(string Path, string Name)> GetNotBlacklistedMods()
		=> GetMods().Where(m => !ConfigurationManager.Config.PenumbraModsBlacklist.Contains(m)).ToList();

	/// <returns>A dictionary of affected items in <paramref name="collectionName"/> via name and known objects or null.</returns>
	internal IReadOnlyDictionary<string, dynamic?> GetChangedItemsForCollection(string collectionName) {
		try {
			return GetChangedItemsSubscriber.Invoke(collectionName);
		} catch (Exception) {
			return new Dictionary<string, dynamic?>();
		}
	}
	private const string PenumbraCollectionTmp = "Dresser ZZZ";
	internal IEnumerable<(uint ItemId, string ModModelPath)> GetChangedItemIdsForMod(string modPath, string modName) {

		List<(uint ItemId, string ModModelPath)> items = new();

		var res5 = PluginServices.Penumbra.TrySetMod(PenumbraCollectionTmp, modPath, true);
		PluginLog.Warning($"Enable mod (path:){modPath}: {res5}");

		//var tempCollection = $"DaCol_{modCount}";
		//PluginLog.Debug($"Mod enabled, creating temp collection {tempCollection}");
		//var statusCreatedTmpCol = PluginServices.Penumbra.CreateNamedTemporaryCollection(tempCollection);
		//Task.Run(async delegate { await Task.Delay(500); }).Wait();
		//if (statusCreatedTmpCol == PenumbraApiEc.Success) {

		//foreach ((var optionGroup, var options) in modSettings.Item2.Value.EnabledOptions) {
		//	PluginServices.Penumbra.TrySetModSettings(tempCollection, mod.Path, mod.Name, optionGroup, options.ToList());
		//}

		Task.Run(async delegate { await Task.Delay(50); }).Wait();
		var changedItems = PluginServices.Penumbra.GetChangedEquipItemsForCollection(PenumbraCollectionTmp);


		foreach (var i in changedItems) {
			items.Add((i.ItemId, i.ModelString));
		}

		//var ddd = PluginServices.Penumbra.RemoveTemporaryCollectionByName(tempCollection);
		//PluginLog.Debug($"{tempCollection} {(ddd == PenumbraApiEc.Success ? "removed" : "NOT REMOVED")}");
		//}
		var res6 = PluginServices.Penumbra.TrySetMod(PenumbraCollectionTmp, modPath, false);
		PluginLog.Debug($"Re-enable mod {modPath}: {res6}");

		return items;
	}

	internal List<(string Path, string Name)> GetEnabledModsForCollection(string collection, bool allowInheritance) {
		List<(string Path, string Name)> DaCollModsSettings = new();
		foreach (var mod in PluginServices.Penumbra.GetMods()) {
			var modSettings = PluginServices.Penumbra.GetCurrentModSettings(collection, mod.Path, mod.Name, allowInheritance);
			if (modSettings.Item1 == PenumbraApiEc.Success && modSettings.Item2.HasValue && modSettings.Item2.Value.EnabledState) {
				PluginServices.Storage.ModsReloadingMax++;
				PluginLog.Debug($"Found ACTIVE mod {mod.Name} || {mod.Path}");

				DaCollModsSettings.Add(mod);
			}
		}
		return DaCollModsSettings;
	}
	internal List<InventoryItem> GetChangedInventoryItemForMods(List<(string Path, string Name)> mods) {
		List<InventoryItem> tmpItemList = new();
		foreach (var mod3 in mods) {

			foreach (var i in PluginServices.Penumbra.GetChangedItemIdsForMod(mod3.Path, mod3.Name)) {
				var item = new InventoryItem((InventoryType)InventoryTypeExtra.ModdedItems, i.ItemId.Copy(), mod3.Name.Copy()!, mod3.Path.Copy()!, i.ModModelPath.Copy()!);
				// todo: add icon path
				tmpItemList.Add(item);
				//PluginLog.Debug($"Added item {item.ItemId} [{item.FormattedName}] for mod {item.ModName} || {item.ModDirectory}");
			}
			PluginServices.Storage.ModsReloadingCur++;
		}
		return tmpItemList;
	}

	internal IEnumerable<EquipItem> GetChangedEquipItemsForCollection(string collectionName) {
		return this.GetChangedItemsForCollection(collectionName).Where(o => {
			try {
				EquipItem ddd = (PseudoEquipItem)o.Value;
				return true;
			} catch (Exception) { }

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

	internal bool AddMod(string path) {
		try {
			return AddModSubscriber.Invoke(path) == PenumbraApiEc.Success;
		} catch (Exception) {
			return false;
		}
	}

	internal bool ReloadMod(string directoryName) {
		try {
			return ReloadModSubscriber.Invoke(directoryName, "") == PenumbraApiEc.Success;
		} catch (Exception) {
			return false;
		}
	}

	internal bool SetModPath(string directoryName, string newPath) {
		try {
			return SetModPathSubscriber.Invoke(directoryName, "", newPath) == PenumbraApiEc.Success;
		} catch (Exception) {
			return false;
		}
	}

	internal bool DeleteMod(string directoryName) {
		try {
			return DeleteModSubscriber.Invoke(directoryName, "") == PenumbraApiEc.Success;
		} catch (Exception) {
			return false;
		}
	}

	/// <summary>
	/// Copy all current settings for a mod to another mod.
	/// </summary>
	/// <param name="collectionName">Specify the collection to work in, leave empty or null to do it in all collections.</param>
	/// <param name="modDirectoryFrom">Specify the mod to take the settings from via its directory name.</param>
	/// <param name="modDirectoryTo">Specify the mod to put the settings on via its directory name. If the mod does not exist, it will be added as unused settings.</param>
	/// <returns>CollectionMissing if collectionName is not empty but does not exist or Success.</returns>
	/// <remarks>If the target mod exists, the settings will be fixed before being applied. If the source mod does not exist, it will use unused settings if available and remove existing settings otherwise.</remarks>
	internal bool CopyModSettings(string from, string to) {
		try {
			return CopyModSettingsSubscriber.Invoke("", from, to) == PenumbraApiEc.Success;
		} catch (Exception) {
			return false;
		}
	}

	/// <returns>A list of the names of all currently installed collections.</returns>
	internal IList<string>? GetCollections() {
		try {
			return GetCollectionsSubscriber.Invoke();
		} catch (Exception) {
			return null;
		}
	}

	/// <summary> Try to set the enabled state of a mod in a collection. </summary>
	internal bool TrySetMod(string collection, string directory, bool enabled) {
		try {
			return TrySetModSubscriber.Invoke(collection, directory, "", enabled) == PenumbraApiEc.Success;
		} catch (Exception) {
			return false;
		}
	}

	/// <summary> Try to set the inheritance state of a mod in a collection. </summary>
	/// <returns>ModMissing, CollectionMissing, NothingChanged or Success.</returns>
	internal PenumbraApiEc TryInheritMod(string collectionName, string modDirectory, string modName, bool inherit) {
		try {
			return TryInheritModSubscriber.Invoke(collectionName, modDirectory, modName, inherit);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
		}
	}


	/// <summary>
	/// Get the internal full filesystem path including search order for the specified mod
	/// given by its <paramref name="modDirectory" /> name or <paramref name="modName" />.
	/// </summary>
	/// <returns>On Success, the full path and a bool indicating whether this is default (false) or manually set (true).
	/// Otherwise returns ModMissing if the mod can not be found.</returns>
	internal string? GetModPath(string directoryName) {
		try {
			var (status, path, _) = GetModPathSubscriber.Invoke(directoryName, "");
			return status == PenumbraApiEc.Success ? path : null;
		} catch (Exception) {
			return null;
		}
	}

	/// <summary>
	/// Obtain the enabled state, the priority, the settings of a mod given by its <paramref name="modDirectory" /> name or <paramref name="modName" /> in the specified collection.
	/// </summary>
	/// <param name="collectionName">Specify the collection.</param>
	/// <param name="modDirectory">Specify the mod via its directory name.</param>
	/// <param name="modName">Specify the mod via its (non-unique) display name.</param>
	/// <param name="allowInheritance">Whether the settings need to be from the given collection or can be inherited from any other by it.</param>
	/// <returns>ModMissing, CollectionMissing or Success. <para />
	/// On Success, a tuple of Enabled State, Priority, a dictionary of option group names and lists of enabled option names and a bool whether the settings are inherited or not.</returns>
	internal CurrentSettings GetCurrentModSettings(string collectionName, string modDirectory, string modName, bool allowInheritance) {
		try {
			return GetCurrentModSettingsSubscriber.Invoke(collectionName, modDirectory, modName, allowInheritance);
		} catch (Exception) {
			return (PenumbraApiEc.UnknownError, null);
		}
	}

	/// <summary> Try to set a specific option group of a mod in the given collection to a specific value. </summary>
	/// <remarks>Removes inheritance. Single Selection groups should provide a single option, Multi Selection can provide multiple.
	/// If any setting can not be found, it will not change anything.</remarks>
	/// <returns>ModMissing, CollectionMissing, OptionGroupMissing, SettingMissing, NothingChanged or Success.</returns>
	internal PenumbraApiEc TrySetModSetting(string collectionName, string modDirectory, string modName, string optionGroupName, string option) {
		try {
			return TrySetModSettingSubscriber.Invoke(collectionName, modDirectory, modName, optionGroupName, option);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
		}
	}
	internal PenumbraApiEc TrySetModSettings(string collectionName, string modDirectory, string modName, string optionGroupName, IReadOnlyList<string> options) {
		try {
			return TrySetModSettingsSubscriber.Invoke(collectionName, modDirectory, modName, optionGroupName, options);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
		}
	}


	/// <summary>
	/// Create a temporary collection of the given <paramref name="name"/>.
	/// </summary>
	/// <param name="name">The intended name. It may not be empty or contain symbols invalid in a path used by XIV.</param>
	/// <returns>Success, InvalidArgument if name is not valid for a collection, or TemporaryCollectionExists.</returns>
	internal PenumbraApiEc CreateNamedTemporaryCollection(string collectionName) {
		try {
			return CreateNamedTemporaryCollectionSubscriber.Invoke(collectionName);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
		}
	}

	/// <summary>
	/// Remove the temporary collection of the given name.
	/// </summary>
	/// <param name="collectionName">The chosen temporary collection to remove.</param>
	/// <returns>NothingChanged or Success.</returns>
	internal PenumbraApiEc RemoveTemporaryCollectionByName(string collectionName) {
		try {
			return RemoveTemporaryCollectionByNameSubscriber.Invoke(collectionName);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
		}
	}

	/// <summary>
	/// Set a temporary mod with the given paths, manipulations and priority and the name tag to a specific collection.
	/// </summary>
	/// <param name="tag">Custom name for the temporary mod.</param>
	/// <param name="collectionName">Name of the collection the mod should apply to. Can be a temporary collection name.</param>
	/// <param name="paths">List of redirections (can be swaps or redirections).</param>
	/// <param name="manipString">Zipped Base64 string of meta manipulations.</param>
	/// <param name="priority">Desired priority.</param>
	/// <returns>CollectionMissing, InvalidGamePath, InvalidManipulation or Success.</returns>
	internal PenumbraApiEc AddTemporaryMod(string tag, string collectionName, Dictionary<string, string> paths, string manipString, int priority) {
		try {
			return AddTemporaryModSubscriber.Invoke(tag, collectionName, paths, manipString, priority);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
		}
	}

	/// <summary>
	/// Remove the temporary mod with the given tag and priority from the temporary mods applying to a specific collection, if it exists.
	/// </summary>
	/// <param name="tag">The tag to look for.</param>
	/// <param name="collectionName">Name of the collection the mod should apply to. Can be a temporary collection name.</param>
	/// <param name="priority">The initially provided priority.</param>
	/// <returns>CollectionMissing, NothingDone or Success.</returns>
	internal PenumbraApiEc RemoveTemporaryMod(string tag, string collectionName, int priority) {
		try {
			return RemoveTemporaryModSubscriber.Invoke(tag, collectionName, priority);
		} catch (Exception) {
			return PenumbraApiEc.UnknownError;
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
	}
}
