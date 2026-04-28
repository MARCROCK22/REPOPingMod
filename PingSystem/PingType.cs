using UnityEngine;

namespace REPOPingMod.PingSystem
{
    public enum PingType
    {
        GoHere = 0,
        Danger = 1,
        Enemy = 2,
        Loot = 3
    }

    public static class PingTypeExtensions
    {
        public static Color GetColor(this PingType type)
        {
            return type switch
            {
                PingType.GoHere => new Color(0.2f, 0.6f, 1f, 1f),
                PingType.Danger => new Color(1f, 0.2f, 0.2f, 1f),
                PingType.Enemy => new Color(1f, 0.6f, 0.1f, 1f),
                PingType.Loot => new Color(0.2f, 0.9f, 0.3f, 1f),
                _ => Color.white
            };
        }

        public static string GetLabel(this PingType type)
        {
            return type switch
            {
                PingType.GoHere => "Go Here",
                PingType.Danger => "Danger",
                PingType.Enemy => "Enemy",
                PingType.Loot => "Loot",
                _ => "Ping"
            };
        }
    }
}
