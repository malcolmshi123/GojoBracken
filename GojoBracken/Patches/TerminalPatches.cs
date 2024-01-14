using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

namespace GojoBracken.Patches
{
    [HarmonyPatch]
    internal class TerminalPatches
    {
        [HarmonyPatch(typeof(Terminal), "Awake")]
        [HarmonyPostfix]

        public static void EditTerminal(Terminal __instance)
        {
            __instance.terminalNodes.allKeywords[36].word = "gojo";
            __instance.enemyFiles[1].creatureName = "Gojo";
            __instance.enemyFiles[1].displayVideo = LC_API.BundleAPI.BundleLoader.GetLoadedAsset<VideoClip>("assets/Gojo.m4v");
            __instance.enemyFiles[1].displayText = LC_API.BundleAPI.BundleLoader.GetLoadedAsset<TextAsset>("assets/Gojo.txt").text;
        }
    }
}
