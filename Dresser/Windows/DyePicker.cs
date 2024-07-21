using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Dresser;
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
	private static int? LastSelectedItemKey = null;
	private static int Columns = 12;
	private static int IndexKey = 0;
	//public static Stain? CurrentDye2 = null;
	public static Dictionary<ushort, Stain?> CurrentDyesInEditor = new();
	public static Dictionary<ushort,byte?> CurrentDyeList = new();
	private string SearchBarLabel = $"##dye_search";
	private string SearchBarHint = "Search...";
	private string DyeNameSearch = "";

	private static int RowFromKey(int key) => (int)Math.Floor((double)(key / Columns));
	private static int ColFromKey(int key) => key % Columns;
	public static readonly IEnumerable<Stain> Dyes = PluginServices.DataManager.GetExcelSheet<Stain>()!
		.Where(i => i.IsValid())
		.OrderBy(i => i.Shade).ThenBy(i => i.SubOrder);
	public static Stain Dye(byte stain) => PluginServices.DataManager.GetExcelSheet<Stain>()!
		.First(i => i.RowId == stain);

	public static void SetSelection(InventoryItem? item, bool resetSelected = true) {
			if (item?.Item.IsDyeable1() ?? false) SetSelection(item.Stain, 1, resetSelected);
			if (item?.Item.IsDyeable2() ?? false) SetSelection(item.Stain2, 2, resetSelected);
	}
	public static void SetSelection(byte stain, ushort index, bool resetSelected = true) {
		CurrentDyeList[index] = stain;
		CurrentDyesInEditor[index] = Dye(stain);
		if (resetSelected) LastSelectedItemKey = null;
	}
	public static void SetSelection(Stain stain, ushort index, bool resetSelected = true) {
		CurrentDyeList[index] = (byte)stain.RowId;
		CurrentDyesInEditor[index] = stain;
		if(resetSelected) LastSelectedItemKey = null;
	}

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
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseUp) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey??0) - 1 && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey??0);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseDown) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey??0) + 1 && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey??0);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseUpFast) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey??0) - FastScrollLineJump && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey??0);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseDownFast) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey??0) + FastScrollLineJump && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey??0);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseLeft) && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey??0) - 1 && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey??0);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseRight) && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey??0) + 1 && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey??0);
					selecting |= SearchBarValidated;
				}

				if (selecting) {
					if (ImGui.IsKeyPressed(KeyBindBrowseUp) || ImGui.IsKeyPressed(KeyBindBrowseDown) || ImGui.IsKeyPressed(KeyBindBrowseUpFast) || ImGui.IsKeyPressed(KeyBindBrowseDownFast))
						ImGui.SetScrollY(ImGui.GetCursorPosY() - (ImGui.GetWindowHeight() / 2));

					if (GearBrowser.SelectedSlot.HasValue) {

						PluginServices.ApplyGearChange.ApplyDye(ConfigurationManager.Config.SelectedCurrentPlate, GearBrowser.SelectedSlot.Value, (byte)i.RowId, DyeIndex);
					}

					// assigning cache vars
					LastSelectedItemKey = IndexKey;
					isOneSelected = true;
					CurrentDyeList[DyeIndex] = (byte)i.RowId;
					CurrentDyesInEditor[DyeIndex] = i;
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
		if (LastSelectedItemKey == null && CurrentDyeList.TryGetValue(DyeIndex, out var stain) && stain != null) isActive = i.RowId == stain.Value;
		else isActive = IndexKey == LastSelectedItemKey;

		Vector2 startPos = default;
		if (isActive) {
			startPos = ImGui.GetCursorScreenPos();
		}
		var selecting = false;
		try {
			selecting = ImGui.ColorButton($"{i.Name}##{i.RowId}", i.ColorVector4(), ImGuiColorEditFlags.NoDragDrop, ConfigurationManager.Config.DyePickerDyeSize);
			selecting |= !CurrentDyeList.ContainsValue((byte)i.RowId) && ImGui.IsItemHovered() && (ImGui.GetIO().KeyCtrl || ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Left]);

		} catch (Exception e) {
			PluginLog.Error(e, "Error in DrawDyePickerItem");
		}
		if (isActive) {
			var draw = ImGui.GetWindowDrawList();
			var endPos = startPos + ConfigurationManager.Config.DyePickerDyeSize;
			var thicknessOutter = ConfigurationManager.Config.DyePickerDyeSize.X * 0.3f;
			var thicknessInner = thicknessOutter * 0.4f;
			var selectionColorOutter = ConfigurationManager.Config.DyePickerHighlightSelection;
			var selectionColorInner = new Vector4(selectionColorOutter.X * 0.8f, selectionColorOutter.Y * 0.8f, selectionColorOutter.Z * 0.8f, 0.9f);

			draw.AddRect(
				startPos,
				endPos,
				ImGui.ColorConvertFloat4ToU32(selectionColorOutter),
				ConfigurationManager.Config.DyePickerDyeSize.X / 5,
				ImDrawFlags.None,
				thicknessOutter
				);


			draw.AddRect(
				startPos,
				endPos,
				ImGui.ColorConvertFloat4ToU32(selectionColorInner),
				ConfigurationManager.Config.DyePickerDyeSize.X / 5,
				ImDrawFlags.None,
				thicknessInner
				);

		}

		return (selecting, ImGui.IsItemFocused());
	}

	public static ushort DyeIndex = 1;
	public static List<ushort> DyeIndexList = new() {1,2};
	private static void DrawDyePickerHeader() {
		var spacingPrebuttons = ImGui.GetContentRegionAvail().X
			- ConfigurationManager.Config.DyePickerDyeSize.X
			- ImGui.GetStyle().ItemInnerSpacing.X * 3 // * by number of icon, cause it's between them (and left end item)
			- (ImGui.GetStyle().FramePadding.X * 6); // * by number of icons x2, cause on each sides of the icon

		foreach (var dyeIndex in DyeIndexList) {
			if (!CurrentDyesInEditor.TryGetValue(dyeIndex, out var i)) i = null;

			Vector4? textCo = dyeIndex == DyeIndex ? ConfigurationManager.Config.DyePickerDye1Or2SelectedColor : null;
			Vector4  BgCo   = dyeIndex == DyeIndex ? ConfigurationManager.Config.DyePickerDye1Or2SelectedBg : ImGui.GetStyle().Colors[(int)ImGuiCol.ChildBg];

			var childFrameCol = ImRaii.PushColor(ImGuiCol.FrameBg, BgCo);
			if (ImGui.BeginChildFrame(115919u + dyeIndex, new Vector2((spacingPrebuttons / 2) - (ImGui.GetStyle().FramePadding.X * 2), ConfigurationManager.Config.DyePickerDyeSize.Y + (ImGui.GetStyle().FramePadding.Y*2)))){

				var clicked = false;

				var colorLabel = $"{i?.Name ?? "No Color"}##{i?.RowId ?? 0}##stain{dyeIndex}##selected1";
				var colorVec4 = i?.ColorVector4() ?? new Vector4(0, 0, 0, 0);
				var colorFlags = ImGuiColorEditFlags.NoDragDrop;
				if (i == null) colorFlags |= ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.AlphaPreview;
				clicked |= ImGui.ColorButton(colorLabel, colorVec4, colorFlags, ConfigurationManager.Config.DyePickerDyeSize);

				ImGui.SameLine();
				clicked |= GuiHelpers.ButtonNoBg((hl, v) => ImGui.Button(colorLabel + "##textbutton" + hl), $"##zzzzzz{DyeIndex}##{i?.RowId}","",textCo);

				if (clicked) {
					DyeIndex = dyeIndex;
					LastSelectedItemKey = null;
				}

				ImGui.EndChildFrame();
			}
			childFrameCol.Dispose();
			ImGui.SameLine();
		}

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
