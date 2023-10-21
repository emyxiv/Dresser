using CriticalCommonLib.Sheets;

using Dalamud.Interface.Internal;
using Dalamud.Utility;

using Dresser.Structs.Dresser;
using Dresser.Logic;

using Lumina.Data.Files;

using System;
using System.Collections.Generic;
using System.IO;

namespace Dresser.Services {
	public class ModdedIconStorage : IDisposable {

		private readonly Dictionary<string, IDalamudTextureWrap?> _iconsModded;

		public ModdedIconStorage() {
			_iconsModded = new();
		}

		public IDalamudTextureWrap? Get(InventoryItem? inventoryItem) {
			if(inventoryItem != null && !inventoryItem.ModDirectory.IsNullOrWhitespace() && !inventoryItem.ModIconPath.IsNullOrWhitespace()) {
				var ic = LoadIconPenumbra(Path.Combine(inventoryItem.ModDirectory, inventoryItem.ModIconPath));
				if(ic != null) return ic;
			}
			return null;
		}
		public static string IconToTexPath(uint id, bool hr = true, bool hq = false) {
			return $"ui/icon/{id / 1000 * 1000:000000}/{(hq?"hq/":"")}{id:000000}{(hr? Storage.HighResolutionSufix:"")}.tex";
		}
		public static List<string> PossibleIconPaths(uint id) {
			return new List<string>() {
				IconToTexPath(id, true, false),
				IconToTexPath(id, true, true),
				IconToTexPath(id, false, false),
				IconToTexPath(id, false, true),
			};
		}
		public IDalamudTextureWrap? LoadIconPenumbra(string path) {
			if (_iconsModded.TryGetValue(path, out var ret))
				return ret;

			var penumbramodDir = PluginServices.Penumbra.GetModDirectoryCached();
			if (penumbramodDir != null) {
				var pathdd = Path.Combine(penumbramodDir, path);

				try {
					var tex = PluginServices.DataManager.GameData.GetFileFromDisk<TexFile>(pathdd);
					ret = PluginServices.TextureProvider.GetTextureFromFile(new FileInfo(pathdd));
					// also returns null or will generate more failed attempts
					_iconsModded[path] = ret;
					return ret;

				} catch (Exception e){
					_iconsModded[path] = null;
					PluginLog.Warning(e,$"Unable to load icon from {pathdd}, blacklisting it");
				}

			}

			return null;
		}
		public void Dispose() {
			foreach (var icon in _iconsModded.Values)
				icon?.Dispose();
			_iconsModded.Clear();
		}

	}
}
