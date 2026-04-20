using System;
using System.Numerics;

using Dresser.Logic;
using Dresser.Services;

using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;
using KamiToolKit.Premade.Node.Simple;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace Dresser.UI.Ktk.Components {
	/// <summary>
	/// Factory for creating KTK image nodes from UldBundles and icon IDs.
	/// Uses UldPartResolver for UV coordinates and KTK's native texture loading
	/// (with automatic theme resolution).
	/// </summary>
	internal static unsafe class KtkTextureFactory {

		/// <summary>
		/// Create an ImageNode displaying a specific UldBundle texture part.
		/// The node uses KTK's native texture loading with theme resolution.
		/// </summary>
		public static ImageNode? CreateImageNode(UldBundle uldBundle, UldPartResolver resolver, Vector2? size = null) {
			var part = resolver.Resolve(uldBundle);
			if (part == null) {
				PluginLog.Warning($"Failed to resolve UldBundle {uldBundle.Handle} to Part");
				return null;
			}

			var node = new ImageNode {
				Size = size ?? new Vector2(part.Width, part.Height),
				NodeFlags = NodeFlags.Visible | NodeFlags.Enabled,
				WrapMode = WrapMode.Stretch,
			};
			node.AddPart(part);
			node.PartId = 0;
			return node;
		}

		/// <summary>
		/// Create an IconImageNode for a game item icon.
		/// </summary>
		public static IconImageNode CreateIconImageNode(uint iconId, Vector2? size = null) {
			var node = new IconImageNode {
				Size = size ?? new Vector2(40, 40),
				NodeFlags = NodeFlags.Visible | NodeFlags.Enabled,
			};
			if (iconId > 0) node.IconId = iconId;
			return node;
		}

		/// <summary>
		/// Create a full IconNode (with frame, hover border, tooltip support) for an item.
		/// This is the KTK equivalent of the ImGui ItemIcon component.
		/// </summary>
		public static IconNode CreateItemIconNode(uint iconId = 0, Vector2? size = null) {
			var node = new IconNode {
				Size = size ?? new Vector2(44, 48),
				NodeFlags = NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.EmitsEvents,
			};
			if (iconId > 0) node.IconId = iconId;
			return node;
		}

		/// <summary>
		/// Load all frame parts from IconA_Frame.tex into an ImageNode.
		/// Equivalent to IconNodeTextureHelper.LoadIconAFrameTexture() but
		/// resolved from ULD data at runtime instead of hardcoded.
		/// </summary>
		public static ImageNode CreateFrameImageNode(UldPartResolver resolver, Vector2? size = null) {
			// Use the existing KTK helper which has correct hardcoded UV coords for IconA_Frame
			var node = new ImageNode {
				Size = size ?? new Vector2(48, 48),
				NodeFlags = NodeFlags.Visible | NodeFlags.Enabled,
				WrapMode = WrapMode.Tile,
			};
			IconNodeTextureHelper.LoadIconAFrameTexture(node);
			return node;
		}

		/// <summary>
		/// Create an ImageNode for an empty equipment slot placeholder.
		/// Uses the armoury board slot icons (Character.tex or ArmouryBoard.tex).
		/// </summary>
		public static ImageNode? CreateEmptySlotNode(Interop.Agents.GlamourPlateSlot slot, UldPartResolver resolver, Vector2? size = null) {
			if (!PluginServices.ImageGuiCrop.EmptyGlamourPlateSlot.TryGetValue(slot, out var uldBundle))
				return null;
			return CreateImageNode(uldBundle, resolver, size);
		}
	}
}
