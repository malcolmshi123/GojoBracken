using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace GojoBracken.Editor
{
    public static class AuditGojoMaterials
    {
        [Serializable] private sealed class Entry
        {
            public string prefab, renderer, material, shader, pipelineTag;
            public bool shaderSupported, shaderHasError;
            public int renderQueue;
            public string[] textures;
        }
        [Serializable] private sealed class Report
        {
            public string kind = "authoring-material-audit", unityVersion, generatedUtc, pipeline, colorSpace;
            public string limits = "Authoring renderer only. Empty shader pipeline tags and isSupported do not certify game compatibility. No assets modified.";
            public Entry[] materials;
        }

        [MenuItem("GojoBracken/Audit native prefab materials (read only)")]
        public static void Run()
        {
            if (Application.isPlaying || Application.unityVersion != "2022.3.62f2")
                throw new InvalidOperationException("Use the pinned Editor outside Play mode.");
            var entries = new List<Entry>();
            foreach (string path in new[] { "Assets/LocalModelStudy/NativeGojo/Gojo.prefab", "Assets/GojoBracken/Prefabs/Aura.prefab" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) throw new InvalidOperationException("Missing local prefab: " + path);
                foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.sharedMaterials.Length == 0) throw new InvalidOperationException("No materials: " + renderer.name);
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || material.shader == null) throw new InvalidOperationException("Missing material/shader: " + renderer.name);
                        var textures = new List<string>();
                        foreach (string property in material.GetTexturePropertyNames())
                        {
                            var texture = material.GetTexture(property);
                            textures.Add(property + "=" + (texture != null ? AssetDatabase.GetAssetPath(texture) : "<none>"));
                        }
                        entries.Add(new Entry { prefab = path, renderer = renderer.name,
                            material = AssetDatabase.GetAssetPath(material), shader = material.shader.name,
                            pipelineTag = material.GetTag("RenderPipeline", false, "<untagged>"),
                            shaderSupported = material.shader.isSupported, shaderHasError = ShaderUtil.ShaderHasError(material.shader),
                            renderQueue = material.renderQueue, textures = textures.ToArray() });
                    }
                }
            }
            var pipeline = GraphicsSettings.currentRenderPipeline;
            var report = new Report { unityVersion = Application.unityVersion, generatedUtc = DateTime.UtcNow.ToString("o"),
                pipeline = pipeline != null ? pipeline.GetType().FullName : "built-in authoring renderer",
                colorSpace = QualitySettings.activeColorSpace.ToString(), materials = entries.ToArray() };
            string output = Path.Combine(Directory.GetParent(Application.dataPath).Parent.FullName, "material-audit-" + Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("MATERIAL AUDIT PASS: " + output);
        }
    }
}
