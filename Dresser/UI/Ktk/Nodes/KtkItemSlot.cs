using System;
using System.Numerics;

using Dresser.Interop.Agents;
using Dresser.Logic;
using Dresser.Models.ViewModels;
using Dresser.Services;
using Dresser.UI.Ktk.Components;
using Dresser.UI.Ktk.Extensions;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node.Simple;

namespace Dresser.UI.Ktk.Nodes {
	/// <summary>
	/// A composite KTK node representing a single equipment slot in the CurrentGear window.
	/// Displays either an item icon (with frame and native tooltip) or an empty-slot placeholder.
	/// </summary>
	internal sealed unsafe class KtkItemSlot : SimpleComponentNode {

		private readonly IconNode _iconNode;
		private readonly ImageNode _frameNode;
		private readonly ImageNode? _emptySlotNode;
		private readonly GlamourPlateSlot _slot;

		public Action<GlamourPlateSlot>? OnSlotClicked;
		public Action<GlamourPlateSlot>? OnSlotMiddleClicked;
		public Action<GlamourPlateSlot>? OnSlotHovered;
		public Action<GlamourPlateSlot>? OnSlotUnhovered;

		private bool _isEmpty = true;

		public KtkItemSlot(GlamourPlateSlot slot, UldPartResolver resolver) {
			_slot = slot;
			Size = new Vector2(48, 48);

			// Item icon (hidden when slot is empty)
			_iconNode = KtkTextureFactory.CreateItemIconNode(size: new Vector2(44, 48));
			_iconNode.Position = new Vector2(0, 0);
			_iconNode.IsVisible = false;
			_iconNode.IconExtras.CooldownNode.IsVisible = false;
			_iconNode.IconExtras.ResourceCostTextNode.IsVisible = false;
			_iconNode.IconExtras.QuantityTextNode.IsVisible = false;
			_iconNode.IconExtras.AntsNode.IsVisible = false;
			_iconNode.AttachNode(this);

			// Frame overlay
			_frameNode = KtkTextureFactory.CreateFrameImageNode(resolver, new Vector2(48, 48));
			_frameNode.Position = new Vector2(-2, 0);
			_frameNode.PartId = 4; // ItemSlot frame (index 4 in IconA_Frame)
			_frameNode.AttachNode(this);

			// Empty slot placeholder icon (shown when no item equipped)
			_emptySlotNode = KtkTextureFactory.CreateEmptySlotNode(slot, resolver, new Vector2(48, 48));
			if (_emptySlotNode != null) {
				_emptySlotNode.Position = new Vector2(0, 0);
				_emptySlotNode.IsVisible = true;
				_emptySlotNode.AttachNode(this);
			}

			// Mouse events
			CollisionNode.AddEvent(AtkEventType.MouseClick, OnMouseClick);
			CollisionNode.AddEvent(AtkEventType.MouseOver, OnMouseOver);
			CollisionNode.AddEvent(AtkEventType.MouseOut, OnMouseOut);
		}

		/// <summary>
		/// Update the slot display with item data.
		/// </summary>
		public void Update(ItemRenderData? data) {
			if (data == null || data.IsEmpty) {
				SetEmpty();
				return;
			}

			_isEmpty = false;

			// Show item icon
			if (data.IconId > 0)
				_iconNode.IconId = data.IconId;
			_iconNode.IsVisible = true;
			_iconNode.ItemTooltip = data.ItemId;

			// Hide empty placeholder
			if (_emptySlotNode != null)
				_emptySlotNode.IsVisible = false;

			// Hover border follows selection state
			_iconNode.IconExtras.HoveredBorderImageNode.IsVisible = false;
		}

		/// <summary>
		/// Show the empty-slot placeholder for this slot.
		/// </summary>
		public void SetEmpty() {
			_isEmpty = true;
			_iconNode.IsVisible = false;
			_iconNode.ItemTooltip = 0;
			if (_emptySlotNode != null)
				_emptySlotNode.IsVisible = true;
		}

		/// <summary>
		/// Set whether this slot is visually selected (highlighted).
		/// </summary>
		public void SetSelected(bool selected) {
			_iconNode.IconExtras.HoveredBorderImageNode.IsVisible = selected;
		}

		public GlamourPlateSlot Slot => _slot;

		private void OnMouseClick(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
			if (atkEventData->IsLeftClick)
				OnSlotClicked?.Invoke(_slot);
			else if (atkEventData->IsRightClick && !_isEmpty)
				OnSlotMiddleClicked?.Invoke(_slot);
		}

		private void OnMouseOver(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
			OnSlotHovered?.Invoke(_slot);
		}

		private void OnMouseOut(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
			OnSlotUnhovered?.Invoke(_slot);
		}
	}
}
