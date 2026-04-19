using System;
using System.Collections.Generic;
using System.Numerics;

using Dresser.Logic;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Controllers;

using Penumbra.GameData.Data;

namespace Dresser.Interop.Overlays {
	internal unsafe class MiragePlateOverlayController : IDisposable {
		private const int RadioButtonOffsetId = 6;
		private const int PlateCount = 20;

		private readonly AddonController _controller;
		private Dictionary<uint, Vector4?> _tabColours = CreateEmptyTabs();
		private bool _hasActiveHighlights = false;
		private bool _needsClear = false;

		public MiragePlateOverlayController() {
			_controller = new AddonController {
				AddonName = "MiragePrismMiragePlate",
				OnRefresh = OnRefresh,
				OnUpdate = OnUpdate,
				OnFinalize = OnFinalize,
			};
			_controller.Enable();
		}

		private void OnRefresh(AtkUnitBase* addon) {
			UpdateState();
		}

		private void OnUpdate(AtkUnitBase* addon) {
			if (_hasActiveHighlights) {
				// Re-apply every frame to combat game hover/selection overrides
				ApplyTabColors(addon);
			} else if (_needsClear) {
				// Clear once after highlights are removed
				ApplyTabColors(addon);
				_needsClear = false;
			}
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
			RefreshActiveState();
		}

		private void ApplyTabColors(AtkUnitBase* addon) {
			if (addon == null) return;
			foreach (var (tab, newColour) in _tabColours) {
				var nodeId = (uint)(RadioButtonOffsetId + tab);
				var radioButton = (AtkComponentNode*)addon->GetNodeById(nodeId);
				if (radioButton == null || (ushort)radioButton->AtkResNode.Type < 1000) return;

				var nineGrid = radioButton->Component->UldManager.SearchNodeById(4);

				if (nineGrid == null) continue;

				if (newColour.HasValue) {
					nineGrid->Color.A = (byte)(newColour.Value.W * 255.0f);
					nineGrid->AddBlue = (short)(newColour.Value.Z * 255.0f);
					nineGrid->AddRed = (short)(newColour.Value.X * 255.0f);
					nineGrid->AddGreen = (short)(newColour.Value.Y * 255.0f);
					nineGrid->MultiplyRed = 30;
					nineGrid->MultiplyGreen = 30;
					nineGrid->MultiplyBlue = 30;
				} else {
					nineGrid->Color.A = 255;
					nineGrid->AddBlue = 0;
					nineGrid->AddRed = 0;
					nineGrid->AddGreen = 0;
					nineGrid->MultiplyRed = 100;
					nineGrid->MultiplyGreen = 100;
					nineGrid->MultiplyBlue = 100;
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

		private void RefreshActiveState() {
			var wasActive = _hasActiveHighlights;
			_hasActiveHighlights = false;
			foreach (var color in _tabColours.Values) {
				if (color.HasValue) {
					_hasActiveHighlights = true;
					break;
				}
			}
			// If we just went from active to inactive, need one final clear pass
			if (wasActive && !_hasActiveHighlights)
				_needsClear = true;
		}

		public void DebugSetTabColor(uint tabIndex, Vector4 color) {
			if (tabIndex < PlateCount)
				_tabColours[tabIndex] = color;
			RefreshActiveState();
		}

		public void DebugClearTabs() {
			_tabColours = CreateEmptyTabs();
			RefreshActiveState();
		}
        public bool DebugIsVisible() {
            try {
                return FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentMiragePrismMiragePlate.Instance()->IsAddonShown();
            } catch {
                return false;
            }
        }
        public string DebugGetName() {
            return _controller.AddonName;
        }

		public void Dispose() {
			_controller.Dispose();
		}
	}
}
