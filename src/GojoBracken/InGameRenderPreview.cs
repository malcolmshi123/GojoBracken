using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GojoBracken
{
    internal sealed class InGameRenderPreview : IDisposable
    {
        internal const string BundleName = "gojobracken-hdrp-material-check";
        private const float AuraReferenceHeight = 2.10f;
        private const string RequestName = "gojobracken-preview.request";
        private readonly string directory;
        private readonly Func<Camera> soloCamera;
        private readonly Action<string> info;
        private readonly Action<string> warning;
        private AssetBundle bundle;
        private GameObject anchor;
        private GameObject visual;
        private RaycastHit deck;
        private AudioClip silence;
        private Camera camera;
        private int sceneHandle;
        private bool waitingLogged;
        private BrackenCosmeticController controller;

        internal InGameRenderPreview(string directory, Func<Camera> soloCamera, Action<string> info, Action<string> warning)
        {
            this.directory = directory;
            this.soloCamera = soloCamera;
            this.info = info;
            this.warning = warning;
            info("[RenderPreview] Ready for calm, full, height, or clear requests beside the plugin DLL.");
        }

        internal static Func<Camera> BindSoloCamera(Assembly game)
        {
            var round = game.GetType("StartOfRound", true);
            var player = game.GetType("GameNetcodeStuff.PlayerControllerB", true);
            var instance = round.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            const BindingFlags fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var connected = round.GetField("connectedPlayersAmount", fields);
            var local = round.GetField("localPlayerController", fields);
            var gameplay = player.GetField("gameplayCamera", fields);
            if (instance?.PropertyType != round || connected?.FieldType != typeof(int)
                || local?.FieldType != player || gameplay?.FieldType != typeof(Camera))
                throw new InvalidOperationException("The v81 solo camera members are unavailable.");

            return () =>
            {
                var current = instance.GetValue(null) as Component;
                if (current == null || (int)connected.GetValue(current) != 0) return null;
                var localPlayer = local.GetValue(current) as Component;
                return localPlayer == null ? null : gameplay.GetValue(localPlayer) as Camera;
            };
        }

        internal string RequestPath => Path.Combine(directory, RequestName);

        internal void Poll()
        {
            string request = RequestPath;
            try
            {
                if (anchor != null && (camera == null || !camera.isActiveAndEnabled || camera != soloCamera()
                    || SceneManager.GetActiveScene().handle != sceneHandle))
                    Clear();
                if (!File.Exists(request))
                {
                    waitingLogged = false;
                    return;
                }

                string command = File.ReadAllText(request).Trim().ToLowerInvariant();
                if (!waitingLogged) info("[RenderPreview] Found request file.");
                if (command == "calm" || command == "full")
                {
                    var current = soloCamera();
                    if (current == null || !current.isActiveAndEnabled)
                    {
                        if (!waitingLogged) info("[RenderPreview] Request queued. Waiting for a solo gameplay camera.");
                        waitingLogged = true;
                        return;
                    }
                }
                waitingLogged = false;
                File.Delete(request);
                switch (command)
                {
                    case "calm": Show(0f); break;
                    case "full": Show(12f); break;
                    case "clear": Clear(); info("[RenderPreview] Cleared."); break;
                    default:
                        if (command.StartsWith("height ", StringComparison.Ordinal)) ChangeHeight(command.Substring(7));
                        else warning("[RenderPreview] Expected calm, full, height <metres>, or clear.");
                        break;
                }
            }
            catch (IOException error) { warning("[RenderPreview] File operation failed: " + error.Message); }
            catch (Exception error) { Clear(); warning("[RenderPreview] Preview failed: " + error.Message); }
        }

        private void Show(float anger)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null || pipeline.GetType().FullName
                != "UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset")
                throw new InvalidOperationException("An active HDRP camera is required.");

            if (anchor == null) Create();
            controller.Apply(new BrackenObservation(0, anger, false, false, false, false), true, 10f);
            info($"[RenderPreview] Applied anger {anger:0}/12, aura {controller.LastFrame.AuraLevel:0.00}, " +
                $"scene={SceneManager.GetActiveScene().name}, HDRP, {QualitySettings.activeColorSpace}, {SystemInfo.graphicsDeviceType}.");
        }

        private void Create()
        {
            camera = soloCamera();
            if (camera == null || !camera.isActiveAndEnabled)
                throw new InvalidOperationException("No active solo gameplay camera. Enter a solo ship and retry.");
            var forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < .01f)
                throw new InvalidOperationException("Look horizontally before placing the preview.");
            forward.Normalize();

            if (bundle == null)
                bundle = AssetBundle.LoadFromFile(Path.Combine(directory, BundleName));
            if (bundle == null)
                throw new InvalidOperationException("Could not open " + BundleName + ".");
            var model = bundle.LoadAsset<GameObject>("gojo");
            var aura = bundle.LoadAsset<GameObject>("aura");
            BrackenModelAttachment.Validate(model);
            if (aura == null)
                throw new InvalidOperationException("The preview bundle is missing the aura.");

            Vector3 estimatedFeet = camera.transform.position + forward * 3.5f - Vector3.up * 1.55f;
            deck = FindDeck(estimatedFeet);
            sceneHandle = SceneManager.GetActiveScene().handle;
            anchor = new GameObject("GojoBracken local HDRP preview");
            anchor.transform.SetPositionAndRotation(estimatedFeet,
                Quaternion.LookRotation(-forward, Vector3.up));
            visual = new GameObject("Owned preview model");
            visual.transform.SetParent(anchor.transform, false);
            UnityEngine.Object.Instantiate(model, visual.transform, false);
            GojoLocomotion.TryCreate(anchor.transform, visual.transform, out var facingStatus);
            float height = AlignHeight(3.000f);
            silence = AudioClip.Create("Silent render fixture", 480, 1, 48000, false);
            if (!BrackenCosmeticController.TryCreate(anchor.transform, BrackenV81Presentation.SupportedMvid,
                aura, silence, 12f, 1f, out controller, out var error))
                throw new InvalidOperationException(error);
            ScaleAura(height);
            info("[RenderPreview] Placed local model: " + facingStatus);
        }

        private void ChangeHeight(string value)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float height)
                || float.IsNaN(height) || height < 1f || height > 3f)
            {
                warning("[RenderPreview] Height must be between 1.000 and 3.000 m.");
                return;
            }
            if (anchor == null || visual == null || deck.collider == null)
            {
                warning("[RenderPreview] Place Gojo with calm before changing height.");
                return;
            }
            ScaleAura(AlignHeight(height));
        }

        private float AlignHeight(float targetHeight)
        {
            float beforeY = anchor.transform.position.y;
            var standing = MeasureStandingMesh(visual);
            float originalHeight = standing.height;
            float scale = 1f;
            if (Mathf.Abs(originalHeight - targetHeight) > .005f)
            {
                scale = targetHeight / originalHeight;
                visual.transform.localScale *= scale;
                standing = MeasureStandingMesh(visual);
            }
            info(FormattableString.Invariant(
                $"[RenderPreview] Standing height: original={originalHeight:F3}, target={targetHeight:F3}, final={standing.height:F3} m, scale={scale:F3}."));
            float lowestMeshY = standing.bottom;
            float correction = deck.point.y - lowestMeshY + .005f;
            anchor.transform.position += Vector3.up * correction;
            info(FormattableString.Invariant(
                $"[RenderPreview] Floor alignment: collider={deck.collider.name}, deckY={deck.point.y:F3}, anchorYBefore={beforeY:F3}, meshBottomOffset={lowestMeshY - beforeY:F3}, correction={correction:F3} m."));
            return standing.height;
        }

        private void ScaleAura(float height)
        {
            float scale = Mathf.Min(height / AuraReferenceHeight, 1.25f);
            controller.SetEffectScale(scale);
            info(FormattableString.Invariant($"[RenderPreview] Aura geometry scale: {scale:F3} at standing height {height:F3} m."));
        }

        private RaycastHit FindDeck(Vector3 estimatedFeet)
        {
            const float aboveFeet = .65f;
            const float belowFeet = 1.05f;
            const float minimumUpNormal = .8f;
            const int layerMask = Physics.DefaultRaycastLayers;
            var origin = estimatedFeet + Vector3.up * aboveFeet;
            var hits = Physics.RaycastAll(origin, Vector3.down, aboveFeet + belowFeet,
                layerMask, QueryTriggerInteraction.Ignore);
            info(FormattableString.Invariant(
                $"[RenderPreview] Floor probe: origin=({origin.x:F3}, {origin.y:F3}, {origin.z:F3}), down={aboveFeet + belowFeet:F3} m, estimatedFeetY={estimatedFeet.y:F3}, mask=0x{layerMask:X8}, triggers=ignored, minimumNormalY={minimumUpNormal:F2}, hits={hits.Length}."));

            float closest = float.PositiveInfinity;
            RaycastHit deck = default;
            foreach (var hit in hits)
            {
                float error = Mathf.Abs(hit.point.y - estimatedFeet.y);
                if (hit.normal.y < minimumUpNormal || error >= closest) continue;
                closest = error;
                deck = hit;
            }

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < Math.Min(hits.Length, 12); i++)
            {
                var hit = hits[i];
                string result = hit.collider == deck.collider && hit.point == deck.point ? "selected"
                    : hit.normal.y < minimumUpNormal ? "surface too steep" : "farther from estimated feet";
                LogFloorHit("primary", hit, estimatedFeet.y, result);
            }
            if (hits.Length > 12) info($"[RenderPreview] Floor probe: {hits.Length - 12} additional primary hits omitted.");

            if (deck.collider == null)
            {
                LogNearbySurfaces(estimatedFeet, aboveFeet, belowFeet, minimumUpNormal, layerMask);
                throw new InvalidOperationException("No walkable surface near the preview's estimated foot height.");
            }
            return deck;
        }

        private void LogNearbySurfaces(Vector3 estimatedFeet, float aboveFeet, float belowFeet, float minimumUpNormal, int layerMask)
        {
            const float height = 1.5f;
            const float distance = 4.5f;
            var origin = estimatedFeet + Vector3.up * height;
            var hits = Physics.RaycastAll(origin, Vector3.down, distance, Physics.AllLayers, QueryTriggerInteraction.Collide);
            info(FormattableString.Invariant(
                $"[RenderPreview] Floor diagnostic: origin=({origin.x:F3}, {origin.y:F3}, {origin.z:F3}), down={distance:F3} m, all layers and triggers, hits={hits.Length}."));
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < Math.Min(hits.Length, 12); i++)
            {
                var hit = hits[i];
                string reasons = "";
                float offset = hit.point.y - estimatedFeet.y;
                if (offset > aboveFeet || offset < -belowFeet) reasons += "outside primary height range, ";
                if ((layerMask & (1 << hit.collider.gameObject.layer)) == 0) reasons += "layer excluded by primary mask, ";
                if (hit.collider.isTrigger) reasons += "trigger, ";
                if (hit.normal.y < minimumUpNormal) reasons += "surface too steep, ";
                LogFloorHit("diagnostic", hit, estimatedFeet.y,
                    reasons.Length == 0 ? "eligible by filters but absent from primary results" : reasons.TrimEnd(',', ' '));
            }
            if (hits.Length > 12) info($"[RenderPreview] Floor diagnostic: {hits.Length - 12} additional hits omitted.");
        }

        private void LogFloorHit(string probe, RaycastHit hit, float estimatedY, string result)
        {
            int layer = hit.collider.gameObject.layer;
            info(FormattableString.Invariant(
                $"[RenderPreview] Floor {probe} hit: collider={hit.collider.name}, layer={layer} ({LayerMask.LayerToName(layer)}), y={hit.point.y:F3}, offset={hit.point.y - estimatedY:F3} m, normalY={hit.normal.y:F3}, trigger={hit.collider.isTrigger}, result={result}."));
        }

        private static (float bottom, float height) MeasureStandingMesh(GameObject visual)
        {
            float lowest = float.PositiveInfinity;
            float highest = float.NegativeInfinity;
            var baked = new Mesh();
            var vertices = new System.Collections.Generic.List<Vector3>();
            try
            {
                foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    if (!skin.enabled) continue;
                    skin.BakeMesh(baked, true);
                    baked.GetVertices(vertices);
                    foreach (var vertex in vertices)
                    {
                        float y = skin.transform.TransformPoint(vertex).y;
                        lowest = Mathf.Min(lowest, y);
                        highest = Mathf.Max(highest, y);
                    }
                }
            }
            finally { UnityEngine.Object.Destroy(baked); }

            float height = highest - lowest;
            if (float.IsNaN(height) || float.IsInfinity(height) || height <= .01f)
                throw new InvalidOperationException("Could not measure the Gojo mesh for deck alignment.");
            return (lowest, height);
        }

        private void Clear()
        {
            if (anchor != null)
            {
                anchor.SetActive(false);
                UnityEngine.Object.Destroy(anchor);
            }
            if (silence != null) UnityEngine.Object.Destroy(silence);
            anchor = null;
            visual = null;
            deck = default;
            silence = null;
            camera = null;
            controller = null;
        }

        public void Dispose()
        {
            Clear();
            if (bundle != null) bundle.Unload(true);
            bundle = null;
        }
    }

    // BepInEx's manager can be disabled during startup. The preview follows the game session instead.
    internal sealed class InGameRenderPreviewRunner : MonoBehaviour
    {
        private static InGameRenderPreviewRunner instance;
        private InGameRenderPreview preview;
        private Action<string> info;
        private float nextPoll;
        private float nextHeartbeat;
        private int pollCount;

        private static Action<ScriptableRenderContext, Camera> cameraRendered;

        internal static void StartPreview(string directory, Func<Camera> soloCamera,
            Action<string> info, Action<string> warning)
        {
            if (cameraRendered != null) return;
            float nextAttempt = 0f;
            cameraRendered = (_, renderedCamera) =>
            {
                if (instance != null && instance.isActiveAndEnabled) return;
                float now = Time.realtimeSinceStartup;
                if (now < nextAttempt || renderedCamera == null || renderedCamera.cameraType != CameraType.Game) return;

                try
                {
                    if (renderedCamera != soloCamera()) return;
                    nextAttempt = now + 1f;
                    if (instance != null)
                    {
                        Destroy(instance.gameObject);
                        return;
                    }

                    var host = new GameObject("GojoBracken render preview runner");
                    DontDestroyOnLoad(host);
                    instance = host.AddComponent<InGameRenderPreviewRunner>();
                    instance.info = info;
                    instance.preview = new InGameRenderPreview(directory, soloCamera, info, warning);
                    SceneManager.sceneLoaded += instance.OnSceneLoaded;
                    info($"[RenderPreview] Solo gameplay camera rendered in {SceneManager.GetActiveScene().name}. " +
                        $"Runner {instance.GetInstanceID()} watching: {instance.preview.RequestPath}");
                }
                catch (Exception error)
                {
                    nextAttempt = now + 1f;
                    warning("[RenderPreview] Camera bootstrap failed: " + error.Message);
                }
            };

            RenderPipelineManager.endCameraRendering += cameraRendered;
            Application.quitting += StopBootstrap;
            info("[RenderPreview] Armed. Waiting for the solo gameplay camera to render.");
        }

        private static void StopBootstrap()
        {
            RenderPipelineManager.endCameraRendering -= cameraRendered;
            Application.quitting -= StopBootstrap;
            cameraRendered = null;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (preview == null || now < nextPoll) return;
            nextPoll = now + .5f;
            pollCount++;
            if (pollCount == 1 || now >= nextHeartbeat)
            {
                nextHeartbeat = now + 15f;
                info($"[RenderPreview] Runner {GetInstanceID()} poll {pollCount}, " +
                    $"active scene={SceneManager.GetActiveScene().name}, request present={File.Exists(preview.RequestPath)}.");
            }
            preview.Poll();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            info($"[RenderPreview] Runner {GetInstanceID()} saw scene load: {scene.name} ({mode}). Polls so far: {pollCount}.");
            nextHeartbeat = 0f;
        }

        private void OnDisable()
        {
            info?.Invoke($"[RenderPreview] Runner {GetInstanceID()} disabled after {pollCount} polls, active scene={SceneManager.GetActiveScene().name}.");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            info?.Invoke($"[RenderPreview] Runner {GetInstanceID()} destroyed after {pollCount} polls.");
            preview?.Dispose();
            if (instance == this) instance = null;
        }
    }
}
