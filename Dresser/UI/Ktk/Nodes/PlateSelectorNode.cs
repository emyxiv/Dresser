using System;
using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Premade.Node.Simple;
using KamiToolKit.Timelines;
using Lumina.Text.ReadOnly;

namespace Dresser.UI.Ktk.Nodes;

public class PlateSelectorNode : SimpleComponentNode {

    private readonly List<PlateRadioNode> radioButtons = [];

    public PlateSelectorNode() {
        BuildTimelines();
    }

    public ReadOnlySeString? SelectedOption {
        get => radioButtons.FirstOrDefault(button => button.IsSelected)?.String;
        set {
            if (value == null)
                return;

            foreach (var radioButton in radioButtons) {
                radioButton.IsChecked = radioButton.String == value;
                radioButton.IsSelected = radioButton.String == value;
            }

            RecalculateLayout();
        }
    }

    public float HorizontalPadding { get; set; } = -9.0f;
    public float VerticalPadding { get; set; } = 1.0f;
    public int MaxRows { get; set; } = 20;

    public void AddButton(ReadOnlySeString label, Action callback) {
        var newRadioButton = new PlateRadioNode {
            Size = new System.Numerics.Vector2(46.0f, 20.0f),
            String = label,
            Callback = callback,
            NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents | NodeFlags.Focusable | NodeFlags.RespondToMouse
        };

        newRadioButton.AddEvent(AtkEventType.ButtonClick, () => ClickHandler(newRadioButton));

        radioButtons.Add(newRadioButton);
        newRadioButton.AttachNode(this);

        if (radioButtons.Count is 1) {
            newRadioButton.IsChecked = true;
            newRadioButton.IsSelected = true;
        }

        RecalculateLayout();
    }

    public void RemoveButton(ReadOnlySeString label) {
        var button = radioButtons.FirstOrDefault(button => button.String == label);
        if (button is null) return;

        button.Dispose();
        radioButtons.Remove(button);
        RecalculateLayout();
    }

    public void Clear() {
        foreach (var node in radioButtons) {
            node.Dispose();
        }

        radioButtons.Clear();
    }

    private void RecalculateLayout() {
        var yPosition = 0.0f;
        var xPosition = 0.0f;

        var xMax = 0.0f;
        var yMax = 0.0f;

        foreach (var index in Enumerable.Range(0, radioButtons.Count)) {
            var button = radioButtons[index];

            button.Y = yPosition;
            button.X = xPosition;
            if(index % MaxRows == MaxRows - 1) {
                yPosition = 0.0f;
                xPosition += button.Width + HorizontalPadding; // move to next column, adjust as needed
            } else {
                yPosition += button.Height + VerticalPadding;
            }

            xMax = Math.Max(xMax, button.X + button.Width);
            yMax = Math.Max(yMax, button.Y + button.Height);
        }

        Height = yMax; // Set height based on last button position
        Width = xMax; // Set width based on last column
    }

    private void ClickHandler(PlateRadioNode selectedButton) {
        foreach (var radioButton in radioButtons) {
            radioButton.IsChecked = false;
            radioButton.IsSelected = false;
        }

        selectedButton.IsChecked = true;
        selectedButton.IsSelected = true;
    }

    private void BuildTimelines() {
        AddTimeline(new TimelineBuilder()
            .BeginFrameSet(1, 19)
            .AddLabel(1, 101, AtkTimelineJumpBehavior.PlayOnce, 0)
            .AddLabel(10, 102, AtkTimelineJumpBehavior.PlayOnce, 0)
            .EndFrameSet()
            .Build()
        );
    }
}
