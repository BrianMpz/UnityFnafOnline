using System;
using UnityEngine;

public static class XPManager
{
    public const uint MaxXp = 1000000000;
    private const uint BaseLevel = 1;
    private const uint BaseXp = 100; // Base XP needed for level 1 → 2
    private const float LevelScalingFactor = 2f; // XP scaling curve exponent — higher values make later levels require more XP

    /// <summary>
    /// Calculates the total XP required to reach a specific level.
    /// </summary>
    public static uint GetTotalXpForLevel(uint level)
    {
        if (level == BaseLevel) return 0;

        float total = 0;
        for (uint i = 1; i < level; i++)
        {
            total += BaseXp * Mathf.Pow(i, LevelScalingFactor);
        }

        return (uint)Mathf.FloorToInt(total);
    }

    /// <summary>
    /// Returns the player's current level based on their total XP.
    /// </summary>
    public static uint GetLevelFromXp(uint totalXp)
    {
        uint level = BaseLevel;

        while (GetTotalXpForLevel(level + 1) <= totalXp)
        {
            level++;
        }
        return level;
    }

    /// <summary>
    /// Returns the XP required to reach the next level.
    /// </summary>
    public static uint GetXpToNextLevel(uint currentXp)
    {
        uint currentLevel = GetLevelFromXp(currentXp);
        uint nextLevel = currentLevel + 1;
        return GetTotalXpForLevel(nextLevel) - currentXp;
    }

    /// <summary>
    /// Returns the player's current progress between levels as a value from 0 to 1.
    /// </summary>
    public static float GetLevelProgress(uint currentXp)
    {
        uint currentLevel = GetLevelFromXp(currentXp);
        uint xpThisLevelStart = GetTotalXpForLevel(currentLevel);
        uint xpNextLevelStart = GetTotalXpForLevel(currentLevel + 1);

        uint xpuintoCurrentLevel = currentXp - xpThisLevelStart;
        uint xpForThisLevel = xpNextLevelStart - xpThisLevelStart;

        if (xpForThisLevel == 0) return 1f;

        return Mathf.Clamp01((float)xpuintoCurrentLevel / xpForThisLevel);
    }

}
