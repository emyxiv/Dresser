using AllaganLib.GameSheets.Sheets;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Models;
using Dresser.Gui.Components;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Dresser.Gui {
	public class TagManager : Window, IDisposable {
		private Plugin Plugin;
		private string SearchFilter = string.Empty;
		private GlamourPlateSlot? SelectedSlotFilter = null;
		private Tag? SelectedTag = null;
		private string EditingTagName = string.Empty;
		private GlamourPlateSlot? EditingTagSlot = null;
		private bool IsEditingTag = false;
		private string NewTagName = string.Empty;
		private Dictionary<string, bool> SlotFoldStates = new(); // Track which slots are folded using string keys

		public TagManager(Plugin plugin) : base("Tag Manager", ImGuiWindowFlags.NoScrollbar) {
			this.SizeConstraints = new WindowSizeConstraints {
				MinimumSize = new Vector2(400, 300),
				MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
			};
			this.Plugin = plugin;
		}

		public void Dispose() { }

		public override void Draw() {
			var tags = Tag.All();

			// Filter and toolbar section in one line
			ImGui.AlignTextToFramePadding();
			ImGui.Text("Search:");
			ImGui.SameLine();
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 3.5f - ImGui.GetStyle().ItemSpacing.X);
			ImGui.InputText("##TagSearchFilter", ref SearchFilter, 128);

			ImGui.SameLine();
			ImGui.Text("Slot:");
			ImGui.SameLine();
			var slotOptions = new[] { "All Slots", "No Slot Restriction" }
				.Concat(Enum.GetValues<GlamourPlateSlot>().Select(s => s.ToString().AddSpaceBeforeCapital()))
				.ToArray();
			var selectedSlotIndex = SelectedSlotFilter.HasValue 
				? (int)SelectedSlotFilter.Value + 2
				: (SearchFilter.IsNullOrEmpty() ? 0 : 1);
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 3f - ImGui.GetStyle().ItemSpacing.X);
			if (ImGui.Combo("##TagSlotFilter", ref selectedSlotIndex, slotOptions)) {
				SelectedSlotFilter = selectedSlotIndex switch {
					0 => null, // All slots
					1 => null, // No slot restriction (but we'll filter)
					_ => (GlamourPlateSlot)(selectedSlotIndex - 2)
				};
			}

			// New Tag input in toolbar
			ImGui.SameLine();
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() * 4 - ImGui.GetStyle().ItemSpacing.X * 3);
			if (ImGui.InputTextWithHint("##NewTagNameInput","Add new tag...", ref NewTagName, 128, ImGuiInputTextFlags.EnterReturnsTrue)) {
				CreateNewTag();
			}
			ImGui.SameLine();
			if(GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Plus, "Create new tag", default, "##CreateTag")) {
				CreateNewTag();
			}

			// Export button
			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.FileExport, "Export tags to clipboard (Hold Ctrl for clear json export)", default, "##ExportTags")) {
				ExportTagsToClipboard();
			}

			// Import button
			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.FileImport, "Import tags from clipboard", default, "##ImportTags")) {
				ImportTagsFromClipboard();
			}

			// Delete all button
			ImGui.SameLine();
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Bomb, "Hold CTRL+SHIFT to unlock the delete all button. This will delete all tags.", new Vector2(ImGui.GetFrameHeight(), 0), "##DeleteAllTags")) {
				ShowDeleteAllConfirmation();
			}

			ImGui.Separator();

			// Filter tags based on search and slot
			var filteredTags = tags
				.Where(t => SearchFilter.IsNullOrEmpty() || t.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase))
				.Where(t => {
					if (selectedSlotIndex == 1) return !t.Slot.HasValue; // No slot restriction
					if (SelectedSlotFilter.HasValue) return t.Slot.HasValue && t.Slot.Value == SelectedSlotFilter.Value;
					return true; // All slots
				})
				.OrderBy(t => t.Slot)
				.ThenBy(t => t.Name)
				.ToList();

			// Main content area with two sections: tag list and editor
			var contentAvailable = ImGui.GetContentRegionAvail();

			// Dynamic width for tag list
			var maxTagNameWidth = filteredTags.Any() ? filteredTags.Max(t => ImGui.CalcTextSize(t.Name).X) : 150f;
			var maxSlotNameWidth = Enum.GetValues<GlamourPlateSlot>().Cast<GlamourPlateSlot>()
				.Select(s => s.ToString().AddSpaceBeforeCapital())
				.Select(n => ImGui.CalcTextSize(n).X)
				.DefaultIfEmpty(0)
				.Max();
			var tagLineMaxWidth = ImGui.GetStyle().IndentSpacing + maxTagNameWidth;
			var slotLineMaxWidth = (ImGui.GetFontSize() * 2) + ImGui.GetStyle().FramePadding.X + maxSlotNameWidth;
			var listWidth = Math.Max(tagLineMaxWidth, slotLineMaxWidth) + (ImGui.GetStyle().FramePadding.X * 2) + (ImGui.GetFontSize() * 2);

			ImGui.BeginGroup();
			DrawTagList(filteredTags, listWidth);
			ImGui.EndGroup();

			ImGui.SameLine();

			ImGui.BeginGroup();
			DrawTagEditor(contentAvailable.X - listWidth - ImGui.GetStyle().ItemSpacing.X);
			ImGui.EndGroup();

			// Draw delete all confirmation modal
			DrawDeleteAllConfirmationModal();
		}

		private void DrawTagList(List<Tag> filteredTags, float width) {
			ImGui.BeginChildFrame(1, new Vector2(width, ImGui.GetContentRegionAvail().Y));
			ImGui.Text($"Tags ({filteredTags.Count})");
			ImGui.Separator();

			// Group tags by slot using int keys to avoid null key issues
			// Key: -1 for null (universal), 0+ for enum values
			var tagsBySlotKey = new Dictionary<int, List<Tag>>();
			var slotKeyToSlot = new Dictionary<int, GlamourPlateSlot?>(); // Map int key back to actual slot

			foreach (var tag in filteredTags) {
				var slotKey = tag.Slot.HasValue ? (int)tag.Slot.Value : -1;

				if (!tagsBySlotKey.ContainsKey(slotKey)) {
					tagsBySlotKey[slotKey] = new List<Tag>();
					slotKeyToSlot[slotKey] = tag.Slot;
				}
				tagsBySlotKey[slotKey].Add(tag);
			}

			// Initialize fold states for new slots
			foreach (var slotKey in tagsBySlotKey.Keys) {
				var foldStateKey = slotKey == -1 ? "slot_null" : $"slot_{slotKey}";
				if (!SlotFoldStates.ContainsKey(foldStateKey)) {
					SlotFoldStates[foldStateKey] = true; // Default to open
				}
			}

			// Sort slots: universal (-1) first, then by enum value
			var sortedSlotKeys = tagsBySlotKey.Keys.OrderBy(k => k).ToList();

			// Draw slot groups with tree structure
			foreach (var slotKey in sortedSlotKeys) {
				var tags = tagsBySlotKey[slotKey];
				if (tags.Count == 0) continue;

				var slot = slotKeyToSlot[slotKey];
				var slotName = slot.HasValue ? slot.Value.ToString().AddSpaceBeforeCapital() : "Universal Tags";
				var foldStateKey = slotKey == -1 ? "slot_null" : $"slot_{slotKey}";

				// Safely get the fold state for this slot
				bool isFolded = SlotFoldStates.TryGetValue(foldStateKey, out var foldState) ? foldState : true;
				ImGui.SetNextItemOpen(isFolded);

				if (ImGui.TreeNode($"{slotName}##slot_{foldStateKey}")) {
					SlotFoldStates[foldStateKey] = true;

					foreach (var tag in tags) {
						var isSelected = SelectedTag?.Id == tag.Id;
						var tagColor = tag.Color();
						//ImGui.PushStyleColor(ImGuiCol.Button, isSelected ? tagColor * 1.2f : tagColor * 0.3f);
						//ImGui.PushStyleColor(ImGuiCol.ButtonHovered, tagColor * 0.5f);
						//ImGui.PushStyleColor(ImGuiCol.ButtonActive, tagColor * 0.7f);

						var buttonLabel = $"{tag.Name}##TagButton{tag.Id}";
						ImGui.Indent();
						if (ImGui.Selectable(buttonLabel)) {
						//if (ImGui.Button(buttonLabel, new Vector2(ImGui.GetContentRegionAvail().X, 0))) {
							SelectedTag = tag;
							EditingTagName = tag.Name;
							EditingTagSlot = tag.Slot;
							IsEditingTag = false;
						}
						ImGui.Unindent();

						//ImGui.PopStyleColor(3);

						// Context menu for delete
						//if (ImGui.BeginPopupContextItem($"TagListContext{tag.Id}")) {
						//	if (ImGui.Selectable("Delete", false)) {
						//		Tag.Remove(tag);
						//		if (SelectedTag?.Id == tag.Id) {
						//			SelectedTag = null;
						//			IsEditingTag = false;
						//		}
						//		ImGui.CloseCurrentPopup();
						//	}
						//	ImGui.EndPopup();
						//}
					}

					ImGui.TreePop();
				} else {
					SlotFoldStates[foldStateKey] = false;
				}
			}

			ImGui.EndChildFrame();
		}

		private void CreateNewTag() {
			NewTagName = NewTagName.Trim();
			if (NewTagName.IsNullOrEmpty()) {
				return;
			}

			// Check if tag with this name already exists
			var existingTag = Tag.TagNameEquals(NewTagName);
			if (existingTag != null) {
				PluginLog.Warning($"Tag '{NewTagName}' already exists");
				return;
			}

			// Create new tag
			var newTag = new Tag(NewTagName);
			ConfigurationManager.Config.SavedTags.Add(newTag);
			ConfigurationManager.Config.Save();

			PluginLog.Debug($"Created new tag: {NewTagName}");
			NewTagName = string.Empty;

			// Select the newly created tag
			SelectedTag = newTag;
			EditingTagName = newTag.Name;
			EditingTagSlot = newTag.Slot;
			IsEditingTag = false;
		}

		private void ExportTagsToClipboard() {
			try {
				var exportData = new {
					tags = ConfigurationManager.Config.SavedTags.ToList(),
					tagLinks = ConfigurationManager.Config.ItemTags.ToList()
				};

				var json = JsonConvert.SerializeObject(exportData, Formatting.None);

				// Check if Ctrl is held to skip compression
				bool skipCompression = ImGui.GetIO().KeyCtrl;

				string clipboardContent;
				if (skipCompression) {
					clipboardContent = json;
				} else {
					// Compress and encode
					var compressed = CompressString(json);
					clipboardContent = Convert.ToBase64String(compressed);
				}

				ImGui.SetClipboardText(clipboardContent);
				var importErrorMessage = $"Exported {ConfigurationManager.Config.SavedTags.Count} tags to clipboard" + (skipCompression ? " (clear json)" : "");
				PluginLog.Information(importErrorMessage);
				PluginServices.ChatGui.Print(importErrorMessage);
			} catch (Exception ex) {
				PluginLog.Error($"Failed to export tags: {ex.Message}");
				var importErrorMessage = $"Export failed: {ex.Message}";
				PluginServices.ChatGui.PrintError(importErrorMessage);

			}
		}

		private void ImportTagsFromClipboard() {
			try {
				var clipboardContent = ImGui.GetClipboardText();
				if (clipboardContent.IsNullOrEmpty()) {
					PluginServices.ChatGui.PrintError("Clipboard is empty");
					return;
				}

				string json = clipboardContent;

				// Try to decompress if it looks like base64
				if (!clipboardContent.StartsWith("{")) {
					try {
						var compressed = Convert.FromBase64String(clipboardContent);
						json = DecompressString(compressed);
					} catch {
						// If decompression fails, try treating it as raw JSON
						json = clipboardContent;
					}
				}

				var importData = JsonConvert.DeserializeObject<ImportExportData>(json);
				if (importData == null || (importData.tags.Count == 0 && importData.tagLinks.Count == 0)) {
					PluginServices.ChatGui.PrintError("Invalid or empty import data");
					return;
				}

				int tagsAdded = 0;
				int tagsMerged = 0;
				var importedTagIdMap = new Dictionary<uint, uint>(); // Old ID -> New ID

				// Import tags with merging
				foreach (var importedTag in importData.tags) {
					var existingTag = Tag.TagNameEquals(importedTag.Name);
					if (existingTag != null) {
						// Merge: map the old ID to the existing one
						importedTagIdMap[importedTag.Id] = existingTag.Id;
						tagsMerged++;
					} else {
						// Create new tag
						var newTag = new Tag(importedTag.Name) {
							Slot = importedTag.Slot
						};
						ConfigurationManager.Config.SavedTags.Add(newTag);
						importedTagIdMap[importedTag.Id] = newTag.Id;
						tagsAdded++;
					}
				}

				// Import tag links
				int linksAdded = 0;
				foreach (var importedLink in importData.tagLinks) {
					if (importedTagIdMap.TryGetValue(importedLink.Tag, out var newTagId)) {
						var newLink = new TagLink(importedLink.Item, newTagId);
						if (!ConfigurationManager.Config.ItemTags.Contains(newLink)) {
							ConfigurationManager.Config.ItemTags.Add(newLink);
							TagStore.AddTag(newLink);
							linksAdded++;
						}
					}
				}

				ConfigurationManager.Config.Save();
				TagStore.LoadLinks();

				var importErrorMessage = $"Imported {tagsAdded} new tags, merged {tagsMerged} existing tags, associated {linksAdded} items to tags";

				PluginServices.ChatGui.Print(importErrorMessage);
				PluginLog.Information(importErrorMessage);
			} catch (Exception ex) {
				PluginLog.Error($"Failed to import tags: {ex.Message}");
				var importErrorMessage = $"Import failed: {ex.Message}";
				PluginServices.ChatGui.PrintError(importErrorMessage);
			}
		}

		private void ShowDeleteAllConfirmation() {
			ImGui.OpenPopup("Delete All Tags##Confirmation");
		}

		private byte[] CompressString(string text) {
			var buffer = Encoding.UTF8.GetBytes(text);
			using (var ms = new MemoryStream()) {
				using (var gzs = new GZipStream(ms, CompressionMode.Compress, true)) {
					gzs.Write(buffer, 0, buffer.Length);
				}
				return ms.ToArray();
			}
		}

		private string DecompressString(byte[] buffer) {
			using (var ms = new MemoryStream(buffer)) {
				using (var gzs = new GZipStream(ms, CompressionMode.Decompress, true)) {
					using (var sr = new StreamReader(gzs)) {
						return sr.ReadToEnd();
					}
				}
			}
		}

		private void DrawTagEditor(float width) {
			ImGui.BeginChildFrame(2, new Vector2(width, ImGui.GetContentRegionAvail().Y));
			ImGui.Text("Tag Editor");
			ImGui.Separator();

			if (SelectedTag == null) {
				ImGui.TextDisabled("Select a tag to edit");
				ImGui.EndChildFrame();
				return;
			}

			ImGui.AlignTextToFramePadding();
			ImGui.Text("Name:");
			ImGui.SameLine();
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			if (!IsEditingTag) {
				ImGui.TextUnformatted(EditingTagName);
				ImGui.SameLine();
				if (ImGui.SmallButton("Edit##EditTagName")) {
					IsEditingTag = true;
				}
			} else {
				if (ImGui.InputText("##EditTagNameInput", ref EditingTagName, 128, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll)) {
					if (!EditingTagName.IsNullOrEmpty() && EditingTagName != SelectedTag.Name) {
						SelectedTag.Name = EditingTagName;
					}
					IsEditingTag = false;
				}
				if (ImGui.IsItemDeactivated()) {
					IsEditingTag = false;
				}
			}

			// Slot assignment
			ImGui.AlignTextToFramePadding();
			ImGui.Text("Slot Restriction:");
			ImGui.SameLine();

			var slotOptions = new[] { "Universal (no slot restriction)" }
				.Concat(Enum.GetValues<GlamourPlateSlot>().Select(s => s.ToString().AddSpaceBeforeCapital()))
				.ToArray();
			var currentSlotIndex = EditingTagSlot.HasValue ? (int)EditingTagSlot.Value + 1 : 0;

			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			if (ImGui.Combo("##TagSlotAssignment", ref currentSlotIndex, slotOptions)) {
				var newSlot = currentSlotIndex == 0 ? null : (GlamourPlateSlot?)(currentSlotIndex - 1);
				if (!EqualityComparer<GlamourPlateSlot?>.Default.Equals(newSlot, EditingTagSlot)) {
					EditingTagSlot = newSlot;
					// Update the tag - would need to add method to Tag class or do reflection
					var field = typeof(Tag).GetProperty("Slot");
					if (field != null) {
						field.SetValue(SelectedTag, newSlot);
						PluginLog.Debug($"Updated tag '{SelectedTag.Name}' slot to {(newSlot?.ToString() ?? "universal")}");
						ConfigurationManager.Config.Save(); // Save configuration
					}
				}
			}

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			// Tag stats with slot breakdown
			var itemsWithTag = TagStore.GetItemsForTag(SelectedTag.Id);
			ImGui.Text($"Items with this tag: {itemsWithTag.Count}");
			
			if (itemsWithTag.Count > 0) {
				ImGui.Indent();
				DrawTagItemsBySlot(itemsWithTag);
				ImGui.Unindent();
			}

			ImGui.Spacing();

			// Delete button
			ImGui.SetCursorPosY(ImGui.GetWindowHeight() - ImGui.GetFrameHeightWithSpacing() - ImGui.GetStyle().WindowPadding.Y);
			if(GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Trash, "Hold CTRL+SHIFT to unlock the delete button. The deletion of this tag cannot be undone.", new Vector2(ImGui.GetContentRegionAvail().X, 0), "Delete Tag##TagManager")) {
				Tag.Remove(SelectedTag);
				SelectedTag = null;
				IsEditingTag = false;
			}

			ImGui.EndChildFrame();
		}
		private void DrawTagItemsBySlot(HashSet<uint> itemIds) {
			// Group items by their slot
			var itemsBySlot = new Dictionary<GlamourPlateSlot, int>();

			foreach (var itemId in itemIds) {
				try {
					// Get the item from the sheet
					
					var itemSheet = PluginServices.SheetManager.GetSheet<ItemSheet>();
					if (itemSheet == null) continue;

					var itemRow = itemSheet.GetRow(itemId);
					if (itemRow == null) continue;

					// Get the slot for this item
					var slot = itemRow.GlamourPlateSlot();
					if (slot == null) continue;

					if (!itemsBySlot.ContainsKey(slot.Value)) {
						itemsBySlot[slot.Value] = 0;
					}
					itemsBySlot[slot.Value]++;
				} catch {
					// Skip items that can't be processed
					continue;
				}
			}

			// Display the breakdown
			if (itemsBySlot.Count == 0) {
				ImGui.TextDisabled("(no items found)");
				return;
			}

			// Sort by slot order
			var sortedSlots = itemsBySlot.OrderBy(kvp => (int)kvp.Key).ToList();

			foreach (var kvp in sortedSlots) {
				var slot = kvp.Key;
				var count = kvp.Value;
				var slotName = slot.ToString().AddSpaceBeforeCapital();
				ImGui.BulletText($"{count} {slotName}");
				ImGui.SameLine();
				if(GuiHelpers.IconButtonTooltip(FontAwesomeIcon.ArrowCircleLeft,$"Change this tag's slot for {slotName}.", default, $"{slot}##ChangeSlotFor##TagManager")) {
					EditingTagSlot = slot;
					SelectedTag?.Slot = slot;
					PluginLog.Debug($"Updated tag '{SelectedTag?.Name}' slot to {slot}");
					ConfigurationManager.Config.Save(); // Save configuration
				}
			}
		}

		private void DrawDeleteAllConfirmationModal() {
			var center = ImGui.GetMainViewport().GetCenter();
			ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

			bool isOpen = true;
			if (ImGui.BeginPopupModal("Delete All Tags##Confirmation", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), "WARNING: This action cannot be undone!");
				ImGui.Spacing();
				ImGui.Text($"Are you sure you want to delete ALL {ConfigurationManager.Config.SavedTags.Count} tags?");
				ImGui.Text("This will also remove all tag associations from items.");
				ImGui.Spacing();

				var buttonWidth = (ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemSpacing.X) / 2;
				if (ImGui.Button("Delete All", new Vector2(buttonWidth, 0))) {
					DeleteAllTags();
					ImGui.CloseCurrentPopup();
				}

				ImGui.SameLine();
				if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0))) {
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}
		}

		private void DeleteAllTags() {
			try {
				var tagIds = ConfigurationManager.Config.SavedTags.Select(t => t.Id).ToList();

				// Remove all tag associations
				foreach (var tagId in tagIds) {
					var itemIds = TagStore.GetItemsForTag(tagId).ToList();
					foreach (var itemId in itemIds) {
						var link = ConfigurationManager.Config.ItemTags.FirstOrDefault(l => l.Item == itemId && l.Tag == tagId);
						if (link.Item != 0 || link.Tag != 0) {
							ConfigurationManager.Config.ItemTags.Remove(link);
							TagStore.RemoveTag(link);
						}
					}
				}

				// Remove all tags
				ConfigurationManager.Config.SavedTags.Clear();
				ConfigurationManager.Config.Save();
				TagStore.LoadLinks();

				SelectedTag = null;
				IsEditingTag = false;
				var importErrorMessage = "All tags deleted";
				PluginLog.Information($"Deleted all {tagIds.Count} tags");
				PluginServices.ChatGui.Print(importErrorMessage);
			} catch (Exception ex) {
				PluginLog.Error($"Failed to delete all tags: {ex.Message}");
				var importErrorMessage = $"Failed to delete tags: {ex.Message}";
				PluginServices.ChatGui.PrintError(importErrorMessage);
			}
		}
	}

	public class ImportExportData {
		public List<Tag> tags { get; set; } = [];
		public List<TagLink> tagLinks { get; set; } = [];
	}
}

