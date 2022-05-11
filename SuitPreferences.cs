using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;

namespace ShockwaveSuit {
    public static class SuitPreferences {
        private static MelonPreferences_Category suitCategory;
        public static MelonPreferences_Entry<bool> verbose;
        public static MelonPreferences_Entry<bool> ledResponse;
        public static MelonPreferences_Entry<int> pulseRate;
        public static MelonPreferences_Entry<int> hapticMode;

        public enum HapticsResponseMode {
            OnHit,
            OnMiss
        }

        static SuitPreferences() {
            suitCategory = MelonPreferences.CreateCategory("ShockwaveSuit");
            pulseRate = suitCategory.CreateEntry("Pulse Rate", 50);
            verbose = suitCategory.CreateEntry("Verbose", false);
            ledResponse = suitCategory.CreateEntry("LED Response", true);
            hapticMode = suitCategory.CreateEntry("Haptic Mode", (int)HapticsResponseMode.OnHit);
        }
    }
}
