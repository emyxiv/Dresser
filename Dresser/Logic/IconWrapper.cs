using CriticalCommonLib.Sheets;

using Dalamud.Utility;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Internal;

using Dresser.Structs.Dresser;
using Lumina.Data.Files;

using System;
using System.Collections.Generic;

namespace Dresser.Logic {
	public class IconWrapper {

		public static IDalamudTextureWrap? Get(int? id) {
			if (id == null) return null;
			return PluginServices.TextureProvider.GetIcon((uint)id); ;
		}
		public static IDalamudTextureWrap? Get(ItemEx? itemEx) {
			if (itemEx == null)
				return null;
			return PluginServices.TextureProvider.GetIcon(itemEx.Icon);
		}
		public static IDalamudTextureWrap? Get(InventoryItem? inventoryItem)
			=> PluginServices.ModdedIconStorage.Get(inventoryItem) ?? Get(inventoryItem?.Item);

		private TexFile? LoadIconHq(uint id) {
			var path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}_hr1.tex";
			return PluginServices.DataManager.GetFile<TexFile>(path);
		}
	}
}
