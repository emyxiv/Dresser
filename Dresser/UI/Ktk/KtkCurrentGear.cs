using System;
using System.Collections.Generic;
using System.Numerics;

using Dresser.Interop.Agents;
using Dresser.Logic;
using Dresser.Models;
using Dresser.Models.ViewModels;
using Dresser.Services;
using Dresser.UI.Ktk.Nodes;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node.Simple;

namespace Dresser.UI.Ktk {
	/// <summary>
	/// KamiToolKit NativeAddon implementation of the CurrentGear window.
	/// Displays 12 equipment slots in a 2x6 grid with action buttons.
	/// Uses shared ViewModels (ItemRenderData) for data, KTK native nodes for rendering.
	/// Auto-falls-back to ImGui on unhandled exceptions.
	/// </summary>
	internal sealed unsafe class KtkCurrentGear : NativeAddon, IDisposable {

		private readonly UldPartResolver _resolver;
		private readonly Dictionary<GlamourPlateSlot, KtkItemSlot> _slots = new();
		private SimpleComponentNode _mainContainer = null!;
		private GridNode _slotsGrid = null!;
		private bool _hasCrashed;

		/// <summary>
		/// Called by Plugin.cs when a KTK crash occurs and we need to fall back.
		/// </summary>
		public Action? OnCrashFallback;

		public KtkCurrentGear() : base() {
			PluginLog.Debug("KtkCurrentGear: constructor called");
			_resolver = new UldPartResolver();
		}

		public static readonly List<GlamourPlateSlot> SlotOrder = new() {
			GlamourPlateSlot.MainHand, GlamourPlateSlot.OffHand,
			GlamourPlateSlot.Head, GlamourPlateSlot.Ears,
			GlamourPlateSlot.Body, GlamourPlateSlot.Neck,
			GlamourPlateSlot.Hands, GlamourPlateSlot.Wrists,
			GlamourPlateSlot.Legs, GlamourPlateSlot.RightRing,
			GlamourPlateSlot.Feet, GlamourPlateSlot.LeftRing,
		};

		protected override void OnSetup(AtkUnitBase* addon) {
			PluginLog.Debug($"KtkCurrentGear.OnSetup: called (InternalAddon=0x{(nint)addon:X})");
			try {
				PluginLog.Debug($"KtkCurrentGear.OnSetup: ContentStartPosition={ContentStartPosition} ContentSize={ContentSize}");
				_mainContainer = new SimpleComponentNode {
					Position = ContentStartPosition,
					Size = ContentSize,
				};
				_mainContainer.AttachNode(this);
				PluginLog.Debug("KtkCurrentGear.OnSetup: container attached, building slot grid");

				BuildSlotGrid();
				RecalculateSize();

				PluginLog.Debug("KtkCurrentGear.OnSetup: complete");
			} catch (Exception e) {
				PluginLog.Error(e, "KtkCurrentGear.OnSetup crashed");
				HandleCrash();
			}
		}

        private void RecalculateSize() {
			var newSize = (_slotsGrid.Size * SlotScale) // slots grid size
				// + (ContentPadding * 2.0f + new Vector2(0, 4))
				+ ContentStartPosition // + title bar height and top padding
				+ new Vector2(0, 25) // + extra height because there is something missing in this calculation and the window is too short, this is a temporary hack until I figure out what it is 
				;
			SetWindowSize(newSize);
			_mainContainer.Size = newSize;
        }

        protected override void OnUpdate(AtkUnitBase* addon) {
			if (_hasCrashed) return;
			try {
				RefreshSlots();
			} catch (Exception e) {
				PluginLog.Error(e, "KtkCurrentGear.OnUpdate crashed");
				HandleCrash();
			}
		}

		private void BuildSlotGrid() {
			PluginLog.Debug($"KtkCurrentGear.BuildSlotGrid: creating {SlotOrder.Count} slots");
			_slotsGrid = new GridNode {
				Position = new Vector2(0, 0),
				Size = new Vector2(48, 48) * new Vector2(2, 6),
				GridSize = new GridSize(2, 6),
				Scale = new Vector2(SlotScale),
			};
			_slotsGrid.AttachNode(_mainContainer);

			int col = 0;
			int row = 0;
			foreach (var slot in SlotOrder) {
				var slotNode = new KtkItemSlot(slot, _resolver);
				slotNode.OnSlotClicked = OnSlotClicked;
				slotNode.OnSlotMiddleClicked = OnSlotMiddleClicked;
				slotNode.OnSlotHovered = OnSlotHovered;
				slotNode.OnSlotUnhovered = OnSlotUnhovered;

				slotNode.AttachNode(_slotsGrid[col, row]);
				_slots[slot] = slotNode;
				PluginLog.Debug($"KtkCurrentGear.BuildSlotGrid: attached slot {slot} at [{col},{row}]");

				// 2-column layout: increment column, wrap to next row
				col++;
				if (col >= 2) {
					col = 0;
					row++;
				}
			}
			PluginLog.Debug("KtkCurrentGear.BuildSlotGrid: complete");
		}

		private void RefreshSlots() {
			var selectedSlot = ConfigurationManager.Config.CurrentGearSelectedSlot;

			foreach (var (slot, slotNode) in _slots) {
				var item = PluginServices.ApplyGearChange.GetCurrentPlateItem(slot);
				var renderData = ItemRenderData.From(item, slot);
				slotNode.Update(renderData);
				slotNode.SetSelected(slot == selectedSlot);
			}
		}

		// --- Event Handlers ---

		private void OnSlotClicked(GlamourPlateSlot slot) {
			try {
				PluginServices.ApplyGearChange.ExecuteCurrentItem(slot);
			} catch (Exception e) {
				PluginLog.Error(e, $"Error handling slot click for {slot}");
			}
		}

		private void OnSlotMiddleClicked(GlamourPlateSlot slot) {
			try {
				var item = PluginServices.ApplyGearChange.GetCurrentPlateItem(slot);
				if (item != null)
					PluginServices.ApplyGearChange.ExecuteCurrentContextRemoveItem(item, slot);
			} catch (Exception e) {
				PluginLog.Error(e, $"Error handling slot middle click for {slot}");
			}
		}

		private static void OnSlotHovered(GlamourPlateSlot slot) {
			// Could highlight the slot in the browser
		}

		private static void OnSlotUnhovered(GlamourPlateSlot slot) {
			// Clear hover state
		}

		// --- Crash Recovery ---

		private void HandleCrash() {
			_hasCrashed = true;
			PluginLog.Error("KtkCurrentGear crashed — falling back to ImGui");
			try {
				Close();
			} catch {
				// Best effort close
			}
			OnCrashFallback?.Invoke();
		}

		private static float SlotScale => 1.7f * ConfigurationManager.Config.IconSizeMult;

		public new void Dispose() {
			_resolver.Dispose();
			_slots.Clear();
			base.Dispose();
		}
	}
}
