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
        private static readonly (int state, float anger, bool carrying)[] CyclePhases =
        {
            (0, 0f, false),
            (1, 4f, false),
            (2, 12f, false),
            (2, 6f, true),
            (1, 2f, false),
            (2, 12f, false),
            (0, 0f, false)
        };

        private readonly string directory;
        private readonly Func<Camera> soloCamera;
        private readonly Action<string> info;
        private readonly Action<string> warning;
        private AssetBundle bundle;
        private GameObject anchor;
        private GameObject visual;
        private RaycastHit deck;
        private AudioClip previewClip;
        private Camera camera;
        private int sceneHandle;
        private bool waitingLogged;
        private BrackenCosmeticController controller;
        private bool walking;
        private float walkTime, maxAuraOffset;
        private Vector3 walkStart, walkEnd;
        private int cycleIndex = -1, cycleVoiceAtStart;
        private float cycleTime, cycleAggroHold;
        private string cycleAudio;

        internal InGameRenderPreview(string directory, Func<Camera> soloCamera, Action<string> info, Action<string> warning)
        {
            this.directory = directory;
            this.soloCamera = soloCamera;
            this.info = info;
            this.warning = warning;
            info("[RenderPreview] Ready for calm, full, walk, cycle, height, or clear requests beside the plugin DLL.");
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
                if (anchor != null && !IsCurrentSession())
                    Clear();
                if (!File.Exists(request))
                {
                    waitingLogged = false;
                    return;
                }

                string command = File.ReadAllText(request).Trim().ToLowerInvariant();
                if (!waitingLogged) info("[RenderPreview] Found request file.");
                if (command == "calm" || command == "full" || command == "walk" || command == "cycle")
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
                    case "walk": StartWalk(); break;
                    case "cycle": StartCycle(); break;
                    case "clear": Clear(); info("[RenderPreview] Cleared."); break;
                    default:
                        if (command.StartsWith("height ", StringComparison.Ordinal)) ChangeHeight(command.Substring(7));
                        else warning("[RenderPreview] Expected calm, full, walk, cycle, height <metres>, or clear.");
                        break;
                }
            }
            catch (IOException error) { warning("[RenderPreview] File operation failed: " + error.Message); }
            catch (Exception error) { Clear(); warning("[RenderPreview] Preview failed: " + error.Message); }
        }

        private void Show(float anger, bool testTone = false)
        {
            walking = false;
            cycleIndex = -1;
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == null || pipeline.GetType().FullName
                != "UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset")
                throw new InvalidOperationException("An active HDRP camera is required.");

            if (anchor == null) Create(testTone);
            controller.Apply(new BrackenObservation(0, anger, false, false, false, false), true, 10f);
            info($"[RenderPreview] Applied anger {anger:0}/12, aura {controller.LastFrame.AuraLevel:0.00}, " +
                $"scene={SceneManager.GetActiveScene().name}, HDRP, {QualitySettings.activeColorSpace}, {SystemInfo.graphicsDeviceType}.");
        }

        private void Create(bool testTone)
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
            BrackenModelAttachment.UseStandingPose(visual.transform);
            float height = AlignHeight(3.000f);
            var bundledVoice = testTone && bundle.Contains("voice_source")
                ? bundle.LoadAsset<AudioClip>(GojoAssetContract.Voice) : null;
            if (testTone && bundle.Contains("voice_source") && (bundledVoice == null || bundledVoice.samples <= 0))
                throw new InvalidOperationException("The preview bundle declares a voice source but has no valid domain_expansion clip.");

            if (bundledVoice == null) previewClip = CreatePreviewClip(testTone);
            var bundledMusic = bundledVoice != null ? bundle.LoadAsset<AudioClip>(GojoAssetContract.Music) : null;
            if (bundledMusic != null) info("[RenderPreview] Loaded aggro music for the phase cycle.");
            cycleAudio = bundledVoice == null ? "synthetic tone" : "imported voice clip";
            cycleAggroHold = bundledVoice == null ? 2.5f : Mathf.Max(2.5f, bundledVoice.length + .25f);
            if (!BrackenCosmeticController.TryCreate(anchor.transform, BrackenV81Presentation.SupportedMvid,
                aura, bundledVoice ?? previewClip, 12f, 1f, out controller, out var error, bundledMusic))
                throw new InvalidOperationException(error);
            ScaleAura(height);
            info("[RenderPreview] Placed local model: fixed standing pose.");
        }

        private static AudioClip CreatePreviewClip(bool testTone)
        {
            const int sampleRate = 48000;
            if (!testTone) return AudioClip.Create("Silent render fixture", 480, 1, sampleRate, false);

            var samples = new float[sampleRate / 2];
            for (int i = 0; i < samples.Length; i++)
                samples[i] = Mathf.Sin(2f * Mathf.PI * 440f * i / sampleRate) * .08f
                    * Mathf.Sin(Mathf.PI * i / samples.Length);

            var clip = AudioClip.Create("Synthetic aggro test tone", samples.Length, 1, sampleRate, false);
            if (clip.SetData(samples, 0)) return clip;
            UnityEngine.Object.Destroy(clip);
            throw new InvalidOperationException("Could not initialize the preview test tone.");
        }

        private void StartCycle()
        {
            Clear();
            Show(0f, testTone: true);
            cycleIndex = 0;
            cycleTime = -4f;
            cycleVoiceAtStart = 0;
            info($"[RenderPreview] Phase cycle queued. Audio source: {cycleAudio}. Listen on each aggro entry.");
        }

        internal void TickCycle(float deltaTime)
        {
            if (cycleIndex < 0 || deltaTime <= 0f) return;

            try
            {
                if (!IsCurrentSession())
                {
                    Clear();
                    return;
                }

                float step = Mathf.Min(deltaTime, .1f);
                cycleTime += step;
                if (cycleTime <= 0f) return;

                var phase = CyclePhases[cycleIndex];
                controller.Apply(new BrackenObservation(phase.state, phase.anger, phase.state == 2,
                    phase.carrying, false, false), true, step);
                if (cycleTime < (phase.state == 2 && !phase.carrying ? cycleAggroHold : 2.5f)) return;

                int voiceTotal = controller.VoiceRequests;
                info(FormattableString.Invariant(
                    $"[RenderPreview] Phase sample: {controller.LastFrame.Phase}, anger={phase.anger:0}/12, aura={controller.LastFrame.AuraLevel:F2}, new voice requests={voiceTotal - cycleVoiceAtStart}."));
                cycleVoiceAtStart = voiceTotal;
                cycleTime = 0f;
                if (++cycleIndex < CyclePhases.Length) return;

                cycleIndex = -1;
                info($"[RenderPreview] Phase cycle complete: {voiceTotal} voice requests ({cycleAudio}).");
            }
            catch (Exception error)
            {
                Clear();
                warning("[RenderPreview] Phase cycle failed: " + error.Message);
            }
        }

        private bool IsCurrentSession() =>
            anchor != null && camera != null && camera.isActiveAndEnabled && camera == soloCamera()
            && SceneManager.GetActiveScene().handle == sceneHandle;

        private void StartWalk()
        {
            Clear();
            Show(12f);
            walkStart = anchor.transform.position;
            walkEnd = walkStart + anchor.transform.forward * 1.2f;
            for (int i = 1; i <= 2; i++)
            {
                Vector3 sample = deck.point + (walkEnd - walkStart) * (i / 2f);
                if (!Physics.Raycast(sample + Vector3.up * .15f, Vector3.down, out var hit, .25f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    || hit.collider != deck.collider || hit.normal.y < .8f || Mathf.Abs(hit.point.y - sample.y) > .05f)
                    throw new InvalidOperationException("The walk needs a clear, level deck. Face down the ship aisle.");
            }

            walkTime = -4f;
            maxAuraOffset = 0f;
            walking = true;
            info("[RenderPreview] Walk queued: return to the game for a 1.2 m pass with full aura.");
        }

        internal void TickWalk(float deltaTime)
        {
            if (!walking || deltaTime <= 0f) return;

            try
            {
                if (!IsCurrentSession())
                {
                    Clear();
                    return;
                }

                // Limit movement steps after a frame hitch.
                float step = Mathf.Min(deltaTime, .1f);
                walkTime += step;
                if (walkTime <= 0f) return;

                const float duration = 2f;
                anchor.transform.position = Vector3.Lerp(walkStart, walkEnd, walkTime / duration);
                maxAuraOffset = Mathf.Max(maxAuraOffset,
                    Vector3.Distance(anchor.transform.position, controller.WorldPosition));
                if (walkTime < duration) return;

                walking = false;
                info(FormattableString.Invariant(
                    $"[RenderPreview] Walk complete: travel={Vector3.Distance(walkStart, anchor.transform.position):F3} m, max aura anchor offset={maxAuraOffset:F3} m."));
            }
            catch (Exception error)
            {
                Clear();
                warning("[RenderPreview] Walk failed: " + error.Message);
            }
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
            walking = false;
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
            float ratio = height / AuraReferenceHeight;
            float wispScale = Mathf.Min(ratio, 1.25f);
            float flameHeightScale = ratio <= 1f ? ratio
                : Mathf.Lerp(1f, 1.75f, Mathf.InverseLerp(AuraReferenceHeight, 3f, height));
            controller.SetEffectScale(wispScale, flameHeightScale);
            info(FormattableString.Invariant(
                $"[RenderPreview] Aura geometry scale: wisps={wispScale:F3}, flame width/depth={wispScale:F3}, flame height={flameHeightScale:F3} at {height:F3} m."));
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
            walking = false;
            cycleIndex = -1;
            if (anchor != null)
            {
                anchor.SetActive(false);
                UnityEngine.Object.Destroy(anchor);
            }
            if (previewClip != null) UnityEngine.Object.Destroy(previewClip);
            anchor = null;
            visual = null;
            deck = default;
            previewClip = null;
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
        private Func<DevelopmentEffects> openLive;
        private Action<string> liveWarning;
        private Func<Camera> soloCamera;
        private DevelopmentEffects live;
        private bool attemptedLive;
        private bool liveWasReady;
        private string logTag;
        private Action<string> info;
        private float nextPoll;
        private float nextHeartbeat;
        private int pollCount;

        private static Action<ScriptableRenderContext, Camera> cameraRendered;

        internal static void StartPreview(string directory, Func<Camera> soloCamera,
            Action<string> info, Action<string> warning)
        {
            Arm("[RenderPreview]", soloCamera, info, warning, runner =>
            {
                runner.preview = new InGameRenderPreview(directory, soloCamera, info, warning);

                info($"[RenderPreview] Solo gameplay camera rendered in {SceneManager.GetActiveScene().name}. " +
                    $"Runner {runner.GetInstanceID()} watching: {runner.preview.RequestPath}");
            });
        }

        internal static void StartLiveTest(Func<Camera> soloCamera, Func<DevelopmentEffects> openLive,
            Action<string> info, Action<string> warning)
        {
            Arm("[LiveTest]", soloCamera, info, warning, runner =>
            {
                runner.soloCamera = soloCamera;
                runner.openLive = openLive;
                runner.liveWarning = warning;
                info("[LiveTest] Solo camera rendered. Waiting for live Bracken attachment.");
            });
        }

        private static void Arm(string tag, Func<Camera> soloCamera, Action<string> info, Action<string> warning,
            Action<InGameRenderPreviewRunner> configure)
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
                    instance.logTag = tag;
                    SceneManager.sceneLoaded += instance.OnSceneLoaded;
                    configure(instance);
                }
                catch (Exception error)
                {
                    nextAttempt = now + 1f;
                    warning(tag + " Camera bootstrap failed: " + error.Message);
                }
            };

            RenderPipelineManager.endCameraRendering += cameraRendered;
            Application.quitting += StopBootstrap;
            info(tag + " Armed. Waiting for the solo gameplay camera to render.");
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
            if (now < nextPoll) return;
            nextPoll = now + .5f;
            if (openLive != null)
            {
                try
                {
                    if (soloCamera() == null)
                    {
                        live?.Dispose();
                        live = null;
                        attemptedLive = false;
                        if (liveWasReady) info("[LiveTest] Scan suspended: solo gameplay camera unavailable.");
                        liveWasReady = false;
                        return;
                    }
                    if (!liveWasReady) info("[LiveTest] Solo gameplay camera available. Starting scan loop.");
                    liveWasReady = true;
                    if (!attemptedLive)
                    {
                        attemptedLive = true;
                        live = openLive();
                    }
                    pollCount++;
                    live?.Scan();
                }
                catch (Exception error)
                {
                    live?.Dispose();
                    live = null;
                    openLive = null;
                    liveWarning("[LiveTest] Stopped: " + error.Message);
                }
                return;
            }
            if (preview == null) return;
            pollCount++;
            if (pollCount == 1 || now >= nextHeartbeat)
            {
                nextHeartbeat = now + 15f;
                info($"[RenderPreview] Runner {GetInstanceID()} poll {pollCount}, " +
                    $"active scene={SceneManager.GetActiveScene().name}, request present={File.Exists(preview.RequestPath)}.");
            }
            preview.Poll();
        }

        private void LateUpdate()
        {
            preview?.TickWalk(Time.deltaTime);
            preview?.TickCycle(Time.deltaTime);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            info($"{logTag} Runner {GetInstanceID()} saw scene load: {scene.name} ({mode}). Polls so far: {pollCount}.");
            nextHeartbeat = 0f;
        }

        private void OnDisable()
        {
            live?.Dispose();
            live = null;
            info?.Invoke($"{logTag} Runner {GetInstanceID()} disabled after {pollCount} polls, active scene={SceneManager.GetActiveScene().name}.");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            info?.Invoke($"{logTag} Runner {GetInstanceID()} destroyed after {pollCount} polls.");
            preview?.Dispose();
            if (instance == this) instance = null;
        }
    }
}
