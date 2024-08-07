using Dispatch;

using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Structs.Dresser;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;


namespace Dresser.Services {
	public static class ConfigurationManager {
		public static Configuration Config {
			get;
			set;
		} = null!;

		public static string ConfigurationFile {
			get {
				return PluginServices.PluginInterface.ConfigFile.ToString();
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
				SerializationBinder = MinifyBinder,
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

			PluginLog.Verbose("Saving dresser configuration");
			try {
				File.WriteAllText(ConfigurationFile, JsonConvert.SerializeObject(Config, Formatting.None, new JsonSerializerSettings() {
					TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
					TypeNameHandling = TypeNameHandling.Objects,
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
					DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
					SerializationBinder = MinifyBinder,
					ContractResolver = MinifyResolver
				}));

				loadConfigStopwatch.Stop();
				PluginLog.Verbose("Took " + loadConfigStopwatch.Elapsed.TotalSeconds + " to save configuration.");
			} catch (Exception e) {
				PluginLog.Error($"Failed to save dresser configuration due to {e.Message}");
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
		public static MinifyBinder MinifyBinder => _minifyBinder ??= new();
		private static MinifyBinder? _minifyBinder;
	}


	public class MinifyResolver : DefaultContractResolver {
		private readonly Dictionary<string, string> PropertyMappings;

		public MinifyResolver() {
			this.PropertyMappings = new(){
				// from critical impact
				{"Container", "con"},{"Slot", "sl"},{"ItemId", "iid"},{"Spiritbond", "sb"},{"Condition", "cnd"},{"Quantity", "qty"},{"Stain", "stn"},{"Stain2", "stn2"},{"Flags", "flgs"},{"Materia0", "mat0"},{"Materia1", "mat1"},{"Materia2", "mat2"},{"Materia3", "mat3"},{"Materia4", "mat4"},{"MateriaLevel0", "matl0"},{"MateriaLevel1", "matl1"},{"MateriaLevel2", "matl2"},{"MateriaLevel3", "matl3"},{"MateriaLevel4", "matl4"},{"SortedCategory", "soc"},{"SortedSlotIndex", "ssi"},{"SortedContainer", "sc"},{"RetainerId", "retid"},{"GlamourId", "glmid"},

				{"QuantityNeeded", "qtn"},
				{"GearSetNames", "gsn"},
				{"GearSets", "gs"},
				{"ModName", "mn"},
				{"ModModelPath", "mmp"},
				{"ModWebsite", "mw"},
				{"DebugName", "dbn"},
				{"ModDirectory", "md"},
				{"ModAuthor", "ma"},
				{"ModVersion", "mv"},
				{"ModIconPath", "mip"},

			};
		}

		//protected override string resolve
		protected override string ResolvePropertyName(string propertyName) {
			var resolved = PropertyMappings.TryGetValue(propertyName, out var resolvedName);
			return (resolved ? resolvedName : base.ResolvePropertyName(propertyName)) ?? propertyName;
		}
	}

	public class MinifyBinder : DefaultSerializationBinder {
		private readonly Dictionary<string, string> TypeMappings;

		public MinifyBinder() {
			TypeMappings = new() {
				{typeof(Vector4).FullName!, "t-v4"},
				{typeof(InventoryItem).FullName!, "t-ii"},
				{typeof(InventoryItemSet).FullName!, "t-iis"},
				{typeof(GlamourPlateSlot).FullName!, "t-gps"},
				{typeof(Dictionary<GlamourPlateSlot, InventoryItem?>).FullName!, "t-d-gps-iin"},
				{typeof((InventoryItemOrder.OrderMethod, InventoryItemOrder.OrderDirection)).FullName!, "t-t-ordm-ordd"},

			};
		}

		public override void BindToName(Type serializedType, out string? assemblyName, out string? typeName) {
			if (TypeMappings.TryGetValue(serializedType.FullName!, out var customTypeName) && customTypeName != null) {
				typeName = customTypeName;
			} else {
				base.BindToName(serializedType, out assemblyName, out typeName);
			}

			assemblyName = null;
		}


		public override Type BindToType(string? assemblyName, string typeName) {
			if (assemblyName != null && TypeMappings.TryGetValue(typeName, out var customTypeName) && customTypeName != null) {
				var t = Type.GetType(customTypeName);
				if (t != null)
					return t;
			}

			return base.BindToType(assemblyName, typeName);
		}
	}

}

