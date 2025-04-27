
using Dresser.Interop.Hooks;

using Lumina.Excel.Sheets;

namespace Dresser.Structs.Dresser.DyeHistory {



    public class Entry(GlamourPlateSlot slot, ushort dyeIndex, ushort dyeIdFrom, ushort dyeIdTo) {
        public GlamourPlateSlot Slot = slot;
        public ushort DyeIndex = dyeIndex;
        public ushort DyeIdFrom = dyeIdFrom;
        public ushort DyeIdTo = dyeIdTo;


        public Stain StainFrom() {
            if (PluginServices.DataManager.GetExcelSheet<Stain>().TryGetRow(DyeIdFrom, out Stain stain)) return stain;
            return PluginServices.DataManager.GetExcelSheet<Stain>()[0];
        }
        public Stain StainTo() {
            if (PluginServices.DataManager.GetExcelSheet<Stain>().TryGetRow(DyeIdTo, out Stain stain)) return stain;
            return PluginServices.DataManager.GetExcelSheet<Stain>()[0];
        }


    }
}
