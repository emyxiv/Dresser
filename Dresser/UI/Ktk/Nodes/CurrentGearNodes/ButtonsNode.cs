
using System;
using System.Linq;
using System.Numerics;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace Dresser.UI.Ktk.Nodes.CurrentGearNodes;

public unsafe class ButtonsNode : HorizontalFlexNode {
    public required PreviewNode PreviewNode;

    public ButtonsNode() {

        var toggleWeapon = new ImageToggleNode(UldBundle.CircleSmallWeapon) {
            Size = new Vector2(28.0f, 28.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TextTooltip = "Hide/Display main and offhand weapons.",

            ConfigProperty = nameof(Configuration.CurrentGearDisplayWeapon),
            OnToggle = (isEnabled) => PluginServices.Context.LocalPlayer?.SetWeaponVisibility(),
        };
        this.AddNode(toggleWeapon);

        var toggleHeadgear = new ImageToggleNode(UldBundle.CircleSmallHat) {
            Size = new Vector2(28.0f, 28.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TextTooltip = "Hide/Display headgear.",
 
            ConfigProperty = nameof(Configuration.CurrentGearDisplayHat),
            OnToggle = (isEnabled) => PluginServices.Context.LocalPlayer?.SetHatVisibility(),
        };
        this.AddNode(toggleHeadgear);

        var toggleVisor = new ImageToggleNode(UldBundle.CircleSmallVisor) {
            Size = new Vector2(28.0f, 28.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TextTooltip = "Manually adjust visor.",

            ConfigProperty = nameof(Configuration.CurrentGearDisplayVisor),
            OnToggle = (isEnabled) => PluginServices.Context.LocalPlayer?.SetVisorVisibility(),
        };
        this.AddNode(toggleVisor);
        
        var drawWeapon = new ImageToggleNode(UldBundle.CircleSmallViewport) {
            Size = new Vector2(28.0f, 28.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
            TextTooltip = "Sheathe/Draw Weapon.",

            OnToggle = (isEnabled) => {
                PreviewNode?.InspectCharaView->ToggleDrawWeapon(isEnabled);
            },
        };
        this.AddNode(drawWeapon);
        
        RecalculateLayout();
    }
}