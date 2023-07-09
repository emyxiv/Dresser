using Dresser.Extensions;
using Dresser.Services;

using ImGuiNET;

using System.Numerics;

namespace Dresser.Windows.Components {
	internal class Help {

		public static void Open() {
			var dialog = new DialogInfo("Help",
				() => {

				Draw();
				return Dialogs.GenericButtonClose();

			}, (choice) => {
			});

			Plugin.GetInstance().OpenDialog(dialog);
		}


		private static Vector4 DiscordColor = new Vector4(86, 98, 246, 255) / 255;
		public static void Draw() {

			if (ImGui.CollapsingHeader("Introduction & Disclaimer", ImGuiTreeNodeFlags.DefaultOpen)) {
				ConfigurationManager.Config.CollapsibleIntroductionDisclaimer = true;

				ImGui.Text($"Thank you for using Dresser Anywhere");
				ImGui.Spacing();
				ImGui.Text($"This plugin aims to:");
				ImGui.BulletText($"Preview your glam in real conditions, with proper lightings");
				ImGui.BulletText($"Allow the use Glamour Dresser everywhere");
				ImGui.BulletText($"Seamlessly show items stored in other inventories and/or bought easily");
				ImGui.BulletText($"Make you tempted into acquiring extra items");

				ImGui.Spacing();
				ImGui.Text($"This plugin is NOT made to:");
				ImGui.BulletText($"Avoid aquiring items by enjoying seeing them in the preview");
				ImGui.BulletText($"Keep unobtained items appearance permanently");
				ImGui.Spacing();
				ImGui.Text($"This plugin is early alpha stage, expect bugs and missing features.");

				ImGui.AlignTextToFramePadding();
				ImGui.Text($"Suggestions and bug reports are welcome on ");
				ImGui.SameLine();

				ImGui.PushStyleColor(ImGuiCol.Button, DiscordColor);
				if (ImGui.Button("Discord"))
					"https://discord.gg/kXwKDFjXBd".OpenBrowser();
				ImGui.PopStyleColor();

			} else
				ConfigurationManager.Config.CollapsibleIntroductionDisclaimer = false;

			if (ImGui.CollapsingHeader("Starter Tips", ImGuiTreeNodeFlags.DefaultOpen)) {
				ConfigurationManager.Config.CollapsibleStarterTips = true;
				ImGui.Spacing();
				ImGui.Text($"Before starting using the portable plates and gear browser, it is recommended to:");
				ImGui.Indent();
				ImGui.Text($"1. Gather the list of your owned item by opening all your inventories");
				ImGui.Text($"2. Get your current Plates into portable plates by opening the Plate Creation window (in Glamour Dresser)");
				ImGui.Unindent();
			} else
				ConfigurationManager.Config.CollapsibleStarterTips = false;

			if (ImGui.CollapsingHeader("General information", ImGuiTreeNodeFlags.DefaultOpen)) {
				ConfigurationManager.Config.CollapsibleGeneralInformation = true;
				ImGui.Spacing();
				ImGui.Text($"All previews are client sided only, other players won't notice you are glaming");
				ImGui.Text($"The previewed appearance is removed when exiting DresserAnywhere");
				ImGui.Text($"All changes done are saved ");

				ImGui.Spacing();
				ImGui.Text($"Glossary");
				ImGui.Indent();
				ImGui.Text($"Portable Plates: The plates in DresserAnywhere that are used to overwrite vanilla plates");
				ImGui.Unindent();
			} else
				ConfigurationManager.Config.CollapsibleGeneralInformation = false;

			if (ImGui.CollapsingHeader("Other Tips", ImGuiTreeNodeFlags.DefaultOpen)) {
				ConfigurationManager.Config.CollapsibleOtherTips = true;
				ImGui.Spacing();
				ImGui.BulletText($"Icon size can be changed to fit your screen size");
				ImGui.BulletText($"Dye picker size can be changed to fit your screen size");
			} else
				ConfigurationManager.Config.CollapsibleOtherTips = false;

			//if (ImGui.CollapsingHeader("Known issues", ImGuiTreeNodeFlags.DefaultOpen)) {
			//	ConfigurationManager.Config.CollapsibleKnownIssues = true;
			//	ImGui.Spacing();
			//	ImGui.BulletText($"");
			//} else
			//	ConfigurationManager.Config.CollapsibleKnownIssues = false;






		}
	}
}
