using CriticalCommonLib;

using Dalamud.Logging;

using Dresser.Data;
using Dresser.Structs.FFXIV;

using ImGuiNET;

using ImGuiScene;

using Lumina.Data.Files;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using static System.Net.Mime.MediaTypeNames;
using static System.Reflection.Metadata.BlobBuilder;

namespace Dresser.Windows.Components
{
	internal class Plates{
		public static List<GlamourPlateSlot> SlotsLeft = new() {
			GlamourPlateSlot.MainHand,
			GlamourPlateSlot.Head,
			GlamourPlateSlot.Body,
			GlamourPlateSlot.Hands,
			GlamourPlateSlot.Legs,
			GlamourPlateSlot.Feet,
		};
		public static List<GlamourPlateSlot> SlotsRight = new() {
			GlamourPlateSlot.OffHand,
			GlamourPlateSlot.Ears,
			GlamourPlateSlot.Neck,
			GlamourPlateSlot.Wrists,
			GlamourPlateSlot.RightRing,
			GlamourPlateSlot.LeftRing,
		};



		public static void DrawLeftSlots() => DrawSlots(SlotsLeft);

		public static void DrawRightSlots() => DrawSlots(SlotsRight);

		public static void DrawSlots(IEnumerable<GlamourPlateSlot> slots) {
			foreach (var slot in slots) {
				Storage.SlotItemsEx.TryGetValue(slot, out var item);

				var image = PluginServices.IconStorage.Get(item);
				ImGui.ImageButton(image.ImGuiHandle, Browse.IconSize);
			}
		}
		

		public static void Draw() {
			//Gathering.ParseGlamourPlates();

			//if (ImGui.Button($"sync"))
			//	Init();
			//ImGui.SameLine();
			//ImGui.SetNextItemWidth(ImGui.GetFontSize() * 4);
			//ImGui.SliderFloat("##IconSize##slider", ref IconSize, 0.1f, 15f, "%.2f px");

			// TODO: find a way to get a part of textures
			// ring: 28 // bracer: 27 // necklace: 26 // earring: 25 // feet: 24 // legs: 23 // hands: 21 // body: 20 // head: 19 // head: 19 // main weapon: 17 // off weapon: 18
			//if(DataStorage.EmptyEquipTexture != null)
			//	ImGui.Image(DataStorage.EmptyEquipTexture.ImGuiHandle,new Vector2(DataStorage.EmptyEquipTexture.Width, DataStorage.EmptyEquipTexture.Height) * IconSize);

			if (Storage.DisplayPage == null) return;

			DrawDisplay(Storage.DisplayPage.Value);
		}
		private static void DrawDisplay(MiragePage plate) {
			ImGui.Columns(2, "Plate##DrawDisplay", false);
			DrawLeftSlots();
			ImGui.NextColumn();
			DrawRightSlots();
			ImGui.Columns();

		}

	}
}
