using System.Collections.Generic;
using Photon.Pun;
using REPOPingMod.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace REPOPingMod.PingSystem
{
    public class PingManager : MonoBehaviour
    {
        public static PingManager Instance { get; private set; }

        private readonly Dictionary<string, PingMarker> _activePings = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // Fix 6: Subscribe to scene transitions to clear stale ping references
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // Fix 6: Clear all stale ping references on scene transition
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _activePings.Clear();
        }

        // Fix 6: Unsubscribe when destroyed
        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Place a ping at a given position as the local player (with audio + network broadcast).
        /// </summary>
        public void PlacePingLocal(Vector3 position, PingType type, GameObject hitObject = null, string customLabel = null)
        {
            if (LevelGenerator.Instance == null || !LevelGenerator.Instance.Generated)
                return;
            if (SemiFunc.MenuLevel())
                return;

            string localPlayerId = GetLocalPlayerId();
            string localPlayerName = GetLocalPlayerName();

            string hitObjectName = hitObject != null ? hitObject.name : "";
            PlacePing(position, type, localPlayerId, localPlayerName, hitObject, customLabel);
            PingAudio.Play(type);
            PingNetSync.Instance?.BroadcastPing(position, type, localPlayerId, localPlayerName, hitObjectName);

            // Make the local player's arm point at the ping for 1 second
            ArmPointing.StartPointing(position, 1f);
        }

        public void PlacePing(Vector3 position, PingType type, string playerId, string playerName, GameObject hitObject = null, string customLabel = null)
        {
            if (_activePings.TryGetValue(playerId, out var existing) && existing != null)
            {
                Destroy(existing.gameObject);
            }


            var pingObj = new GameObject($"Ping_{type}_{playerName}");
            pingObj.transform.position = position;
            var marker = pingObj.AddComponent<PingMarker>();
            marker.Initialize(type, playerName, playerId, hitObject, customLabel);

            _activePings[playerId] = marker;

            Plugin.Logger.LogInfo($"Ping placed: {type} by {playerName} at {position}");
        }

        // Fix 7: Accept marker reference and compare before removing
        public void OnMarkerDestroyed(string playerId, PingMarker marker)
        {
            if (playerId != null && _activePings.TryGetValue(playerId, out var stored) && stored == marker)
            {
                _activePings.Remove(playerId);
            }
        }

        public void RemovePing(string playerId)
        {
            if (_activePings.TryGetValue(playerId, out var marker) && marker != null)
            {
                Destroy(marker.gameObject);
            }
            _activePings.Remove(playerId);
        }

        /// <summary>
        /// Find a specific object by name near a position.
        /// Used when receiving pings from the network.
        /// </summary>
        public GameObject FindObjectByName(Vector3 position, string objectName)
        {
            var colliders = Physics.OverlapSphere(position, 3f);

            foreach (var col in colliders)
            {
                if (col.isTrigger) continue;
                if (col.gameObject.name == objectName)
                    return col.gameObject;
            }

            // Fallback: check parent names
            foreach (var col in colliders)
            {
                if (col.isTrigger) continue;
                Transform t = col.transform;
                for (int i = 0; i < 3 && t != null; i++)
                {
                    if (t.gameObject.name == objectName)
                        return col.gameObject;
                    t = t.parent;
                }
            }

            return null;
        }

        private string GetLocalPlayerId()
        {
            if (SemiFunc.IsMultiplayer())
            {
                return PhotonNetwork.LocalPlayer.ActorNumber.ToString();
            }
            return "local";
        }

        private string GetLocalPlayerName()
        {
            if (SemiFunc.IsMultiplayer())
            {
                return PhotonNetwork.LocalPlayer.NickName;
            }
            return "Player";
        }
    }
}
