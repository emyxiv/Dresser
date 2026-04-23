using System;
using System.Collections.Generic;
using System.Numerics;

using Dresser.Extensions;

using Dresser.Gui;

using Dresser.Interop.Agents;
using Dresser.Logic;
using Dresser.Models;
using Dresser.Models.ViewModels;
using Dresser.Services;
using Dresser.UI.Ktk.Components;
using Dresser.UI.Ktk.Extensions;

using FFXIVClientStructs.FFXIV.Client.UI;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Classes;

using KamiToolKit.Enums;

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
		private bool _isHovered = false;
		private readonly InventoryItem? _currentItem = null;
		public readonly List<StainNode> StainNodes = [];

		public KtkItemSlot(GlamourPlateSlot slot, UldPartResolver resolver) {
			_slot = slot;
			_currentItem = PluginServices.ApplyGearChange.GetCurrentPlateItem(slot);
			Size = new Vector2(48, 48);

			_iconNode = KtkTextureFactory.CreateItemIconNode(size: new Vector2(44, 48));
			_iconNode.Position = new Vector2(0, 0);
			// _iconNode.IsVisible = false;
			_iconNode.NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents | NodeFlags.Focusable | NodeFlags.RespondToMouse;
			_iconNode.IconExtras.CooldownNode.IsVisible = false;
			_iconNode.IconExtras.ResourceCostTextNode.IsVisible = false;
			_iconNode.IconExtras.QuantityTextNode.IsVisible = false;
			_iconNode.IconExtras.AntsNode.IsVisible = false;
			_iconNode.ItemTooltip = PluginServices.ApplyGearChange.GetCurrentPlateItem(slot)?.ItemId ?? 0; // Set initial tooltip based on currently equipped item
			_iconNode.InventoryItemTooltip = new KamiToolKit.InventoryItemTooltip(FFXIVClientStructs.FFXIV.Client.Game.InventoryType.ArmoryBody, 1); {
				
			}; // Use native item tooltips
			_iconNode.AttachNode(this);

			// Frame overlay
			_frameNode = new ImageNode {
				Size = new Vector2(48, 48),
				NodeFlags = NodeFlags.Visible | NodeFlags.Enabled,
				WrapMode = WrapMode.Stretch,
			};

			_frameNode.AddPart((Part)UldBundle.MirageSlotNormal);
			// KtkTextureFactory.CreateFrameImageNode(resolver, new Vector2(48, 48));
			_frameNode.Position = new Vector2(0, 0);
			// _frameNode.PartId = 0; // ItemSlot frame (index 4 in IconA_Frame)
			_frameNode.NodeId = 18; // Before cooldown node for correct layering
			_frameNode.AttachNode(_iconNode.IconExtras.CooldownNode, KamiToolKit.Classes.NodePosition.BeforeTarget);

			// Empty slot placeholder icon (shown when no item equipped)
			_emptySlotNode = KtkTextureFactory.CreateEmptySlotNode(slot, resolver, new Vector2(32, 32));
			if (_emptySlotNode != null) {
				_emptySlotNode.Position = new Vector2(8, 8);
				_emptySlotNode.IsVisible = true;
				_frameNode.NodeId = 17; // Before cooldown node for correct layering
				_emptySlotNode.AttachNode(_iconNode.IconExtras.CooldownNode, KamiToolKit.Classes.NodePosition.BeforeTarget);
			}

			if (_currentItem != null && _currentItem.Item.IsDyeable1()) {
				var stain1 = new StainNode(_slot, _currentItem?.StainEntry, 0) {
					NodeId = 13,
					Size = new Vector2(18, 18),
					Scale = new Vector2(1.20f),
					Position = new Vector2(25, -1),
					NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
				};
				stain1.AttachNode(_iconNode.IconExtras.AlternateCooldownNode, KamiToolKit.Classes.NodePosition.AfterTarget);
				StainNodes.Add(stain1);
			}
			if( _currentItem != null && _currentItem.Item.IsDyeable2()) {
				var stain2 = new StainNode(_slot, _currentItem?.Stain2Entry, 1) {
					NodeId = 13,
					Size = new Vector2(18, 18),
					Scale = new Vector2(1.20f),
					Position = new Vector2(25, 12),
					NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
				};
				stain2.AttachNode(_iconNode.IconExtras.AlternateCooldownNode, KamiToolKit.Classes.NodePosition.AfterTarget);
				StainNodes.Add(stain2);
			}


			// Mouse events
			CollisionNode.AddEvent(AtkEventType.MouseClick, OnMouseClick);
			CollisionNode.AddEvent(AtkEventType.MouseOver, OnMouseOver);
			CollisionNode.AddEvent(AtkEventType.MouseOut, OnMouseOut);

			// Play a sound effect on click and hover for feedback
			CollisionNode.AddEvent(AtkEventType.MouseClick, () => UIGlobals.PlaySoundEffect(1));
			CollisionNode.AddEvent(AtkEventType.MouseOver, () => UIGlobals.PlaySoundEffect(0));
			// using sfx 0-17 for each slot just for fun, april fools
			// CollisionNode.AddEvent(AtkEventType.MouseOver, () => UIGlobals.PlaySoundEffect((uint)slot));

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
			_iconNode.ItemTooltip = 0;
			_iconNode.IconImage.IsVisible = false;
			if (_emptySlotNode != null)
				_emptySlotNode.IsVisible = true;
		}

		/// <summary>
		/// Set whether this slot is visually selected (highlighted).
		/// </summary>
		public void SetSelected(bool selected) {
			_iconNode.IconExtras.HoveredBorderImageNode.IsVisible = selected || _isHovered;
		}

		public GlamourPlateSlot Slot => _slot;

		private void OnMouseClick(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
			if (atkEventData->IsLeftClick)
				OnSlotClicked?.Invoke(_slot);
			else if (atkEventData->IsRightClick && !_isEmpty)
				OnSlotMiddleClicked?.Invoke(_slot);
		}

		private void OnMouseOver(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
			_isHovered = true;
			OnSlotHovered?.Invoke(_slot);
		}

		private void OnMouseOut(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
			_isHovered = false;
			OnSlotUnhovered?.Invoke(_slot);
		}
	}
}
