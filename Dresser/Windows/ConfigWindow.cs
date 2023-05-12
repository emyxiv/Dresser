using System;
using System.Linq;
using System.Numerics;

using CriticalCommonLib;

using Dalamud.Interface.Windowing;

using Dresser.Logic;

using ImGuiNET;

namespace Dresser.Windows;

public class ConfigWindow : Window, IDisposable {

	public ConfigWindow(Plugin plugin) : base(
		"Dresser Settings",
		ImGuiWindowFlags.None) {
		this.Size = new Vector2(232, 75);
		this.SizeCondition = ImGuiCond.FirstUseEver;

	}

	public void Dispose() { }

	public override void Draw() {
		// can't ref a property, so use a local copy
		//var configValue = this.Configuration.SomePropertyToBeSavedAndWithADefault;
		//if (ImGui.Checkbox("Random Config Bool", ref configValue)) {
		//	Configuration.SomePropertyToBeSavedAndWithADefault = configValue;
		//	//can save immediately on change, if you don't want to provide a "Save and Close" button
		//	Configuration.Save();
		//}
		ImGui.Text($"Browser");
		DrawBrowserConfigs();
		ImGui.Text($"Inventory Memory");
		DrawInventoryConfigs();

	}

	public void DrawBrowserConfigs() {
		ImGui.SetNextItemWidth(ImGui.GetFontSize() * 3);
		var iconSizeMult = ConfigurationManager.Config.IconSizeMult;
		if (ImGui.DragFloat("##IconSize##slider", ref iconSizeMult, 0.01f, 0.1f, 4f, "%.2f %")) {
			ConfigurationManager.Config.IconSizeMult = iconSizeMult;
			ConfigurationManager.SaveAsync();
		}
		ImGui.SameLine();
		ImGui.Text("%");
		ImGui.Checkbox($"Show items icons##displayCategory##GearBrowserConfig", ref ConfigurationManager.Config.ShowImagesInBrowser);
		ImGui.Checkbox($"Fade unavailable items when hidding tooltips (Hold Alt)##Images##GearBrowserConfig", ref ConfigurationManager.Config.FadeIconsIfNotHiddingTooltip);


	}
	public void DrawInventoryConfigs() {
		if (ImGui.Button("Force Save All configs and inventories")) {
			ConfigurationManager.Save();
		}
		DrawInventoryStatusTable();

	}

	private void DrawInventoryStatusTable() {


		if (ImGui.BeginTable("CharacterTable", 3, ImGuiTableFlags.BordersV |
											 ImGuiTableFlags.BordersOuterV |
											 ImGuiTableFlags.BordersInnerV |
											 ImGuiTableFlags.BordersH |
											 ImGuiTableFlags.BordersOuterH |
											 ImGuiTableFlags.BordersInnerH)) {
			ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 100.0f, (uint)0);
			ImGui.TableSetupColumn("Items", ImGuiTableColumnFlags.WidthStretch, 100.0f, (uint)1);
			ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, 100.0f, (uint)2);
			ImGui.TableHeadersRow();

			var characters = PluginServices.CharacterMonitor.GetPlayerCharacters();
			if (characters.Length == 0) {
				ImGui.TableNextRow();
				ImGui.Text("No characters available.");
				ImGui.TableNextColumn();
				ImGui.TableNextColumn();
			}
			for (var index = 0; index < characters.Length; index++) {
				ImGui.TableNextRow();
				var character = characters[index].Value;
				ImGui.TableNextColumn();
				if (character.Name != "") {
					ImGui.Text(character.Name);
					ImGui.SameLine();
				}

				ImGui.TableNextColumn();
				int itemCount = 0;
				if(ConfigurationManager.Config.SavedInventories.TryGetValue(character.CharacterId, out var invs)){
					itemCount = invs.Sum(c => c.Value.Count);
				}


				ImGui.Text(itemCount.ToString());
				ImGui.SameLine();

				ImGui.TableNextColumn();
				if (ImGui.SmallButton("Clear All Bags##" + index)) {
					ImGui.OpenPopup("Are you sure?##" + index);
				}
				if (ImGui.BeginPopupModal("Are you sure?##" + index)) {
					ImGui.Text(
						"Are you sure you want to clear all the bags stored for this character?.\nThis operation cannot be undone!\n\n");
					ImGui.Separator();

					if (ImGui.Button("OK", new Vector2(120, 0) * ImGui.GetIO().FontGlobalScale)) {
						PluginServices.InventoryMonitor.ClearCharacterInventories(character.CharacterId);
						ImGui.CloseCurrentPopup();
					}

					ImGui.SetItemDefaultFocus();
					ImGui.SameLine();
					if (ImGui.Button("Cancel", new Vector2(120, 0) * ImGui.GetIO().FontGlobalScale)) {
						ImGui.CloseCurrentPopup();
					}

					ImGui.EndPopup();
				}
			}
			ImGui.EndTable();
		}

	}
}
