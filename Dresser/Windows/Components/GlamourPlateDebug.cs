using CriticalCommonLib;
using CriticalCommonLib.Enums;

using Dresser.Extensions;
using Dresser.Services;

using ImGuiNET;

using Lumina.Excel.GeneratedSheets;

using System;
using System.Linq;

using GlamourPlates = Dresser.Interop.Hooks.GlamourPlates;

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
		}

		private unsafe static void CheckAgent() {
			var agent = GlamourPlates.MiragePrismMiragePlateAgent;
			if (agent == null) {
				return;
			}

			var pointerNint = ((IntPtr)agent + HeadSize);
			ImGui.Text($"Agent Pointer: {pointerNint}");
			if (ImGui.IsItemClicked()) pointerNint.ToString().ToClipboard();
			ImGui.SameLine();
			ImGui.Text($" {pointerNint:X8}");
			if (ImGui.IsItemClicked()) pointerNint.ToString("X8").ToClipboard();
			
			var editorInfo = *(IntPtr*)pointerNint;
			if (editorInfo == IntPtr.Zero) {
				ImGui.TextColored(ConfigurationManager.Config.ColorBad, $"agent inactive");
				return;
			}
			ImGui.TextColored(ConfigurationManager.Config.ColorGood, $"agent active");


			ImGui.Text($"editorInfo: {editorInfo}");
			if (ImGui.IsItemClicked()) editorInfo.ToString().ToClipboard();
			ImGui.SameLine();
			ImGui.Text($" {editorInfo:X8}");
			if (ImGui.IsItemClicked()) editorInfo.ToString("X8").ToClipboard();
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
			if (GlamourPlates.MiragePrismMiragePlateAgent == null) {
				return;
			}

			var editorInfo = *(IntPtr*)((IntPtr)GlamourPlates.MiragePrismMiragePlateAgent + HeadSize);
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
			if (GlamourPlates.MiragePrismMiragePlateAgent == null) {
				return;
			}

			var editorInfo = *(IntPtr*)((IntPtr)GlamourPlates.MiragePrismMiragePlateAgent + HeadSize);
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

	}
}
