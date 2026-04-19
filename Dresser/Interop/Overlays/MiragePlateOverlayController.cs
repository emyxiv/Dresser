using System;
using System.Collections.Generic;
using System.Numerics;

using Dresser.Logic;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Controllers;

namespace Dresser.Interop.Overlays {
	internal unsafe class MiragePlateOverlayController : IDisposable {
		private const int RadioButtonOffsetId = 6;
		private const int PlateCount = 20;

		private readonly AddonController _controller;
		private Dictionary<uint, Vector4?> _tabColours = CreateEmptyTabs();

		public MiragePlateOverlayController() {
			_controller = new AddonController {
				AddonName = "MiragePrismMiragePlate",
				OnRefresh = OnRefresh,
				OnFinalize = OnFinalize,
			};
			_controller.Enable();
		}

		private void OnRefresh(AtkUnitBase* addon) {
			UpdateState();
			ApplyTabColors(addon);
		}

		private void OnFinalize(AtkUnitBase* addon) {
			ClearTabColors(addon);
		}

		public void UpdateState() {
			var plateHighlight = PluginServices.ApplyGearChange.HighlightPlatesRadio;
			var saveButton = PluginServices.ApplyGearChange.HighlightSaveButton;

			if (plateHighlight != null && plateHighlight.Count > 0) {
				_tabColours = CreateEmptyTabs();
				foreach (var (plateIndex, color) in plateHighlight) {
					_tabColours[plateIndex] = color;
				}
			} else {
				_tabColours = CreateEmptyTabs();
			}
		}

		private void ApplyTabColors(AtkUnitBase* addon) {
			if (addon == null) return;
			foreach (var (tab, newColour) in _tabColours) {
				var nodeId = (uint)(RadioButtonOffsetId + tab);
				var radioButton = (AtkComponentNode*)addon->GetNodeById(nodeId);
				if (radioButton == null || (ushort)radioButton->AtkResNode.Type < 1000) return;
				var atkResNode = (AtkResNode*)radioButton;
				if (newColour.HasValue) {
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

		private void ClearTabColors(AtkUnitBase* addon) {
			_tabColours = CreateEmptyTabs();
			ApplyTabColors(addon);
		}

		private static Dictionary<uint, Vector4?> CreateEmptyTabs() {
			var dict = new Dictionary<uint, Vector4?>(PlateCount);
			for (uint i = 0; i < PlateCount; i++) {
				dict[i] = null;
			}
			return dict;
		}

		public void Dispose() {
			_controller.Dispose();
		}
	}
}
