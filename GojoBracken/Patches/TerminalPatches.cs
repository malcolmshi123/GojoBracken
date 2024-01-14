using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GojoBracken.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]

        public static void EditTerminal(Terminal __instance)
        {
            __instance.terminalNodes.allKeywords[36].word = "gojo";
            __instance.enemyFiles[1].creatureName = "Gojo";
        }
    }
}
