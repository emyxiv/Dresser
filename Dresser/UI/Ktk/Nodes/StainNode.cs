

using System;
using System.Numerics;

using AllaganLib.Shared.Extensions;

using CsvHelper;

using Dresser.Extensions;
using Dresser.Gui;
using Dresser.Interop.Agents;
using Dresser.Services;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node.Simple;

using Lumina.Excel.Sheets;



namespace Dresser.UI.Ktk.Nodes {

	internal sealed unsafe class StainNode : SimpleComponentNode {
        public GlamourPlateSlot Slot { get; }
        private Stain? Stain { get; }
        private ushort StainIndex { get; }
		public Action<GlamourPlateSlot, Stain?, ushort>? OnSlotClicked;
        private readonly ImageNode _imageNode;
        private readonly NineGridNode _highlightNode;
        public StainNode (GlamourPlateSlot slot, Stain? stain, ushort stainIndex) {

            Slot = slot;
            Stain = stain;
            StainIndex = stainIndex;
            var uldPart = Stain == null || Stain.Value.RowId == 0 ? (Part)UldBundle.StainCircleEmpty : (Part)UldBundle.StainCircleFilled;

            _imageNode = new ImageNode {
                TextureResolveTheme = false,
                Size = new Vector2(18, 18),
                WrapMode = KamiToolKit.Enums.WrapMode.Stretch,
                NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents | NodeFlags.AnchorTop | NodeFlags.AnchorLeft,
                AddColor = Stain?.ColorToStainAdd() ?? Vector3.Zero,
            };
            _imageNode.AddPart(uldPart);
            _imageNode.AttachNode(this);

            _highlightNode = new NineGridNode() {
                NodeFlags = NodeFlags.Visible | NodeFlags.Enabled,
                Position = new Vector2(-4, -8),
                Size = new Vector2(41, 43),
                Offsets = new Vector4(23, 24, 24, 23),
                Parts = [(Part)UldBundle.SlotHighlight],
                Scale = new Vector2(0.75f),
                // Alpha = 0.8f
                IsVisible = false,
                
            };
            _highlightNode.AttachNode(this);


			CollisionNode.AddEvent(AtkEventType.MouseClick, OnMouseClick);
			CollisionNode.AddEvent(AtkEventType.MouseOver, OnMouseOver);
			CollisionNode.AddEvent(AtkEventType.MouseOut, OnMouseOut);

			// SFX
			CollisionNode.AddEvent(AtkEventType.MouseClick, () => UIGlobals.PlaySoundEffect(1));
			CollisionNode.AddEvent(AtkEventType.MouseOver, () => UIGlobals.PlaySoundEffect(0));
        }

        private void OnMouseOut() {
            _highlightNode.IsVisible = false;
        }

        private void OnMouseOver() {
            _highlightNode.IsVisible = true;
        }

        private void OnMouseClick() {
            OnSlotClicked?.Invoke(Slot, Stain, StainIndex);
        }
    }
}