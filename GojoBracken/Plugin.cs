using BepInEx;
using HarmonyLib;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GojoBracken
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "FreakyFriday.GojoBracken";
        private const string modName = "Gojo Bracken Mod";
        private const string modVersion = "1.0.0";

        private Harmony harmony;

        private void Awake()
        {
            harmony = new Harmony(modGUID);
            harmony.PatchAll();
            Logger.LogInfo($"The Bracken is now limitless :)");
        }

    }
}