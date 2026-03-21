using CriticalCommonLib.Enums;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Utility;

using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Dresser;
using Dresser.Windows.Components;

using OtterGui.Text.EndObjects;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Dresser.Windows {
	public class TagManager : Window, IDisposable {
		private Plugin Plugin;
		private string SearchFilter = string.Empty;
		private GlamourPlateSlot? SelectedSlotFilter = null;
		private Tag? SelectedTag = null;
		private string EditingTagName = string.Empty;
		private GlamourPlateSlot? EditingTagSlot = null;
		private bool IsEditingTag = false;
		private string NewTagName = string.Empty;

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

			// Filter section
			ImGui.AlignTextToFramePadding();
			ImGui.Text("Search:");
			ImGui.SameLine();
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2 - ImGui.GetStyle().ItemSpacing.X);
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
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			if (ImGui.Combo("##TagSlotFilter", ref selectedSlotIndex, slotOptions)) {
				SelectedSlotFilter = selectedSlotIndex switch {
					0 => null, // All slots
					1 => null, // No slot restriction (but we'll filter)
					_ => (GlamourPlateSlot)(selectedSlotIndex - 2)
				};
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
			var listWidth = contentAvailable.X / 2.5f;

			ImGui.BeginGroup();
			DrawTagList(filteredTags, listWidth);
			ImGui.EndGroup();

			ImGui.SameLine();

			ImGui.BeginGroup();
			DrawTagEditor(contentAvailable.X - listWidth - ImGui.GetStyle().ItemSpacing.X);
			ImGui.EndGroup();
		}

		private void DrawTagList(List<Tag> filteredTags, float width) {
			ImGui.BeginChildFrame(1, new Vector2(width, ImGui.GetContentRegionAvail().Y));
			ImGui.Text($"Tags ({filteredTags.Count})");
			ImGui.Separator();

			GlamourPlateSlot? currentSlot = null;
			foreach (var tag in filteredTags) {
				// Draw slot header
				if (tag.Slot.HasValue && !EqualityComparer<GlamourPlateSlot?>.Default.Equals(tag.Slot, currentSlot)) {
					if (currentSlot.HasValue) ImGui.Spacing();
					ImGui.TextDisabled(tag.Slot.Value.ToString().AddSpaceBeforeCapital());
					ImGui.Separator();
					currentSlot = tag.Slot;
				} else if (!tag.Slot.HasValue && currentSlot.HasValue) {
					ImGui.Spacing();
					ImGui.TextDisabled("Universal Tags");
					ImGui.Separator();
					currentSlot = null;
				}

				var isSelected = SelectedTag?.Id == tag.Id;
				var tagColor = tag.Color();
				ImGui.PushStyleColor(ImGuiCol.Button, isSelected ? tagColor * 1.2f : tagColor * 0.3f);
				ImGui.PushStyleColor(ImGuiCol.ButtonHovered, tagColor * 0.5f);
				ImGui.PushStyleColor(ImGuiCol.ButtonActive, tagColor * 0.7f);

				var buttonLabel = $"{tag.Name}##TagButton{tag.Id}";
				if (ImGui.Button(buttonLabel, new Vector2(ImGui.GetContentRegionAvail().X, 0))) {
					SelectedTag = tag;
					EditingTagName = tag.Name;
					EditingTagSlot = tag.Slot;
					IsEditingTag = false;
				}

				ImGui.PopStyleColor(3);

				// Context menu for delete
				if (ImGui.BeginPopupContextItem($"TagListContext{tag.Id}")) {
					if (ImGui.Selectable("Delete", false)) {
						Tag.Remove(tag);
						if (SelectedTag?.Id == tag.Id) {
							SelectedTag = null;
							IsEditingTag = false;
						}
						ImGui.CloseCurrentPopup();
					}
					ImGui.EndPopup();
				}
			}

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			// Create new tag section
			ImGui.AlignTextToFramePadding();
			ImGui.Text("New Tag:");
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemSpacing.X);
			if (ImGui.InputText("##NewTagNameInput", ref NewTagName, 128, ImGuiInputTextFlags.EnterReturnsTrue)) {
				CreateNewTag();
			}

			ImGui.SameLine();
			if (ImGui.Button("+##CreateNewTag", new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight()))) {
				CreateNewTag();
			}
			GuiHelpers.Tooltip("Create a new tag (or press Enter in the input field)");

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
						// Rename logic - would need to be added to Tag class
						PluginLog.Warning("Tag renaming not yet implemented in Tag class");
					}
					IsEditingTag = false;
				}
				if (ImGui.IsItemDeactivated()) {
					IsEditingTag = false;
				}
			}

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			// Slot assignment
			ImGui.AlignTextToFramePadding();
			ImGui.Text("Slot Restriction:");
			ImGui.Spacing();

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

			// Tag stats
			var itemsWithTag = TagStore.GetItemsForTag(SelectedTag.Id);
			ImGui.Text($"Items with this tag: {itemsWithTag.Count}");

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
	}
}
