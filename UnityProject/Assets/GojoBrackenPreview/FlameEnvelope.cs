using UnityEngine;

namespace GojoBracken.Authoring
{
    // A continuous authoring-only shell: particles add detail, not the main silhouette.
    public sealed class FlameEnvelope : MonoBehaviour
    {
        private Mesh mesh;
        private Material material;
        private MeshRenderer shellRenderer;
        private MaterialPropertyBlock properties;

        public static FlameEnvelope Create(Transform parent)
        {
            var shader = Shader.Find("GojoBracken/PreviewFlameEnvelope");
            if (shader == null) return null;
            var root = new GameObject("Continuous flame envelope - authoring only");
            root.transform.SetParent(parent, false);
            var shell = root.AddComponent<FlameEnvelope>();
            shell.mesh = BuildMesh();
            root.AddComponent<MeshFilter>().sharedMesh = shell.mesh;
            shell.shellRenderer = root.AddComponent<MeshRenderer>();
            shell.material = new Material(shader);
            shell.shellRenderer.sharedMaterial = shell.material;
            shell.shellRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shell.shellRenderer.receiveShadows = false;
            shell.properties = new MaterialPropertyBlock();
            return shell;
        }

        public void SetStrength(float anger, float intensity = 1f)
        {
            properties.SetFloat("_Strength", Mathf.Clamp01(anger));
            properties.SetFloat("_Intensity", Mathf.Clamp(intensity, 0f, 3f));
            shellRenderer.SetPropertyBlock(properties);
        }

        public static Mesh BuildMesh()
        {
            const int rings = 40;
            const int sides = 64;
            var vertices = new Vector3[(rings + 1) * (sides + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[rings * sides * 6];
            for (int ring = 0; ring <= rings; ring++)
            {
                float height = ring / (float)rings;
                // Broad around the body, closing into one tall flame tip.
                float radius = 0.72f * Mathf.Pow(Mathf.Max(0.0001f, Mathf.Sin(Mathf.PI * (0.12f + height * 0.88f))), 0.75f);
                for (int side = 0; side <= sides; side++)
                {
                    float angle = side / (float)sides * Mathf.PI * 2f;
                    int index = ring * (sides + 1) + side;
                    vertices[index] = new Vector3(Mathf.Cos(angle) * radius, height * 3.0f, Mathf.Sin(angle) * radius);
                    uv[index] = new Vector2(side / (float)sides, height);
                    if (ring == rings || side == sides) continue;
                    int next = index + sides + 1;
                    int triangle = (ring * sides + side) * 6;
                    triangles[triangle] = index;
                    triangles[triangle + 1] = next;
                    triangles[triangle + 2] = index + 1;
                    triangles[triangle + 3] = index + 1;
                    triangles[triangle + 4] = next;
                    triangles[triangle + 5] = next + 1;
                }
            }
            var result = new Mesh { name = "Continuous flame study shell" };
            result.vertices = vertices;
            result.uv = uv;
            result.triangles = triangles;
            result.RecalculateNormals();
            result.bounds = new Bounds(new Vector3(0f, 1.75f, 0f), new Vector3(4f, 5f, 4f));
            return result;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                Destroy(mesh);
                Destroy(material);
            }
            else
            {
                DestroyImmediate(mesh);
                DestroyImmediate(material);
            }
        }
    }
}
