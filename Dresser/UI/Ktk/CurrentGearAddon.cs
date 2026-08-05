using System;
using System.Numerics;

using Dresser.Logic;
using Dresser.Services;
using Dresser.UI.Ktk.Nodes;
using Dresser.UI.Ktk.Nodes.CurrentGearNodes;

using FFXIVClientStructs.FFXIV.Component.GUI;

using KamiToolKit.BaseTypes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.Simplified;

namespace Dresser.UI.Ktk {
	/// <summary>
	/// KamiToolKit NativeAddon implementation of the CurrentGear window.
	/// Displays 12 equipment slots in a 2x6 grid with action buttons.
	/// Uses shared ViewModels (ItemRenderData) for data, KTK native nodes for rendering.
	/// Auto-falls-back to ImGui on unhandled exceptions.
	/// </summary>
	internal sealed unsafe class CurrentGearAddon : NativeAddon, IDisposable {

		private CurrentGearContentsNode _mainContainer = null!;
		private ButtonsNode _bottomButtonContainer = null!;
		private bool _hasCrashed;

		/// <summary>
		/// Called by Plugin.cs when a KTK crash occurs and we need to fall back.
		/// </summary>
		public Action? OnCrashFallback;

		public CurrentGearAddon() : base() {
			PluginLog.Debug("KtkCurrentGear: constructor called");
		}

		// find the window node with the right class
		private MiragePlateWindowNode? WindowMirage
			=> (this.WindowNode is MiragePlateWindowNode) ? this.WindowNode as MiragePlateWindowNode : null;


		protected override void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan) {
			PluginLog.Debug($"KtkCurrentGear.OnSetup: called (InternalAddon=0x{(nint)addon:X})");
			try {
				var mainPosition = WindowMirage?.ContentStartPosition + new Vector2(10f, 10f) ?? new Vector2(25f);

				_mainContainer = new CurrentGearContentsNode(this.InternalAddon) {
					AlignmentFlags = FlexFlags.FitContentHeight | FlexFlags.CenterHorizontally | FlexFlags.CenterVertically,
					Position = mainPosition,
					ItemSpacing = 5f,
				};
				_mainContainer.AttachNode(this);

				// buttons
				_bottomButtonContainer = new ButtonsNode {
					AlignmentFlags = FlexFlags.FitContentHeight | FlexFlags.CenterHorizontally | FlexFlags.CenterVertically,
					Width = _mainContainer.Width,
					Position = mainPosition + new Vector2(0f, _mainContainer.Height),
					PreviewNode = _mainContainer.PreviewNode,
				};
				_bottomButtonContainer.AttachNode(this);

				PluginLog.Debug("KtkCurrentGear.OnSetup: container attached, building slot grid");
				WindowMirage?.Width = _mainContainer.Width + (2 * 24f);
				WindowMirage?.Height = WindowMirage.HeaderHeight + (3 * 15f)
					+ _mainContainer.Height
					+ _bottomButtonContainer.Height;


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

		protected override void OnUpdate(AtkUnitBase* addon) {
			if (_hasCrashed) return;
			try {
				_mainContainer.OnUpdate();
			} catch (Exception e) {
				PluginLog.Error(e, "KtkCurrentGear.OnUpdate crashed");
				HandleCrash();
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


	}
}
