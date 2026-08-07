using System;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Engine
{
    public enum PokerHandType
    {
        HighCard,
        Pair,
        Spectrum,      // All 5 cards of different colors
        TwoPair,
        ThreeOfAKind,
        FullHouse,
        FourOfAKind,
        Flush          // All 5 cards of same color
    }

    public class PokerHandEvaluator
    {
        public static PokerHandType Evaluate(List<GemColor> cards)
        {
            if (cards == null || cards.Count < 5) return PokerHandType.HighCard;

            Dictionary<GemColor, int> counts = new Dictionary<GemColor, int>();
            foreach (var c in cards)
            {
                if (!counts.ContainsKey(c)) counts[c] = 0;
                counts[c]++;
            }

            if (counts.Count == 1) return PokerHandType.Flush;
            if (counts.Count == 5) return PokerHandType.Spectrum;

            bool has3 = false;
            int pairs = 0;

            foreach (var kvp in counts.Values)
            {
                if (kvp == 4) return PokerHandType.FourOfAKind;
                if (kvp == 3) has3 = true;
                if (kvp == 2) pairs++;
            }

            if (has3 && pairs == 1) return PokerHandType.FullHouse;
            if (has3) return PokerHandType.ThreeOfAKind;
            if (pairs == 2) return PokerHandType.TwoPair;
            if (pairs == 1) return PokerHandType.Pair;

            return PokerHandType.HighCard;
        }

        public static int GetHandPoints(PokerHandType hand)
        {
            switch (hand)
            {
                case PokerHandType.Flush: return 2500;
                case PokerHandType.FourOfAKind: return 2000;
                case PokerHandType.FullHouse: return 1500;
                case PokerHandType.ThreeOfAKind: return 1000;
                case PokerHandType.TwoPair: return 750;
                case PokerHandType.Spectrum: return 500;
                case PokerHandType.Pair: return 250;
                default: return 100;
            }
        }
    }
}
