using CriticalCommonLib;
using CriticalCommonLib.Resolvers;

using Dispatch;

using Dresser.Logic;

using Newtonsoft.Json;

using System;
using System.Diagnostics;
using System.IO;


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
		public static void Load() {
			PluginLog.Verbose("Loading configuration");

			var loadConfigStopwatch = new Stopwatch();
			loadConfigStopwatch.Start();

			if (!File.Exists(ConfigurationFile)) {
				Config = new Configuration();
				Config.Load();
				return;
			}

			string jsonText = File.ReadAllText(ConfigurationFile);
			jsonText = jsonText.Replace("\"$type\":\"CriticalCommonLib.Models.InventoryItem, CriticalCommonLib\"", "\"$type\":\"Dresser.Structs.Dresser.InventoryItem, Dresser\"");
			var inventoryToolsConfiguration = JsonConvert.DeserializeObject<Configuration>(jsonText, new JsonSerializerSettings() {
				//DefaultValueHandling = DefaultValueHandling,
				ContractResolver = MinifyResolver
			});
			if (inventoryToolsConfiguration == null) {
				Config = new Configuration();
				Config.Load();
				return;
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
		public static MinifyResolver MinifyResolver => _minifyResolver ??= new();
		private static MinifyResolver? _minifyResolver;
	}
}