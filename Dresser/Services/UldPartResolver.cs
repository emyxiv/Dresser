using System;
using System.Collections.Generic;
using System.Numerics;

using Dalamud.Interface;

using Dresser.Logic;

using KamiToolKit.Classes;

namespace Dresser.Services {
	/// <summary>
	/// Resolves UldBundle definitions into KTK Part objects with correct UV coordinates.
	/// Uses Dalamud's UldWrapper to read part data from ULD files, then produces
	/// KTK-compatible Part objects. KTK's LoadTexture handles theme resolution automatically.
	/// </summary>
	internal class UldPartResolver : IDisposable {

		// Cache: uldPath → { texPath → Part[] }
		private readonly Dictionary<string, Dictionary<string, Part[]>> _cache = new();
		private readonly Dictionary<string, UldWrapper> _loadedUlds = new();

		/// <summary>
		/// Resolve a UldBundle into a KTK Part with correct UV coordinates.
		/// Returns null if the ULD/tex/index combination cannot be resolved.
		/// </summary>
		public Part? Resolve(UldBundle uldBundle) {
			try {
				var parts = GetPartsForTexture(uldBundle.Uld, uldBundle.Tex);
				if (parts != null && uldBundle.Index < parts.Length) {
					return parts[uldBundle.Index];
				}
			} catch (Exception e) {
				PluginLog.Error(e, $"Failed to resolve UldBundle {uldBundle.Handle}");
			}
			return null;
		}

		/// <summary>
		/// Resolve all parts for a given ULD + texture combination.
		/// </summary>
		private Part[]? GetPartsForTexture(string uldPath, string texPath) {
			// Check cache
			if (_cache.TryGetValue(uldPath, out var texCache) && texCache.TryGetValue(texPath, out var cachedParts)) {
				return cachedParts;
			}

			// Load ULD
			var uld = GetOrLoadUld(uldPath);
			if (uld?.Uld == null) return null;

			// Find the asset index matching the tex path
			var assetIndex = FindAssetIndex(uld, texPath);
			if (assetIndex < 0) return null;

			// Read part data for this texture from the ULD
			var parts = ReadPartsFromUld(uld, assetIndex, texPath);
			if (parts == null) return null;

			// Cache
			if (!_cache.ContainsKey(uldPath))
				_cache[uldPath] = new();
			_cache[uldPath][texPath] = parts;

			return parts;
		}

		private UldWrapper? GetOrLoadUld(string uldPath) {
			if (_loadedUlds.TryGetValue(uldPath, out var existing))
				return existing;

			// Try from ImageGuiCrop first (already loaded)
			if (PluginServices.ImageGuiCrop.Ulds.TryGetValue(uldPath, out var fromCache)) {
				_loadedUlds[uldPath] = fromCache;
				return fromCache;
			}

			// Load fresh
			if (!PluginServices.DataManager.FileExists(uldPath)) return null;
			var uld = PluginServices.PluginInterface.UiBuilder.LoadUld(uldPath);
			if (uld == null || !uld.Valid) return null;

			_loadedUlds[uldPath] = uld;
			return uld;
		}

		private static int FindAssetIndex(UldWrapper uld, string texPath) {
			if (uld.Uld == null) return -1;

			// Normalize: strip _hr1 for comparison (ULD stores base paths)
			var normalizedTex = texPath.Replace("_hr1", string.Empty);

			var assets = uld.Uld.AssetData;
			if (assets == null) return -1;

			for (int i = 0; i < assets.Length; i++) {
				var assetPath = new string(assets[i].Path).TrimEnd('\0');
				if (assetPath.Equals(normalizedTex, StringComparison.OrdinalIgnoreCase)) {
					return i;
				}
			}

			// Also try with just the filename
			var texFileName = System.IO.Path.GetFileName(normalizedTex);
			for (int i = 0; i < assets.Length; i++) {
				var assetPath = new string(assets[i].Path).TrimEnd('\0');
				var assetFileName = System.IO.Path.GetFileName(assetPath);
				if (assetFileName.Equals(texFileName, StringComparison.OrdinalIgnoreCase)) {
					return i;
				}
			}

			return -1;
		}

		private static Part[]? ReadPartsFromUld(UldWrapper uld, int assetIndex, string texPath) {
			if (uld.Uld == null) return null;

			// Strip _hr1 from tex path for KTK (KTK's LoadTexture strips it internally)
			var ktkTexPath = texPath.Replace("_hr1", string.Empty);

			try {
				// Access the ULD's part data
				// UldFile.Parts contains part lists, each with entries having TextureId, U, V, W, H
				var partsData = uld.Uld.Parts;
				if (partsData == null) return null;

				// Find the parts list that matches our asset
				// The asset's Id (1-based) corresponds to TextureId in parts entries
				var assetId = uld.Uld.AssetData[assetIndex].Id;

				var result = new List<Part>();
				foreach (var partsList in partsData) {
					if (partsList.Parts == null) continue;
					foreach (var entry in partsList.Parts) {
						if (entry.TextureId == assetId) {
							result.Add(new Part {
								Id = (uint)result.Count,
								TexturePath = ktkTexPath,
								U = entry.U,
								V = entry.V,
								Width = entry.W,
								Height = entry.H,
							});
						}
					}
				}

				return result.Count > 0 ? result.ToArray() : null;
			} catch (Exception e) {
				PluginLog.Error(e, $"Failed to read parts from ULD for asset index {assetIndex}");
				return null;
			}
		}

		public void Dispose() {
			// Don't dispose ULDs from ImageGuiCrop's cache
			foreach (var (path, uld) in _loadedUlds) {
				if (!PluginServices.ImageGuiCrop.Ulds.ContainsKey(path)) {
					uld.Dispose();
				}
			}
			_loadedUlds.Clear();
			_cache.Clear();
		}
	}
}
