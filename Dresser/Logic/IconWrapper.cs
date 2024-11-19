using AllaganLib.GameSheets.Sheets.Rows;

using Dalamud.Interface.Textures;

using Dresser.Structs.Dresser;

using Lumina.Data.Files;

namespace Dresser.Logic {
	public class IconWrapper {

		public static ISharedImmediateTexture Get(uint? id) {
			if (id == null) return null;
			return PluginServices.TextureProvider.GetFromGameIcon(new GameIconLookup((uint)id));
				//.GetIcon((uint)id); ;
		}
		public static ISharedImmediateTexture Get(ItemRow? itemEx) {
			if (itemEx == null)
				return null;
			return PluginServices.TextureProvider.GetFromGameIcon(new GameIconLookup(itemEx.Icon));
		}
		public static ISharedImmediateTexture Get(InventoryItem? inventoryItem)
			=> PluginServices.ModdedIconStorage.Get(inventoryItem) ?? Get(inventoryItem?.Item);

		private TexFile? LoadIconHq(uint id) {
			var path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}_hr1.tex";
			return PluginServices.DataManager.GetFile<TexFile>(path);
		}
	}
}
