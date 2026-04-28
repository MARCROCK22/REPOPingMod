using BepInEx.Configuration;
using UnityEngine;

namespace REPOPingMod.PingSystem
{
    public class PingConfig
    {
        public ConfigEntry<KeyCode> PingKey { get; }
        public ConfigEntry<float> PingDuration { get; }
        public ConfigEntry<float> PingFadeDuration { get; }
        public ConfigEntry<float> MaxPingDistance { get; }
        public ConfigEntry<float> SoundVolume { get; }
        public ConfigEntry<bool> SoundEnabled { get; }
        public ConfigEntry<bool> ShowDistance { get; }
        public ConfigEntry<bool> ShowPlayerName { get; }
        public ConfigEntry<float> RadialWheelDelay { get; }

        public PingConfig(ConfigFile config)
        {
            PingKey = config.Bind("Input", "PingKey", KeyCode.Mouse2,
                "Key to activate ping");

            PingDuration = config.Bind("Ping", "PingDuration", 4f,
                new ConfigDescription("How long the ping marker lasts (seconds)",
                    new AcceptableValueRange<float>(1f, 30f)));

            PingFadeDuration = config.Bind("Ping", "PingFadeDuration", 2f,
                new ConfigDescription("Fade out duration at end of ping (seconds)",
                    new AcceptableValueRange<float>(0f, 5f)));

            MaxPingDistance = config.Bind("Ping", "MaxPingDistance", 100f,
                new ConfigDescription("Maximum raycast distance for placing pings",
                    new AcceptableValueRange<float>(10f, 500f)));

            SoundVolume = config.Bind("Audio", "SoundVolume", 0.7f,
                new ConfigDescription("Ping sound volume",
                    new AcceptableValueRange<float>(0f, 1f)));

            SoundEnabled = config.Bind("Audio", "SoundEnabled", true,
                "Enable or disable ping sounds");

            ShowDistance = config.Bind("Display", "ShowDistance", true,
                "Show distance on ping marker");

            ShowPlayerName = config.Bind("Display", "ShowPlayerName", true,
                "Show player name on ping marker");

            RadialWheelDelay = config.Bind("Input", "RadialWheelDelay", 0.3f,
                new ConfigDescription("Seconds to hold before radial wheel opens",
                    new AcceptableValueRange<float>(0.1f, 1f)));
        }
    }
}
