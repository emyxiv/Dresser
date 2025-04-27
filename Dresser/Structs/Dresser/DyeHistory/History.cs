using System.Collections.Generic;

using Dresser.Logic;

namespace Dresser.Structs.Dresser.DyeHistory {
    public class History {

        private Dictionary<ushort, Plate> Plates = [];

        public Plate GetHistory(ushort plateId) {
            if (!Plates.TryGetValue(plateId, out var plate)) {
                Plates[plateId] = new();
            }
            return Plates[plateId];
        }
    }
}
