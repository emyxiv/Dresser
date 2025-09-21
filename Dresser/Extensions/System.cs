﻿using Dalamud.Bindings.ImGui;

using Penumbra.String.Functions;

using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Dresser.Extensions {
	internal static class System {

		private static Regex AddSpaceBeforeCapitalRegex = new(@"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", RegexOptions.Compiled);
		public static string AddSpaceBeforeCapital(this string str)
			=> AddSpaceBeforeCapitalRegex.Replace(str, " $0");

		public static void OpenBrowser(this string url) {
			Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
		}
		public static void ToClipboard(this string text) {
			ImGui.SetClipboardText(text);
		}








		/// <summary> Compress a byte array with a prepended version. </summary>
		public static unsafe byte[] Compress(this byte[] data, byte version) {
			using var compressedStream = new MemoryStream();
			using var zipStream = new GZipStream(compressedStream, CompressionMode.Compress);
			zipStream.Write(data, 0, data.Length);
			zipStream.Flush();

			var ret = new byte[compressedStream.Length + 1];
			ret[0] = version;
			fixed (byte* ptr1 = compressedStream.GetBuffer(), ptr2 = ret) {
				MemoryUtility.MemCpyUnchecked(ptr2 + 1, ptr1, (int)compressedStream.Length);
			}

			return ret;
		}

		/// <summary> Compress a string with a prepended version. </summary>
		public static byte[] Compress(this string data, byte version) {
			var bytes = Encoding.UTF8.GetBytes(data);
			return bytes.Compress(version);
		}

		/// <summary> Decompress a byte array into a returned version byte and an array of the remaining bytes. </summary>
		public static byte Decompress(this byte[] compressed, out byte[] decompressed) {
			var ret = compressed[0];
			using var compressedStream = new MemoryStream(compressed, 1, compressed.Length - 1);
			using var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
			using var resultStream = new MemoryStream();
			zipStream.CopyTo(resultStream);
			decompressed = resultStream.ToArray();
			return ret;
		}

		/// <summary> Decompress a byte array into a returned version byte and a string of the remaining bytes as UTF8. </summary>
		public static byte DecompressToString(this byte[] compressed, out string decompressed) {
			var ret = compressed.Decompress(out var bytes);
			decompressed = Encoding.UTF8.GetString(bytes);
			return ret;
		}
	}
}
