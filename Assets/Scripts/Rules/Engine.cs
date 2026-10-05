using System;
using System.Collections.Generic;
using System.Linq;

namespace MemonPatta.Rules
{
    // Card ids are unique per physical card: deck * 52 + suit * 13 + (rank - 1).
    public static class Cards
    {
        static readonly string[] SuitNames = { "Clubs", "Diamonds", "Hearts", "Spades" };
        static readonly string[] RankNames = { "", "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };

        public static int Rank(int id) { return id % 13 + 1; }
        public static int Suit(int id) { return (id / 13) % 4; }
        public static int Value(int id)
        {
            int r = Rank(id);
            return r == 1 ? 3 : r >= 11 ? 2 : 1;
        }
        public static string Name(int id) { return RankNames[Rank(id)] + " of " + SuitNames[Suit(id)]; }
    }

    public enum Phase { AwaitDraw = 0, Playing = 1, RoundOver = 2 }

    public class PlayerState
    {
        public int Actor;
        public string Name;
        public List<int> Hand = new List<int>();
        public bool Licensed;
        public int Required = -1;
        public int PlayedValue;
        public int BaseScore;
        public int RoundScore;
        public int Total;
        public int RoundsWon;
    }

    public class Meld
    {
        public int Id;
        public bool IsRun;
        public int Owner;
        public List<int> Cards = new List<int>();
    }

    public struct Result
    {
        public bool Ok;
        public string Error;
        public static Result Success { get { return new Result { Ok = true }; } }
        public static Result Fail(string error) { return new Result { Ok = false, Error = error }; }
    }

    // Pure rules engine (no UnityEngine). The room's master client runs it; every command is validated here
    // and an invalid command leaves the state untouched.
    public class Engine
    {
        public readonly List<PlayerState> Players = new List<PlayerState>();
        public readonly List<int> DrawPile = new List<int>();   // top = last
        public readonly List<int> Discard = new List<int>();    // top = last
        public readonly List<Meld> Melds = new List<Meld>();
        public int Active;
        public Phase Phase;
        public int Round;
        public int Winner = -1;
        public int TurnSerial;
        public string Log = "";

        Random rng;
        int nextMeldId = 1;

        public PlayerState ActivePlayer { get { return Players[Active]; } }
        public PlayerState Find(int actor) { return Players.FirstOrDefault(p => p.Actor == actor); }

        public void Begin(IList<KeyValuePair<int, string>> seats, int seed)
        {
            rng = new Random(seed);
            Players.Clear();
            Round = 0;
            Winner = -1;
            foreach (var s in seats) Players.Add(new PlayerState { Actor = s.Key, Name = s.Value });
            StartRound();
        }

        public void StartRound()
        {
            Round++;
            Melds.Clear();
            nextMeldId = 1;
            DrawPile.Clear();
            Discard.Clear();

            int decks = Players.Count <= 2 ? 1 : 2;
            for (int d = 0; d < decks; d++)
                for (int i = 0; i < 52; i++) DrawPile.Add(d * 52 + i);
            Shuffle(DrawPile);

            foreach (var p in Players)
            {
                p.Hand.Clear();
                p.Licensed = false;
                p.Required = -1;
                p.PlayedValue = 0;
                p.BaseScore = 0;
                p.RoundScore = 0;
            }
            for (int i = 0; i < 13; i++)
                foreach (var p in Players) p.Hand.Add(PopDraw());
            Discard.Add(PopDraw());

            int winnerIndex = Players.FindIndex(p => p.Actor == Winner);
            Active = (Round == 1 || winnerIndex < 0) ? rng.Next(Players.Count) : winnerIndex;
            Winner = -1;
            Phase = Phase.AwaitDraw;
            TurnSerial++;
            Log = "Round " + Round + " begins. " + ActivePlayer.Name + " goes first.";
        }

        int PopDraw()
        {
            int id = DrawPile[DrawPile.Count - 1];
            DrawPile.RemoveAt(DrawPile.Count - 1);
            return id;
        }

        void Shuffle(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }

        // ---- commands ----

        Result CheckTurn(int actor, Phase expected, out PlayerState p)
        {
            p = Find(actor);
            if (p == null) return Result.Fail("You are not in this game.");
            if (Phase == Phase.RoundOver) return Result.Fail("The round is over.");
            if (Players[Active] != p) return Result.Fail("It is not your turn.");
            if (Phase != expected)
                return Result.Fail(expected == Phase.AwaitDraw ? "You have already drawn this turn." : "Draw a card first.");
            return Result.Success;
        }

        public Result DrawFromPile(int actor)
        {
            PlayerState p;
            var r = CheckTurn(actor, Phase.AwaitDraw, out p);
            if (!r.Ok) return r;

            if (DrawPile.Count == 0 && Discard.Count > 1)
            {
                // Top discard stays; everything under it becomes the new draw pile.
                int top = Discard[Discard.Count - 1];
                DrawPile.AddRange(Discard.GetRange(0, Discard.Count - 1));
                Discard.Clear();
                Discard.Add(top);
                Shuffle(DrawPile);
            }
            if (DrawPile.Count == 0) return Result.Fail("The draw pile is empty. Take from the discard pile.");

            p.Hand.Add(PopDraw());
            Phase = Phase.Playing;
            Log = p.Name + " drew from the pile.";
            return Result.Success;
        }

        // depth 0 = top of the discard pile.
        public Result TakeFromDiscard(int actor, int depth)
        {
            PlayerState p;
            var r = CheckTurn(actor, Phase.AwaitDraw, out p);
            if (!r.Ok) return r;
            if (depth < 0 || depth >= Discard.Count) return Result.Fail("That discard card does not exist.");

            int index = Discard.Count - 1 - depth;
            int required = Discard[index];
            var taken = Discard.GetRange(index, depth + 1);
            var test = new List<int>(p.Hand);
            test.AddRange(taken);
            if (!CanUseRequired(p, test, required))
                return Result.Fail("You can't take the " + Cards.Name(required) + ": you would have no legal way to play it this turn.");

            Discard.RemoveRange(index, depth + 1);
            p.Hand.AddRange(taken);
            p.Required = required;
            Phase = Phase.Playing;
            Log = p.Name + " took " + taken.Count + " card(s) from the discard pile (needs the " + Cards.Name(required) + ").";
            return Result.Success;
        }

        // Same checks as TakeFromDiscard but without changing any state (used by bots).
        public bool CanTake(int actor, int depth)
        {
            var p = Find(actor);
            if (p == null || Phase != Phase.AwaitDraw || Players[Active] != p) return false;
            if (depth < 0 || depth >= Discard.Count) return false;
            int index = Discard.Count - 1 - depth;
            var test = new List<int>(p.Hand);
            test.AddRange(Discard.GetRange(index, depth + 1));
            return CanUseRequired(p, test, Discard[index]);
        }

        public Result PlayMeld(int actor, IList<int> ids)
        {
            PlayerState p;
            var r = CheckTurn(actor, Phase.Playing, out p);
            if (!r.Ok) return r;
            r = CheckHandCards(p, ids);
            if (!r.Ok) return r;

            bool run = IsRun(ids);
            bool set = IsSet(ids);
            if (!p.Licensed)
            {
                if (!run) return Result.Fail("You must play a Licensing run first: 3 or more consecutive cards of the same suit.");
            }
            else if (!run && !set)
            {
                return Result.Fail("A meld is a run (3+ consecutive, same suit, Ace low) or a set (3 or 4 of the same rank).");
            }

            var meld = new Meld { Id = nextMeldId++, IsRun = run, Owner = actor };
            meld.Cards.AddRange(run ? ids.OrderBy(Cards.Rank) : ids);
            Melds.Add(meld);
            Consume(p, ids);

            bool licensedNow = !p.Licensed;
            p.Licensed = true;
            Log = p.Name + (licensedNow ? " is now Licensed with a run." : run ? " played a run." : " played a set.");
            return Result.Success;
        }

        public Result ExtendMeld(int actor, int meldId, IList<int> ids)
        {
            PlayerState p;
            var r = CheckTurn(actor, Phase.Playing, out p);
            if (!r.Ok) return r;
            if (!p.Licensed) return Result.Fail("You can't extend melds until you are Licensed.");
            var meld = Melds.FirstOrDefault(m => m.Id == meldId);
            if (meld == null) return Result.Fail("That meld does not exist.");
            if (ids == null || ids.Count == 0) return Result.Fail("Select the cards to add.");
            r = CheckHandCards(p, ids);
            if (!r.Ok) return r;
            if (p.Required >= 0 && ids.Contains(p.Required) && meld.Owner != actor)
                return Result.Fail("The Required Card must be played on one of your own melds.");

            var all = new List<int>(meld.Cards);
            all.AddRange(ids);
            if (meld.IsRun)
            {
                if (!IsRun(all)) return Result.Fail("Those cards don't extend that run (same suit, consecutive, Ace low).");
                meld.Cards = all.OrderBy(Cards.Rank).ToList();
            }
            else
            {
                if (meld.Cards.Count >= 4) return Result.Fail("A 4-card set can't be extended.");
                if (ids.Count != 1 || Cards.Rank(ids[0]) != Cards.Rank(meld.Cards[0]))
                    return Result.Fail("A 3-card set can only take one card of the same rank.");
                meld.Cards = all;
            }
            Consume(p, ids);
            Log = p.Name + " extended a " + (meld.IsRun ? "run." : "set.");
            return Result.Success;
        }

        public Result DiscardCard(int actor, int cardId)
        {
            PlayerState p;
            var r = CheckTurn(actor, Phase.Playing, out p);
            if (!r.Ok) return r;
            if (!p.Hand.Contains(cardId)) return Result.Fail("That card is not in your hand.");
            if (p.Required >= 0)
                return Result.Fail("You must play the Required Card (" + Cards.Name(p.Required) + ") on one of your melds before you discard.");
            if (p.Hand.Count < 2) return Result.Fail("You only have one card left. Finish the round with it.");

            p.Hand.Remove(cardId);
            Discard.Add(cardId);
            Log = p.Name + " discarded the " + Cards.Name(cardId) + ".";
            EndTurn();
            return Result.Success;
        }

        public Result FinishRound(int actor, int cardId)
        {
            PlayerState p;
            var r = CheckTurn(actor, Phase.Playing, out p);
            if (!r.Ok) return r;
            if (p.Required >= 0)
                return Result.Fail("You must play the Required Card (" + Cards.Name(p.Required) + ") before you finish.");
            if (p.Hand.Count != 1 || p.Hand[0] != cardId) return Result.Fail("To finish you must have exactly one card left in hand.");

            Winner = p.Actor;
            Phase = Phase.RoundOver;
            foreach (var q in Players)
            {
                // The face-down final card counts as a card left in hand.
                int left = q.Hand.Sum(c => Cards.Value(c));
                q.BaseScore = q.PlayedValue - left;
                q.RoundScore = q.BaseScore * (q == p ? 10 : 5);
                q.Total += q.RoundScore;
            }
            p.RoundsWon++;
            Log = p.Name + " finished the round!";
            TurnSerial++;
            return Result.Success;
        }

        // Turn timer expiry: draw if needed, then discard a random card (never the Required Card). Melds stay.
        public void Timeout()
        {
            if (Phase == Phase.RoundOver) return;
            var p = ActivePlayer;
            if (Phase == Phase.AwaitDraw)
            {
                if (!DrawFromPile(p.Actor).Ok)
                {
                    EndTurn();
                    Log = p.Name + " ran out of time.";
                    return;
                }
            }
            var options = p.Hand.Where(c => c != p.Required).ToList();
            p.Required = -1;
            if (options.Count > 0 && p.Hand.Count > 1)
            {
                int pick = options[rng.Next(options.Count)];
                p.Hand.Remove(pick);
                Discard.Add(pick);
            }
            Log = p.Name + " ran out of time.";
            EndTurn();
        }

        // Returns false when fewer than two players remain.
        public bool RemovePlayer(int actor)
        {
            int index = Players.FindIndex(p => p.Actor == actor);
            if (index < 0) return Players.Count >= 2;
            var gone = Players[index];
            Discard.InsertRange(0, gone.Hand);
            Players.RemoveAt(index);
            Log = gone.Name + " left the game.";

            if (Phase != Phase.RoundOver && Players.Count > 0)
            {
                if (index < Active) Active--;
                else if (index == Active)
                {
                    if (Active >= Players.Count) Active = 0;
                    Phase = Phase.AwaitDraw;
                    TurnSerial++;
                }
            }
            return Players.Count >= 2;
        }

        void EndTurn()
        {
            Active = (Active + 1) % Players.Count;
            Phase = Phase.AwaitDraw;
            TurnSerial++;
        }

        // ---- validation helpers ----

        Result CheckHandCards(PlayerState p, IList<int> ids)
        {
            if (ids == null || ids.Count == 0) return Result.Fail("Select some cards first.");
            if (ids.Distinct().Count() != ids.Count) return Result.Fail("The same card was selected twice.");
            if (ids.Any(c => !p.Hand.Contains(c))) return Result.Fail("Those cards are not all in your hand.");
            if (p.Hand.Count - ids.Count < 1) return Result.Fail("Keep one card in hand: you finish by placing your last card face down.");
            return Result.Success;
        }

        void Consume(PlayerState p, IList<int> ids)
        {
            foreach (var c in ids)
            {
                p.Hand.Remove(c);
                p.PlayedValue += Cards.Value(c);
                if (c == p.Required) p.Required = -1;
            }
        }

        public static bool IsRun(IList<int> ids)
        {
            if (ids.Count < 3) return false;
            int suit = Cards.Suit(ids[0]);
            if (ids.Any(c => Cards.Suit(c) != suit)) return false;
            var ranks = ids.Select(Cards.Rank).ToList();
            if (ranks.Distinct().Count() != ranks.Count) return false;
            return ranks.Max() - ranks.Min() == ranks.Count - 1; // Ace is rank 1 (low only), so K-A-2 style wraps fail
        }

        public static bool IsSet(IList<int> ids)
        {
            if (ids.Count < 3 || ids.Count > 4) return false;
            int rank = Cards.Rank(ids[0]);
            return ids.All(c => Cards.Rank(c) == rank);
        }

        // Can the Required Card be played this turn with this hand (plus current own melds)?
        bool CanUseRequired(PlayerState p, List<int> hand, int req)
        {
            int rank = Cards.Rank(req), suit = Cards.Suit(req);
            var suited = new HashSet<int>(hand.Where(c => c != req && Cards.Suit(c) == suit).Select(Cards.Rank));
            suited.Add(rank);

            int lo = rank, hi = rank;
            while (suited.Contains(lo - 1)) lo--;
            while (suited.Contains(hi + 1)) hi++;
            if (hi - lo + 1 >= 3) return true;           // new run (also the Licensing run)
            if (!p.Licensed) return false;

            if (hand.Count(c => c != req && Cards.Rank(c) == rank) >= 2) return true; // new set
            foreach (var m in Melds.Where(x => x.Owner == p.Actor))
            {
                if (!m.IsRun)
                {
                    if (m.Cards.Count == 3 && Cards.Rank(m.Cards[0]) == rank) return true;
                }
                else if (Cards.Suit(m.Cards[0]) == suit)
                {
                    int min = m.Cards.Min(Cards.Rank), max = m.Cards.Max(Cards.Rank);
                    if (rank > max && Enumerable.Range(max + 1, rank - max - 1).All(suited.Contains)) return true;
                    if (rank < min && Enumerable.Range(rank + 1, min - rank - 1).All(suited.Contains)) return true;
                }
            }
            return false;
        }
    }
}
