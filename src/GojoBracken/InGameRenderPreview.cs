using System;
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
        private const string RequestName = "gojobracken-preview.request";
        private readonly string directory;
        private readonly Func<Camera> soloCamera;
        private readonly Action<string> info;
        private readonly Action<string> warning;
        private AssetBundle bundle;
        private GameObject anchor;
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
            info("[RenderPreview] Ready for calm, full, or clear requests beside the plugin DLL.");
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
                    default: warning("[RenderPreview] Expected calm, full, or clear."); break;
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

            sceneHandle = SceneManager.GetActiveScene().handle;
            anchor = new GameObject("GojoBracken local HDRP preview");
            anchor.transform.SetPositionAndRotation(camera.transform.position + forward * 3.5f - Vector3.up * 1.55f,
                Quaternion.LookRotation(-forward, Vector3.up));
            var visual = new GameObject("Owned preview model");
            visual.transform.SetParent(anchor.transform, false);
            UnityEngine.Object.Instantiate(model, visual.transform, false);
            GojoLocomotion.TryCreate(anchor.transform, visual.transform, out var facingStatus);
            silence = AudioClip.Create("Silent render fixture", 480, 1, 48000, false);
            if (!BrackenCosmeticController.TryCreate(anchor.transform, BrackenV81Presentation.SupportedMvid,
                aura, silence, 12f, 1f, out controller, out var error))
                throw new InvalidOperationException(error);
            info("[RenderPreview] Placed local model: " + facingStatus);
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
