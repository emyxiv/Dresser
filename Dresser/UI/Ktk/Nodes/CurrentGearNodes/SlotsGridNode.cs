
using System;
using System.Collections.Generic;
using System.Numerics;

using Dresser.Gui;
using Dresser.Interop.Agents;
using Dresser.Logic;
using Dresser.Models.ViewModels;
using Dresser.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace Dresser.UI.Ktk.Nodes.CurrentGearNodes;

public class SlotsGridNode : GridNode {
    public static float SlotScale => 1.5f 
    // * ConfigurationManager.Config.IconSizeMult
    ;
    private readonly UldPartResolver _resolver;
    private readonly Dictionary<GlamourPlateSlot, KtkItemSlot> _slots = new();
    public static readonly List<GlamourPlateSlot> SlotOrder = new() {
        GlamourPlateSlot.MainHand, GlamourPlateSlot.OffHand,
        GlamourPlateSlot.Head, GlamourPlateSlot.Ears,
        GlamourPlateSlot.Body, GlamourPlateSlot.Neck,
        GlamourPlateSlot.Hands, GlamourPlateSlot.Wrists,
        GlamourPlateSlot.Legs, GlamourPlateSlot.RightRing,
        GlamourPlateSlot.Feet, GlamourPlateSlot.LeftRing,
    };
    private static int SlotToGridIndex(GlamourPlateSlot slot) {
        var index = SlotOrder.IndexOf(slot);
        if(index < 0) {
            throw new Exception("failed to resolve slot index");
        }
        return index;
    }
    public SlotsGridNode() {
        _resolver = PluginServices.UldPartResolver;
    }
    public void Setup() {


        // _resolver = PluginServices.UldPartResolver;
        PluginLog.Debug($"KtkCurrentGear.BuildSlotGrid: creating {SlotOrder.Count} slots");
        PluginLog.Debug($"GridSize {GridSize} {Scale}");

        foreach (var slot in SlotOrder) {
            var slotNode = new KtkItemSlot(slot, _resolver) {
                OnSlotClicked = OnSlotClicked,
                OnSlotMiddleClicked = OnSlotMiddleClicked,
                OnSlotHovered = OnSlotHovered,
                OnSlotUnhovered = OnSlotUnhovered
            };
            slotNode.StainNodes.ForEach(s => s.OnSlotClicked = OnStainClicked);
            _slots[slot] = slotNode;
            slotNode.AttachNode(this[SlotToGridIndex(slot)]);
        }

        // this.RecalculateLayout();
        PluginLog.Debug("KtkCurrentGear.BuildSlotGrid: complete");
    }
    public void OnUpdate() {
        RefreshSlots();
    }
    public void RefreshSlots() {
        var selectedSlot = ConfigurationManager.Config.CurrentGearSelectedSlot;

        foreach (var (slot, slotNode) in _slots) {
            var item = PluginServices.ApplyGearChange.GetCurrentPlateItem(slot);
            var renderData = ItemRenderData.From(item, slot);
            slotNode.Update(renderData);
            slotNode.SetSelected(slot == selectedSlot);
        }
    }
    protected override void Dispose(bool isNativeDestructor) {
        if (IsDisposed) return;

        _resolver.Dispose();
        _slots.Clear();

        base.Dispose(isNativeDestructor);
    }




    private void OnSlotClicked(GlamourPlateSlot slot) {
        try {
            PluginServices.ApplyGearChange.ExecuteCurrentItem(slot);
        } catch (Exception e) {
            PluginLog.Error(e, $"Error handling slot click for {slot}");
        }
    }

    private void OnSlotMiddleClicked(GlamourPlateSlot slot) {
        try {
            var item = PluginServices.ApplyGearChange.GetCurrentPlateItem(slot);
            if (item != null)
                PluginServices.ApplyGearChange.ExecuteCurrentContextRemoveItem(item, slot);
        } catch (Exception e) {
            PluginLog.Error(e, $"Error handling slot middle click for {slot}");
        }
    }

    private static void OnSlotHovered(GlamourPlateSlot slot) {
        // Could highlight the slot in the browser
    }

    private static void OnSlotUnhovered(GlamourPlateSlot slot) {
        // Clear hover state
    }
    private static void OnStainClicked(GlamourPlateSlot slot, Lumina.Excel.Sheets.Stain? stain, ushort stainIndex) {
        try {
            PluginServices.ApplyGearChange.ExecuteCurrentItem(slot);
            DyePicker.DyeIndex = (ushort)(stainIndex+1);
            Plugin.GetInstance().GearBrowser.SwitchToDyesMode();
        } catch (Exception e) {
            PluginLog.Error(e, $"Error handling stain click for {slot} stain {stainIndex}");
        }

    }



}
