
using System;
using System.Linq;
using System.Numerics;

using Dresser.Logic;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace Dresser.UI.Ktk.Nodes.CurrentGearNodes;

public unsafe class CurrentGearContentsNode : HorizontalFlexNode {
    public SlotsGridNode SlotsGrid = null!;
    public PlateSelectorNode PlateSelector = null!;
    public PreviewNode PreviewNode = null!;
    private readonly float Padding = 10.0f;
    private readonly Vector2 MarginX = new(5, 5);
    public CurrentGearContentsNode(AtkUnitBase* parentAddon) {

        // plate selector
        PlateSelector = new PlateSelectorNode() {
            // Position = new Vector2(0, 0),
            // Size = new Vector2(200, 30),
            MaxRows = ConfigurationManager.Config.NumberofPendingPlateNextColumn,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
        };
        this.AddNode(PlateSelector);

        // item slots
        SlotsGrid = new SlotsGridNode {
            // Position = new Vector2(PlateSelector.Size.X + Padding, 0),
            Size = new Vector2(48, 48) * new Vector2(2, 6),
            GridSize = new GridSize(2, 6),
            Scale = new Vector2(SlotsGridNode.SlotScale),
        };
        SlotsGrid.Setup();
        this.AddNode(SlotsGrid);
        PluginLog.Debug($"created SlotsGrid: {SlotsGrid.GridSize} {SlotsGrid.GridSize}");

        // preview
        PreviewNode = new PreviewNode(parentAddon) {
            // Position = new Vector2(10.0f, 10.0f),
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
        };
        this.AddNode(PreviewNode);


        // adjust size
        Width = NodeList.Sum(node => node.Width * node.ScaleX) + (NodeList.Count * ItemSpacing);

        RecalculateLayout();
    }

    public void OnUpdate() {
        SlotsGrid.OnUpdate();
    }

    /// <inheritdoc />
    protected override void OnRecalculateLayout() {
        // var step = Width / NodeList.Count;

        if (NodeList.Count != 0 && AlignmentFlags.HasFlag(FlexFlags.FitContentHeight)) {
            Height = NodeList.Max(node => node.Height * node.ScaleY);
        }

        foreach (var index in Enumerable.Range(0, NodeList.Count)) {

            // if (AlignmentFlags.HasFlag(FlexFlags.CenterHorizontally)) {
            //     NodeList[index].X = step * index + step / 2.0f - NodeList[index].Width / 2.0f;
            // }
            // else {
            //     NodeList[index].X = step * index;
            // }
            var previousNodes = NodeList.Take(index);
            NodeList[index].X = previousNodes.Sum(node => node.Width * node.ScaleX) + (previousNodes.Count() * ItemSpacing);

            if (AlignmentFlags.HasFlag(FlexFlags.FitHeight)) {
                NodeList[index].Height = Height;
            }

            if (AlignmentFlags.HasFlag(FlexFlags.CenterVertically)) {
                NodeList[index].Y = Height / 2 - NodeList[index].Height / 2;
            }

            // if (AlignmentFlags.HasFlag(FlexFlags.FitWidth)) {
            //     NodeList[index].Width = step - ItemSpacing;
            // }
        }
    }
}
