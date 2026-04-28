using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace REPOPingMod
{
    /// <summary>
    /// Makes the player's right arm point at the ping location for 1 second.
    /// Remote players: Harmony postfix on PlayerAvatarRightArm.Update
    /// Local player: arm follows camera direction naturally (FP arm has no IK)
    /// </summary>
    public static class ArmPointing
    {
        private class PointState
        {
            public bool isPointing;
            public Vector3 worldTarget;
            public float timer;
        }

        private static readonly Dictionary<int, PointState> _states = new();

        public static void Initialize(Harmony harmony)
        {
            // Patch PlayerAvatarRightArm.Update for remote player arm pointing
            try
            {
                var original = AccessTools.Method(typeof(PlayerAvatarRightArm), "Update");
                if (original != null)
                {
                    var postfix = new HarmonyMethod(typeof(ArmPointing), nameof(RemoteArmPostfix));
                    harmony.Patch(original, postfix: postfix);
                    Plugin.Logger.LogInfo("[ArmPointing] Patched PlayerAvatarRightArm.Update");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger.LogWarning($"[ArmPointing] Failed to patch: {ex.Message}");
            }

            // Clean up state on scene transitions
            SceneManager.sceneLoaded += (scene, mode) => _states.Clear();
        }

        /// <summary>
        /// Called when local player pings. No-op for local visual (FP arm has no IK).
        /// Remote clients see the pointing via StartPointingForPlayer + Harmony postfix.
        /// </summary>
        public static void StartPointing(Vector3 worldPosition, float duration = 1f)
        {
            // Not visible in first person — pointing is only seen by other players
        }

        /// <summary>
        /// Start pointing for a specific remote player (by Photon actor number).
        /// Called when receiving a ping from another player.
        /// </summary>
        public static void StartPointingForPlayer(int actorNumber, Vector3 worldPosition, float duration = 1f)
        {
            var avatars = Object.FindObjectsOfType<PlayerAvatar>();
            foreach (var avatar in avatars)
            {
                if (avatar.photonView != null &&
                    avatar.photonView.Owner != null &&
                    avatar.photonView.Owner.ActorNumber == actorNumber)
                {
                    int id = avatar.GetInstanceID();
                    if (!_states.ContainsKey(id))
                        _states[id] = new PointState();

                    _states[id].isPointing = true;
                    _states[id].worldTarget = worldPosition;
                    _states[id].timer = duration;
                    return;
                }
            }
        }

        /// <summary>
        /// Harmony postfix on PlayerAvatarRightArm.Update — drives remote arm pointing.
        /// Only fires on remote player avatars (local player's is disabled by the game).
        /// </summary>
        private static void RemoteArmPostfix(PlayerAvatarRightArm __instance)
        {
            if (__instance.playerAvatar == null) return;

            int id = __instance.playerAvatar.GetInstanceID();
            if (!_states.TryGetValue(id, out var state) || !state.isPointing)
                return;

            float dt = Time.deltaTime;
            state.timer -= dt;
            if (state.timer <= 0f)
            {
                state.isPointing = false;
                _states.Remove(id);
                return;
            }

            // Override arm pose to extended/pointing position
            if (__instance.rightArmTransform == null) return;
            __instance.rightArmTransform.localEulerAngles = __instance.grabberPose;

            // Move aim target to the ping world position
            Transform aimTarget = __instance.grabberAimTarget;
            if (aimTarget == null) return;

            Transform parent = __instance.rightArmParentTransform;
            if (parent == null) return;

            // Set aim target to ping position (no clamping — we want to point anywhere)
            Vector3 prevPos = aimTarget.position;
            aimTarget.position = Vector3.Lerp(prevPos, state.worldTarget, 30f * dt);

            // LookAt to compute target rotation
            Quaternion savedLocal = parent.localRotation;
            parent.LookAt(aimTarget);
            Quaternion targetRot = parent.localRotation;

            // Restore and spring to target
            parent.localRotation = savedLocal;
            parent.localRotation = SemiFunc.SpringQuaternionGet(
                __instance.grabberSteerSpring, targetRot, dt);
        }
    }
}
