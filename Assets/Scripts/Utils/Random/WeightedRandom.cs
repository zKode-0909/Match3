using System.Collections.Generic;
using System;
/*
public static class WeightedRandom
{
    public static EltType Pick(IReadOnlyList<EltType> options)
    {
        if (options == null || options.Count == 0)
            throw new ArgumentException("No options provided.");

        float total = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            float w = options[i].weight;
            if (w > 0f) total += w;
        }

        if (total <= 0f)
            throw new ArgumentException("All weights are zero.");

        float roll = UnityEngine.Random.value * total; // [0, total)

        for (int i = 0; i < options.Count; i++)
        {
            float w = options[i].weight;
            if (w <= 0f) continue;

            roll -= w;
            if (roll < 0f)
                return options[i].item;
        }

        // Fallback (shouldn't happen due to float precision, but safe)
        return options[options.Count - 1].item;
    }
}*/