using AllaganLib.GameSheets.Service;
using AllaganLib.GameSheets.Sheets;

using CriticalCommonLib.Models;

using Dalamud.Plugin.Services;

using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace Dresser.Services {
    public class InventoryItemFactory {
        private readonly SheetManager _sheetManager;
        private readonly IDataManager _dataManager;
        public InventoryItemFactory(SheetManager sheetManager, IDataManager dataManager) {
            _sheetManager = sheetManager;
            _dataManager = dataManager;
            ItemSheet = _sheetManager.GetSheet<ItemSheet>();
            StainSheet = _dataManager.GetExcelSheet<Stain>();
        }

        public ExcelSheet<Stain> StainSheet { get; set; }
        public ItemSheet ItemSheet { get; set; }
    }
}