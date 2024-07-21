using Dalamud.Interface.Textures;
using Dalamud.Utility;

using Dresser.Logic;
using Dresser.Structs.Dresser;

using Lumina.Data.Files;

using System;
using System.Collections.Generic;
using System.IO;

namespace Dresser.Services {
	public class ModdedIconStorage : IDisposable {


		public ModdedIconStorage() {
		}

		public ISharedImmediateTexture? Get(InventoryItem? inventoryItem) {
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
		public ISharedImmediateTexture? LoadIconPenumbra(string path) {
			var penumbramodDir = PluginServices.Penumbra.GetModDirectoryCached();
			if (penumbramodDir != null) {
				var pathdd = Path.Combine(penumbramodDir, path);

				try {
					var tex = PluginServices.DataManager.GameData.GetFileFromDisk<TexFile>(pathdd);
					return PluginServices.TextureProvider.GetFromFile(pathdd);

				} catch (Exception e){
					PluginLog.Warning(e,$"Unable to load icon from {pathdd}");
				}

			}

			return null;
		}
		public void Dispose() {
		}

	}
}
