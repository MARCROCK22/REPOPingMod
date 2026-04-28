using System;
using System.Globalization;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using REPOLib.Modules;
using REPOPingMod.PingSystem;
using UnityEngine;

namespace REPOPingMod.Networking
{
    public class PingNetSync : MonoBehaviour
    {
        public static PingNetSync Instance { get; private set; }

        private NetworkedEvent _pingEvent;
        private bool _initialized;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void Initialize()
        {
            try
            {
                _pingEvent = new NetworkedEvent("PingSystem_PlacePing", HandlePingEventData);
                _initialized = true;
                Plugin.Logger.LogInfo("Ping network sync initialized via REPOLib NetworkedEvent");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"REPOLib NetworkedEvent init failed: {ex.Message} — local-only mode");
                _initialized = false;
            }
        }

        public void BroadcastPing(Vector3 position, PingType type, string playerId, string playerName, string hitObjectName = "")
        {
            if (!_initialized) return;
            if (!SemiFunc.IsMultiplayer()) return;

            // Format: x|y|z|type|playerId|hitObjectName|playerName (playerName last since it may contain |)
            string data = string.Join("|",
                position.x.ToString(CultureInfo.InvariantCulture),
                position.y.ToString(CultureInfo.InvariantCulture),
                position.z.ToString(CultureInfo.InvariantCulture),
                ((int)type).ToString(),
                playerId,
                hitObjectName ?? "",
                playerName);

            var raiseEventOptions = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others
            };
            var sendOptions = SendOptions.SendReliable;

            _pingEvent.RaiseEvent(data, raiseEventOptions, sendOptions);
        }

        private void HandlePingEventData(EventData eventData)
        {
            if (eventData.CustomData is string data)
                HandlePingReceived(data);
        }

        private static readonly System.Collections.Generic.Dictionary<string, float> _lastPingTime = new();

        private void HandlePingReceived(string data)
        {
            try
            {
                string[] parts = data.Split('|');
                if (parts.Length < 7) return;

                float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float z = float.Parse(parts[2], CultureInfo.InvariantCulture);
                Vector3 position = new Vector3(x, y, z);

                PingType type = (PingType)int.Parse(parts[3]);
                if (!System.Enum.IsDefined(typeof(PingType), type))
                    return;

                string playerId = parts[4];
                string hitObjectName = parts[5];
                string playerName = string.Join("|", parts, 6, parts.Length - 6);

                if (SemiFunc.IsMultiplayer() &&
                    playerId == PhotonNetwork.LocalPlayer.ActorNumber.ToString())
                    return;

                // Rate limit: max 1 ping per player per 0.5 seconds to prevent freezing
                float now = Time.time;
                if (_lastPingTime.TryGetValue(playerId, out float lastTime) && now - lastTime < 0.5f)
                    return;
                _lastPingTime[playerId] = now;

                // Find hit object for preview (now async — won't freeze)
                GameObject hitObject = null;
                if (!string.IsNullOrEmpty(hitObjectName))
                    hitObject = PingManager.Instance?.FindObjectByName(position, hitObjectName);

                PingManager.Instance?.PlacePing(position, type, playerId, playerName, hitObject);

                // Make the remote player's arm point at the ping
                if (int.TryParse(playerId, out int actorNum))
                    ArmPointing.StartPointingForPlayer(actorNum, position, 1f);

                // Only play sound if within 20m
                var cam = Camera.main;
                if (cam != null && Vector3.Distance(cam.transform.position, position) <= 20f)
                    PingAudio.Play(type);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning($"Failed to process received ping: {ex.Message}");
            }
        }
    }
}
