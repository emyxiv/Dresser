using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Data;
using Dalamud.Interface.Internal;
using Dalamud.Utility;
using Dalamud.Interface;

using Lumina.Data.Files;
using Lumina.Data.Parsing.Uld;
using Dalamud.Logging;

namespace Dresser.Logic {
	//
	// Summary:
	//     Wrapper for multi-icon sprite sheets defined by ULD files.
	public class UldWrapper : IDisposable {

		private readonly Dictionary<string, (uint Id, int Width, int Height, bool HD, byte[] RgbaData)> textures = new Dictionary<string, (uint, int, int, bool, byte[])>();

		//
		// Summary:
		//     Gets the loaded ULD file if it exists.
		public UldFile? Uld { get; private set; }

		//
		// Summary:
		//     Gets a value indicating whether the requested ULD could be loaded.
		public bool Valid => Uld != null;

		//
		// Summary:
		//     Initializes a new instance of the Dalamud.Interface.UldWrapper class, wrapping
		//     an ULD file.
		//
		// Parameters:
		//   uiBuilder:
		//     The UiBuilder used to load textures.
		//
		//   uldPath:
		//     The requested ULD file.
		internal UldWrapper(string uldPath) {
			Uld = PluginServices.DataManager.GetFile<UldFile>(uldPath);
		}

		//
		// Summary:
		//     Load a part of a multi-icon sheet as a texture.
		//
		// Parameters:
		//   texturePath:
		//     The path of the requested texture.
		//
		//   part:
		//     The index of the desired icon.
		//
		// Returns:
		//     A TextureWrap containing the requested part if it exists and null otherwise.
		public IDalamudTextureWrap? LoadTexturePart(string texturePath, int part) {
			//PluginLog.Debug("LoadTexturePart 1");

			if (!Valid) {
				return null;
			}
			//PluginLog.Debug("LoadTexturePart 2");

			if (!textures.TryGetValue(texturePath, out var value)) {
				(uint, int, int, bool, byte[])? texture = GetTexture(texturePath);
				//PluginLog.Debug($"LoadTexturePart 3: {(texture == null ? "NULL": "valid")}");
				if (!texture.HasValue) {
					//PluginLog.Debug("LoadTexturePart 4");
					return null;
				}

				value = texture.Value;
				textures[texturePath] = value;
			}
			//PluginLog.Debug("LoadTexturePart 5");

			return CreateTexture(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, part);
		}

		//
		// Summary:
		//     Clear all stored data and remove the loaded ULD.
		public void Dispose() {
			textures.Clear();
			Uld = null;
		}

		private IDalamudTextureWrap? CreateTexture(uint id, int width, int height, bool hd, byte[] rgbaData, int partIdx) {
			int num = 0;
			UldRoot.PartData? partData = null;
			foreach (UldRoot.PartData item in Uld!.Parts.SelectMany((UldRoot.PartsData p) => p.Parts)) {
				if (item.TextureId == id && num++ == partIdx) {
					partData = item;
					break;
				}
			}

			//PluginLog.Debug("CreateTexture 1");
			if (!partData.HasValue) {
				return null;
			}
			//PluginLog.Debug($"CreateTexture 2");

			UldRoot.PartData partData2;
			if (!hd) {
				partData2 = partData.Value;
			} else {
				UldRoot.PartData value = partData.Value;
				value.H = (ushort)(partData.Value.H * 2);
				value.W = (ushort)(partData.Value.W * 2);
				value.U = (ushort)(partData.Value.U * 2);
				value.V = (ushort)(partData.Value.V * 2);
				partData2 = value;
			}
			//PluginLog.Debug("CreateTexture 3");

			UldRoot.PartData part = partData2;
			return CopyRect(width, height, hd, rgbaData, part);
		}

		private IDalamudTextureWrap? CopyRect(int width, int height, bool hd, byte[] rgbaData, UldRoot.PartData part) {

			//if (part.V + part.W > width*2 || part.U + part.H > height*2) {
				PluginLog.Debug($"CopyRect 0: {part.V} + {part.W} > {width} || {part.U} + {part.H} > {height}");
				//return null;
			//}
			//PluginLog.Debug("CopyRect 1");

			byte[] array = new byte[part.W * part.H * 4];
			for (int i = 0; i < part.H; i++) {
				Span<byte> span = rgbaData.AsSpan().Slice(((part.V + i) * width + part.U) * 4, part.W * 4);
				Span<byte> destination = array.AsSpan(i * part.W * 4);
				span.CopyTo(destination);
			}
			//PluginLog.Debug("CopyRect 2");

			return PluginServices.PluginInterface.UiBuilder.LoadImageRaw(array, part.W, part.H, 4);
		}

		private (uint Id, int Width, int Height, bool HD, byte[] RgbaData)? GetTexture(string texturePath) {
			//PluginLog.Debug("GetTexture 1");
			if (!Valid) {
				return null;
			}

			//PluginLog.Debug("GetTexture 2");
			texturePath = texturePath.Replace("_hr1", string.Empty);
			var texturePathSave = texturePath;
			//texturePath = texturePath.Replace("/", string.Empty); // fix alt themes
			texturePath = texturePath.Replace("fourth/", string.Empty); // fix alt themes
			uint num = uint.MaxValue;
			UldRoot.TextureEntry[] assetData = Uld!.AssetData;
			for (int i = 0; i < assetData.Length; i++) {
				UldRoot.TextureEntry textureEntry = assetData[i];
				int length = Math.Min(textureEntry.Path.Length, texturePath.AsSpan().Length);
				if (textureEntry.Path.AsSpan().Slice(0, length).SequenceEqual(texturePath.AsSpan().Slice(0, length))) {
					num = textureEntry.Id;
					break;
				}
			}
			//PluginLog.Debug($"GetTexture 3: textureEntryId {num}");

			if (num == uint.MaxValue) {
				return null;
			}

			//PluginLog.Debug("GetTexture 4");
			texturePath = texturePathSave;

			string path = texturePath.Replace(".tex", "_hr1.tex");
			bool item = true;
			TexFile? file = PluginServices.DataManager.GetFile<TexFile>(path);
			if (file == null) {
				item = false;
				file = PluginServices.DataManager.GetFile<TexFile>(texturePath);
				//PluginLog.Debug("GetTexture 5");
				if (file == null) {
					return null;
				}
			}
			//PluginLog.Debug($"GetTexture 6: {path} {num}");

			return new (uint, int, int, bool, byte[])?((num, file.Header.Width, file.Header.Height, item, file.GetRgbaImageData()));
		}
	}
}