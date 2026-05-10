using System;

namespace FinalProjekt.Core;

public static class PayoutEngine
{
    public static long GetLogPayout(long bet, double baseMultiplier)
    {
        double scaling = 1.2; 
        double logBonus = Math.Log10(Math.Max(1, bet)) * scaling;
        double finalMultiplier = baseMultiplier + logBonus;
        
        return (long)Math.Round(bet * finalMultiplier);
    }
}