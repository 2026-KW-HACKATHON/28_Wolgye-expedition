using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

// Portrait prototype for Unity 6000.3 / AR Foundation 6.3.5.
// Keep the existing AR camera/ARCameraBackground active. Only the displayed view switches.
[DefaultExecutionOrder(10000)]
public sealed partial class S25DualCameraAR : MonoBehaviour
{
    [Header("Existing rear AR scene")]
    [SerializeField] private ARCameraManager rearCameraManager;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private ARRaycastManager raycastManager;
    [Tooltip("Optional existing monster. It is not moved or cloned; its layers are temporarily changed.")]
    [SerializeField] private Transform existingMonster;
    [Tooltip("Reserve an unused layer. Default 30 must not contain plane/debug meshes.")]
    [Range(0, 31)] [SerializeField] private int monsterLayer = 30;
    [SerializeField] private string frontCameraId = "1";

    [Header("Approximate front-to-rear calibration (portrait)")]
    [Tooltip("Front lens position in the REAR display-oriented camera's local axes, metres. Zero is an approximation.")]
    [SerializeField] private Vector3 frontOffsetMeters = Vector3.zero;
    [Tooltip("Small correction applied AFTER nominal rear-to-front yaw of 180 degrees.")]
    [SerializeField] private Vector3 frontAngleCorrection = Vector3.zero;
    [Range(0.5f, 2f)] [SerializeField] private float focalScale = 1;
    [Tooltip("Principal point correction as a fraction of image width/height.")]
    [SerializeField] private Vector2 principalShift = Vector2.zero;
    [Range(-150, 150)] [SerializeField] private float poseTimeAdjustmentMs;
    [SerializeField] private bool mirrorSelfie;

    [Header("v3.1 diagnostic comparisons")]
    [SerializeField] private bool usePoseHistory = true;
    [SerializeField] private bool useManualFov;
    [Range(30, 110)] [SerializeField] private float manualVerticalFov = 65;
    private float sensorAgeMs;
    private string lastTrackingEvent = "No transition recorded";
    private Matrix4x4 renderProjection;
    private Vector3 renderPosition;
    private int renderCount;
    private Vector2 diagnosticScroll;
    private Camera rearCamera, frontCamera;
    private AndroidJavaObject bridge;
    private Texture2D videoTexture;
    private RenderTexture virtualTexture;
    private byte[] rgba;
    private float fx, fy, cx, cy;
    private bool showFront, showCalibration, running, placing, haveImage;
    private string note = "Start front stream after rear reaches SessionTracking.";
    private float lastUploadedTime = -1, lastRearTime = -1, nextStatusPoll;
    private long? lastRearTimestamp;
    private long rearFrames, uploadedFrames;
    private NativeStatus native = new NativeStatus();
    private Pose imagePose;
    private double imageTime;
    private int oldRearMask;
    private ScreenOrientation oldOrientation;
    private readonly List<PoseSample> history = new List<PoseSample>(240);
    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private readonly Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    private ARAnchor demoAnchor;
    private Transform demoTarget;
    private Material demoMaterial;
    private int lifecycle;

    [Serializable] private sealed class NativeStatus
    {
        public string state = "IDLE", optics = "", cameraId = "", timestampSource = "";
        public long converted = 0, frameAgeMs = -1;
    }
    private struct PoseSample { public double time; public Pose pose; }

    private void OnEnable()
    {
        lifecycle++;
        ARSession.stateChanged += OnTrackingChanged;
        Camera.onPreRender += CaptureRender;
        RenderPipelineManager.beginCameraRendering += CaptureSrpRender;
        oldOrientation = Screen.orientation;
        Screen.orientation = ScreenOrientation.Portrait;
        if (rearCameraManager == null) rearCameraManager = FindFirstObjectByType<ARCameraManager>();
        if (anchorManager == null) anchorManager = FindFirstObjectByType<ARAnchorManager>();
        if (raycastManager == null) raycastManager = FindFirstObjectByType<ARRaycastManager>();
        if (rearCameraManager == null || !rearCameraManager.TryGetComponent(out rearCamera))
        {
            note = "Assign the existing rear AR Camera Manager.";
            return;
        }
        oldRearMask = rearCamera.cullingMask;
        rearCamera.cullingMask |= 1 << monsterLayer;
        rearCameraManager.frameReceived += OnRearFrame;
        frontCamera = new GameObject("S25 Front Render Camera").AddComponent<Camera>();
        frontCamera.clearFlags = CameraClearFlags.SolidColor;
        frontCamera.backgroundColor = Color.clear;
        frontCamera.cullingMask = 1 << monsterLayer;
        frontCamera.nearClipPlane = 0.03f;
        frontCamera.farClipPlane = 100;
        frontCamera.allowHDR = false;
        frontCamera.allowMSAA = false;
        frontCamera.useOcclusionCulling = false;
        frontCamera.stereoTargetEye = StereoTargetEyeMask.None;
        frontCamera.enabled = false;
        if (existingMonster != null) SetMonsterLayer(existingMonster);
    }

    private void OnTrackingChanged(ARSessionStateChangedEventArgs e)
    {
        lastTrackingEvent = $"{Time.realtimeSinceStartup:F1}s: {e.state} / {ARSession.notTrackingReason}";
        InvalidateMarkerSamples();
        history.Clear(); // Do not interpolate across a loss/recovery of the tracking coordinate frame.
    }

    private void CaptureSrpRender(ScriptableRenderContext context, Camera camera) => CaptureRender(camera);
    private void CaptureRender(Camera camera)
    {
        if (camera != frontCamera) return;
        renderProjection = camera.projectionMatrix;
        renderPosition = camera.transform.position;
        renderCount++;
    }

    private void OnRearFrame(ARCameraFrameEventArgs e)
    {
        if (!e.timestampNs.HasValue || e.timestampNs == lastRearTimestamp) return;
        lastRearTimestamp = e.timestampNs;
        rearFrames++;
        lastRearTime = Time.realtimeSinceStartup;
    }

    private void StartStream()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (rearCamera == null || ARSession.state != ARSessionState.SessionTracking ||
            rearCameraManager.currentFacingDirection != CameraFacingDirection.World)
        { note = "Rear must be World / SessionTracking before Start."; return; }
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        { Permission.RequestUserPermission(Permission.Camera); note = "Grant camera permission and press Start again."; return; }
        try
        {
            if (bridge == null)
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                bridge = new AndroidJavaObject("com.example.s25dualcamera.FrontFrameSource", activity);
            }
            bridge.Call("start", frontCameraId);
            running = true;
            haveImage = false;
            showFront = false;
            uploadedFrames = 0;
            note = "Wait for Unity frames to increase, then switch view.";
        }
        catch (Exception e) { note = e.ToString(); Debug.LogException(e); }
#else
        note = "Run this prototype on the S25, not in Editor.";
#endif
    }

    private void StopStream()
    {
        try { bridge?.Call("stop"); } catch (Exception e) { Debug.LogWarning(e.Message); }
        running = showFront = haveImage = false;
        native = new NativeStatus { state = "STOPPED" };
        CancelFrozen();
        if (frontCamera != null) frontCamera.enabled = false;
        note = "Front stopped; rear AR remains active.";
    }

    private void Update()
    {
        if (!running || bridge == null) return;
        try
        {
            byte[] packet = bridge.Call<byte[]>("takeFrame");
            if (packet != null && packet.Length >= 48) Upload(packet);
            if (Time.realtimeSinceStartup >= nextStatusPoll)
            {
                nextStatusPoll = Time.realtimeSinceStartup + 0.5f;
                native = JsonUtility.FromJson<NativeStatus>(bridge.Call<string>("snapshot")) ?? new NativeStatus();
            }
        }
        catch (Exception e) { note = e.Message; StopStream(); note = e.Message; Debug.LogException(e); }
    }

    private void Upload(byte[] packet)
    {
        if (!BitConverter.IsLittleEndian) throw new NotSupportedException("Little-endian packet required");
        if (BitConverter.ToInt32(packet, 0) != 0x33465241) throw new InvalidOperationException("Wrong native packet version");
        int w = BitConverter.ToInt32(packet, 4), h = BitConverter.ToInt32(packet, 8);
        if (w <= 0 || h <= 0 || w > 2048 || h > 2048 || packet.Length != 48L + (long) w * h * 4)
            throw new InvalidOperationException("Invalid frame dimensions");
        fx = BitConverter.ToSingle(packet, 32); fy = BitConverter.ToSingle(packet, 36);
        cx = BitConverter.ToSingle(packet, 40); cy = BitConverter.ToSingle(packet, 44);
        if (!(fx > 0) || !(fy > 0)) throw new InvalidOperationException("Invalid focal estimate");
        long sampleMs = BitConverter.ToInt64(packet, 24);
        long nowMs = bridge.Call<long>("clockMs");
        sensorAgeMs = nowMs - sampleMs;
        double ageSeconds = Math.Max(0, Math.Min(2, (nowMs - sampleMs) / 1000.0));
        imageTime = Time.realtimeSinceStartupAsDouble - ageSeconds;
        if (videoTexture == null || videoTexture.width != w || videoTexture.height != h)
        {
            if (videoTexture != null) Destroy(videoTexture);
            if (virtualTexture != null) { frontCamera.targetTexture = null; virtualTexture.Release(); Destroy(virtualTexture); }
            videoTexture = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            videoTexture.wrapMode = TextureWrapMode.Clamp;
            videoTexture.filterMode = FilterMode.Bilinear;
            rgba = new byte[w * h * 4];
            virtualTexture = new RenderTexture(w * 2, h * 2, 24, RenderTextureFormat.ARGB32);
            virtualTexture.antiAliasing = 1;
            virtualTexture.Create();
            frontCamera.targetTexture = virtualTexture;
        }
        Buffer.BlockCopy(packet, 48, rgba, 0, rgba.Length);
        videoTexture.LoadRawTextureData(rgba);
        videoTexture.Apply(false, false);
        haveImage = true;
        uploadedFrames++;
        lastUploadedTime = Time.realtimeSinceStartup;
    }

    private void LateUpdate()
    {
        if (rearCamera == null) return;
        double now = Time.realtimeSinceStartupAsDouble;
        history.Add(new PoseSample { time = now, pose = new Pose(rearCamera.transform.position, rearCamera.transform.rotation) });
        while (history.Count > 240 || (history.Count > 1 && now - history[0].time > 2)) history.RemoveAt(0);
        if (frontCamera == null || !haveImage) return;
        // Approximate timestamp alignment. AR pose prediction and camera latency remain calibration factors.
        imagePose = usePoseHistory ? FindPose(imageTime + poseTimeAdjustmentMs / 1000.0)
            : new Pose(rearCamera.transform.position, rearCamera.transform.rotation);
        frontCamera.transform.SetPositionAndRotation(
            imagePose.position + imagePose.rotation * frontOffsetMeters,
            imagePose.rotation * Quaternion.Euler(0, 180, 0) * Quaternion.Euler(frontAngleCorrection));
        float usedFx = fx * focalScale, usedFy = fy * focalScale;
        float usedCx = cx + principalShift.x * videoTexture.width;
        float usedCy = cy + principalShift.y * videoTexture.height;
        if (useManualFov)
        {
            usedFx = usedFy = videoTexture.height / (2 * Mathf.Tan(manualVerticalFov * Mathf.Deg2Rad / 2));
            usedCx = videoTexture.width / 2f; usedCy = videoTexture.height / 2f;
        }
        frontCamera.projectionMatrix = Projection(usedFx, usedFy,
            usedCx, usedCy,
            videoTexture.width, videoTexture.height, frontCamera.nearClipPlane, frontCamera.farClipPlane);
        frontCamera.enabled = showFront;
    }

    private Pose FindPose(double at)
    {
        if (history.Count == 0) return new Pose(rearCamera.transform.position, rearCamera.transform.rotation);
        if (at <= history[0].time) return history[0].pose;
        for (int i = 1; i < history.Count; i++) if (history[i].time >= at)
        {
            PoseSample a = history[i - 1], b = history[i];
            float t = (float) ((at - a.time) / Math.Max(0.000001, b.time - a.time));
            return new Pose(Vector3.Lerp(a.pose.position, b.pose.position, t), Quaternion.Slerp(a.pose.rotation, b.pose.rotation, t));
        }
        return history[history.Count - 1].pose;
    }

    public static Matrix4x4 Projection(float fx, float fy, float cx, float cy, int w, int h, float near, float far)
    {
        Matrix4x4 p = Matrix4x4.zero;
        p[0, 0] = 2 * fx / w; p[1, 1] = 2 * fy / h;
        p[0, 2] = 1 - 2 * cx / w; p[1, 2] = 2 * cy / h - 1;
        p[2, 2] = -(far + near) / (far - near);
        p[2, 3] = -2 * far * near / (far - near);
        p[3, 2] = -1;
        return p;
    }

    private async void PlaceDemo(bool onPlane)
    {
        if (placing || showFront || rearCamera == null) return;
        if (anchorManager == null || !anchorManager.isActiveAndEnabled || ARSession.state != ARSessionState.SessionTracking)
        { note = "Add/enable AR Anchor Manager on XR Origin and wait for tracking."; return; }
        Pose pose;
        if (onPlane)
        {
            if (raycastManager == null || !raycastManager.Raycast(new Vector2(Screen.width / 2f, Screen.height / 2f), hits, TrackableType.PlaneWithinPolygon))
            { note = "No detected plane at screen centre. Scan a floor/table or use 1.5m target."; return; }
            pose = hits[0].pose;
        }
        else pose = new Pose(rearCamera.transform.position + rearCamera.transform.forward * 1.5f, Quaternion.identity);
        int token = lifecycle;
        placing = true;
        try
        {
            var result = await anchorManager.TryAddAnchorAsync(pose);
            if (!result.status.IsSuccess()) { note = "Anchor creation failed: " + result.status; return; }
            if (this == null || token != lifecycle || !isActiveAndEnabled)
            { if (result.value != null) Destroy(result.value.gameObject); return; }
            if (demoAnchor != null) Destroy(demoAnchor.gameObject);
            demoAnchor = result.value;
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Shared World Target";
            marker.layer = monsterLayer;
            marker.transform.SetParent(demoAnchor.transform, false);
            marker.transform.localPosition = onPlane ? Vector3.up * 0.1f : Vector3.zero;
            marker.transform.localScale = Vector3.one * 0.2f;
            if (demoMaterial == null)
            {
                Shader shader = Resources.Load<Shader>("S25DiagnosticUnlit");
                if (shader == null) throw new InvalidOperationException("Missing Resources/S25DiagnosticUnlit.shader");
                demoMaterial = new Material(shader);
                demoMaterial.SetColor("_Color", new Color(0.05f, 0.95f, 0.8f, 1));
            }
            marker.GetComponent<Renderer>().sharedMaterial = demoMaterial;
            demoTarget = marker.transform;
            note = "Target anchored. Switch to front, then physically aim the FRONT lens at the same target location.";
        }
        catch (Exception e) { note = e.Message; Debug.LogException(e); }
        finally { placing = false; }
    }

    private void SetMonsterLayer(Transform root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!originalLayers.ContainsKey(t.gameObject)) originalLayers.Add(t.gameObject, t.gameObject.layer);
            t.gameObject.layer = monsterLayer;
        }
    }

    private void OnApplicationPause(bool paused) { if (paused) StopStream(); }
    private void OnDisable()
    {
        CleanupMarker();
        lifecycle++;
        ARSession.stateChanged -= OnTrackingChanged;
        Camera.onPreRender -= CaptureRender;
        RenderPipelineManager.beginCameraRendering -= CaptureSrpRender;
        StopStream();
        try { bridge?.Call("dispose"); } catch (Exception e) { Debug.LogWarning(e.Message); }
        bridge?.Dispose(); bridge = null;
        if (rearCameraManager != null) rearCameraManager.frameReceived -= OnRearFrame;
        if (rearCamera != null) rearCamera.cullingMask = oldRearMask;
        foreach (var entry in originalLayers) if (entry.Key != null) entry.Key.layer = entry.Value;
        originalLayers.Clear();
        if (frontCamera != null) { frontCamera.targetTexture = null; Destroy(frontCamera.gameObject); }
        if (virtualTexture != null) { virtualTexture.Release(); Destroy(virtualTexture); }
        if (videoTexture != null) Destroy(videoTexture);
        if (demoAnchor != null) Destroy(demoAnchor.gameObject);
        if (demoMaterial != null) Destroy(demoMaterial);
        history.Clear();
        Screen.orientation = oldOrientation;
    }

    private void OnGUI()
    {
        if (markerMode) { MarkerGUI(); return; }
        int oldDepth = GUI.depth;
        GUI.depth = -1000;
        Matrix4x4 oldMatrix = GUI.matrix;
        if (showFront && haveImage && videoTexture != null)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.blackTexture);
            float s = Mathf.Min(Screen.width / (float)videoTexture.width, Screen.height / (float)videoTexture.height);
            Rect r = new Rect((Screen.width - videoTexture.width * s) / 2,
                (Screen.height - videoTexture.height * s) / 2, videoTexture.width * s, videoTexture.height * s);
            if (mirrorSelfie) GUIUtility.ScaleAroundPivot(new Vector2(-1, 1), r.center);
            GUI.DrawTexture(r, videoTexture, ScaleMode.StretchToFill, false);
            bool tracked = ARSession.state == ARSessionState.SessionTracking;
            if (virtualTexture != null && tracked && Time.realtimeSinceStartup - lastUploadedTime < 1)
                GUI.DrawTexture(r, virtualTexture, ScaleMode.StretchToFill, true);
            GUI.matrix = oldMatrix;
        }
        float scale = Mathf.Max(1, Screen.width / 600f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float top = (Screen.height - Screen.safeArea.yMax) / scale + 8;
        GUILayout.BeginArea(new Rect(10, top, Screen.width / scale - 20, Mathf.Min(840, Screen.height / scale - top - 10)), GUI.skin.box);
        GUILayout.Label("S25 Dual Camera AR v4 — marker calibration");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start front", GUILayout.Height(40))) StartStream();
        GUI.enabled = haveImage && running;
        if (GUILayout.Button(showFront ? "Show REAR" : "Show FRONT", GUILayout.Height(40))) showFront = !showFront;
        GUI.enabled = true;
        if (GUILayout.Button("Stop", GUILayout.Height(40))) StopStream();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUI.enabled = !showFront && !placing;
        if (GUILayout.Button("Plane target", GUILayout.Height(36))) PlaceDemo(true);
        if (GUILayout.Button("1.5m target", GUILayout.Height(36))) PlaceDemo(false);
        GUI.enabled = true;
        if (GUILayout.Button(showCalibration ? "Hide calibration" : "Calibration", GUILayout.Height(36))) showCalibration = !showCalibration;
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Marker calibration v4", GUILayout.Height(40))) { markerMode = true; mirrorSelfie = false; }
        float age = lastRearTime < 0 ? -1 : Time.realtimeSinceStartup - lastRearTime;
        GUILayout.Label($"Rear: {ARSession.state} | frames {rearFrames}, age {age:F2}s\n" +
            $"Front {frontCameraId}: {native.state} | Unity frames {uploadedFrames}\n" +
            $"{native.optics}\n{note}", new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 15 });
        Transform target = existingMonster != null ? existingMonster : demoTarget;
        if (target != null && haveImage && frontCamera != null)
        {
            Vector3 v = frontCamera.WorldToViewportPoint(target.position);
            bool inside = v.z > 0 && v.x >= 0 && v.x <= 1 && v.y >= 0 && v.y <= 1;
            GUILayout.Label($"Target in FRONT view: {inside} | world {target.position.ToString("F2")}");
        }
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(usePoseHistory ? "Pose: HISTORY" : "Pose: LIVE", GUILayout.Height(34)))
            usePoseHistory = !usePoseHistory;
        if (GUILayout.Button(useManualFov ? $"FOV: {manualVerticalFov:F0}deg" : "FOV: AUTO", GUILayout.Height(34)))
            useManualFov = !useManualFov;
        GUILayout.EndHorizontal();
        diagnosticScroll = GUILayout.BeginScrollView(diagnosticScroll, GUILayout.Height(showCalibration ? 170 : 245));
        GUILayout.Label(Diagnostics(target), new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 14 });
        GUILayout.EndScrollView();
        if (showCalibration)
        {
            if (useManualFov) manualVerticalFov = Slider("Manual vertical FOV", manualVerticalFov, 30, 110);
            focalScale = Slider("Focal scale", focalScale, 0.5f, 2);
            frontAngleCorrection.y = Slider("Yaw correction", frontAngleCorrection.y, -15, 15);
            frontAngleCorrection.x = Slider("Pitch correction", frontAngleCorrection.x, -15, 15);
            frontAngleCorrection.z = Slider("Roll correction", frontAngleCorrection.z, -15, 15);
            frontOffsetMeters.x = Slider("Lens offset X (m)", frontOffsetMeters.x, -0.12f, 0.12f);
            frontOffsetMeters.y = Slider("Lens offset Y (m)", frontOffsetMeters.y, -0.12f, 0.12f);
            frontOffsetMeters.z = Slider("Lens offset Z (m)", frontOffsetMeters.z, -0.04f, 0.04f);
            principalShift.x = Slider("Principal X / width", principalShift.x, -0.15f, 0.15f);
            principalShift.y = Slider("Principal Y / height", principalShift.y, -0.15f, 0.15f);
            poseTimeAdjustmentMs = Slider("Pose time adjustment ms", poseTimeAdjustmentMs, -150, 150);
            mirrorSelfie = GUILayout.Toggle(mirrorSelfie, "Mirror BOTH video and 3D");
        }
        GUILayout.EndArea();
        GUI.matrix = oldMatrix;
        GUI.depth = oldDepth;
    }

    private string Diagnostics(Transform target)
    {
        if (!running || !haveImage || rearCamera == null || frontCamera == null || videoTexture == null) return "Start stream to collect diagnostics.";
        Matrix4x4 p = frontCamera.projectionMatrix;
        float fovV = 2 * Mathf.Atan(1 / Mathf.Max(0.00001f, Mathf.Abs(p[1, 1]))) * Mathf.Rad2Deg;
        float fovH = 2 * Mathf.Atan(1 / Mathf.Max(0.00001f, Mathf.Abs(p[0, 0]))) * Mathf.Rad2Deg;
        string result = $"Rear pos {rearCamera.transform.position.ToString("F2")}\n" +
            $"Front pos {frontCamera.transform.position.ToString("F2")}\n" +
            $"Pose gap {Vector3.Distance(rearCamera.transform.position, imagePose.position):F3}m / " +
            $"{Quaternion.Angle(rearCamera.transform.rotation, imagePose.rotation):F1}deg\n" +
            $"Frame age {sensorAgeMs:F0}ms | source {native.timestampSource}\n" +
            $"Image {videoTexture.width}x{videoTexture.height}, fx/fy {fx:F2}/{fy:F2}, cx/cy {cx:F1}/{cy:F1}\n" +
            $"FOV H/V {fovH:F1}/{fovV:F1}deg | scale {focalScale:F2}\n" +
            $"Projection m00/m11 {p[0, 0]:F3}/{p[1, 1]:F3}\n" +
            $"Render #{renderCount} m00/m11 {renderProjection[0, 0]:F3}/{renderProjection[1, 1]:F3}, pos {renderPosition.ToString("F2")}\n";
        if (target != null)
        {
            float dr = Vector3.Distance(rearCamera.transform.position, target.position);
            float df = Vector3.Distance(frontCamera.transform.position, target.position);
            Vector3 view = frontCamera.WorldToViewportPoint(target.position);
            result = $"DISTANCE rear/front {dr:F3} / {df:F3} m | front depth {view.z:F3}m\n" + result;
            result += $"Target world {target.position.ToString("F3")} | scale {target.lossyScale.ToString("F3")}\n";
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                float low = float.PositiveInfinity, high = float.NegativeInfinity;
                bool allAhead = true;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 v = frontCamera.WorldToViewportPoint(corner);
                    allAhead &= v.z > frontCamera.nearClipPlane;
                    low = Mathf.Min(low, v.y); high = Mathf.Max(high, v.y);
                }
                float displayHeight = Mathf.Min(Screen.height, Screen.width * videoTexture.height / (float)videoTexture.width);
                result += allAhead ? $"Projected bounds height ~{(high - low) * displayHeight:F1} screen px\n" : "Target crosses/behind camera plane\n";
            }
            ARAnchor a = target.GetComponentInParent<ARAnchor>();
            result += a == null ? "Anchor: none on target parent\n" : $"Anchor: {a.trackingState} | pos {a.transform.position.ToString("F3")}\n";
        }
        result += $"Tracking now: {ARSession.state}/{ARSession.notTrackingReason}\nLast transition: {lastTrackingEvent}\n";
        return result;
    }

    private static float Slider(string label, float value, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {value:F3}", GUILayout.Width(265));
        float result = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.EndHorizontal();
        return result;
    }
}
