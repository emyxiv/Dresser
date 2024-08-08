using CriticalCommonLib;
using CriticalCommonLib.Enums;

using Dresser.Extensions;
using Dresser.Interop;
using Dresser.Interop.Hooks;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Dresser;

using ImGuiNET;

using Lumina.Excel.GeneratedSheets;


using System;
using System.Linq;
using System.Numerics;

using GlamourPlates = Dresser.Interop.Hooks.GlamourPlates;
using UsedStains = System.Collections.Generic.Dictionary<(uint, uint), uint>;

namespace Dresser.Windows.Components {
	internal static class GlamourPlateDebug {

		private static int SlotSize = 60;
		private static int HeadSize = 40;
		private static int HeadOffset = 36;
		public static void Draw() {
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10); ImGui.InputInt("head size##GlamourPlate#Debug", ref HeadSize);
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10); ImGui.InputInt("head offset##GlamourPlate#Debug", ref HeadOffset);
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10); ImGui.InputInt("slot size##GlamourPlate#Debug", ref SlotSize);

			CheckAgent();

			if (ImGui.CollapsingHeader("One Slot")) {
				DrawSlotData();
			}
			if (ImGui.CollapsingHeader("Show All")) {
				DrawAllDataHex();
			}
			if (ImGui.CollapsingHeader("Test Functions")) {
				DrawTestFunctions();
			}
			if (ImGui.CollapsingHeader("Test Dresser contents")) {
				DrawDresserContentsChecks();
			}

		}

		private unsafe static void CheckAgent() {
			var agent = GlamourPlates.MiragePlateAgent;
			if (agent == null) {
				return;
			}

			var pointerNint = ((IntPtr)agent + HeadSize);
			ImGui.Text($"Agent Pointer: {pointerNint}");
			if (ImGui.IsItemClicked()) pointerNint.ToString().ToClipboard();
			ImGui.SameLine();
			ImGui.Text($" {pointerNint:X8}");
			if (ImGui.IsItemClicked()) pointerNint.ToString("X8").ToClipboard();

			var data = *(AgentMiragePrismMiragePlateData**)((nint)agent + Offsets.HeadSize);
			if (data == null) {
				ImGui.TextColored(ConfigurationManager.Config.ColorBad, $"agent inactive");
				return;
			}
			ImGui.TextColored(ConfigurationManager.Config.ColorGood, $"agent active");


			//ImGui.Text($"editorInfo: {editorInfo}"); if (ImGui.IsItemClicked()) editorInfo.ToString().ToClipboard(); ImGui.SameLine(); ImGui.Text($" {editorInfo:X8}"); if (ImGui.IsItemClicked()) editorInfo.ToString("X8").ToClipboard();

			var slotSelected = data->SelectedItemIndex;
			var slotSelectedPtr = (IntPtr)slotSelected;
			
			//ImGui.Text($"slot selected: {slotSelectedPtr}"); if (ImGui.IsItemClicked()) slotSelectedPtr.ToString().ToClipboard(); ImGui.SameLine(); ImGui.Text($" {slotSelectedPtr:X8}"); if (ImGui.IsItemClicked()) slotSelectedPtr.ToString("X8").ToClipboard();
			ImGui.Text($"slot selected: {slotSelected}");
		}

		private static string DataToHex(IntPtr baseAddress, int size) {
			unsafe {
				byte* ptr = (byte*)baseAddress; // Cast the pointer to a byte pointer
				byte[] data = new byte[size];

				for (int i = 0; i < size; i++) {
					data[i] = ptr[i]; // Copy the bytes from the pointer to the array
				}

				// Use the byte array
				string hexString = BitConverter.ToString(data);
				return hexString.Replace('-', ' ');
			}
		}



		private static int SlotData_PlateIndex = 0;
		private static int SlotData_SlotIndex = 0;
		private unsafe static void DrawSlotData() {
			if (GlamourPlates.MiragePlateAgent == null) {
				return;
			}

			var editorInfo = *(IntPtr*)((IntPtr)GlamourPlates.MiragePlateAgent + HeadSize);
			if (editorInfo == IntPtr.Zero) {
				ImGui.TextColored(ConfigurationManager.Config.ColorBad, $"agent inactive");
				return;
			}

			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10); ImGui.InputInt("Plate##GlamourPlate#Debug", ref SlotData_PlateIndex); if (SlotData_PlateIndex < 0) SlotData_PlateIndex = 0; if (SlotData_PlateIndex > Storage.PlateNumber) SlotData_PlateIndex = Storage.PlateNumber;
			ImGui.SetNextItemWidth(ImGui.GetFontSize() * 10); ImGui.InputInt("Slot##GlamourPlate#Debug", ref SlotData_SlotIndex); if (SlotData_SlotIndex < 0) SlotData_SlotIndex = 0; if (SlotData_SlotIndex > 11) SlotData_SlotIndex = 11;


			var offset = (SlotSize * SlotData_SlotIndex) + (SlotSize * 12 * SlotData_PlateIndex) + HeadOffset;
			var slotPtr = editorInfo + offset;
			var hexString = DataToHex(slotPtr, SlotSize).Replace("00", "  ");
			GuiHelpers.TextWithFont($"[{SlotData_PlateIndex:D2}:{SlotData_SlotIndex:D2}]{offset:D6}: {hexString}", GuiHelpers.Font.Mono);


			var itemId = *(uint*)slotPtr;
			var stainId = *(byte*)(slotPtr + 24);
			var stainId2 = *(byte*)(slotPtr + 25);
			var stainPreviewId = *(byte*)(slotPtr + 26);
			var stainPreviewId2 = *(byte*)(slotPtr + 27);
			var actualStainId = stainPreviewId == 0 ? stainId : stainPreviewId;
			var actualStainId2 = stainPreviewId2 == 0 ? stainId2 : stainPreviewId2;



			ImGui.Text($"stainId: {stainId} 0x{stainId:X}");
			if (stainId != 0) {
				var stain = PluginServices.DataManager.Excel.GetSheet<Stain>()?.First(s => s.RowId == stainId);
				if (stain != null) {
					ImGui.SameLine();
					ImGui.TextColored(stain.ColorVector4(), stain.Name);
				}
			}

			ImGui.Text($"stainId2: {stainId2} 0x{stainId2:X}");
			if (stainId2 != 0) {
				var stain2 = PluginServices.DataManager.Excel.GetSheet<Stain>()?.First(s => s.RowId == stainId2);
				if (stain2 != null) {
					ImGui.SameLine();
					ImGui.TextColored(stain2.ColorVector4(), stain2.Name);
				}
			}


			ImGui.Text($"stainPreviewId: {stainPreviewId} 0x{stainPreviewId:X}");
			if (stainPreviewId != 0) {
				var stainPreview = PluginServices.DataManager.Excel.GetSheet<Stain>()?.First(s => s.RowId == stainPreviewId);
				if (stainPreview != null) {
					ImGui.SameLine();
					ImGui.TextColored(stainPreview.ColorVector4(), stainPreview.Name);
				}
			}
			ImGui.Text($"stainPreviewId2: {stainPreviewId2} 0x{stainPreviewId2:X}");
			if (stainPreviewId2 != 0) {
				var stainPreview = PluginServices.DataManager.Excel.GetSheet<Stain>()?.First(s => s.RowId == stainPreviewId2);
				if (stainPreview != null) {
					ImGui.SameLine();
					ImGui.TextColored(stainPreview.ColorVector4(), stainPreview.Name);
				}
			}
			ImGui.Text($"actualStainId: {actualStainId} 0x{actualStainId:X}");
			ImGui.Text($"actualStainId2: {actualStainId2} 0x{actualStainId2:X}");


			ImGui.Text($"itemId: {itemId} 0x{itemId:X}");
			if (itemId != 0) {
				var item = Service.ExcelCache.AllItems[itemId];
				if (item != null) {

					var invItem = item.ToInventoryItem(InventoryType.Bag0);
					invItem.Stain = stainId;
					invItem.Stain2 = stainId2;
					ItemIcon.DrawIcon(invItem);
				}
			}


		}
		private unsafe static void DrawAllDataHex() {
			if (GlamourPlates.MiragePlateAgent == null) {
				return;
			}

			var editorInfo = *(IntPtr*)((IntPtr)GlamourPlates.MiragePlateAgent + HeadSize);
			if (editorInfo == IntPtr.Zero) {
				ImGui.TextColored(ConfigurationManager.Config.ColorBad, $"agent inactive");
				return;
			}

			GuiHelpers.TextWithFont($"[  :  ]      :                               10                            20                            30                            40                            50                            60                            70                            80", GuiHelpers.Font.Mono);

			var platNumber = Storage.PlateNumber;
			for (int p = 0; p <= platNumber; p++) {
				for (int s = 0; s < 12; s++) {

					var offset = (SlotSize * s) + (SlotSize * 12 * p) + HeadOffset;
					var slotPtr = editorInfo + offset;
					var hexString = DataToHex(slotPtr, SlotSize)
						.Replace("00", "  ")
						;
					GuiHelpers.TextWithFont($"[{p:D2}:{s:D2}]{offset:D6}: {hexString}", GuiHelpers.Font.Mono);
				}
			}
		}

		private static void DrawTestFunctions() {
			UsedStains usedStains = new();
			//if (ImGui.Button("Clean")) PluginServices.GlamourPlates.ClearGlamourPlateSlot(GlamourPlateSlot.Head);
			//ImGui.SameLine();

			if (ImGui.Button("Set")) {
				var inventoryItem = new InventoryItem(InventoryType.GlamourChest, 24591);
				if (inventoryItem.ItemId != 0) {
					PluginServices.GlamourPlates.SetGlamourPlateSlot(inventoryItem, ref usedStains);
				}
			}
			ImGui.SameLine();

			if (ImGui.Button("Set dyed")) {
				var inventoryItem = new InventoryItem(InventoryType.GlamourChest, 24591) {
					Stain = 16,
					Stain2 = 82,
				};
				if (inventoryItem.ItemId != 0) {
					PluginServices.GlamourPlates.SetGlamourPlateSlot(inventoryItem, ref usedStains);
				}
			}
			ImGui.SameLine();

			if (ImGui.Button("get armoire dyed")) {
				var inventoryItem = new InventoryItem(InventoryType.GlamourChest, 20489) {
					Stain = 12,
					Stain2 = 96,
				};
				if (inventoryItem.ItemId != 0) {
					PluginServices.GlamourPlates.SetGlamourPlateSlot(inventoryItem, ref usedStains);
				}
			}
			//ImGui.SameLine();

			//if (ImGui.Button("Dye 1")) {
			//	PluginServices.GlamourPlates.ModifyGlamourPlateSlot(GlamourPlateSlot.Head, 87, 0, ref usedStains);
			//}
			//ImGui.SameLine();

			//if (ImGui.Button("Dye 2")) {
			//	PluginServices.GlamourPlates.ModifyGlamourPlateSlot(GlamourPlateSlot.Head, 91, 1, ref usedStains);
			//}
		}
		private static void DrawDresserContentsChecks() {
			var dresserContents = GlamourPlates.DresserContents;

			if (ImGui.BeginTable("##DresserContents##DrawDresserDresserContentsChecks##GlamourPlateDebug", 3)) {
				ImGui.TableSetupColumn("index");
				ImGui.TableSetupColumn("id");
				ImGui.TableSetupColumn("stains");
				ImGui.TableHeadersRow();

				for (var i=0; i < Offsets.TotalBoxSlot; i++) {
					var item = dresserContents.FirstOrDefault(z=>z.Slot == i);
					var exists = item.ItemId != 0;
					ImGui.TableNextColumn();
					ImGui.TextUnformatted($"{(exists ? item.Slot : "")}");
					ImGui.TableNextColumn();
					var icon = IconWrapper.Get(item.IconId);
					ImGui.Image(icon.GetWrapOrEmpty().ImGuiHandle, new Vector2(ImGui.GetFontSize()));
					ImGui.SameLine();
					ImGui.TextUnformatted($"{(exists ? item.ItemId:"")}");
					ImGui.TableNextColumn();
					ImGui.TextUnformatted($"{(exists ? item.Stain1:"")} + {(exists ? item.Stain2:"")}");
				}

				ImGui.EndTable();
			}
		}

	}
}
