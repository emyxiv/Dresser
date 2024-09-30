using System;

namespace Dresser.Enums
{
    [Flags]
    public enum BehaviorOnOpen
    {
        SandboxPlateWithWearingGlam= 0,
        SandboxPlateAndStrip = 1,
        LastOpenedPortablePlate = 2,
        
        
        // chose one
        // DoNotChangeCurrentAppearance = 0,
        // Strip = 2,

        // options
        // NotAtGlamourDresser = 4096,
        // SandboxPlate = 8192,
        // LastOpenedPortablePlate = 16384,
    }
}
