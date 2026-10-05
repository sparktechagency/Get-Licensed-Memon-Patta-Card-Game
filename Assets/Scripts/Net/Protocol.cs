using System;

namespace MemonPatta.Net
{
    // Event codes sent with PhotonNetwork.RaiseEvent.
    public static class EventCodes
    {
        public const byte Command = 1;   // client -> master client (CommandDto json)
        public const byte View = 2;      // master -> one player (ViewDto json, only that player's hand)
        public const byte Reject = 3;    // master -> one player (plain text reason)
        public const byte MatchEnd = 4;  // master -> everyone (standings text)
    }

    [Serializable]
    public class CommandDto
    {
        public string type;   // draw, take, play, extend, discard, finish, next, end
        public int a;         // take: discard depth; extend: meld id; discard/finish: card id
        public int[] ids;     // play/extend: card ids
    }

    [Serializable]
    public class PlayerDto
    {
        public int actor;
        public string name;
        public int handCount;
        public bool licensed;
        public int played;
        public int remaining;
        public int baseScore;
        public int roundScore;
        public int total;
    }

    [Serializable]
    public class MeldDto
    {
        public int id;
        public bool isRun;
        public int owner;
        public int[] cards;
    }

    // Snapshot of the game for ONE player: it never contains other hands or the draw pile order.
    [Serializable]
    public class ViewDto
    {
        public int round;
        public int phase;        // 0 AwaitDraw, 1 Playing, 2 RoundOver
        public int activeActor;
        public int winner;
        public int[] hand;
        public int required;     // -1 when none
        public int drawCount;
        public int[] discard;    // top is last
        public MeldDto[] melds;
        public PlayerDto[] players;
        public double deadline;  // PhotonNetwork.Time based, 0 = no timer
        public string log;
    }
}
