using ImGuiNET;

using System.Diagnostics;
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
	}
}
