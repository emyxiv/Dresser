using CriticalCommonLib;
using CriticalCommonLib.Models;
using CriticalCommonLib.Resolvers;

using Dalamud.Logging;

using Dispatch;

using Dresser.Logic;
using Dresser.Windows;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Dresser.Services {
	public static class ConfigurationManager {
		public static Configuration Config {
			get;
			set;
		} = null!;

		public static string ConfigurationFile {
			get {
				return Service.Interface.ConfigFile.ToString();
			}
		}
		public static string InventoryFile {
			get {
				return Path.Join(Service.Interface.ConfigDirectory.FullName, "inventories.json");
			}
		}
		public static void Load() {
			PluginLog.Verbose("Loading configuration");

			var loadConfigStopwatch = new Stopwatch();
			loadConfigStopwatch.Start();

			if (!File.Exists(ConfigurationFile)) {
				Config = new Configuration();
				Config.SortOrder = InventoryItemOrder.Defaults();
				Config.MarkReloaded();
				return;
			}

			string jsonText = File.ReadAllText(ConfigurationFile);
			var inventoryToolsConfiguration = JsonConvert.DeserializeObject<Configuration>(jsonText, new JsonSerializerSettings() {
				DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
				ContractResolver = MinifyResolver
			});
			if (inventoryToolsConfiguration == null) {
				Config = new Configuration();
				Config.MarkReloaded();
				return;
			}

			loadConfigStopwatch.Stop();
			// inventory migration
			if (inventoryToolsConfiguration.InventoriesMigrated < 0) {
				PluginLog.Verbose("Migrating inventories");
				var temp = JObject.Parse(jsonText);
				if (temp.ContainsKey("SavedInventories")) {
					var inventories = temp["SavedInventories"]?.ToObject<Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>>>();
					inventoryToolsConfiguration.SavedInventories = inventories ??
																   new Dictionary<ulong, Dictionary<InventoryCategory,
																	   List<InventoryItem>>>();
				}

				inventoryToolsConfiguration.InventoriesMigrated = 0;
			} else {
				inventoryToolsConfiguration.SavedInventories = LoadSavedInventories() ?? new();
			}

			loadConfigStopwatch.Stop();
			PluginLog.Verbose("Took " + loadConfigStopwatch.Elapsed.TotalSeconds + " to load inventories.");
			Config = inventoryToolsConfiguration;

			// Config migration
			Config.Migrate();

			Config.MarkReloaded();
		}

		public static void Save() {
			var loadConfigStopwatch = new Stopwatch();
			loadConfigStopwatch.Start();

			PluginLog.Verbose("Saving allagan tools configuration");
			try {
				File.WriteAllText(ConfigurationFile, JsonConvert.SerializeObject(Config, Formatting.None, new JsonSerializerSettings() {
					TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
					TypeNameHandling = TypeNameHandling.Objects,
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
					DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
					ContractResolver = MinifyResolver
				}));

				loadConfigStopwatch.Stop();
				PluginLog.Verbose("Took " + loadConfigStopwatch.Elapsed.TotalSeconds + " to save configuration.");

				SaveSavedInventories(Config.SavedInventories);
				if (PluginServices.Context.IsBrowserWindowOpen) GearBrowser.RecomputeItems();
			} catch (Exception e) {
				PluginLog.Error($"Failed to save allagan tools configuration due to {e.Message}");
			}
		}



		private static SerialQueue _saveQueue = new SerialQueue();

		public static void SaveAsync() {
			_saveQueue.DispatchAsync(Save);
		}

		public static void ClearQueue() {
			_saveQueue.Dispose();
			_saveQueue = null!;
		}

		public static Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>>? LoadSavedInventories(string? fileName = null) {
			try {
				fileName ??= InventoryFile;
				PluginLog.Verbose("Loading inventories from " + fileName);
				var cacheFile = new FileInfo(fileName);
				string json = File.ReadAllText(cacheFile.FullName, Encoding.UTF8);
				return JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>>>(json, new JsonSerializerSettings() {
					DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
					ContractResolver = MinifyResolver
				});
			} catch (Exception e) {
				PluginLog.Error("Error while parsing saved saved inventory data, " + e.Message);
				return null;
			}
		}

		public static MinifyResolver MinifyResolver => _minifyResolver ??= new();
		private static MinifyResolver? _minifyResolver;

		public static void SaveSavedInventories(Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> savedInventories) {

			Dictionary<ulong, Dictionary<InventoryCategory, List<InventoryItem>>> newSavedInventories = new();

			foreach (var key in savedInventories.Keys) {
				newSavedInventories[key] = new Dictionary<InventoryCategory, List<InventoryItem>>();
				var inventoryDict = savedInventories[key];
				foreach (var key2 in inventoryDict.Keys) {
					var newList = inventoryDict[key2].ToList();
					newSavedInventories[key][key2] = newList;
				}
			}

			var cacheFile = new FileInfo(InventoryFile);
			PluginLog.Verbose($"Saving inventory data at {cacheFile.FullName} {newSavedInventories.Count()}");
			try {
				File.WriteAllText(cacheFile.FullName, JsonConvert.SerializeObject(newSavedInventories, Formatting.None, new JsonSerializerSettings() {
					TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
					TypeNameHandling = TypeNameHandling.Objects,
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
					DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
					ContractResolver = MinifyResolver
				}));
			} catch (Exception e) {
				PluginLog.Error($"Failed to save inventories due to {e.Message}");
			}
		}
	}
}