using System;
using System.Numerics;

using Dresser.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node.Simple;
using KamiToolKit.Timelines;
using Lumina.Text.ReadOnly;

namespace Dresser.UI.Ktk.Nodes;

internal unsafe class PlateRadioNode : ComponentNode<AtkComponentRadioButton, AtkUldComponentDataRadioButton> {
    public readonly TextNode LabelNode;
    public readonly NineGridNode SelectedImageNode;

    public readonly NineGridNode UnselectedImageNode;

    public PlateRadioNode() {
        SetInternalComponentType(ComponentType.RadioButton);

        UnselectedImageNode = new NineGridNode {
            NodeId = 4,
            Position = new Vector2(-2, -1),
            Size = new Vector2(46.0f, 26.0f),
            Parts = [(Part)UldBundle.MiragePlateRadio],
            Offsets = new Vector4(0, 0, 16, 16),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents | NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.AnchorRight,
        };
        UnselectedImageNode.AttachNode(this);

        SelectedImageNode = new NineGridNode {
            NodeId = 3,
            Position = new Vector2(-2, -1),
            Size = new Vector2(46.0f, 26.0f),
            Parts = [(Part)UldBundle.MiragePlateRadioSelected],
            Offsets = new Vector4(0, 0, 16, 16),
            IsVisible = false,
            NodeFlags = NodeFlags.Enabled | NodeFlags.EmitsEvents | NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.AnchorRight,
        };
        SelectedImageNode.AttachNode(this);

        LabelNode = new TextNode {
            NodeId = 2,
            Position = new Vector2(11.0f, 2.0f),
            Size = new Vector2(17.0f, 20.0f),
            FontType = FontType.Axis,
            FontSize = 12,
            TextColor = new Vector4(0.392f, 0.392f, 0.392f, 1.0f),
            TextOutlineColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f),
            AlignmentType = AlignmentType.Center,
        };
        LabelNode.AttachNode(this);

        BuildTimelines();

        Data->Nodes[0] = LabelNode.NodeId;
        Data->Nodes[1] = UnselectedImageNode.NodeId;
        Data->Nodes[2] = 0;
        Data->Nodes[3] = 0;

        AddEvent(AtkEventType.ButtonClick, ClickHandler);
        AddEvent(AtkEventType.MouseOver, OnMouseOver);
		AddEvent(AtkEventType.MouseOut, OnMouseOut);

        InitializeComponentEvents();
    }

    public Action? Callback { get; set; }

    public ReadOnlySeString String {
        get => LabelNode.String;
        set {
            LabelNode.String = value;
            // Width = LabelNode.Width + LabelNode.Position.X;
        }
    }

    public bool IsChecked {
        get => Component->IsChecked;
        set => Component->SetChecked(value);
    }

    public bool IsSelected {
        get => Component->IsSelected;
        set {
            Component->IsSelected = value;
            UnselectedImageNode.IsVisible = !value;
            SelectedImageNode.IsVisible = value;
        }
    }

    private void ClickHandler(AtkEventListener* thisPtr, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, AtkEventData* atkEventData) {
        Callback?.Invoke();
    }
    private void OnMouseOver() {
        SelectedImageNode.AddColor = (new Vector3(0.531f, 0.531f, 0.531f) * 2) - new Vector3(1f);
        UnselectedImageNode.AddColor = (new Vector3(0.531f, 0.531f, 0.531f) * 2) - new Vector3(1f);
    }
    private void OnMouseOut() {
        SelectedImageNode.AddColor = new Vector3(0);
        UnselectedImageNode.AddColor = new Vector3(0);
    }

    private void BuildTimelines() {
        AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, new Vector2(24, 62))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, new Vector2(24, 44))
            .EndFrameSet()
            .Build()
        );

        CollisionNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 159)
            .AddEmptyFrame(1)
            .EndFrameSet()
            .Build()
        );

        UnselectedImageNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, alpha: 255)
            .AddFrame(1, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, alpha: 255)
            .AddFrame(12, alpha: 255)
            .AddFrame(10, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(12, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, alpha: 255)
            .AddFrame(20, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(30, 39)
            .AddFrame(30, alpha: 102)
            .AddFrame(30, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(80, 80, 80))
            .EndFrameSet()
            .BeginFrameSet(40, 49)
            .AddFrame(40, alpha: 255)
            .AddFrame(40, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(50, 59)
            .AddFrame(50, alpha: 255)
            .AddFrame(52, alpha: 255)
            .AddFrame(50, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(52, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(60, 69)
            .AddFrame(60, alpha: 255)
            .AddFrame(60, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(70, 79)
            .AddFrame(70, alpha: 255)
            .AddFrame(72, alpha: 255)
            .AddFrame(70, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(72, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(80, 89)
            .AddFrame(80, alpha: 255)
            .AddFrame(80, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(90, 99)
            .AddFrame(90, alpha: 102)
            .AddFrame(90, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(80, 80, 80))
            .EndFrameSet()
            .BeginFrameSet(100, 109)
            .AddFrame(100, alpha: 255)
            .AddFrame(100, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(110, 119)
            .AddFrame(110, alpha: 255)
            .AddFrame(112, alpha: 255)
            .AddFrame(110, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(112, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(120, 129)
            .AddFrame(120, alpha: 255)
            .AddFrame(120, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(130, 139)
            .AddFrame(130, alpha: 255)
            .AddFrame(130, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(140, 149)
            .AddFrame(140, alpha: 255)
            .AddFrame(140, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(150, 159)
            .AddFrame(150, alpha: 255)
            .AddFrame(150, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .Build()
        );

        SelectedImageNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(60, 69)
            .AddFrame(60, alpha: 255)
            .AddFrame(60, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(70, 79)
            .AddFrame(70, alpha: 255)
            .AddFrame(72, alpha: 255)
            .AddFrame(70, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(72, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(80, 89)
            .AddFrame(80, alpha: 255)
            .AddFrame(80, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(90, 99)
            .AddFrame(90, alpha: 102)
            .AddFrame(90, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(80, 80, 80))
            .EndFrameSet()
            .BeginFrameSet(100, 109)
            .AddFrame(100, alpha: 255)
            .AddFrame(100, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(110, 119)
            .AddFrame(110, alpha: 255)
            .AddFrame(112, alpha: 255)
            .AddFrame(110, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(112, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(120, 129)
            .AddFrame(120, alpha: 0)
            .AddFrame(122, alpha: 255)
            .AddFrame(120, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(122, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(130, 139)
            .AddFrame(130, alpha: 255)
            .AddFrame(132, alpha: 0)
            .AddFrame(130, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(132, addColor: new Vector3(16, 16, 16), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(140, 149)
            .AddFrame(140, alpha: 0)
            .AddFrame(142, alpha: 255)
            .AddFrame(140, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(142, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(150, 159)
            .AddFrame(150, alpha: 255)
            .AddFrame(152, alpha: 0)
            .AddFrame(150, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .AddFrame(152, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .Build()
        );

        LabelNode.AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 9)
            .AddFrame(1, alpha: 255)
            .AddFrame(1, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(10, 19)
            .AddFrame(10, alpha: 255)
            .AddFrame(10, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(20, 29)
            .AddFrame(20, alpha: 255)
            .AddFrame(20, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(30, 39)
            .AddFrame(30, alpha: 102)
            .AddFrame(30, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(80, 80, 80))
            .EndFrameSet()
            .BeginFrameSet(40, 49)
            .AddFrame(40, alpha: 255)
            .AddFrame(40, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(50, 59)
            .AddFrame(50, alpha: 255)
            .AddFrame(50, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(60, 69)
            .AddFrame(60, alpha: 255)
            .AddFrame(60, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(70, 79)
            .AddFrame(70, alpha: 255)
            .AddFrame(70, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(80, 89)
            .AddFrame(80, alpha: 255)
            .AddFrame(80, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(90, 99)
            .AddFrame(90, alpha: 102)
            .AddFrame(90, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(80, 80, 80))
            .EndFrameSet()
            .BeginFrameSet(100, 109)
            .AddFrame(100, alpha: 255)
            .AddFrame(100, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(110, 119)
            .AddFrame(110, alpha: 255)
            .AddFrame(110, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(120, 129)
            .AddFrame(120, alpha: 255)
            .AddFrame(120, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(130, 139)
            .AddFrame(130, alpha: 255)
            .AddFrame(130, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(140, 149)
            .AddFrame(140, alpha: 255)
            .AddFrame(140, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .BeginFrameSet(150, 159)
            .AddFrame(150, alpha: 255)
            .AddFrame(150, addColor: new Vector3(0, 0, 0), multiplyColor: new Vector3(100, 100, 100))
            .EndFrameSet()
            .Build()
        );
    }
}
