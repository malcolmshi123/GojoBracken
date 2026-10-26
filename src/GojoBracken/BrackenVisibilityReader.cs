using System;
using System.Reflection;
using UnityEngine;

namespace GojoBracken
{
    internal readonly struct BrackenVisibility
    {
        internal readonly bool Visible;
        internal readonly int Layer;
        internal BrackenVisibility(bool visible, int layer) { Visible = visible; Layer = layer; }
    }

    // Read the game's cached body arrays, never enumerate descendants (which can include carried corpses).
    internal sealed class BrackenVisibilityReader
    {
        private readonly FieldInfo skins;
        private readonly FieldInfo meshes;

        internal BrackenVisibilityReader(Type type)
        {
            skins = Bind(type, "skinnedMeshRenderers", typeof(SkinnedMeshRenderer[]));
            meshes = Bind(type, "meshRenderers", typeof(MeshRenderer[]));
        }

        // EnemyAI.Start initializes these arrays. A newly spawned instance can be found before Start.
        internal bool IsReady(Component enemy)
        {
            if (skins == null || meshes == null) return true; // Let TryRead report permanent schema failure.
            return skins.GetValue(enemy) != null && meshes.GetValue(enemy) != null;
        }

        internal bool TryRead(Component enemy, out BrackenVisibility visibility, out string error)
        {
            visibility = default;
            error = null;
            try
            {
                if (enemy == null || skins == null || meshes == null)
                    throw new InvalidOperationException("Bracken renderer fields are missing or have unexpected types.");
                var skinArray = skins.GetValue(enemy) as SkinnedMeshRenderer[];
                var meshArray = meshes.GetValue(enemy) as MeshRenderer[];
                if (skinArray == null || meshArray == null || skinArray.Length + meshArray.Length == 0)
                    throw new InvalidOperationException("Bracken body renderer arrays are not initialized or empty.");
                bool visible = false;
                int layer = -1;
                int fallbackLayer = -1;
                Accumulate(skinArray, enemy.transform, ref visible, ref layer, ref fallbackLayer);
                Accumulate(meshArray, enemy.transform, ref visible, ref layer, ref fallbackLayer);
                visibility = new BrackenVisibility(visible, visible ? layer : fallbackLayer);
                return true;
            }
            catch (Exception exception)
            {
                error = "Bracken visibility unavailable: " + exception.Message;
                return false;
            }
        }

        private static void Accumulate(Renderer[] group, Transform enemy, ref bool visible, ref int layer, ref int fallbackLayer)
        {
            foreach (var renderer in group)
            {
                if (renderer == null || !renderer.transform.IsChildOf(enemy))
                    throw new InvalidOperationException("Bracken body renderer is missing or outside its hierarchy.");
                if (fallbackLayer < 0) fallbackLayer = renderer.gameObject.layer;
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.forceRenderingOff) continue;
                int current = renderer.gameObject.layer;
                if (layer >= 0 && layer != current)
                    throw new InvalidOperationException("Visible Bracken body renderers use mixed layers; no safe single aura layer.");
                layer = current;
                visible = true;
            }
        }

        private static FieldInfo Bind(Type type, string name, Type expected)
        {
            for (; type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field.FieldType == expected ? field : null;
            }
            return null;
        }
    }
}
