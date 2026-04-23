using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface.Windowing;

using Dresser.Gui;

using Dresser.Interop.Agents;
using Dresser.Logic;
using Dresser.Models;
using Dresser.Models.ViewModels;
using Dresser.Services;
using Dresser.UI.Ktk.Nodes;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node;
using KamiToolKit.Premade.Node.Simple;
using KamiToolKit.Timelines;

namespace Dresser.UI.Ktk {
	/// <summary>
	/// KamiToolKit NativeAddon implementation of the CurrentGear window.
	/// Displays 12 equipment slots in a 2x6 grid with action buttons.
	/// Uses shared ViewModels (ItemRenderData) for data, KTK native nodes for rendering.
	/// Auto-falls-back to ImGui on unhandled exceptions.
	/// </summary>
	internal sealed unsafe class KtkCurrentGear : NativeAddon, IDisposable {

		private readonly UldPartResolver _resolver;
		private readonly Dictionary<GlamourPlateSlot, KtkItemSlot> _slots = new();
		private SimpleComponentNode _mainContainer = null!;
		private GridNode _slotsGrid = null!;
		private bool _hasCrashed;

		/// <summary>
		/// Called by Plugin.cs when a KTK crash occurs and we need to fall back.
		/// </summary>
		public Action? OnCrashFallback;

		public KtkCurrentGear() : base() {
			PluginLog.Debug("KtkCurrentGear: constructor called");
			_resolver = PluginServices.UldPartResolver;
		}

		public static readonly List<GlamourPlateSlot> SlotOrder = new() {
			GlamourPlateSlot.MainHand, GlamourPlateSlot.OffHand,
			GlamourPlateSlot.Head, GlamourPlateSlot.Ears,
			GlamourPlateSlot.Body, GlamourPlateSlot.Neck,
			GlamourPlateSlot.Hands, GlamourPlateSlot.Wrists,
			GlamourPlateSlot.Legs, GlamourPlateSlot.RightRing,
			GlamourPlateSlot.Feet, GlamourPlateSlot.LeftRing,
		};

		private Vector2 MarginX = new(20, 0);
		protected override void OnSetup(AtkUnitBase* addon) {
			PluginLog.Debug($"KtkCurrentGear.OnSetup: called (InternalAddon=0x{(nint)addon:X})");
			try {
				PluginLog.Debug($"KtkCurrentGear.OnSetup: ContentStartPosition={ContentStartPosition} ContentSize={ContentSize}");
				_mainContainer = new SimpleComponentNode {
					Position = ContentStartPosition + MarginX,
					Size = ContentSize,
				};
				_mainContainer.AttachNode(this);
				PluginLog.Debug("KtkCurrentGear.OnSetup: container attached, building slot grid");

				BuildSlotGrid();
				BuildBottomButtons();
				RecalculateSize();

				PluginLog.Debug("KtkCurrentGear.OnSetup: complete");
			} catch (Exception e) {
				PluginLog.Error(e, "KtkCurrentGear.OnSetup crashed");
				HandleCrash();
			}
		}



        public static Func<WindowNodeBase>? CreateWindowNodeFunc => () => {
			
			// var size = new Vector2(220, 420);
			var window = new MiragePlateWindowNode ();
			// window.Size = size;

			// window.ConfigurationButtonNode.IsVisible = true;

			// var part = PluginServices.UldPartResolver.Resolve(UldBundle.MiragePrismMiragePlate_Frame);
			// if (part == null) {
			// 	PluginLog.Warning("Failed to resolve MiragePrismMiragePlate_Frame for window background");
			// } else {
			// 	// window.BackgroundImageNode.DetachNode();
			// 	// window.BackgroundNode.DetachNode();
			// 	// window.BorderNode.DetachNode();

			// 	// var nineGridNode = new NineGridNode {
			// 	// 	// Size = window.ContentSize,
			// 	// 	Parts = [part],
			// 	// 	Offsets = new Vector4(20, 260, 80, 80),
			// 	// 	NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.Fill | NodeFlags.EmitsEvents,
			// 	// 	NodeId = 11, // replace BackgroundNode
			// 	// };
			// 	// nineGridNode.AddTimeline(new TimelineBuilder()
			// 	// 	.AddFrameSetWithFrame(1, 9, 1, addColor: new Vector3(0.0f), multiplyColor: new Vector3(80.0f))
			// 	// 	.AddFrameSetWithFrame(10, 19, 10, addColor: new Vector3(0.0f), multiplyColor: new Vector3(100.0f))
			// 	// 	.AddFrameSetWithFrame(20, 29, 20, addColor: new Vector3(0.0f), multiplyColor: new Vector3(80.0f))
			// 	// 	.Build());

			// 	// nineGridNode.AttachNode(window.HeaderCollisionNode, NodePosition.AfterTarget);

			// 	// window.DividingLineNode.Alpha = 127;
			// 	// window.DividingLineNode.TexturePath = "ui/uld/WindowA_Line.tex";
			// 	// window.DividingLineNode.Height = 28;
			// 	// window.DividingLineNode.Color = 
			// 	// window.TitleNode.TextColor = new Vector4(new Vector3(0.932f), 1.0f);
			// 	// window.TitleNode.TextOutlineColor = new Vector4(new Vector3(0.502f), 1.0f);
			// 	// window.TitleNode. NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.Visible | NodeFlags.AnchorLeft | NodeFlags.EmitsEvents;
			// }

			return window;
		};




		private void BuildSlotGrid() {
			PluginLog.Debug($"KtkCurrentGear.BuildSlotGrid: creating {SlotOrder.Count} slots");
			_slotsGrid = new GridNode {
				Position = new Vector2(0, 0),
				Size = new Vector2(48, 48) * new Vector2(2, 6),
				GridSize = new GridSize(2, 6),
				Scale = new Vector2(SlotScale),
			};
			_slotsGrid.AttachNode(_mainContainer);

			int col = 0;
			int row = 0;
			foreach (var slot in SlotOrder) {
				var slotNode = new KtkItemSlot(slot, _resolver);
				slotNode.OnSlotClicked = OnSlotClicked;
				slotNode.OnSlotMiddleClicked = OnSlotMiddleClicked;
				slotNode.OnSlotHovered = OnSlotHovered;
				slotNode.OnSlotUnhovered = OnSlotUnhovered;
				slotNode.StainNodes.ForEach(s => s.OnSlotClicked = OnStainClicked);

				slotNode.AttachNode(_slotsGrid[col, row]);
				_slots[slot] = slotNode;
				PluginLog.Debug($"KtkCurrentGear.BuildSlotGrid: attached slot {slot} at [{col},{row}]");

				// 2-column layout: increment column, wrap to next row
				col++;
				if (col >= 2) {
					col = 0;
					row++;
				}
			}
			PluginLog.Debug("KtkCurrentGear.BuildSlotGrid: complete");
		}
		private void BuildBottomButtons() {
			var buttonContainer = new SimpleComponentNode {
				Position = new Vector2(0, _slotsGrid.Size.Y * SlotScale + 10), // Below slots with some padding
				Size = new Vector2(200, 30),
			};
			buttonContainer.AttachNode(_mainContainer);


			var toggleWeapon = new ImageToggleNode(UldBundle.CircleSmallWeapon) {
				Size = new Vector2(28.0f, 28.0f),
				Position = new Vector2(0.0f, 0.0f),
				NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
				TextTooltip = "Hide/Display main and offhand weapons.",
			};
			toggleWeapon.AttachNode(buttonContainer);
			var toggleHeadgear = new ImageToggleNode(UldBundle.CircleSmallHat) {
				Size = new Vector2(28.0f, 28.0f),
				Position = new Vector2(28.0f, 0.0f),
				NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
				TextTooltip = "Hide/Display headgear.",
			};
			toggleHeadgear.AttachNode(buttonContainer);
			var toggleVisor = new ImageToggleNode(UldBundle.CircleSmallVisor) {
				Size = new Vector2(28.0f, 28.0f),
				Position = new Vector2(56.0f, 0.0f),
				NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
				TextTooltip = "Manually adjust visor.",
			};
			toggleVisor.AttachNode(buttonContainer);
        }


        private void RecalculateSize() {

			var innerSize = _slotsGrid.Size * SlotScale;

			_mainContainer.CollisionNode.Size = innerSize;
			_mainContainer.Size = innerSize;
			
			
// 
			var newSize = (_slotsGrid.Size * SlotScale) // slots grid size
				+ (MarginX * 2)
				// + (ContentPadding * 2.0f + new Vector2(0, 4))
				// + ContentStartPosition // + title bar height and top padding
				+ new Vector2(0, this.WindowNode?.HeaderHeight ?? 0) // + extra height for title bar and padding, since ContentStartPosition doesn't seem to be working correctly for some reason
				+ new Vector2(0, 65) // button height + padding
				;
			SetWindowSize(newSize);
			
			// .GetCollisionNodeById(1).Size = newSize; // Background node
        }
        protected override void OnUpdate(AtkUnitBase* addon) {
			if (_hasCrashed) return;
			try {
				RefreshSlots();
			} catch (Exception e) {
				PluginLog.Error(e, "KtkCurrentGear.OnUpdate crashed");
				HandleCrash();
			}
		}
		private void RefreshSlots() {
			var selectedSlot = ConfigurationManager.Config.CurrentGearSelectedSlot;

			foreach (var (slot, slotNode) in _slots) {
				var item = PluginServices.ApplyGearChange.GetCurrentPlateItem(slot);
				var renderData = ItemRenderData.From(item, slot);
				slotNode.Update(renderData);
				slotNode.SetSelected(slot == selectedSlot);
			}
		}

		// --- Event Handlers ---

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



		// --- Crash Recovery ---

		private void HandleCrash() {
			_hasCrashed = true;
			PluginLog.Error("KtkCurrentGear crashed — falling back to ImGui");
			try {
				Close();
			} catch {
				// Best effort close
			}
			OnCrashFallback?.Invoke();
		}

		private static float SlotScale => 1.5f 
		// * ConfigurationManager.Config.IconSizeMult
		;

		public new void Dispose() {
			_resolver.Dispose();
			_slots.Clear();
			base.Dispose();
		}
	}
}
