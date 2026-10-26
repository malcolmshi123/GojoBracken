using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace GojoBracken
{
    // Opt-in inspection only. Never instantiates, modifies, or activates a game object.
    internal sealed class BrackenRigReport
    {
        private readonly Type enemyType;
        private readonly string directory;
        private readonly string pluginVersion;
        private readonly Action<string> info;
        private readonly Action<string> warning;
        private float nextAttempt;
        private int attempts;
        private bool finished;

        internal BrackenRigReport(Type enemyType, string directory, string pluginVersion,
            Action<string> info, Action<string> warning)
        {
            this.enemyType = enemyType;
            this.directory = directory;
            this.pluginVersion = pluginVersion;
            this.info = info;
            this.warning = warning;
            if (enemyType == null || !typeof(Component).IsAssignableFrom(enemyType))
            {
                finished = true;
                warning("[RigReport] Bracken component type unavailable; no report was exported.");
            }
            else
            {
                info("[RigReport] Looking for an already-loaded Bracken prefab for up to two minutes. No enemy will be spawned.");
            }
        }

        internal void Update(float now)
        {
            if (finished || now < nextAttempt) return;
            nextAttempt = now + 5f;
            attempts++;
            try
            {
                foreach (var item in Resources.FindObjectsOfTypeAll(enemyType))
                {
                    var enemy = item as Component;
                    // Exclude all scene instances, including inactive enemies.
                    if (enemy == null || enemy.gameObject.scene.IsValid()) continue;
                    string report = Describe(enemy, pluginVersion);
                    Directory.CreateDirectory(directory);
                    string filename = "bracken-rig-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
                        + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".txt";
                    string path = Path.Combine(directory, filename);
                    File.WriteAllText(path, report, new UTF8Encoding(false));
                    finished = true;
                    info("[RigReport] Exported loaded prefab report: " + path);
                    return;
                }
                if (attempts == 1 || attempts % 5 == 0)
                    info("[RigReport] Scan " + attempts + "/24 complete; no loaded prefab asset found.");
                if (attempts >= 24)
                {
                    finished = true;
                    warning("[RigReport] No loaded Bracken prefab was found. No report was exported; do not search for an enemy. Return this message for the next inspection approach.");
                }
            }
            catch (Exception exception)
            {
                finished = true;
                warning("[RigReport] Export stopped after " + exception.GetType().Name + ": " + exception.Message);
            }
        }

        internal void Cancel(string reason)
        {
            if (finished) return;
            finished = true;
            warning("[RigReport] Search cancelled after " + attempts + "/24 scans: " + reason + ". No report was exported.");
        }
        internal static string Describe(Component enemy, string version)
        {
            var root = enemy.transform;
            var text = new StringBuilder();
            text.AppendLine("GojoBracken loaded-prefab rig report; schema=1");
            text.AppendLine("plugin=" + Clean(version) + "; unity=" + Application.unityVersion
                + "; application=" + Clean(Application.version));
            text.AppendLine("utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            text.AppendLine("source=loaded prefab asset; not a spawned enemy or an observed animation pose");
            text.AppendLine("component=" + enemy.GetType().FullName + "; gameAssemblyMvid=" + enemy.GetType().Module.ModuleVersionId);
            var pipeline = GraphicsSettings.currentRenderPipeline;
            text.AppendLine("pipeline=" + (pipeline == null ? "<null: built-in or not initialized>"
                : pipeline.GetType().FullName + "; assemblyVersion=" + pipeline.GetType().Assembly.GetName().Version));
            var anger = FindField(enemy.GetType(), "angerMeter");
            text.AppendLine("angerFieldType=" + (anger == null ? "<unavailable>" : anger.FieldType.FullName)
                + "; replication and authoritative range are NOT established by this report");
            text.AppendLine();
            text.AppendLine("AUTHORED STATE LABELS (not verified phase meanings)");
            var states = ReadField(enemy, "enemyBehaviourStates") as IEnumerable;
            int stateIndex = 0;
            if (states != null)
                foreach (var state in states)
                    text.AppendLine(stateIndex++ + ": " + Clean(ReadField(state, "name")));
            if (stateIndex == 0) text.AppendLine("<unavailable>");

            text.AppendLine();
            text.AppendLine("TRANSFORMS (prefab-local TRS, sibling-indexed paths)");
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < Math.Min(transforms.Length, 512); i++)
            {
                var node = transforms[i];
                Quaternion r = node.localRotation;
                text.AppendLine(PathOf(root, node) + "; position=" + V(node.localPosition)
                    + "; rotation=(" + F(r.x) + "," + F(r.y) + "," + F(r.z) + "," + F(r.w)
                    + "); scale=" + V(node.localScale) + "; activeSelf=" + node.gameObject.activeSelf);
            }
            if (transforms.Length > 512) text.AppendLine("<hierarchy truncated at 512 transforms>");

            text.AppendLine();
            text.AppendLine("RENDERERS AND BONE REFERENCES (no mesh/texture data exported)");
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                text.AppendLine(PathOf(root, renderer.transform) + "; type=" + renderer.GetType().Name);
                foreach (var material in renderer.sharedMaterials)
                    text.AppendLine("  material=" + (material == null ? "<missing>" : Clean(material.name))
                        + "; shader=" + (material == null || material.shader == null ? "<missing>" : Clean(material.shader.name)));
                var skin = renderer as SkinnedMeshRenderer;
                if (skin == null) continue;
                text.AppendLine("  rootBone=" + PathOf(root, skin.rootBone) + "; vertexCount="
                    + (skin.sharedMesh == null ? "<missing>" : skin.sharedMesh.vertexCount.ToString(CultureInfo.InvariantCulture)));
                var bones = skin.bones;
                for (int i = 0; i < bones.Length; i++)
                    text.AppendLine("  bone[" + i + "]=" + PathOf(root, bones[i]));
            }

            text.AppendLine();
            text.AppendLine("ANIMATORS (controller metadata only; no animation is played)");
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                text.AppendLine(PathOf(root, animator.transform) + "; enabled=" + animator.enabled
                    + "; applyRootMotion=" + animator.applyRootMotion + "; isInitialized=" + animator.isInitialized);
                var avatar = animator.avatar;
                text.AppendLine("  avatar=" + (avatar == null ? "<none>" : Clean(avatar.name)
                    + "; valid=" + avatar.isValid + "; human=" + avatar.isHuman));
                var controller = animator.runtimeAnimatorController;
                text.AppendLine("  controller=" + (controller == null ? "<none>" : Clean(controller.name)));
                if (controller == null) continue;
                foreach (var clip in controller.animationClips)
                    text.AppendLine("  clip=" + (clip == null ? "<missing>" : Clean(clip.name)
                        + "; seconds=" + F(clip.length) + "; humanMotion=" + clip.humanMotion + "; loop=" + clip.isLooping));
                if (!animator.isInitialized)
                {
                    text.AppendLine("  parameters=<unavailable: prefab Animator not initialized; no rebind or playback attempted>");
                    continue;
                }
                try
                {
                    var parameters = animator.parameters;
                    text.AppendLine("  parameterCount=" + parameters.Length + "; prefab metadata may be unavailable before initialization");
                    foreach (var parameter in parameters)
                        text.AppendLine("  parameter=" + Clean(parameter.name) + "; type=" + parameter.type);
                }
                catch (Exception exception)
                {
                    text.AppendLine("  parameters=<unavailable:" + exception.GetType().Name + ">");
                }
            }
            return text.ToString();
        }

        private static FieldInfo FindField(Type type, string name)
        {
            for (; type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            return null;
        }

        private static object ReadField(object target, string name)
        {
            return target == null ? null : FindField(target.GetType(), name)?.GetValue(target);
        }

        private static string PathOf(Transform root, Transform node)
        {
            if (node == null) return "<missing>";
            if (node == root) return ".";
            if (!node.IsChildOf(root)) return "<outside-prefab>/" + Clean(node.name);
            string path = "";
            for (; node != root; node = node.parent)
                path = "/" + Clean(node.name) + "[" + node.GetSiblingIndex() + "]" + path;
            return "." + path;
        }

        private static string Clean(object value)
        {
            return value == null ? "<unavailable>" : Convert.ToString(value, CultureInfo.InvariantCulture)
                .Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static string F(float value) { return value.ToString("R", CultureInfo.InvariantCulture); }
        private static string V(Vector3 value) { return "(" + F(value.x) + "," + F(value.y) + "," + F(value.z) + ")"; }
    }
}
