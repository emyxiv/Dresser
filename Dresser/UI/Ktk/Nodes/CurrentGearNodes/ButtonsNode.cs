
using System;
using System.Linq;
using System.Numerics;

using Dresser.Logic;
using Dresser.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace Dresser.UI.Ktk.Nodes.CurrentGearNodes;

public unsafe class ButtonsNode : HorizontalFlexNode {
    public ButtonsNode() {

        var toggleWeapon = new ImageToggleNode(UldBundle.CircleSmallWeapon) {
            Size = new Vector2(28.0f, 28.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TextTooltip = "Hide/Display main and offhand weapons.",
        };
        this.AddNode(toggleWeapon);

        var toggleHeadgear = new ImageToggleNode(UldBundle.CircleSmallHat) {
            Size = new Vector2(28.0f, 28.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TextTooltip = "Hide/Display headgear.",
        };
        this.AddNode(toggleHeadgear);

        var toggleVisor = new ImageToggleNode(UldBundle.CircleSmallVisor) {
            Size = new Vector2(28.0f, 28.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TextTooltip = "Manually adjust visor.",
        };
        this.AddNode(toggleVisor);
        
        RecalculateLayout();
    }
}