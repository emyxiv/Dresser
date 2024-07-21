using CriticalCommonLib.Services.Ui;

using Dresser.Logic;

using FFXIVClientStructs.FFXIV.Component.GUI;

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Dresser.Interop.GameUi {
	public class AtkMiragePrismMiragePlate : AtkOverlay {
		public override WindowName WindowName { get; set; } = WindowName.MiragePrismMiragePlate;
		public int RadioButtonOffsetId = 6;
		public int SaveButtonId = 114;




		public unsafe short CurrentPlate {
			get {
				var addon = AtkUnitBase;
				if (addon != null && addon.AtkUnitBase != null) {
					var actualAddon = (InventoryMiragePrismMiragePlateAddon*)addon.AtkUnitBase;
					return actualAddon->SelectedPlate;
				}
				return -1;
			}
		}


		public unsafe void SetTabColors(Dictionary<uint, Vector4?> indexedTabColours) {
			var atkBaseWrapper = AtkUnitBase;
			if (atkBaseWrapper == null) return;
			foreach (var colour in indexedTabColours) {
				Vector4? newColour = colour.Value;
				var tab = colour.Key;

				var nodeId = (uint)(RadioButtonOffsetId + tab);
				var radioButton = (AtkComponentNode*)atkBaseWrapper.AtkUnitBase->GetNodeById(nodeId);
				if (radioButton == null || (ushort)radioButton->AtkResNode.Type < 1000) return;
				var atkResNode = (AtkResNode*)radioButton;
				if (newColour.HasValue) {
					PluginLog.Debug($"Coloring tab {tab} into color {newColour * 255f}");
					atkResNode->Color.A = (byte)(newColour.Value.W * 255.0f);
					atkResNode->AddBlue = (short)(newColour.Value.Z * 255.0f);
					atkResNode->AddRed = (short)(newColour.Value.X * 255.0f);
					atkResNode->AddGreen = (short)(newColour.Value.Y * 255.0f);
					atkResNode->MultiplyRed = 30;
					atkResNode->MultiplyGreen = 30;
					atkResNode->MultiplyBlue = 30;
				} else {
					atkResNode->Color.A = 255;
					atkResNode->AddBlue = 0;
					atkResNode->AddRed = 0;
					atkResNode->AddGreen = 0;
					atkResNode->MultiplyRed = 100;
					atkResNode->MultiplyGreen = 100;
					atkResNode->MultiplyBlue = 100;

				}
			}
		}

		public override void Update() {}
	}

	[StructLayout(LayoutKind.Explicit, Size = 0x2DD)]
	public struct InventoryMiragePrismMiragePlateAddon {
		[FieldOffset(0)] public AtkUnitBase AtkUnitBase;
		[FieldOffset(0x2DC)] public byte SelectedPlate;
	}
}
