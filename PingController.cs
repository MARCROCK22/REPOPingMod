using UnityEngine;
using REPOPingMod.Input;
using REPOPingMod.PingSystem;

namespace REPOPingMod
{
    public class PingController : MonoBehaviour
    {
        private RadialWheel _radialWheel;

        private float _pressStartTime;
        private bool _isPressed;
        private bool _wheelOpened;

        private bool _hasHitPoint;
        private Vector3 _savedHitPoint;
        private Vector3 _savedHitNormal;
        private GameObject _savedHitObject;
        private float _lastPingTime;

        public void Initialize(RadialWheel radialWheel)
        {
            _radialWheel = radialWheel;
        }

        private void Update()
        {
            bool pingDown = UnityEngine.Input.GetMouseButtonDown(2);
            bool pingUp = UnityEngine.Input.GetMouseButtonUp(2);
            bool rightDown = UnityEngine.Input.GetMouseButtonDown(1);

            float wheelDelay = Plugin.PingConfig.RadialWheelDelay.Value;

            if (pingDown)
            {
                _pressStartTime = Time.time;
                _isPressed = true;
                _wheelOpened = false;
                _hasHitPoint = TryRaycast(out _savedHitPoint, out _savedHitNormal, out _savedHitObject);
            }

            if (_isPressed && !_wheelOpened)
            {
                float holdTime = Time.time - _pressStartTime;
                if (holdTime >= wheelDelay)
                {
                    _wheelOpened = true;
                    _radialWheel?.Show();
                }
            }

            if (_wheelOpened && rightDown)
            {
                _radialWheel?.Hide();
                _isPressed = false;
                _wheelOpened = false;
            }

            if (_isPressed && pingUp)
            {
                _isPressed = false;

                if (_hasHitPoint && Time.time - _lastPingTime >= 0.5f)
                {
                    _lastPingTime = Time.time;
                    PingType type;
                    if (_wheelOpened)
                    {
                        PingType? selected = _radialWheel?.GetSelectedType();
                        _radialWheel?.Hide();

                        if (!selected.HasValue)
                        {
                            _wheelOpened = false;
                            return;
                        }
                        type = selected.Value;
                    }
                    else
                    {
                        // Auto-detect type based on what was hit
                        type = DetectPingType(_savedHitObject);
                    }

                    Vector3 pingPosition = _savedHitPoint + _savedHitNormal * 0.3f;
                    // Preview for items, enemies, and cart
                    GameObject previewObj = null;
                    if (_savedHitObject != null && (type != PingType.GoHere || IsCart(_savedHitObject)))
                        previewObj = _savedHitObject;
                    PingManager.Instance?.PlacePingLocal(pingPosition, type, previewObj);
                }
                else if (_wheelOpened)
                {
                    _radialWheel?.Hide();
                }

                _wheelOpened = false;
            }
        }

        private bool TryRaycast(out Vector3 hitPoint, out Vector3 hitNormal, out GameObject hitObject)
        {
            hitPoint = Vector3.zero;
            hitNormal = Vector3.up;
            hitObject = null;

            var camera = Camera.main;
            if (camera == null) return false;

            float maxDist = Plugin.PingConfig.MaxPingDistance.Value;
            var origin = camera.transform.position;
            var forward = camera.transform.forward;

            var ray = new Ray(origin, forward);

            // 11=Player, 14=RoomVolume, 18=PlayerOnlyCollision, 19=NavmeshOnly
            // Doors (23=PhysGrabObjectHinge) are NOT ignored — triggers are already filtered
            int ignoreMask = (1 << 11) | (1 << 14) | (1 << 18) | (1 << 19);
            int layerMask = ~ignoreMask;

            var hits = Physics.RaycastAll(ray, maxDist, layerMask, QueryTriggerInteraction.Ignore);

            if (hits.Length == 0) return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (h.collider.isTrigger) continue;

                // Skip hits very close to the player (own grab colliders)
                if (h.distance < 0.5f) continue;

                hitPoint = h.point;
                hitNormal = h.normal;
                hitObject = h.collider.gameObject;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Detect ping type based on what was hit.
        /// </summary>
        private static PingType DetectPingType(GameObject hitObj)
        {
            if (hitObj == null)
                return PingType.GoHere;

            bool isCart = IsCart(hitObj);
            bool isEnemy = IsEnemy(hitObj);
            bool isItem = IsItem(hitObj);
            Plugin.Logger.LogInfo($"[DETECT] Object: {hitObj.name} | Layer: {hitObj.layer} | IsCart: {isCart} | IsEnemy: {isEnemy} | IsItem: {isItem}");

            if (isEnemy)
                return PingType.Enemy;

            // Cart check BEFORE item — cart has ItemAttributes but should be GoHere
            if (isCart)
                return PingType.GoHere;

            if (isItem)
                return PingType.Loot;

            return PingType.GoHere;
        }

        private static bool IsEnemy(GameObject obj)
        {
            return obj.GetComponentInParent<EnemyHealth>() != null
                || obj.GetComponentInParent<EnemyParent>() != null
                || obj.GetComponentInParent<EnemyRigidbody>() != null;
        }

        private static bool IsItem(GameObject obj)
        {
            return obj.GetComponentInParent<ValuableObject>() != null
                || obj.GetComponentInParent<ItemAttributes>() != null;
        }

        private static bool IsCart(GameObject obj)
        {
            return obj.GetComponentInParent<PhysGrabCart>() != null;
        }

        /// <summary>
        /// Get a custom label for special objects.
        /// </summary>
        private static string GetCustomLabel(GameObject hitObj)
        {
            if (hitObj != null && IsCart(hitObj))
                return "Cart is here";
            return null;
        }
    }
}
