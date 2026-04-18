using System;
using System.Linq;

using CriticalCommonLib;

using Dalamud.Utility;

using Lumina.Excel.Sheets;
using Lumina.Extensions;


namespace Dresser.Services {
	internal partial class Storage : IDisposable {

		public Storage() {

			// store change pose icon id based on the alias in case ID changes
			ChangePoseIconId = PluginServices.DataManager.Excel.GetSheet<Emote>().FirstOrNull(e => e.TextCommand.Value.ShortCommand == "/cpose" || e.TextCommand.Value.Command == "/cpose")?.Icon ?? 0; // 7.4 = 246268

			// store total valid classjob count
			ClassJobsTotalCount = PluginServices.DataManager.Excel.GetSheet<ClassJob>().Count(cj => !cj.Abbreviation.ToString().IsNullOrWhitespace());

			// store max levels
			MaxEquipLevel = (ushort)PluginServices.DataManager.Excel.GetSheet<Item>().Max(a=>a.LevelEquip);
			MaxItemLevel = PluginServices.DataManager.Excel.GetSheet<Item>().Max(a=>a.LevelItem.Value.RowId);

			// ui colors:
			// bad: 14

			var uicolorsheet = PluginServices.DataManager.GetExcelSheet<UIColor>()!;

			RarityColors = new(){
				// items colors:
				{ 0, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 2)) },
				// white: 33/549
				{ 1, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 549)) },
				// green: 42/67/551
				{ 2, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 551)) },
				// blue : 37/38/553
				{ 3, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 553)) },
				// purple: 522/48/555
				{ 4, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 555)) },
				// orange:557
				{ 5, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 557)) },
				// yellow:559
				{ 6, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 559)) },
				// pink: 578/556
				{ 7, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 561)) },
				// gold: 563
				{ 8, Utils.ConvertUiColorToColor(uicolorsheet.First(c => c.RowId == 563)) },
			};


			InitItemTypes();
			PluginServices.OnPluginLoaded += LoadAdditionalItems;
		}
		public void Dispose() {
			foreach((var k, (var h, var s)) in FontHandles) h.Dispose();
			FontHandles.Clear();
		}
	}
}
