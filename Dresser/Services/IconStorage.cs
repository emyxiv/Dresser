using CriticalCommonLib.Models;
using CriticalCommonLib.Sheets;

using Dalamud.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Internal;

using Lumina.Data.Files;

using System;
using System.Collections.Generic;

namespace Dresser.Services {
	public class IconStorage : IDisposable {

		private readonly Dictionary<uint, IDalamudTextureWrap> _icons;

		public IconStorage() {
			_icons = new Dictionary<uint, IDalamudTextureWrap>();
		}

		public IDalamudTextureWrap this[int id]
			=> LoadIcon((uint)id);
		public IDalamudTextureWrap Get(int? id)
			=> this[id ?? 0];
		public IDalamudTextureWrap Get(ItemEx? itemEx) {
			if (itemEx == null)
				return this[0];
			return this[itemEx.Icon];
		}
		public IDalamudTextureWrap Get(InventoryItem? inventoryItem)
			=> Get(inventoryItem?.Item);

		private TexFile? LoadIconHq(uint id) {
			var path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}_hr1.tex";
			return PluginServices.DataManager.GetFile<TexFile>(path);
		}
		public IDalamudTextureWrap LoadIcon(uint id) {
			if (_icons.TryGetValue(id, out var ret))
				return ret;

			ret = PluginServices.TextureProvider.GetIcon(id);

			//var icon = LoadIconHq(id) ?? PluginServices.DataManager.GetIcon(id)!;
			//var iconData = icon.GetRgbaImageData();


			//ret = PluginServices.PluginInterface.UiBuilder.LoadImageRaw(iconData, icon.Header.Width, icon.Header.Height, 4);
			_icons[id] = ret;
			return ret;
		}
		public void Dispose() {
			foreach (var icon in _icons.Values)
				icon.Dispose();
			_icons.Clear();
		}

		~IconStorage()
			=> Dispose();
	}
}
