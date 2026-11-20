using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

[Serializable]
public sealed class BundleSnapshot
{
    public string[] aliases;
    public AudioSnapshot[] audio;
    public NamedDigest[] text, aura;
    private static bool readingRelease;
    [Serializable] public sealed class NamedDigest { public string name, hash, details; }
    [Serializable] public sealed class AudioSnapshot { public string name, hash; public int samples, channels, frequency; }
    public static string Hash(byte[] data)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
    }
    public static BundleSnapshot Capture(AssetBundle bundle, bool source = false)
    {
        readingRelease = source;
        var result = new BundleSnapshot { aliases = bundle.GetAllAssetNames().OrderBy(n => n, StringComparer.Ordinal).ToArray() };
        result.audio = new[] { "domain_expansion", "aggro_music" }.Select(name =>
        {
            var clip = bundle.LoadAsset<AudioClip>(name);
            GojoModelChecks.Require(clip != null, "Missing released audio: " + name);
            clip.LoadAudioData();
            var samples = new float[clip.samples * clip.channels];
            GojoModelChecks.Require(clip.GetData(samples, 0), "Cannot read audio samples: " + name);
            var bytes = new byte[samples.Length * 4]; Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return new AudioSnapshot { name = name, hash = Hash(bytes), samples = clip.samples, channels = clip.channels, frequency = clip.frequency };
        }).ToArray();
        result.text = new[] { "asset_credits", "voice_source", "uniform_voice_boost" }.Select(name => new NamedDigest
        {
            name = name,
            hash = Hash(bundle.LoadAsset<TextAsset>(name).bytes)
        }).ToArray();
        var root = bundle.LoadAsset<GameObject>("aura");
        result.aura = root.GetComponentsInChildren<Transform>(true).SelectMany(node => new UnityEngine.Object[] { node.gameObject }.Concat(node.GetComponents<Component>())).Select(component =>
        {
            string details = Serialize(component, root.transform);
            return new NamedDigest
            {
                name = AnimationUtility.CalculateTransformPath(component is GameObject go ? go.transform : ((Component)component).transform, root.transform) + ":" + component.GetType().FullName,
                hash = Hash(Encoding.UTF8.GetBytes(details)),
                details = details
            };
        }).OrderBy(item => item.name, StringComparer.Ordinal).ToArray();
        return result;
    }
    private static string Serialize(UnityEngine.Object value, Transform root)
    {
        var result = new StringBuilder();
        var serialized = new SerializedObject(value);
        var property = serialized.GetIterator();
        bool enterChildren = true;
        while (property.Next(enterChildren))
        {
            enterChildren = property.propertyType != SerializedPropertyType.ObjectReference;
            string path = property.propertyPath;
            if (new[] { "m_Name", "m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset", "m_NavMeshLayer", "m_StaticEditorFlags", "m_Icon" }.Any(name => path == name || path.StartsWith(name + ".", StringComparison.Ordinal))) continue;
            string data;
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic: continue;
                case SerializedPropertyType.Integer: data = property.longValue.ToString(CultureInfo.InvariantCulture); break;
                case SerializedPropertyType.Boolean: data = property.boolValue.ToString(); break;
                case SerializedPropertyType.Float: data = property.doubleValue.ToString("R", CultureInfo.InvariantCulture); break;
                case SerializedPropertyType.String: data = Convert.ToBase64String(Encoding.UTF8.GetBytes(property.stringValue)); break;
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character: data = property.intValue.ToString(CultureInfo.InvariantCulture); break;
                case SerializedPropertyType.ObjectReference: data = Reference(property.objectReferenceValue, root); break;
                case SerializedPropertyType.Color: var c = property.colorValue; data = Numbers(c.r, c.g, c.b, c.a); break;
                case SerializedPropertyType.Vector2: var v2 = property.vector2Value; data = Numbers(v2.x, v2.y); break;
                case SerializedPropertyType.Vector3: var v3 = property.vector3Value; data = Numbers(v3.x, v3.y, v3.z); break;
                case SerializedPropertyType.Vector4: var v4 = property.vector4Value; data = Numbers(v4.x, v4.y, v4.z, v4.w); break;
                case SerializedPropertyType.Quaternion: var q = property.quaternionValue; data = Numbers(q.x, q.y, q.z, q.w); break;
                case SerializedPropertyType.Rect: var r = property.rectValue; data = Numbers(r.x, r.y, r.width, r.height); break;
                case SerializedPropertyType.Bounds: var b = property.boundsValue; data = Numbers(b.center.x, b.center.y, b.center.z, b.extents.x, b.extents.y, b.extents.z); break;
                case SerializedPropertyType.AnimationCurve:
                    var curve = property.animationCurveValue;
                    data = curve.preWrapMode + ":" + curve.postWrapMode + ":" + string.Join(";", curve.keys.Select(k => Numbers(k.time, k.value, k.inTangent, k.outTangent, k.inWeight, k.outWeight) + ":" + k.weightedMode)); break;
                case SerializedPropertyType.Gradient:
                    var info = typeof(SerializedProperty).GetProperty("gradientValue", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    var gradient = (Gradient)info.GetValue(property, null);
                    data = gradient.mode + ":" + string.Join(";", gradient.colorKeys.Select(k => Numbers(k.time, k.color.r, k.color.g, k.color.b, k.color.a))) + ":" + string.Join(";", gradient.alphaKeys.Select(k => Numbers(k.time, k.alpha))); break;
                case SerializedPropertyType.Hash128: data = property.hash128Value.ToString(); break;
                default: throw new InvalidOperationException("Unmapped aura property: " + path + " " + property.propertyType);
            }
            result.Append(path).Append('=').Append(data).Append('\n');
        }
        return result.ToString();
    }
    private static string Numbers(params float[] values) => string.Join(",", values.Select(v => v.ToString("R", CultureInfo.InvariantCulture)));
    private static string Reference(UnityEngine.Object value, Transform root)
    {
        if (value == null) return "null";
        if (value is Shader) return "Shader:" + value.name;
        if (value is Texture2D texture)
        {
            string hash;
            if (readingRelease) hash = BuildGojoBundle.ReadTextureResources().Single(item => item.name == texture.name).sha256;
            else
            {
                GojoModelChecks.Require(texture.isReadable, "Transferred aura texture must be readable for exact verification");
                hash = Hash(texture.GetRawTextureData<byte>().ToArray());
            }
            return "Texture:" + texture.width + ":" + texture.height + ":" + texture.mipmapCount + ":" + texture.format + ":" + texture.isDataSRGB + ":" + hash;
        }
        if (value is Material) return "Material:" + Hash(Encoding.UTF8.GetBytes(Serialize(value, root)));
        if (value is Mesh mesh)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    foreach (var p in mesh.vertices) { writer.Write(p.x); writer.Write(p.y); writer.Write(p.z); }
                    foreach (var p in mesh.normals) { writer.Write(p.x); writer.Write(p.y); writer.Write(p.z); }
                    foreach (var p in mesh.uv) { writer.Write(p.x); writer.Write(p.y); }
                    foreach (var p in mesh.colors) { writer.Write(p.r); writer.Write(p.g); writer.Write(p.b); writer.Write(p.a); }
                    foreach (int i in mesh.triangles) writer.Write(i);
                }
                return "Mesh:" + Hash(stream.ToArray());
            }
        }
        if (value is GameObject gameObject) return "GameObject:" + AnimationUtility.CalculateTransformPath(gameObject.transform, root);
        if (value is Component component) return component.GetType().FullName + ":" + AnimationUtility.CalculateTransformPath(component.transform, root);
        throw new InvalidOperationException("Unmapped aura reference: " + value.GetType().FullName + " " + value.name);
    }
}
