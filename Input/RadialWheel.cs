using System.Collections.Generic;
using REPOPingMod.PingSystem;
using UnityEngine;

namespace REPOPingMod.Input
{
    public class RadialWheel : MonoBehaviour
    {
        private bool _visible;
        private PingType? _hoveredType;
        private Vector2 _screenCenter;

        private float _wheelRadius = 120f;
        private float _sectionSize = 80f;
        private float _deadZone = 30f;

        // Cached textures (created once, reused)
        private Texture2D _bgTexture;
        private Texture2D _hoverGoHereTexture;
        private Texture2D _hoverDangerTexture;
        private Texture2D _hoverEnemyTexture;
        private Texture2D _hoverLootTexture;
        private Texture2D _sectionBgTexture;

        private CursorLockMode _savedCursorLockState;
        private bool _savedCursorVisible;

        // Fix 3: Per-type normal styles to avoid shared-style mutation
        private readonly Dictionary<PingType, GUIStyle> _normalStyles = new();
        private GUIStyle _hoveredStyle;

        private void Awake()
        {
            _bgTexture = MakeSolidTexture(new Color(0f, 0f, 0f, 0.6f));
            _sectionBgTexture = MakeSolidTexture(new Color(0.2f, 0.2f, 0.2f, 0.7f));

            _hoverGoHereTexture = MakeSolidTexture(WithAlpha(PingType.GoHere.GetColor(), 0.8f));
            _hoverDangerTexture = MakeSolidTexture(WithAlpha(PingType.Danger.GetColor(), 0.8f));
            _hoverEnemyTexture = MakeSolidTexture(WithAlpha(PingType.Enemy.GetColor(), 0.8f));
            _hoverLootTexture = MakeSolidTexture(WithAlpha(PingType.Loot.GetColor(), 0.8f));

            // Fix 3: Create one GUIStyle per ping type so each holds its own color
            foreach (PingType type in System.Enum.GetValues(typeof(PingType)))
            {
                _normalStyles[type] = new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Normal,
                    normal = { textColor = type.GetColor() }
                };
            }

            _hoveredStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }

        public void Show()
        {
            _visible = true;
            _hoveredType = null;
            _screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            _savedCursorLockState = Cursor.lockState;
            _savedCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

        }

        public void Hide()
        {
            _visible = false;
            _hoveredType = null;
            Cursor.lockState = _savedCursorLockState;
            Cursor.visible = _savedCursorVisible;

        }

        public PingType? GetSelectedType()
        {
            return _hoveredType;
        }

        private void Update()
        {
            if (!_visible) return;

            Vector2 mousePos = new Vector2(UnityEngine.Input.mousePosition.x, UnityEngine.Input.mousePosition.y);
            Vector2 delta = mousePos - _screenCenter;
            float distance = delta.magnitude;

            if (distance < _deadZone)
            {
                _hoveredType = null;
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if (angle >= 45f && angle < 135f)
                _hoveredType = PingType.GoHere;  // Top
            else if (angle >= 135f && angle < 225f)
                _hoveredType = PingType.Loot;    // Left
            else if (angle >= 225f && angle < 315f)
                _hoveredType = PingType.Enemy;   // Bottom
            else
                _hoveredType = PingType.Danger;  // Right
        }

        private void OnGUI()
        {
            if (!_visible) return;

            float bgSize = _wheelRadius + 40f;
            float guiCenterY = Screen.height - _screenCenter.y;
            GUI.DrawTexture(
                new Rect(_screenCenter.x - bgSize, guiCenterY - bgSize, bgSize * 2f, bgSize * 2f),
                _bgTexture);

            DrawSection(PingType.GoHere, new Vector2(0f, _wheelRadius), _hoverGoHereTexture);
            DrawSection(PingType.Danger, new Vector2(_wheelRadius, 0f), _hoverDangerTexture);
            DrawSection(PingType.Enemy, new Vector2(0f, -_wheelRadius), _hoverEnemyTexture);
            DrawSection(PingType.Loot, new Vector2(-_wheelRadius, 0f), _hoverLootTexture);
        }

        private void DrawSection(PingType type, Vector2 offset, Texture2D hoverTexture)
        {
            bool isHovered = _hoveredType == type;
            float size = isHovered ? _sectionSize * 1.2f : _sectionSize;

            float guiX = _screenCenter.x + offset.x - size / 2f;
            float guiY = (Screen.height - _screenCenter.y) - offset.y - size / 2f;
            Rect rect = new Rect(guiX, guiY, size, size);

            GUI.DrawTexture(rect, isHovered ? hoverTexture : _sectionBgTexture);

            // Fix 3: Use per-type style instead of mutating a shared _normalStyle
            GUIStyle style = isHovered ? _hoveredStyle : _normalStyles[type];
            GUI.Label(rect, type.GetLabel(), style);
        }

        private static Texture2D MakeSolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        private static Color WithAlpha(Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }
    }
}
