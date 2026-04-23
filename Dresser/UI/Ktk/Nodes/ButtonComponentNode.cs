using System.Numerics;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Extensions;

using Dresser.Services;
using Dresser.Logic;


namespace Dresser.UI.Ktk.Nodes;

public unsafe class ButtonComponentNode : ButtonBase {
    public readonly ImageNode ImageNode;
    public readonly ImageNode ImageBackgroundNode;


    public ButtonComponentNode() {
        ImageBackgroundNode = new ImageNode {
            Position = new Vector2(-6.0f, -6.0f),
            Size = new Vector2(40.0f, 40.0f),
            TextureResolveTheme = false,
        };

        var partButtonBg = PluginServices.UldPartResolver.Resolve(UldBundle.MiragePrismMiragePlate_CloseButtonBg);
        if(partButtonBg == null) {
            PluginLog.Warning("Failed to resolve MiragePrismMiragePlate_CloseButtonBg for item slot frame");
            throw new System.Exception("Failed to resolve MiragePrismMiragePlate_CloseButtonBg for item slot frame");
        }

        ImageBackgroundNode.AddPart(partButtonBg);
        ImageBackgroundNode.AttachNode(this);




        ImageNode = new ImageNode {
            Size = new Vector2(28.0f, 28.0f),
            TextureResolveTheme = false,
        };
        var partButton = PluginServices.UldPartResolver.Resolve(UldBundle.MiragePrismMiragePlate_CloseButton);
        if(partButton == null) {
            PluginLog.Warning("Failed to resolve MiragePrismMiragePlate_CloseButton for item slot frame");
            throw new System.Exception("Failed to resolve MiragePrismMiragePlate_CloseButton for item slot frame");
        }
        ImageNode.AddPart(partButton);
        ImageNode.AttachNode(this);







        LoadTimelines();

        InitializeComponentEvents();
    }



    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        ImageNode.Size = Size;
    }

    private void LoadTimelines()
        => LoadTwoPartTimelines(this, ImageNode);
}

