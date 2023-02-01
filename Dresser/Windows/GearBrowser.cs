using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ImGuiNET;

using Dalamud.Interface.Windowing;
using Dalamud.Logging;

using CriticalCommonLib;
using CriticalCommonLib.Enums;
using CriticalCommonLib.Extensions;
using CriticalCommonLib.Models;

using Dresser.Data;
using Dresser.Extensions;
using Dresser.Interop.Hooks;
using Dresser.Windows.Components;

namespace Dresser.Windows {
	public class GearBrowser : Window, IDisposable {

		public GearBrowser() : base(
			"Gear Browser", ImGuiWindowFlags.None) {
			this.SizeConstraints = new WindowSizeConstraints {
				MinimumSize = new Vector2(ImGui.GetFontSize() * 4),
				MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
			};

		}
		public void Dispose() { }


		public static Vector4 CollectionColorBackground = new Vector4(113,98,119,200) / 255;
		public static Vector4 CollectionColorBorder = (new Vector4(116,123,98,255) / 255 * 0.4f) + new Vector4(0,0,0,1);
		public static Vector4 CollectionColorScrollbar = (new Vector4(116,123,98,255) / 255 * 0.2f) + new Vector4(0,0,0,1);

		private static int? HoveredItem = null;
		private static string Search = "";
		private static List<InventoryCategory> AllowedCategories = new() {
			InventoryCategory.GlamourChest,
			InventoryCategory.Armoire,
			InventoryCategory.RetainerBags,
			InventoryCategory.RetainerEquipped,
			InventoryCategory.CharacterSaddleBags,
			InventoryCategory.CharacterPremiumSaddleBags,
			InventoryCategory.CharacterEquipped,
			InventoryCategory.CharacterBags,
			InventoryCategory.CharacterArmoryChest,
			InventoryCategory.RetainerMarket,
			InventoryCategory.FreeCompanyBags,
		};
		private static Dictionary<InventoryCategory, bool> DisplayInventoryCategories = AllowedCategories.ToDictionary(c => c, c => true);
		public override void Draw() {
			//TestWindow();

			// top "bar" with controls
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 3);
			ImGui.DragFloat("##IconSize##slider", ref ItemIcon.IconSizeMult, 0.01f, 0.1f, 4f, "%.2f %");
			ImGui.SameLine();
			ImGui.Text("%");


			ImGui.InputTextWithHint("##SearchByName##GearBrowser", "Search", ref Search, 100);
			
			if (ImGui.CollapsingHeader($"Source##Source##GearBrowser")) {
				ImGui.Columns(2);
				int i = 0;
				foreach ((var cat, var willDisplay) in DisplayInventoryCategories) {
					i++;
					if (i % (DisplayInventoryCategories.Count /2 ) == 0)
						ImGui.NextColumn();
					var willDisplayValue = willDisplay;
					if (ImGui.Checkbox($"{cat}##displayCategory", ref willDisplayValue))
						DisplayInventoryCategories[cat] = willDisplayValue;
				}
				
				//ImGui.NextColumn();
				//ImGui.BeginDisabled();
				//bool disb = false;
				//ImGui.Checkbox("Calamity Salvager##GearBrowser", ref disb);
				//ImGui.Checkbox("Relic Replica Vendors##GearBrowser", ref disb);
				//ImGui.Checkbox("Unobtained##GearBrowser", ref disb);
				//ImGui.EndDisabled();
				ImGui.Columns();
			}
			if (ImGui.CollapsingHeader($"Advanced Filtering##Source##GearBrowser")) {
			}


			var items = PluginServices.InventoryMonitor.AllItems.Where(i =>
				!i.IsEmpty
				&& AllowedCategories.Contains(i.Container.ToInventoryCategory())
				&& DisplayInventoryCategories[i.Container.ToInventoryCategory()]
				&& i.Item.ModelMain != 0
				&& i.Item.CanBeEquipedByPlayerCharacter()
				&& (
					//!Search.IsNullOrWhitespace() &&
					i.FormattedName.Contains(Search, StringComparison.OrdinalIgnoreCase)
					)

				)
				.GroupBy(i => i.GetHashCode())
				.Select(i => i.First())
				//.OrderBy(i => i.Item.EquipSlotCategoryEx)
				.OrderByDescending(i => i.Item.LevelEquip)
				//.OrderBy(i => i.Item.LevelItem)
				;


			//ImGui.SameLine();
			ImGui.Text($"Found: {items.Count()}");

			PushStyleCollection();
			ImGui.BeginChildFrame(76, ImGui.GetContentRegionAvail());
			try {

				bool isTooltipActive = false;

				foreach (var item in items) {

					// icon
					bool isHovered = item.GetHashCode() == HoveredItem;
					bool wasHovered = isHovered;
					var iconClicked = ItemIcon.DrawIcon(item, ref isHovered, ref isTooltipActive);
					if (isHovered)
						HoveredItem = item.GetHashCode();
					else if (!isHovered && wasHovered)
						HoveredItem = null;

					// execute when clicked
					if (iconClicked) {
							PluginLog.Verbose($"Execute apply item {item.Item.NameString} {item.Item.RowId}");

							// TODO: make sure the item is still in glam chest or armoire
							if (GlamourPlates.IsGlamingAtDresser() && (item.Container == InventoryType.GlamourChest || item.Container == InventoryType.Armoire)) {
								PluginServices.GlamourPlates.ModifyGlamourPlateSlot(item,
									(i) => Gathering.ParseGlamourPlates()
									);
							}

							Service.ClientState.LocalPlayer?.Equip(item);
					}


					ImGui.SameLine();
					if (ImGui.GetContentRegionAvail().X < ItemIcon.IconSize.X)
						ImGui.NewLine();
				}
			} catch(Exception ex) {
				PluginLog.Error(ex.ToString());
			}

			ImGui.EndChildFrame();
			PopStyleCollection();
		}


		public static void PushStyleCollection() {
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ItemIcon.IconSize / 5f);
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ItemIcon.IconSize / 8f);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 10 * ItemIcon.IconSizeMult);
			ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 3 * ItemIcon.IconSizeMult);
			ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 7 * ItemIcon.IconSizeMult);
			ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(CollectionColorBackground));
			ImGui.PushStyleColor(ImGuiCol.Border, ImGui.ColorConvertFloat4ToU32(CollectionColorBorder));
			ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, ImGui.ColorConvertFloat4ToU32(CollectionColorScrollbar));


			ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 10 * ItemIcon.IconSizeMult);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, ItemIcon.IconSize / 8f);
			ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 3 * ItemIcon.IconSizeMult);
			ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.ColorConvertFloat4ToU32(CollectionColorBackground));
		}
		public static void PopStyleCollection() {
			ImGui.PopStyleColor(4);
			ImGui.PopStyleVar(8);
		}


		private void TestWindow() {
			if(ImGui.Begin("Test Window")) {


				// textures
				var texturePart = ImageGuiCrop.GetPart("character", 17);
				if (texturePart.Item1 != IntPtr.Zero) {
					if (ImageGuiCrop.Textures.TryGetValue("character", out var tex)) {
						ImGui.Text($"s:{tex.Width}*{tex.Height}");
						ImGui.Image(texturePart.Item1, new(tex.Width, tex.Height));
					}
					ImGui.Image(texturePart.Item1, ItemIcon.IconSize, texturePart.Item2, texturePart.Item3);
					ImGui.SameLine();
					ImGui.Image(texturePart.Item1, texturePart.Item4, texturePart.Item2, texturePart.Item3);
				}

				ImGui.End();
			}
		}
	}
}