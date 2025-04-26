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

public class DyePicker {

	const float _multiplicatorDyeSpacing = 0.00001f;



	public void Draw() {
		// TitleBar.CloseButton(this,default,true);

		DrawLogic();
		// if(ImGui.IsKeyPressed(ImGuiKey.Escape)) this.IsOpen = false;
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
	const ushort MaxDyeIndex = 2;
	public static Dictionary<ushort, Stain?> CurrentDyesInEditor = new();
	public static Dictionary<ushort,byte?> CurrentDyeList = new();
	public static InventoryItem? CurrentItem = null;
	private string SearchBarLabel = $"##dye_search";
	private string SearchBarHint = "Search...";
	private string DyeNameSearch => GearBrowser.SearchText();

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


	private static Vector2 _headerPaddingIconSizeMult = new Vector2(0.20f, 0.20f);
	private void DrawLogic() {


		ImGui.BeginChildFrame(9151,new Vector2(ImGui.GetContentRegionAvail().X, ItemIcon.IconSize.Y + (ItemIcon.IconSize.Y *_headerPaddingIconSizeMult.Y *2)), ImGuiWindowFlags.NoScrollbar);
		// ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 0f);
		// ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, ConfigurationManager.Config.DyePickerDyeSize * _multiplicatorDyeSpacing);



		try {
			DrawDyePickerHeader();
		} catch(Exception e) {
			PluginLog.Error(e, "Error while drawing dye picker header line 2");
		}finally {
			// ImGui.PopStyleVar(2);
			ImGui.EndChildFrame();
		}


		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, StainPickerItemSpacing);
		var size = Vector2.Clamp(CalculateStainAvailableSize(), Vector2.Zero, ImGui.GetContentRegionAvail());
		ImGui.BeginChildFrame(9152,size,ImGuiWindowFlags.HorizontalScrollbar);
		try {
			DrawStainAvailable();
		} catch (Exception ex) {
			PluginLog.Warning(ex, "Error in Dye Picker color square rendering.");
		} finally {
			ImGui.EndChildFrame();
			ImGui.PopStyleVar(1);
		}

		ImGui.SameLine();

		ImGui.BeginChildFrame(9153, new Vector2(ImGui.GetContentRegionAvail().X,size.Y));
		try {
			DrawSideBar();
		} catch (Exception ex) {
			PluginLog.Warning(ex, "Error rendering of dye picker side bar.");
		} finally {
			ImGui.EndChildFrame();
		}


	}

	private void DrawStainAvailable() {

		IEnumerable<Stain> dyesFiltered = Dyes;
		// if (DyeNameSearch.Length > 0) {
		// 	dyesFiltered = Dyes.Where(i => i.Name.ToString().Contains(DyeNameSearch, StringComparison.OrdinalIgnoreCase));
		// }


			IndexKey = 0;
			bool isOneSelected = false; // allows one selection per foreach

			foreach (var i in dyesFiltered) {
				bool selecting = false;
				bool isCurrentActive = IndexKey == LastSelectedItemKey;
				bool isMatchingSearch = DyeNameSearch.Length == 0 || (i.Name.ToString().Contains(DyeNameSearch, StringComparison.OrdinalIgnoreCase) || i.Name2.ToString().Contains(DyeNameSearch, StringComparison.OrdinalIgnoreCase));

				var drawnLineTurpe = DrawDyePickerItem(i, isCurrentActive, !isMatchingSearch);
				selecting |= drawnLineTurpe.Item1;

				if (!isOneSelected) {
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseUp) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey ?? 0) - 1 && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey ?? 0);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseDown) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey ?? 0) + 1 && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey ?? 0);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseUpFast) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey ?? 0) - FastScrollLineJump && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey ?? 0);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseDownFast) && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey ?? 0) + FastScrollLineJump && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey ?? 0);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseLeft) && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey ?? 0) - 1 && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey ?? 0);
					selecting |= ImGui.IsKeyPressed(KeyBindBrowseRight) && ColFromKey(IndexKey) == ColFromKey(LastSelectedItemKey ?? 0) + 1 && RowFromKey(IndexKey) == RowFromKey(LastSelectedItemKey ?? 0);
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



	}

	private static int DyeLastSubOrder = -1;
	private static float NewShadeMarginY => ConfigurationManager.Config.DyePickerDyeSize.Y * 0.33f;
	private static Vector2 StainPickerItemSpacing => ConfigurationManager.Config.DyePickerDyeSize * _multiplicatorDyeSpacing;
	private static (bool, bool) DrawDyePickerItem(Stain i, bool isActive, bool faded) {
		bool isThisRealNewLine = IndexKey % Columns == 0;
		bool isThisANewShade = i.SubOrder == 1;

		if (!isThisRealNewLine && isThisANewShade) {
			// skip some index key if we don't finish the row
			int howManyMissedButtons = Columns - (DyeLastSubOrder % Columns);
			IndexKey += howManyMissedButtons;
		} else if (!isThisRealNewLine && !isThisANewShade)
			ImGui.SameLine();
		if (isThisANewShade)
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + NewShadeMarginY) ;

		DyeLastSubOrder = i.SubOrder;

		// as we previously changed the index key, let's calculate calculate isActive again
		if (LastSelectedItemKey == null && CurrentDyeList.TryGetValue(DyeIndex, out var stain) && stain != null) isActive = i.RowId == stain.Value;
		else isActive = IndexKey == LastSelectedItemKey;

		var selecting = DrawStainIcon(i, isActive, faded);
		selecting |= !CurrentDyeList.ContainsValue((byte)i.RowId) && ImGui.IsItemHovered() && (ImGui.GetIO().KeyCtrl || ImGui.GetIO().MouseDown[(int)ImGuiMouseButton.Left]);
		// try {
		//
		// } catch (Exception e) {
		// 	PluginLog.Error(e, "Error in DrawDyePickerItem");
		// }

		return (selecting, ImGui.IsItemFocused());
	}
	private Vector2 CalculateStainAvailableSize() {
		var width = 0f;
		var height = 0f;

		var indexKey = 0;
		var dyeLastSubOrder = -1;

		int numberOfRows = 0;
		int numberOfShades = 0;
		foreach (var i in Dyes)
		{
			bool isThisRealNewLine = indexKey % Columns == 0;
			bool isThisANewShade = i.SubOrder == 1;

			if (!isThisRealNewLine && isThisANewShade) {
				// skip some index key if we don't finish the row
				int howManyMissedButtons = Columns - (dyeLastSubOrder % Columns);
				indexKey += howManyMissedButtons;
				// new shade when it's not a proper newline
				// numberOfRows++;
			}

			if (isThisANewShade) {
				// new shade, add a Y margin and a row
				numberOfShades++;
				numberOfRows++;
			} else if (isThisRealNewLine) {
				numberOfRows++;
			}

			dyeLastSubOrder = i.SubOrder;

			indexKey++;
		}
		width += (Columns * ConfigurationManager.Config.DyePickerDyeSize.X); // size of the stain picker
		width += (Columns-1) * StainPickerItemSpacing.X; // space in between
		width += 2 * ImGui.GetStyle().FramePadding.X; // paddings
		width += ImGui.GetStyle().ScrollbarSize; // scrollbar if needed

		height += ConfigurationManager.Config.DyePickerDyeSize.Y * numberOfRows;
		height += StainPickerItemSpacing.Y * (numberOfRows-1);
		height += NewShadeMarginY * numberOfShades;
		height += 2 * ImGui.GetStyle().FramePadding.Y; // paddings
		height += ImGui.GetStyle().ScrollbarSize; // scrollbar if needed

		return new (width, height);
	}
	private static bool DrawStainIcon(Stain i, bool isActive, bool faded, string extraLabel = "") {

		var bordSize = ConfigurationManager.Config.DyePickerDyeSize * 1.28f;
		var overflowSize = (bordSize - ConfigurationManager.Config.DyePickerDyeSize) / 2;

		var startPos = ImGui.GetCursorScreenPos() - overflowSize;
		var endPos = startPos + bordSize;

		var draw = ImGui.GetWindowDrawList();
		// selecting = ImGui.ColorButton($"{i.Name}##{i.RowId}", i.ColorVector4(), ImGuiColorEditFlags.NoDragDrop, ConfigurationManager.Config.DyePickerDyeSize);
		// var selecting = GuiHelpers.GameButton(UldBundle.ColorChooser_StainColor, $"{i.Name}##{i.RowId}","",ConfigurationManager.Config.DyePickerDyeSize,i.ColorVector4());

		var size = ConfigurationManager.Config.DyePickerDyeSize;
		var tint = i.ColorVector4();
		var hiddenLabel = extraLabel + $"##{i.Name}##{i.RowId}##InvisibleButton";
		var colorStainTexWrap = PluginServices.ImageGuiCrop.GetPart(UldBundle.ColorChooser_StainColor);
		if (colorStainTexWrap == null) return false;
		var pos = ImGui.GetCursorScreenPos();
		var wasHovered = GuiHelpers.IsHovered(hiddenLabel);

		var colorStain = i.ColorVector4();
		var colorStainNotStained = Vector4.Zero.WithAlpha(1f);
		var colorBorder = Vector4.One.Darken(0.3f);
		var colorMetal = i.ColorVector4().Saturate(0.5f).Lighten(0.7f).WithAlpha(0.78f);
		var colorSearchMatch = Vector4.One.WithAlpha(0.4f);

		if (faded) {
			colorStain = colorStain.WithAlpha(0.25f);
			colorStainNotStained = colorStainNotStained.WithAlpha(0.25f);
			colorMetal = colorMetal.WithAlpha(0.15f);
		}

		// draw the color
		draw.AddImage(colorStainTexWrap.ImGuiHandle, startPos, endPos, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(colorStain));

		// draw the
		if (i.RowId == 0) {
			var colorStainNotStainedTexWrap = PluginServices.ImageGuiCrop.GetPart(UldBundle.ColorChooser_StainNotStained);
			if (colorStainNotStainedTexWrap != null) draw.AddImage(colorStainNotStainedTexWrap.ImGuiHandle, startPos, endPos, Vector2.Zero, Vector2.One, ImGui.ColorConvertFloat4ToU32(colorStainNotStained));
		}
		// var hovered = ImGui.IsMouseHoveringRect(pos, pos + size);

		var selecting = ImGui.InvisibleButton(hiddenLabel,size);
		var hovered = ImGui.IsItemHovered();

		GuiHelpers.Hovering(hiddenLabel, ImGui.IsItemHovered());

		GuiHelpers.Tooltip(() => {
			ImGui.Text($"{i.Name}");
		});

		// if dye is metallic, put some gloss
		if (i.Unknown1) {
			var stainMetalTex = PluginServices.ImageGuiCrop.GetPart(UldBundle.ColorChooser_StainMetallic);
			// if (stainMetalTex != null) draw.AddImage(
			// 	stainMetalTex.ImGuiHandle,
			// 	startPos,
			// 	endPos,
			// 	Vector2.One,
			// 	Vector2.Zero,
			// 	ImGui.ColorConvertFloat4ToU32(Vector4.One.Darken(0.3f).WithAlpha(0.9f)));
			if (stainMetalTex != null) draw.AddImage(
				stainMetalTex.ImGuiHandle,
				startPos,
				endPos,
				Vector2.One,
				Vector2.Zero,
				ImGui.ColorConvertFloat4ToU32(colorMetal));
			// if (stainMetalTex != null) draw.AddImage(
			// 	stainMetalTex.ImGuiHandle,
			// 	startPos,
			// 	endPos,
			// 	Vector2.One,
			// 	Vector2.Zero,
			// 	ImGui.ColorConvertFloat4ToU32(i.ColorVector4().Darken(0f).WithAlpha(0.3f)));

		}
		// put a border
		var stainBorderTex = PluginServices.ImageGuiCrop.GetPart(UldBundle.ColorChooser_StainOutline);
		if (stainBorderTex != null) draw.AddImage(
			stainBorderTex.ImGuiHandle,
			startPos,
			endPos,
			Vector2.Zero,
			Vector2.One,
			ImGui.ColorConvertFloat4ToU32(colorBorder));

		// if is active, put the outline effect
		if (isActive || wasHovered || GearBrowser.SearchText().Length > 0) {

			var stainHoverTex = PluginServices.ImageGuiCrop.GetPart(UldBundle.ColorChooser_StainHover);
			if (isActive || wasHovered) {
				if (stainHoverTex != null) draw.AddImage(
					stainHoverTex.ImGuiHandle,
					startPos,
					endPos);
			}

			// highlight not faded when searching
			if (GearBrowser.SearchText().Length > 0 && !faded) {
				if (stainHoverTex != null) draw.AddImage(
					stainHoverTex.ImGuiHandle,
					startPos,
					endPos,
					Vector2.Zero,
					Vector2.One,
					ImGui.ColorConvertFloat4ToU32(colorSearchMatch));
			}
		}

		return selecting;
	}

	public static ushort DyeIndex = 1;
	public static void CircleIndex(ushort? index = null){
		if (index.HasValue) {
			DyeIndex = index.Value;
			return;
		}

		var newIndex = DyeIndex++;
		if (newIndex > MaxDyeIndex) newIndex = 1;
		DyeIndex = newIndex;
	}


	private static void DrawDyePickerHeader()
	{

		var buttonsHeight = ItemIcon.IconSize.Y * 0.75f;
		ImGui.SetCursorPos(ImGui.GetCursorPos() + (ItemIcon.IconSize *_headerPaddingIconSizeMult));

		ItemIcon.DrawIcon(CurrentItem);


		ImGui.SameLine();
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (ItemIcon.IconSize.X *_headerPaddingIconSizeMult.X));
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((ItemIcon.IconSize.X - buttonsHeight) / 2));

		var dyeCount = CurrentItem?.Item.Base.DyeCount ?? 1;
		// var numberOfIconButtons = 2; // swapDye and keepDying

		// var iwd = ConfigurationManager.Config.DyePickerDyeSize.X;
		//var iis = ImGui.GetStyle().ItemInnerSpacing.X; // child frame border ? idk
		var isp = ImGui.GetStyle().ItemSpacing.X; // space between 2 items, usually from a SameLine
		//var fpd = ImGui.GetStyle().FramePadding.X; // spacing inside a child frame, appears on both sides
		//var wpd = ImGui.GetStyle().WindowPadding.X; // window
		// var mrg = ImGui.GetFontSize() * 0.5f; // some extra margin because it only shrinks below the window size, not equal

		var posFirstButton = ImGui.GetCursorPos();
		var posFirstButtonScreen = ImGui.GetCursorScreenPos();
		var widthSelectorAllButtons = ImGui.GetContentRegionAvail().X
			// - (isp * (dyeCount -1)) // here we don't have -1 because we always put a SameLine() after the dye selectors
			- (ItemIcon.IconSize.X *_headerPaddingIconSizeMult.X) // right margin
			;

		var widthOfOneDyeSelectorButton =
				(widthSelectorAllButtons / dyeCount) // just divide  the left side by number of dyes
				- (isp * (dyeCount - 1)) // we exclude margins as they will be added outside
			;

		var buttonSize4 = new Vector2(widthOfOneDyeSelectorButton, buttonsHeight);
		var rounding = ItemIcon.IconSize.X * 0.1f;


		// calculate stuff for inside the button
		var stainIconSize = ConfigurationManager.Config.DyePickerDyeSize;
		var paddingDyeButton = (buttonSize4 - stainIconSize) / 2;

		// ConfigurationManager.Config.DyePickerDye1Or2SelectedBg
		var extraMargin = ItemIcon.IconSize * 0.08f;
		var thicknessRect = ItemIcon.IconSize.X * 0.02f;
		ImGui.GetWindowDrawList().AddRect(posFirstButtonScreen-extraMargin, posFirstButtonScreen + new Vector2(widthSelectorAllButtons,buttonSize4.Y) + (extraMargin),ImGui.ColorConvertFloat4ToU32(ConfigurationManager.Config.DyePickerDye1Or2SelectedBg),rounding * 1.5f,0,thicknessRect);

		var dyeIndexList = Enumerable.Range(1, dyeCount).Select(i => (ushort)i);
		foreach (var dyeIndex in dyeIndexList) {
			ImGui.SetCursorPos(posFirstButton
								+ ((dyeIndex-1) // multiply by dye index
									* new Vector2(
										buttonSize4.X // the button size (only on second button to move it on the right
										+ (isp * 2) // some padding
										,0)));
			// adjust height to icon
			// ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX(), ImGui.GetCursorPosY() + ItemIcon.IconSize.Y * 0.15f));

			var posBackup = ImGui.GetCursorScreenPos();
			var posBackup3 = ImGui.GetCursorPos();


			if (!CurrentDyesInEditor.TryGetValue(dyeIndex, out var i)) i = null;

			var isActive = dyeIndex == DyeIndex;
			// Vector4? textCo = dyeIndex == DyeIndex ? ConfigurationManager.Config.DyePickerDye1Or2SelectedColor : null;
			// Vector4  BgCo   = dyeIndex == DyeIndex ? ConfigurationManager.Config.DyePickerDye1Or2SelectedBg : ImGui.GetStyle().Colors[(int)ImGuiCol.ChildBg];

			// var childFrameCol = ImRaii.PushColor(ImGuiCol.FrameBg, BgCo);



			// if (ImGui.BeginChildFrame(115919u + dyeIndex, new Vector2(widthOfOneDyeSelectorButton, ConfigurationManager.Config.DyePickerDyeSize.Y + (ImGui.GetStyle().FramePadding.Y*2)))){
			//

			var colorLabel = $"{i?.Name ?? "No Color"}##{i?.RowId ?? 0}##stain{dyeIndex}##selected1";
			var colorVec4 = i?.ColorVector4() ?? new Vector4(0, 0, 0, 0);
			var clicked = GuiHelpers.ButtonDrawList(colorLabel, buttonSize4, ConfigurationManager.Config.BrowserVerticalTabButtonsBg, ConfigurationManager.Config.DyePickerDye1Or2SelectedBg, rounding, isActive);
			var posBackup2 = ImGui.GetCursorPos();

			ImGui.SameLine();
			ImGui.SetCursorPos(posBackup3);


			// ImGui.SetCursorScreenPos(posBackup);


			var posDyeIdent1 = ImGui.GetCursorScreenPos() + new Vector2(0, paddingDyeButton.Y);
			var iconUld = isActive ? UldBundle.ColorantToggleButton_DyeIndicatorActive : UldBundle.ColorantToggleButton_DyeIndicatorInactive;
			var stainBorderTex = PluginServices.ImageGuiCrop.GetPart(iconUld);
			if (stainBorderTex != null) ImGui.GetWindowDrawList().AddImage(
				stainBorderTex.ImGuiHandle,
				posDyeIdent1,
				posDyeIdent1 + ConfigurationManager.Config.DyePickerDyeSize,
				Vector2.Zero,
				Vector2.One,
				ImGui.ColorConvertFloat4ToU32(Vector4.One));
			var fontHandle = GuiHelpers.FontHandle(GuiHelpers.Font.Title);
			fontHandle.Push();
			ImGui.GetWindowDrawList().AddText(posDyeIdent1 + (ConfigurationManager.Config.DyePickerDyeSize.X * new Vector2( 0.75f, 0.25f)),ImGui.ColorConvertFloat4ToU32(Vector4.One.WithAlpha(0.8f)),$"{dyeIndex.ToString()}");
			fontHandle.Pop();
			// var colorFlags = ImGuiColorEditFlags.NoDragDrop;
			// if (i == null) colorFlags |= ImGuiColorEditFlags.NoPicker | ImGuiColorEditFlags.AlphaPreview;
			// clicked |= ImGui.ColorButton(colorLabel, colorVec4, colorFlags, ConfigurationManager.Config.DyePickerDyeSize);

			// ImGui.SameLine();
			// clicked |= GuiHelpers.ButtonNoBg((hl, v) => ImGui.Button(colorLabel + "##textbutton" + hl), $"##zzzzzz{DyeIndex}##{i?.RowId}","",textCo);

			// ImGui.SetCursorScreenPos(posBackup + ImGui.GetStyle().FramePadding * 2 + new Vector2(ImGui.GetStyle().ItemSpacing.X, 0) + new Vector2(ConfigurationManager.Config.DyePickerDyeSize.X,paddingDyeButton.Y));
			ImGui.SetCursorScreenPos(posDyeIdent1 + new Vector2(
				ConfigurationManager.Config.DyePickerDyeSize.X // the widget above
				+ (ConfigurationManager.Config.DyePickerDyeSize.X * 0.5f) // some margin
				,0));

			if (i != null) {
				DrawStainIcon(i.Value, false, false, $"##from##{dyeIndex}##StainButtonHeader");
			}

			if (clicked) {
				DyeIndex = dyeIndex;
				LastSelectedItemKey = null;
			}

			ImGui.SetCursorPos(posBackup2);
			// 	ImGui.EndChildFrame();
			// }
			// childFrameCol.Dispose();
			ImGui.SameLine();
		}



	}
	private static void DrawSideBar() {

		var dyeCount = CurrentItem?.Item.Base.DyeCount ?? 1;
		// var spacing = ImGui.GetContentRegionAvail().X - widthOfRightSide;
		// ImGui.SetCursorPosX(ImGui.GetCursorPosX() + spacing);

		if (dyeCount != 2) ImGui.BeginDisabled();
		if (GuiHelpers.GameButton(UldBundle.ColorantButton_Swap, "##SwapDyes##DyePicker", "", ConfigurationManager.Config.DyePickerDyeSize * 1.5f)) {
			SwapDyesOneItem();
		}
		if (dyeCount != 2) ImGui.EndDisabled();
		GuiHelpers.Tooltip("Swap dyes between dye 1 and dye 2");

		ImGui.SameLine();

		if (GuiHelpers.GameButton(UldBundle.ColorantButton_Swap, "##SwapDyesAll##DyePicker", "Swap dyes between dye 1 and dye 2 for all items in the plate", ConfigurationManager.Config.DyePickerDyeSize * 1.5f, new(1,0.7f,0.9f,1))) {
			SwapDyesAllItem();
		}

		// ImGui.SameLine();
		GuiHelpers.IconToggleButtonNoBg(FontAwesomeIcon.PaintRoller, ref ConfigurationManager.Config.DyePickerKeepApplyOnNewItem, "##KeepDyingOnNewItem##DyePicker", "Keep dyeing when a new item is selected in the browser", ConfigurationManager.Config.DyePickerDyeSize);

	}



	private static void SwapDyesOneItem() {
		if (!PluginServices.ApplyGearChange.SwapDyesForCurrentSlotInCurrentPlate()) return;
		SwapDyesDyePicker();

	}
	private static void SwapDyesAllItem() {
		PluginServices.ApplyGearChange.SwapDyesForAllItemsInCurrentPlate();
		SwapDyesDyePicker();
	}

	private static void SwapDyesDyePicker() {
		CircleIndex();
		if (CurrentDyesInEditor.TryGetValue(1, out var s1) && CurrentDyesInEditor.TryGetValue(2, out var s2) && s1 != null && s2 != null) {
			CurrentDyesInEditor[1] = s2;
			CurrentDyesInEditor[2] = s1;
			CurrentDyeList[1] = (byte)s2.Value.RowId;
			CurrentDyeList[2] = (byte)s1.Value.RowId;
		}
	}
}
