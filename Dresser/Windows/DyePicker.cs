using Dalamud.Interface.Windowing;
using Dalamud.Logging;

using Dresser.Data;
using Dresser.Data.Excel;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Windows.Components;

using ImGuiNET;

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
	}


	public bool MustDraw = false;
	public override bool DrawConditions() {
		return PluginServices.Context.IsCurrentGearWindowOpen;
	}
	//public override void PreOpenCheck() {
	//	if (MustDraw && !this.IsOpen) this.IsOpen = true;
	//	if (!MustDraw && this.IsOpen) this.IsOpen = false;
	//	base.PreOpenCheck();
	//}

	public override void OnClose() {
		MustDraw = false;
		this.IsOpen = false;
		base.OnClose();
	}

	//public override void Update() {
	//	//if(this.)
	//	base.Update();
	//}

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
	private static bool Focus = false;
	private static bool SearchBarValidated = false;
	public static int LastSelectedItemKey = 0;
	public static int Columns = 12;
	public static int IndexKey = 0;
	public static Dye? ItemForHeader = null;
	public float MinWidth = 400f;
	public string SearchBarLabel = $"##dye_search";
	public string SearchBarHint = "Search...";
	public string DyeNameSearch = "";

	private static int RowFromKey(int key) => (int)Math.Floor((double)(key / Columns));
	private static int ColFromKey(int key) => key % Columns;
	public static readonly IEnumerable<Dye> Dyes = Sheets.GetSheet<Dye>()
		.Where(i => i.IsValid())
		.OrderBy(i => i.Shade).ThenBy(i => i.SubOrder);

	private void DrawLogic() {

		Focus = ImGui.IsWindowFocused() || ImGui.IsWindowHovered();

		ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (ImGui.GetStyle().FramePadding.X * 2));
		SearchBarValidated = ImGui.InputTextWithHint(SearchBarLabel, SearchBarHint, ref DyeNameSearch, 32, ImGuiInputTextFlags.EnterReturnsTrue);

		if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
			ImGui.SetKeyboardFocusHere(-1);


		DrawDyePickerHeader();


		IEnumerable<Dye> dyesFiltered = Dyes;
		if (DyeNameSearch.Length > 0) {
			var DyeNameSearch2 = DyeNameSearch;
			dyesFiltered = Dyes.Where(i => i.Name.Contains(DyeNameSearch2, StringComparison.OrdinalIgnoreCase));
		}

		IndexKey = 0;
		bool isOneSelected = false; // allows one selection per foreach

		foreach (var i in dyesFiltered) {
			bool selecting = false;
			bool isCurrentActive = IndexKey == LastSelectedItemKey;

			var drawnLineTurpe = DrawDyePickerItem(i, isCurrentActive);
			Focus |= ImGui.IsItemFocused();
			selecting |= drawnLineTurpe.Item1;
			Focus |= drawnLineTurpe.Item2;

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
				ItemForHeader = i;
			}
			Focus |= ImGui.IsItemFocused();
			IndexKey++;
		}


		// box has ended
		Focus |= ImGui.IsItemActive();

	}


	private static int DyeLastSubOrder = -1;
	private const int DyePickerWidth = 485;


	private static (bool, bool) DrawDyePickerItem(Dye i, bool isActive) {
		bool isThisRealNewLine = IndexKey % Columns == 0;
		bool isThisANewShade = i.SubOrder == 1;

		if (!isThisRealNewLine && isThisANewShade) {
			// skip some index key if we don't finish the row
			int howManyMissedButtons = 12 - (DyeLastSubOrder % 12);
			IndexKey += howManyMissedButtons;
		} else if (!isThisRealNewLine && !isThisANewShade)
			ImGui.SameLine();
		if (isThisANewShade)
			ImGui.Spacing();

		DyeLastSubOrder = i.SubOrder;

		// as we previously changed the index key, let's calculate calculate isActive again
		isActive = IndexKey == LastSelectedItemKey;

		Vector2 startPos = default;
		if (isActive) {
			startPos = ImGui.GetCursorScreenPos();
		}
		var selecting = false;
		try {
			selecting = ImGui.ColorButton($"{i.Name}##{i.RowId}", i.ColorVector4, ImGuiColorEditFlags.None, ConfigurationManager.Config.DyePickerDyeSize);
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
		var i = ItemForHeader;

		if (i == null) {
			//ImGui.BeginDisabled();
			ImGui.ColorButton($"##notselected##selected1", new Vector4(0, 0, 0, 0), ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.AlphaPreview, ConfigurationManager.Config.DyePickerDyeSize);
			//ImGui.EndDisabled();
			ImGui.SameLine();
			ImGui.Text("");
			return;
		}

		// TODO: configuration to not show this
		//var textSize = ImGui.CalcTextSize(i.Name);
		//float dyeShowcaseWidth = (DyePickerWidth - textSize.X - (ImGui.GetStyle().ItemSpacing.X * 2)) / 2;
		//ImGui.ColorButton($"{i.Name}##{i.RowId}##selected1", i.ColorVector4, ImGuiColorEditFlags.None, new Vector2(dyeShowcaseWidth, textSize.Y));
		ImGui.ColorButton($"{i.Name}##{i.RowId}##selected1", i.ColorVector4, ImGuiColorEditFlags.None, ConfigurationManager.Config.DyePickerDyeSize);
		ImGui.SameLine();
		//ImGui.AlignTextToFramePadding();
		//GuiHelpers.TextWithFont(i.Name, GuiHelpers.Font.TrumpGothic_23);
		ImGui.Text(i.Name);
		//ImGui.SameLine();
		//ImGui.ColorButton($"{i.Name}##{i.RowId}##selected2", i.ColorVector4, ImGuiColorEditFlags.None, new Vector2(dyeShowcaseWidth, textSize.Y));
	}
}
