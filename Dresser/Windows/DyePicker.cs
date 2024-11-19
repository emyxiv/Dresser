using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using Dresser.Extensions;
using Dresser.Logic;
using Dresser.Services;
using Dresser.Structs.Dresser;
using Dresser.Windows.Components;

using ImGuiNET;

using Lumina.Excel.Sheets;

namespace Dresser.Windows;

public class DyePicker :  Window, IDisposable {

	Plugin Plugin;
	const float _multiplicatorDyeSpacing = 0.15f;
	public DyePicker(Plugin plugin) : base(
		"Dye Picker",
		ImGuiWindowFlags.NoTitleBar
		) {
		this.RespectCloseHotkey = true;
		this.SizeConstraints = new WindowSizeConstraints {
			MinimumSize = new Vector2(ImGui.GetFontSize()),
			MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
		};
		this.SizeCondition = ImGuiCond.Once;
		Plugin = plugin;
	}

	public void Dispose() { }
	public override void PreDraw()
		=> Styler.PushStyleCollection();
	public override void PostDraw()
		=> Styler.PopStyleCollection();

	public override void Draw() {
		TitleBar.CloseButton(this,default,true);

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
	public static InventoryItem? CurrentItem = null;
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


		ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
		ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ConfigurationManager.Config.DyePickerDyeSize * _multiplicatorDyeSpacing);

		try {

			ImGui.PushStyleColor(ImGuiCol.Text, ConfigurationManager.Config.PlateSelectorColorTitle);
			try {

				ImGui.Spacing();
				var zz = TitleBar.CloseButtonSize;
				ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - zz.X - (ImGui.GetStyle().FramePadding.X  *  2) - (ImGui.GetStyle().ItemSpacing.X * 2) );
				SearchBarValidated = ImGui.InputTextWithHint(SearchBarLabel, SearchBarHint, ref DyeNameSearch, 32, ImGuiInputTextFlags.EnterReturnsTrue);

				if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
					ImGui.SetKeyboardFocusHere(-1);

			} catch (Exception e) {
				PluginLog.Error(e, "Error while drawing dye picker header line 1");
			} finally {
				ImGui.PopStyleColor(1);
			}

			DrawDyePickerHeader();
		} catch(Exception e) {
			PluginLog.Error(e, "Error while drawing dye picker header line 2");
		}finally {
			ImGui.PopStyleVar(2);
		}


		IEnumerable<Stain> dyesFiltered = Dyes;
		if (DyeNameSearch.Length > 0) {
			var DyeNameSearch2 = DyeNameSearch;
			dyesFiltered = Dyes.Where(i => i.Name.ToString().Contains(DyeNameSearch2, StringComparison.OrdinalIgnoreCase));
		}

		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ConfigurationManager.Config.DyePickerDyeSize * _multiplicatorDyeSpacing);
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
	private static void DrawDyePickerHeader() {

		var dyeCount = CurrentItem?.Item.Base.DyeCount ?? 1;
		var numberOfIconButtons = 2; // swapDye and keepDying

		var iwd = ConfigurationManager.Config.DyePickerDyeSize.X;
		//var iis = ImGui.GetStyle().ItemInnerSpacing.X; // child frame border ? idk
		var isp = ImGui.GetStyle().ItemSpacing.X; // space between 2 items, usually from a SameLine
		//var fpd = ImGui.GetStyle().FramePadding.X; // spacing inside a child frame, appears on both sides
		//var wpd = ImGui.GetStyle().WindowPadding.X; // window
		var mrg = ImGui.GetFontSize() * 0.5f; // some extra margin because it only shrinks below the window size, not equal


		var widthOfRightSide =
			//(wpd * 0)                         // window, actually it's already in GetContentRegionAvail()
			+ (isp * (numberOfIconButtons - 1)) // number of SameLine ?
			+ (iwd * numberOfIconButtons)       // buttons size
			+ mrg                               // extra margin to force shrink and prevent invinite size
			;

		var widthOfLeftSide = ImGui.GetContentRegionAvail().X
			- (isp * dyeCount) // here we don't have -1 because we always put a SameLine() after the dye selectors
			- widthOfRightSide
			;

		var widthOfOneDyeSelectorButton =
			(widthOfLeftSide / dyeCount) // just divide  the left side by number of dyes
			- (isp * dyeCount);          // we exclude margins as they will be added outside



		var dyeIndexList = Enumerable.Range(1, dyeCount).Select(i => (ushort)i);
		foreach (var dyeIndex in dyeIndexList) {
			if (!CurrentDyesInEditor.TryGetValue(dyeIndex, out var i)) i = null;

			Vector4? textCo = dyeIndex == DyeIndex ? ConfigurationManager.Config.DyePickerDye1Or2SelectedColor : null;
			Vector4  BgCo   = dyeIndex == DyeIndex ? ConfigurationManager.Config.DyePickerDye1Or2SelectedBg : ImGui.GetStyle().Colors[(int)ImGuiCol.ChildBg];

			var childFrameCol = ImRaii.PushColor(ImGuiCol.FrameBg, BgCo);
			if (ImGui.BeginChildFrame(115919u + dyeIndex, new Vector2(widthOfOneDyeSelectorButton, ConfigurationManager.Config.DyePickerDyeSize.Y + (ImGui.GetStyle().FramePadding.Y*2)))){

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
		if (dyeCount == 0) {
			if (ImGui.BeginChildFrame(115919u + 0, new Vector2(widthOfOneDyeSelectorButton, ConfigurationManager.Config.DyePickerDyeSize.Y + (ImGui.GetStyle().FramePadding.Y * 2)))) {
				ImGui.TextDisabled($"Undyeable item selected");
				ImGui.EndChildFrame();
			}
			ImGui.SameLine();
		}

		var spacing = ImGui.GetContentRegionAvail().X - widthOfRightSide;
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing);

		if (dyeCount != 2) ImGui.BeginDisabled();
		if (GuiHelpers.IconButtonNoBg(FontAwesomeIcon.CodeCompare, "##SwapDyes##DyePicker", "", ConfigurationManager.Config.DyePickerDyeSize)) {
			if (PluginServices.ApplyGearChange.swapDyes()) { // swap dyes for pending glam plate
															 // also swap dyes in dye picker
				if(CurrentDyesInEditor.TryGetValue(1, out var s1) && CurrentDyesInEditor.TryGetValue(2, out var s2) && s1 != null && s2 != null) {
					CurrentDyesInEditor[1] = s2;
					CurrentDyesInEditor[2] = s1;
					CurrentDyeList[1] = (byte)s2.Value.RowId;
					CurrentDyeList[2] = (byte)s1.Value.RowId;
				}
			}
		}
		if (dyeCount != 2) ImGui.EndDisabled();
		GuiHelpers.Tooltip("Swap dyes between dye 1 and dye 2");
		ImGui.SameLine();
		GuiHelpers.IconToggleButtonNoBg(FontAwesomeIcon.PaintRoller, ref ConfigurationManager.Config.DyePickerKeepApplyOnNewItem, "##KeepDyingOnNewItem##DyePicker", "Keep dyeing when a new item is selected in the browser", ConfigurationManager.Config.DyePickerDyeSize);
	}
}
