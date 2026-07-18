using UnityEngine;

namespace SuperSniper8D
{
    public enum UpgradeTrack
    {
        Damage = 0,     // score multiplier per kill (drives credit income)
        Stability = 1,  // less sway
        Zoom = 2,       // higher scope magnification
        Clip = 3,       // larger magazine
    }

    /// <summary>
    /// The single rifle upgrade tree: four tracks, five levels each, with a
    /// linearly-escalating credit cost per level. All effect curves live here so
    /// the weapon, UI and economy stay in agreement.
    /// </summary>
    public static class Upgrades
    {
        public const int MaxLevel = 5;

        public static readonly string[] Names = { "DAMAGE", "STABILITY", "ZOOM", "CLIP" };

        static readonly int[] BaseCost = { 150, 120, 200, 180 };

        /// <summary>Credits to go from <paramref name="level"/> to the next, or -1 if maxed.</summary>
        public static int NextCost(UpgradeTrack track, int level)
        {
            if (level >= MaxLevel) return -1;
            return BaseCost[(int)track] * (level + 1);
        }

        public static float ScoreMultiplier(int level) => 1f + 0.08f * level;   // 1.00 .. 1.40
        public static float SwayMultiplier(int level) => Mathf.Max(0.35f, 1f - 0.12f * level); // 1.00 .. 0.40
        public static float ScopeMultiplier(int level) => 8f + level;           // 8x .. 13x
        public static int ClipSize(int level) => 5 + level;                     // 5 .. 10

        public static string EffectLabel(UpgradeTrack track, int level)
        {
            switch (track)
            {
                case UpgradeTrack.Damage: return $"x{ScoreMultiplier(level):0.00} score";
                case UpgradeTrack.Stability: return $"{(1f - SwayMultiplier(level)) * 100f:0}% steadier";
                case UpgradeTrack.Zoom: return $"{ScopeMultiplier(level):0}x scope";
                case UpgradeTrack.Clip: return $"{ClipSize(level)} rounds";
                default: return "";
            }
        }
    }
}
