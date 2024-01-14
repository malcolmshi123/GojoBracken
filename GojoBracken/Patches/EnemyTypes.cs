using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GojoBracken.Patches
{
    [HarmonyPatch]
    internal class EnemyTypes
    {
        [HarmonyPatch(typeof(FlowermanAI), "Start")]
        [HarmonyPostfix]
        public static void SummonGojo(FlowermanAI __instance)
        {
            __instance.creatureAngerVoice.clip = LC_API.BundleAPI.BundleLoader.GetLoadedAsset<AudioClip>("assets/AngerVoice.mp3");
            __instance.crackNeckSFX = LC_API.BundleAPI.BundleLoader.GetLoadedAsset<AudioClip>("assets/CrackNeck.mp3");
            __instance.crackNeckAudio.clip = LC_API.BundleAPI.BundleLoader.GetLoadedAsset<AudioClip>("assets/CrackNeck.mp3");
            GameObject.Destroy(__instance.gameObject.transform.Find("FlowermanModel").Find("LOD1").gameObject.GetComponent<SkinnedMeshRenderer>());
            GameObject Gojo = UnityEngine.Object.Instantiate(LC_API.BundleAPI.BundleLoader.GetLoadedAsset<GameObject>("assets/Gojo.prefab"), __instance.gameObject.transform);
            Gojo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        }
    }
}