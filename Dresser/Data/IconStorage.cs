﻿using System;
using System.Collections.Generic;

using ImGuiScene;

using Dalamud.Utility;
using Lumina.Data.Files;

using CriticalCommonLib.Sheets;
using CriticalCommonLib.Models;

namespace Dresser.Data {
	public class IconStorage : IDisposable {

		private readonly Dictionary<uint, TextureWrap> _icons;

		public IconStorage() {
			_icons = new Dictionary<uint, TextureWrap>();
		}

		public TextureWrap this[int id]
			=> LoadIcon((uint)id);
		public TextureWrap Get(int? id)
			=> this[id ?? 0];
		public TextureWrap Get(ItemEx? itemEx) {
			if (itemEx == null)
			return this[0];
			return this[itemEx.Icon];
		}
		public TextureWrap Get(InventoryItem? inventoryItem)
			=> this.Get(inventoryItem?.Item);

		private TexFile? LoadIconHq(uint id) {
			var path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}_hr1.tex";
			return PluginServices.DataManager.GetFile<TexFile>(path);
		}
		public TextureWrap LoadIcon(uint id) {
			if (_icons.TryGetValue(id, out var ret))
				return ret;

			var icon = LoadIconHq(id) ?? PluginServices.DataManager.GetIcon(id)!;
			var iconData = icon.GetRgbaImageData();

			
			ret = PluginServices.PluginInterface.UiBuilder.LoadImageRaw(iconData, icon.Header.Width, icon.Header.Height, 4);
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