using System.Collections.Generic;
using System.Linq;

namespace MemonPatta.Rules
{
    // Greedy bot. It uses the same Engine commands as a human and only looks at its own hand, the table melds
    // and the discard pile. Each Step() performs ONE action so players can follow what the bot does.
    public static class BotBrain
    {
        public static void Step(Engine e)
        {
            var p = e.ActivePlayer;
            int a = p.Actor;

            if (e.Phase == Phase.AwaitDraw)
            {
                // Take the top discard when the engine says it is playable this turn; otherwise draw.
                if (e.CanTake(a, 0) && e.TakeFromDiscard(a, 0).Ok) return;
                if (!e.DrawFromPile(a).Ok && !e.TakeFromDiscard(a, 0).Ok) e.Timeout();
                return;
            }

            if (TryPlay(e, p)) return;

            if (p.Required < 0 && p.Hand.Count == 1)
            {
                e.FinishRound(a, p.Hand[0]);
                return;
            }
            if (p.Required < 0 && p.Hand.Count >= 2 && e.DiscardCard(a, PickDiscard(e, p)).Ok) return;
            e.Timeout();   // could not do anything legal (should be rare)
        }

        static bool TryPlay(Engine e, PlayerState p)
        {
            int a = p.Actor;
            int req = p.Required;

            foreach (var run in Runs(p))
                if ((req < 0 || run.Contains(req)) && e.PlayMeld(a, run).Ok) return true;
            if (!p.Licensed) return false;

            foreach (var set in Sets(p))
                if ((req < 0 || set.Contains(req)) && e.PlayMeld(a, set).Ok) return true;

            foreach (var m in e.Melds.ToList())
                foreach (int c in p.Hand.ToList())
                {
                    if (req >= 0 && c != req) continue;
                    if (c == req && m.Owner != a) continue;
                    if (e.ExtendMeld(a, m.Id, new[] { c }).Ok) return true;
                }
            return false;
        }

        static IEnumerable<int[]> Runs(PlayerState p)
        {
            for (int suit = 0; suit < 4; suit++)
            {
                var byRank = new SortedDictionary<int, int>();
                foreach (int c in p.Hand.Where(c => Cards.Suit(c) == suit))
                    if (!byRank.ContainsKey(Cards.Rank(c)) || c == p.Required) byRank[Cards.Rank(c)] = c;

                var segment = new List<int>();
                int last = -10;
                foreach (var kv in byRank.Concat(new[] { new KeyValuePair<int, int>(99, -1) }))
                {
                    if (kv.Key == last + 1) segment.Add(kv.Value);
                    else
                    {
                        if (segment.Count >= 3) yield return Trim(p, segment);
                        segment = new List<int>();
                        if (kv.Key != 99) segment.Add(kv.Value);
                    }
                    last = kv.Key;
                }
            }
        }

        static IEnumerable<int[]> Sets(PlayerState p)
        {
            foreach (var g in p.Hand.GroupBy(Cards.Rank).Where(g => g.Count() >= 3))
                yield return Trim(p, g.Take(4).ToList());
        }

        // The engine always makes you keep one card in hand.
        static int[] Trim(PlayerState p, List<int> cards)
        {
            var list = new List<int>(cards);
            while (list.Count > 3 && p.Hand.Count - list.Count < 1) list.RemoveAt(list.Count - 1);
            return list.ToArray();
        }

        // Discard the card least likely to join a meld; ties go to the higher value (it costs more if left in hand).
        static int PickDiscard(Engine e, PlayerState p)
        {
            return p.Hand.Where(c => c != p.Required)
                .OrderBy(c => Usefulness(p, c))
                .ThenByDescending(c => Cards.Value(c))
                .First();
        }

        static double Usefulness(PlayerState p, int c)
        {
            int rank = Cards.Rank(c), suit = Cards.Suit(c);
            double score = 0;
            foreach (int o in p.Hand)
            {
                if (o == c) continue;
                if (Cards.Rank(o) == rank) score += 2;
                if (Cards.Suit(o) == suit)
                {
                    int d = System.Math.Abs(Cards.Rank(o) - rank);
                    if (d == 1) score += 2;
                    else if (d == 2) score += 1;
                }
            }
            return score;
        }
    }
}
