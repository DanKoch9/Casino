using System;

namespace FinalProjekt.Core;

public static class PayoutEngine
{
    public static int GetLogPayout(int bet, double baseMultiplier)
    {
        double scaling = 1.2; 
        double logBonus = Math.Log10(Math.Max(1, bet)) * scaling;
        double finalMultiplier = baseMultiplier + logBonus;
        
        return (int)Math.Round(bet * finalMultiplier);
    }
}