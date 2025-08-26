using System.Collections.Generic;
using UnityEngine;

public class GlobalColourManager
{
    private readonly List<Color> availableColors;
    private readonly HashSet<Color> usedColors = new HashSet<Color>();

    public GlobalColourManager(Color[] baseColors)
    {
        availableColors = new List<Color>(baseColors);
    }

    /// <summary>
    /// Get a unique unused color. If all base colors are used, generate a random new one.
    /// </summary>
    public Color GetUniqueColor()
    {
        // If we still have unused predefined colors
        foreach (var color in availableColors)
        {
            if (!usedColors.Contains(color))
            {
                usedColors.Add(color);
                return color;
            }
        }

        // Otherwise generate a random color until we get a new one
        Color randomColor;
        do
        {
            randomColor = new Color(Random.value, Random.value, Random.value);
        }
        while (usedColors.Contains(randomColor));

        usedColors.Add(randomColor);
        return randomColor;
    }

    /// <summary>
    /// Reset used colors (for restarting a game, etc.)
    /// </summary>
    public void Reset()
    {
        usedColors.Clear();
    }
}