using AllaganLib.GameSheets.Sheets.Rows;

using Dresser.Interop.Hooks;
using Dresser.Services;
using Dresser.Windows;

using Lumina.Data;
using Lumina.Excel.Sheets;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Dresser.Structs.Dresser {


	public class Tag {
		public uint Id { get; }
		public string Name { get; }
		public GlamourPlateSlot? Slot { get; set; } = null;
		private uint? CategoryId { get; set; } = null;

		[JsonConstructor]
		public Tag(uint id, string name, GlamourPlateSlot? slot, uint? categoryId) {
			Id = id;
			Name = name;
			Slot = slot;
			CategoryId = categoryId;
		}
		public Tag(string name) : this(GenerateId(), name, null, null) { }

		public HashSet<uint> Items => ByTag(this);


		public Vector4 Color() {
			var hash = (uint)Name.GetHashCode();
			var r = ((hash & 0xFF0000) >> 16) / 255f;
			var g = ((hash & 0x00FF00) >> 8) / 255f;
			var b = (hash & 0x0000FF) / 255f;
			return new Vector4(r, g, b, 1f);
		}

		public static HashSet<Tag> ByItemId(uint itemId) {
			return TagStore.GetTagsForItem(itemId);
		}
		public static HashSet<uint> ByTag(Tag tag) {
			return TagStore.GetItemsForTag(tag.Id);
		}
		public static HashSet<Tag> All() => ConfigurationManager.Config.SavedTags;
		public static IEnumerable<Tag> TagNameContains(string searchTerm) {
			// todo make this more advanced (e.g. fuzzy search, ignore multi spaces, etc)
			return All().Where(t => t.Name.Trim().Contains(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase));
		}
		public static Tag? TagNameEquals(string searchTerm) {
			return All().FirstOrDefault(t => t.Name.Trim().Equals(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase));
		}
		public bool NameEquals(string searchTerm) {
			return Name.Trim().Equals(searchTerm.Trim(), StringComparison.OrdinalIgnoreCase);
		}
		private static uint GenerateId() {
			var existingTags = All();
			if (existingTags.Count == 0)
				return 1;
			var max = existingTags.Select(t => t.Id).Max();
			return max + 1;
		}
		public static Tag NewAndAssign(string name, ItemRow item, bool isNewTagSlot) {
			var tag = new Tag(name);
			tag.Slot = isNewTagSlot ? GearBrowser.SelectedSlot : null;
			ConfigurationManager.Config.SavedTags.Add(tag);

			TagStore.AddTag(new TagLink(item.RowId, tag.Id));
			item.GetSharedModels().ForEach(im => TagStore.AddTag(new TagLink(im.RowId, tag.Id)));
			return tag;
		}
		public static void Remove(Tag tag) {
			var itemIds = TagStore.GetItemsForTag(tag.Id).ToList();
			foreach (var itemId in itemIds) {
				TagStore.RemoveTag(new TagLink(itemId,  tag.Id));
			}
			ConfigurationManager.Config.SavedTags.Remove(tag);
		}
		public void Delete() => Remove(this);


	}

	public static class TagStore {

		public static Dictionary<uint, HashSet<uint>> itemToTags = [];
		public static Dictionary<uint, HashSet<uint>> tagToItems = [];

		public static HashSet<Tag> GetTagsForItem(uint itemId) {
			if (itemToTags.TryGetValue(itemId, out var tagIds)) {
				var tags = new HashSet<Tag>();
				foreach (var tagId in tagIds) {
					var tag = ConfigurationManager.Config.SavedTags.FirstOrDefault(t => t.Id == tagId);
					if (tag != null)
						tags.Add(tag);
				}
				return tags;
			}
			return [];
		}
		public static HashSet<uint> GetTagIdsForItem(uint itemId) {
			if (itemToTags.TryGetValue(itemId, out var tagIds)) {
				return tagIds;
			}
			return [];
		}
		public static HashSet<uint> GetItemsForTag(uint tagId) {
			if (tagToItems.TryGetValue(tagId, out var itemIds)) {
				return itemIds;
			}
			return [];
		}

		public static void LoadLinks() {
			itemToTags = ConfigurationManager.Config.ItemTags.GroupBy(tl => tl.Item).ToDictionary(g => g.Key, g => g.Select(p => p.Tag).ToHashSet());
			tagToItems = ConfigurationManager.Config.ItemTags.GroupBy(tl => tl.Tag).ToDictionary(g => g.Key, g => g.Select(p => p.Item).ToHashSet());
		}

		public static void AddTag(TagLink tagLink) {
			uint itm = tagLink.Item;
			uint tag = tagLink.Tag;

			if (!itemToTags.TryGetValue(itm, out var tags))
				itemToTags[itm] = tags = [];

			if (!tagToItems.TryGetValue(tag, out var itms))
				tagToItems[tag] = itms = [];

			if (tags.Add(tag))
				itms.Add(itm);

			ConfigurationManager.Config.ItemTags.Add(tagLink);
			GearBrowser.RecomputeItems();
		}

		public static void RemoveTag(TagLink tagLink) {

			uint itm = tagLink.Item;
			uint tag = tagLink.Tag;
			if (itemToTags.TryGetValue(itm, out var tags) && tags.Remove(tag)) {
				if (tags.Count == 0) itemToTags.Remove(itm);
				if (tagToItems.TryGetValue(tag, out var itms)) {
					itms.Remove(itm);
					if (itms.Count == 0) tagToItems.Remove(tag);
				}
			}
			ConfigurationManager.Config.ItemTags.Remove(tagLink);
			GearBrowser.RecomputeItems();
		}

	}

	public readonly struct TagLink(uint item, uint tag) {
		public readonly uint Item = item;
		public readonly uint Tag = tag;
	}
}
