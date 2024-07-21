using Dalamud.Interface.Windowing;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Windows.Components;

using ImGuiNET;

using Lumina.Excel.GeneratedSheets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Dresser.Windows;

public class DyePicker :  Window, IDisposable {

	Plugin Plugin;
	public DyePicker(Plugin plugin) : base(
		"Dye Picker",
		ImGuiWindowFlags.AlwaysAutoResize
		| ImGuiWindowFlags.NoScrollbar) {
		this.RespectCloseHotkey = true;
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(10),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
		};
		Plugin = plugin;
	}

	public void Dispose() { }

	public override void Draw() {
		DrawLogic();
		if(ImGui.IsKeyPressed(ImGuiKey.Escape)) this.IsOpen = false;
	}

	public bool MustDraw = false;
	public override bool DrawConditions() {
		return PluginServices.Context.IsCurrentGearWindowOpen;
	}

	public override void OnClose() {
		MustDraw = false;
		this.IsOpen = false;
		base.OnClose();
	}


	//  Method
	// Constants
	private const ImGuiKey KeyBindBrowseUp = ImGuiKey.UpArrow;
	private const ImGuiKey KeyBindBrowseDown = ImGuiKey.DownArrow;
	private const ImGuiKey KeyBindBrowseLeft = ImGuiKey.LeftArrow;
	private const ImGuiKey KeyBindBrowseRight = ImGuiKey.RightArrow;
	private const ImGuiKey KeyBindBrowseUpFast = ImGuiKey.PageUp;
	private const ImGuiKey KeyBindBrowseDownFast = ImGuiKey.PageDown;
	private const int FastScrollLineJump = 8; // number of lines on the screen?

	// Properties
	private static bool SearchBarValidated = false;
	private static int LastSelectedItemKey = 0;
	private static int Columns = 12;
	private static int IndexKey = 0;
	public static Stain? CurrentDye = null;
	private string SearchBarLabel = $"##dye_search";
	private string SearchBarHint = "Search...";
	private string DyeNameSearch = "";

	private static int RowFromKey(int key) => (int)Math.Floor((double)(key / Columns));
	private static int ColFromKey(int key) => key % Columns;
	public static readonly IEnumerable<Stain> Dyes = PluginServices.DataManager.GetExcelSheet<Stain>()!
		.Where(i => i.IsValid())
		.OrderBy(i => i.Shade).ThenBy(i => i.SubOrder);

	private void DrawLogic() {

		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (ImGui.GetFontSize() *0.15f));
		SearchBarValidated = ImGui.InputTextWithHint(SearchBarLabel, SearchBarHint, ref DyeNameSearch, 32, ImGuiInputTextFlags.EnterReturnsTrue);

		if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
			ImGui.SetKeyboardFocusHere(-1);


		DrawDyePickerHeader();


		IEnumerable<Stain> dyesFiltered = Dyes;
		if (DyeNameSearch.Length > 0) {
			var DyeNameSearch2 = DyeNameSearch;
			dyesFiltered = Dyes.Where(i => i.Name.ToString().Contains(DyeNameSearch2, StringComparison.OrdinalIgnoreCase));
		}

		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ConfigurationManager.Config.DyePickerDyeSize * 0.15f);
		ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 100);
		try {

			IndexKey = 0;
			bool isOneSelected = false; // allows one selection per foreach

			foreach (var i in dyesFiltered) {
				bool selecting = false;
				bool isCurrentActive = IndexKey == LastSelectedItemKey;

				var drawnLineTurpe = DrawDyePickerItem(i, isCurrentActive);
				selecting |= drawnLineTurpe.Item1;

				if (!isOneSelected) {
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseUp) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey) - 1 && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseDown) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey) + 1 && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseUpFast) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey) - FastScrollLineJump && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseDownFast) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey) + FastScrollLineJump && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseLeft) && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey) - 1 && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseRight) && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey) + 1 && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey);
					selecting |= SearchBarValidated;
				}

				if (selecting) {
					if (ImGui.IsKeyPressed(KeyBindBrowseUp) || ImGui.IsKeyPressed(KeyBindBrowseDown) || ImGui.IsKeyPressed(KeyBindBrowseUpFast) || ImGui.IsKeyPressed(KeyBindBrowseDownFast))
						ImGui.SetScrollY(ImGui.GetCursorPosY() - (ImGui.GetWindowHeight() / 2));

					if (GearBrowser.SelectedSlot.HasValue)
						PluginServices.ApplyGearChange.ApplyDye(ConfigurationManager.Config.SelectedCurrentPlate, GearBrowser.SelectedSlot.Value, (byte)i.RowId);

					// assigning cache vars
					LastSelectedItemKey = IndexKey;
					isOneSelected = true;
					CurrentDye = i;
				}
				IndexKey++;
			}

		} catch (Exception ex) {
			PluginLog.Warning(ex, "Error in Dye Picker color square rendering.");
		}

		ImGui.PopStyleVar(2);
	}


	private static int DyeLastSubOrder = -1;

	private static (bool, bool) DrawDyePickerItem(Stain i, bool isActive) {
		bool isThisRealNewLine = IndexKey % Columns == 0;
		bool isThisANewShade = i.SubOrder == 1;

		if (!isThisRealNewLine && isThisANewShade) {
			// skip some index key if we don't finish the row
			int howManyMissedButtons = 12 - (DyeLastSubOrder % 12);
			IndexKey += howManyMissedButtons;
		} else if (!isThisRealNewLine && !isThisANewShade)
			ImGui.SameLine();
		if (isThisANewShade)
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ConfigurationManager.Config.DyePickerDyeSize.Y * 0.33f) ;

		DyeLastSubOrder = i.SubOrder;

		// as we previously changed the index key, let's calculate calculate isActive again
		isActive = IndexKey == LastSelectedItemKey;

		Vector2 startPos = default;
		if (isActive) {
			startPos = ImGui.GetCursorScreenPos();
		}
		var selecting = false;
		try {
			selecting = ImGui.ColorButton($"{i.Name}##{i.RowId}", i.ColorVector4(), ImGuiColorEditFlags.NoDragDrop, ConfigurationManager.Config.DyePickerDyeSize);
			selecting |= i != CurrentDye && ImGui.IsItemHovered() && (ImGui.GetIO().KeyCtrl || ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Left]);

		} catch (Exception e) {
			PluginLog.Error(e, "Error in DrawDyePickerItem");
		}
		if (isActive) {
			var draw = ImGui.GetWindowDrawList();
			var endPos = startPos + ConfigurationManager.Config.DyePickerDyeSize;
			var thickness1 = ConfigurationManager.Config.DyePickerDyeSize.X / 10;
			draw.AddRect(
				startPos,
				endPos,
				ImGui.ColorConvertFloat4ToU32(new Vector4(70 / 255f, 133 / 255f, 158 / 255f, 1)),
				ConfigurationManager.Config.DyePickerDyeSize.X / 5,
				ImDrawFlags.None,
				thickness1
				);
			var thickness2 = thickness1 / 2.5f;


			draw.AddRect(
				startPos,
				endPos,
				ImGui.ColorConvertFloat4ToU32(new Vector4(192 / 255f, 233 / 255f, 237 / 255f, 1)),
				ConfigurationManager.Config.DyePickerDyeSize.X / 5,
				ImDrawFlags.None,
				thickness2
				);

		}

		return (selecting, ImGui.IsItemFocused());
	}

	private static void DrawDyePickerHeader() {
		var i = CurrentDye;

		var colorLabel = $"{i?.Name ?? ""}##{i?.RowId ?? 0}##selected1";
		var colorVec4 = i?.ColorVector4() ?? new Vector4(0, 0, 0, 0);
		var colorFlags = ImGuiColorEditFlags.NoDragDrop;
		if(i == null) colorFlags |= ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.AlphaPreview;
		ImGui.ColorButton(colorLabel, colorVec4, colorFlags, ConfigurationManager.Config.DyePickerDyeSize);

		ImGui.SameLine();
		//ImGui.AlignTextToFramePadding();
		//GuiHelpers.TextWithFont(i.Name, GuiHelpers.Font.TrumpGothic_23);
		ImGui.Text(i?.Name ?? "");

		ImGui.SameLine();
		var spacing = ImGui.GetContentRegionAvail().X
			- ConfigurationManager.Config.DyePickerDyeSize.X
			//- GuiHelpers.CalcIconSize(Dalamud.Interface.FontAwesomeIcon.PaintRoller).X // setting icon
			//- GuiHelpers.CalcIconSize(Dalamud.Interface.FontAwesomeIcon.Cog).X // setting icon
			- ImGui.GetStyle().ItemInnerSpacing.X * 1 // * by number of icon, cause it's between them (and left end item)
			- (ImGui.GetStyle().FramePadding.X * 2); // * by number of icons x2, cause on each sides of the icon
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing);

		GuiHelpers.IconToggleButtonNoBg(Dalamud.Interface.FontAwesomeIcon.PaintRoller, ref ConfigurationManager.Config.DyePickerKeepApplyOnNewItem, "##KeepDyingOnNewItem##DyePicker", "Keep dyeing when a new item is selected in the browser", ConfigurationManager.Config.DyePickerDyeSize);
	}
}
