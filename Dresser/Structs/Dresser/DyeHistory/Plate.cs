using System.Collections.Generic;

using CriticalCommonLib.Extensions;

using Dresser.Interop.Hooks;
using Dresser.Logic;

namespace Dresser.Structs.Dresser.DyeHistory {
    public class Plate {
        public int Index = -1;
        public List<Entry> Entries = [];

        public void AddEntry(Entry entry) {

            // this is when there was some undo
			// we need to remove everything that happened after the index
			if (Index < (Entries.Count - 1)) {
                var numberToRemove = Entries.Count - (Index + 1);
				Entries.RemoveRange(Index + 1, numberToRemove);
			}

            Entries.Add(entry);
            Index++;
            // Index = Entries.Count - 1;
        }
        public void AddEntry(GlamourPlateSlot slot, ushort dyeIndex, ushort dyeIdFrom, ushort dyeIdTo) {
            if (dyeIdFrom == dyeIdTo) return;
            AddEntry(new Entry(slot, dyeIndex, dyeIdFrom, dyeIdTo));
        }
        private Entry? GetEntry(int index) {
            if (index < 0 || index >= Entries.Count) {
                return null;
            }
            return Entries[index];
        }


        public Entry? UndoOrRedo(bool forward) {
            if (Entries.Count == 0) return null;

            int newIndexBeforeClamp;
            if (forward) {
                newIndexBeforeClamp = Index + 1;
            }
            else {
                newIndexBeforeClamp = Index - 1;
            }
            var newIndex = int.Clamp(newIndexBeforeClamp, -1, Entries.Count - 1);
            if (newIndex == Index) return null;

            var entry = GetEntry(newIndex > -1 ? newIndex : 0);

            if(entry == null) return null;

            if (newIndex == -1) {
                entry = entry.Copy()!;
                entry.DyeIdTo = entry.DyeIdFrom;
            }

            Index = newIndex;

            return entry;
        }
        public Entry? Undo() => UndoOrRedo(false);
        public Entry? Redo() => UndoOrRedo(true);
    }
}
