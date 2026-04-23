using System;
using System.Numerics;

using Dresser.Logic;
using Dresser.Services;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

using Humanizer;

using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node.Simple;

namespace Dresser.UI.Ktk.Nodes;

public class ImageToggleNode : SimpleComponentNode {
    private readonly ImageNode imageNode;
    // private readonly ClippingMaskNode clipNode;
    private readonly ImageNode highlightNode; // For selected
    // private readonly SimpleImageNode lowlightNode;  // For unselected
    private int _imageSize = 36;
    private int _imageHighlightSize = 36;

    public unsafe ImageToggleNode(UldBundle partBundle) {
        PartBundle = partBundle;

        imageNode = new ImageNode {
            TextureResolveTheme = false,
            // Position = new Vector2(2.0f, 2.0f),
            Size = new Vector2(_imageSize),
            WrapMode = KamiToolKit.Enums.WrapMode.Stretch,
        };
        imageNode.AddPart(PluginServices.UldPartResolver.Resolve(PartBundle) ?? throw new System.Exception($"Failed to resolve {PartBundle.Handle} for image toggle"));
        imageNode.AttachNode(this);

        // clipNode = new SimpleClippingMaskNode {
        //     TextureCoordinates = Vector2.Zero,
        //     TextureSize = new Vector2(32.0f, 32.0f),
        //     TexturePath = "ui/uld/BgPartsMask.tex",
        //     Size = new Vector2(32.0f, 32.0f),
        // };
        // clipNode.AttachNode(this);

       highlightNode = new ImageNode {
            TextureResolveTheme = false,
            IsVisible = false,

            Size = new Vector2(_imageHighlightSize),
            WrapMode = KamiToolKit.Enums.WrapMode.Stretch,
        };
        highlightNode.AddPart(PluginServices.UldPartResolver.Resolve(UldBundle.CircleLargeHighlight) ?? throw new System.Exception($"Failed to resolve {PartBundle.Handle} for highlight state"));
        highlightNode.AttachNode(this);
        // highlightNode = new SimpleImageNode {
        //     Size = new Vector2(36.0f, 36.0f),
        //     IsVisible = false,
        //     TextureCoordinates = new Vector2(69.0f, 1.0f),
        //     TextureSize = new Vector2(36.0f, 36.0f),
        //     TexturePath = "ui/uld/BgParts.tex",
        // };
        // highlightNode.AttachNode(this);

        // lowlightNode = new SimpleImageNode {
        //     Size = new Vector2(36.0f, 36.0f),
        //     IsVisible = false,
        //     TextureCoordinates = new Vector2(141.0f, 1.0f),
        //     TextureSize = new Vector2(36.0f, 36.0f),
        //     TexturePath = "ui/uld/BgParts.tex",
        // };
        // lowlightNode.AttachNode(this);

        // SFX
        CollisionNode.AddEvent(AtkEventType.MouseOver, () => UIGlobals.PlaySoundEffect(0));
        CollisionNode.AddEvent(AtkEventType.MouseClick, () => UIGlobals.PlaySoundEffect(1));

        // Toggle state on click
        CollisionNode.AddEvent(AtkEventType.MouseClick, ToggleValue);
    }

    private void ToggleValue() {
        IsToggled = !IsToggled;
    }

    public UldBundle PartBundle {get; set;}

    public bool IsToggled {
        get;
        set {
            field = value;
            highlightNode.IsVisible = value;
            // lowlightNode.IsVisible = !value;
        }
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        // Icon is 32x32 centered within the 36x36 node
        var sizeDiff = _imageHighlightSize - _imageSize;
        var iconSize = Size - new Vector2(sizeDiff);
        var iconOffset = new Vector2(sizeDiff / 2.0f);
        imageNode.Size = iconSize;
        imageNode.Position = iconOffset;

        // clipNode.Size = iconSize;
        // clipNode.Position = iconOffset;

        highlightNode.Size = Size;
        highlightNode.Position = Vector2.Zero;

        // lowlightNode.Size = Size;
        // lowlightNode.Position = Vector2.Zero;
    }
}
