using System.Collections;
using UnityEngine;

namespace REPOPingMod.PingSystem
{
    public class PingMarker : MonoBehaviour
    {
        public PingType Type { get; private set; }
        public string PlayerName { get; private set; }
        public Vector3 WorldPosition { get; private set; }

        private static Font _cachedFont;
        private static Texture2D _circleTexture;
        private static GUIStyle _labelStyle;
        private static GUIStyle _nameStyle;
        private static GUIStyle _distStyle;

        private string _playerId;
        private float _spawnTime;
        private float _duration;
        private float _fadeDuration;

        // Preview uses RenderTexture directly (no ReadPixels = no GPU stall)
        private RenderTexture _previewRT;
        private string _customLabel;
        private Transform _trackedObject;
        private Vector3 _trackingOffset;

        public void Initialize(PingType type, string playerName, string playerId, GameObject hitObject = null, string customLabel = null)
        {
            Type = type;
            PlayerName = playerName;
            _playerId = playerId;
            _customLabel = customLabel;
            WorldPosition = transform.position;

            if (hitObject != null)
            {
                _trackedObject = hitObject.transform;
                _trackingOffset = WorldPosition - _trackedObject.position;
            }

            var config = Plugin.PingConfig;
            _duration = config.PingDuration.Value;
            _fadeDuration = Mathf.Min(config.PingFadeDuration.Value, _duration);
            _spawnTime = Time.time;

            if (_cachedFont == null)
            {
                _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (_cachedFont == null)
                    _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            if (_circleTexture == null)
                _circleTexture = CreateCircleTexture(64);

            InitStyles();

            // Start async preview capture (spread across multiple frames)
            if (hitObject != null)
                StartCoroutine(CapturePreviewAsync(hitObject));
        }

        private static void InitStyles()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _nameStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _distStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal = { textColor = new Color(0.9f, 0.9f, 0.9f, 0.9f) }
            };
        }

        private void Update()
        {
            float elapsed = Time.time - _spawnTime;
            if (elapsed >= _duration)
            {
                Destroy(gameObject);
                return;
            }

            if (_trackedObject != null)
            {
                WorldPosition = _trackedObject.position + _trackingOffset;
            }
        }

        private void OnGUI()
        {
            var cam = Camera.main;
            if (cam == null) return;

            float elapsed = Time.time - _spawnTime;

            float alpha = 1f;
            if (_fadeDuration > 0f)
            {
                float fadeStart = _duration - _fadeDuration;
                if (elapsed >= fadeStart)
                    alpha = Mathf.Lerp(1f, 0f, (elapsed - fadeStart) / _fadeDuration);
            }

            float popScale = 1f;
            if (elapsed < 0.2f)
                popScale = Mathf.SmoothStep(0f, 1f, elapsed / 0.2f);

            Vector3 viewportPos = cam.WorldToViewportPoint(WorldPosition);
            if (viewportPos.z <= 0f) return;

            Vector3 camPos = cam.transform.position;
            float dist = Vector3.Distance(camPos, WorldPosition);
            if (dist > 15f) return;

            float guiX = viewportPos.x * Screen.width;
            float guiY = (1f - viewportPos.y) * Screen.height;

            Color color = Type.GetColor();
            color.a = alpha;

            float iconSize = 50f * popScale;
            float halfIcon = iconSize / 2f;

            GUI.color = color;
            GUI.DrawTexture(new Rect(guiX - halfIcon, guiY - halfIcon, iconSize, iconSize), _circleTexture);

            // Draw preview — uses RenderTexture directly (no Texture2D needed)
            if (_previewRT != null)
            {
                float previewSize = iconSize * 0.7f;
                float halfPreview = previewSize / 2f;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.DrawTexture(new Rect(guiX - halfPreview, guiY - halfPreview, previewSize, previewSize), _previewRT);
            }

            GUI.color = new Color(1f, 1f, 1f, alpha);
            _labelStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);
            GUI.Label(new Rect(guiX - 60f, guiY - halfIcon - 2f, 120f, 20f), Type.GetLabel(), _labelStyle);

            _nameStyle.normal.textColor = new Color(color.r, color.g, color.b, alpha);
            GUI.Label(new Rect(guiX - 80f, guiY - halfIcon - 22f, 160f, 20f), PlayerName, _nameStyle);

            _distStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f, alpha);
            GUI.Label(new Rect(guiX - 60f, guiY + halfIcon + 2f, 120f, 20f), $"{dist:F0}m", _distStyle);

            GUI.color = Color.white;
        }

        /// <summary>
        /// Async preview capture — spreads work across multiple frames to prevent freezing.
        /// Uses RenderTexture directly (no ReadPixels GPU stall).
        /// </summary>
        private IEnumerator CapturePreviewAsync(GameObject target)
        {
            // === Frame 1: Find the root object with renderers ===
            Transform root = target.transform;
            MeshRenderer[] targetRenderers = null;

            // Check for cart first
            Transform cartCheck = target.transform;
            for (int i = 0; i < 8 && cartCheck != null; i++)
            {
                if (cartCheck.GetComponent("PhysGrabCart") != null)
                {
                    root = cartCheck;
                    targetRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
                    break;
                }
                if (cartCheck.parent != null && cartCheck.parent.GetComponent("LevelGenerator") != null)
                    break;
                cartCheck = cartCheck.parent;
            }

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                root = target.transform;
                for (int depth = 0; depth < 3 && root != null; depth++)
                {
                    targetRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
                    if (targetRenderers != null && targetRenderers.Length > 0)
                        break;
                    if (root.parent != null && root.parent.GetComponent("LevelGenerator") != null)
                        break;
                    root = root.parent;
                }
            }

            if (targetRenderers == null || targetRenderers.Length == 0)
                yield break;

            yield return null;

            // === Frame 2: Clone the object ===
            bool wasActive = root.gameObject.activeSelf;
            root.gameObject.SetActive(false);

            Vector3 studioPos = new Vector3(0f, -5000f, 0f);
            var clone = Object.Instantiate(root.gameObject, studioPos, Quaternion.identity);

            root.gameObject.SetActive(wasActive);

            yield return null;

            // === Frame 3: Strip MonoBehaviours ===
            if (clone == null) yield break;
            foreach (var mb in clone.GetComponentsInChildren<MonoBehaviour>(true))
                if (mb != null) mb.enabled = false;

            yield return null;

            // === Frame 4: Strip physics + audio ===
            if (clone == null) yield break;
            foreach (var rb in clone.GetComponentsInChildren<Rigidbody>(true))
                if (rb != null) rb.isKinematic = true;
            foreach (var col in clone.GetComponentsInChildren<Collider>(true))
                if (col != null) col.enabled = false;
            foreach (var src in clone.GetComponentsInChildren<AudioSource>(true))
                if (src != null) src.enabled = false;
            foreach (var anim in clone.GetComponentsInChildren<Animator>(true))
                if (anim != null) anim.enabled = false;
            foreach (var ps in clone.GetComponentsInChildren<ParticleSystem>(true))
                if (ps != null) ps.Stop();

            yield return null;

            // === Frame 5: Activate, set layer, compute bounds, render ===
            if (clone == null) yield break;
            clone.SetActive(true);

            int photoLayer = 31;
            foreach (Transform t in clone.GetComponentsInChildren<Transform>())
                t.gameObject.layer = photoLayer;

            var cloneRenderers = clone.GetComponentsInChildren<Renderer>(true);
            if (cloneRenderers.Length == 0)
            {
                Object.Destroy(clone);
                yield break;
            }

            Bounds bounds = cloneRenderers[0].bounds;
            for (int i = 1; i < cloneRenderers.Length; i++)
                bounds.Encapsulate(cloneRenderers[i].bounds);

            float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDim < 0.01f)
            {
                Object.Destroy(clone);
                yield break;
            }

            // Studio lights
            var lightObj = new GameObject("PingPreviewLight");
            lightObj.layer = photoLayer;
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1.5f;
            light.cullingMask = 1 << photoLayer;
            lightObj.transform.position = bounds.center + new Vector3(1f, 2f, 1f) * maxDim;
            lightObj.transform.LookAt(bounds.center);

            var fillLightObj = new GameObject("PingPreviewFillLight");
            fillLightObj.layer = photoLayer;
            var fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.8f, 0.85f, 1f);
            fillLight.intensity = 0.8f;
            fillLight.cullingMask = 1 << photoLayer;
            fillLightObj.transform.position = bounds.center + new Vector3(-1f, 0.5f, -1f) * maxDim;
            fillLightObj.transform.LookAt(bounds.center);

            // Camera + RenderTexture (NO ReadPixels)
            var camObj = new GameObject("PingPreviewCam");
            var cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.orthographic = true;
            cam.enabled = false;
            cam.orthographicSize = maxDim * 0.6f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = maxDim * 10f;
            cam.cullingMask = 1 << photoLayer;

            Vector3 cameraOffset = new Vector3(1f, 0.8f, 1f).normalized * maxDim * 3f;
            camObj.transform.position = bounds.center + cameraOffset;
            camObj.transform.LookAt(bounds.center);

            // Render to RenderTexture — keep it alive (no ReadPixels!)
            _previewRT = new RenderTexture(64, 64, 16, RenderTextureFormat.ARGB32);
            cam.targetTexture = _previewRT;
            cam.Render();

            // Cleanup everything except the RenderTexture
            cam.targetTexture = null;
            Object.Destroy(camObj);
            Object.Destroy(lightObj);
            Object.Destroy(fillLightObj);
            Object.Destroy(clone);
        }

        private void OnDestroy()
        {
            PingManager.Instance?.OnMarkerDestroyed(_playerId, this);

            if (_previewRT != null)
            {
                _previewRT.Release();
                Object.Destroy(_previewRT);
            }
        }

        private static Texture2D CreateCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float radius = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    float edge = Mathf.Clamp01((radius - dist) * 2f);
                    pixels[y * size + x] = dist <= radius ? new Color(1f, 1f, 1f, edge) : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
