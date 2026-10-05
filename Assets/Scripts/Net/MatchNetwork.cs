using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using MemonPatta.Rules;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace MemonPatta.Net
{
    public enum NetState { Offline, Connecting, Lobby, Room, Playing }

    // PUN 2 layer. The room's master client hosts the Engine and validates every command; other clients only
    // send commands and render the personal ViewDto they receive.
    public class MatchNetwork : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        public static MatchNetwork Instance { get; private set; }

        public const int MaxPlayers = 5;
        public float turnSeconds = 120f;
        public bool useTurnTimer = true;
        public float botWaitSeconds = 10f;   // after creating a room, wait this long for a human before adding bots
        public int autoBots = 2;
        public float botDelay = 1.2f;
        [Tooltip("Editor testing without a Photon App Id: runs a local one-player room.")]
        public bool offlineTest;

        public NetState State { get; private set; }
        public ViewDto View { get; private set; }

        public event Action<NetState> StateChanged;
        public event Action RoomChanged;
        public event Action<ViewDto> ViewChanged;
        public event Action<string> Toast;
        public event Action<string> MatchEnded;

        Engine engine;                 // master client only
        int lastTurnSerial = -1;
        double deadline;
        string pendingCode;
        int createRetries;
        float countdownEnd = -1f;
        float botTimer;

        // Seconds left before bots are added, or -1 when no countdown is running.
        public float BotCountdown { get { return countdownEnd < 0 ? -1f : Mathf.Max(0f, countdownEnd - Time.unscaledTime); } }

        public string RoomCode { get { return PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.Name : ""; } }
        public bool IsHost { get { return PhotonNetwork.IsMasterClient; } }
        public int LocalActor { get { return PhotonNetwork.LocalPlayer.ActorNumber; } }

        void Awake()
        {
            Instance = this;
            PhotonNetwork.AutomaticallySyncScene = false;
            if (offlineTest) PhotonNetwork.OfflineMode = true;
        }

        void Update()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (engine == null)
            {
                if (countdownEnd >= 0 && State == NetState.Room && PhotonNetwork.InRoom)
                {
                    if (PhotonNetwork.CurrentRoom.PlayerCount > 1) countdownEnd = -1f;
                    else if (Time.unscaledTime >= countdownEnd) BeginMatch(autoBots);
                }
                return;
            }
            if (engine.Phase == Phase.RoundOver) return;

            if (engine.ActivePlayer.Actor < 0)
            {
                botTimer -= Time.unscaledDeltaTime;
                if (botTimer <= 0)
                {
                    botTimer = botDelay;
                    BotBrain.Step(engine);
                    AfterChange();
                }
                return;
            }
            if (!useTurnTimer || deadline <= 0) return;
            if (PhotonNetwork.Time > deadline)
            {
                engine.Timeout();
                AfterChange();
            }
        }

        void SetState(NetState s)
        {
            State = s;
            if (StateChanged != null) StateChanged(s);
        }

        void Say(string msg) { if (Toast != null) Toast(msg); }

        // ---- connection and rooms ----

        public void Connect(string nickname)
        {
            var settings = PhotonNetwork.PhotonServerSettings;
            if (!offlineTest && (settings == null || string.IsNullOrEmpty(settings.AppSettings.AppIdRealtime)))
            {
                Say("Photon App Id is missing. Paste your PUN App Id into PhotonServerSettings.");
                return;
            }
            PhotonNetwork.NickName = nickname;
            if (PhotonNetwork.IsConnected) { SetState(PhotonNetwork.InRoom ? NetState.Room : NetState.Lobby); return; }
            SetState(NetState.Connecting);
            if (!PhotonNetwork.ConnectUsingSettings())
            {
                SetState(NetState.Offline);
                Say("Could not start the connection.");
            }
        }

        public void Disconnect()
        {
            if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
            else SetState(NetState.Offline);
        }

        static string NewCode()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var c = new char[5];
            for (int i = 0; i < c.Length; i++) c[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            return new string(c);
        }

        public void CreateRoom()
        {
            createRetries = 0;
            PhotonNetwork.CreateRoom(NewCode(), new RoomOptions { MaxPlayers = MaxPlayers, IsVisible = false, IsOpen = true });
        }

        public void QuickMatch()
        {
            createRetries = 0;
            PhotonNetwork.JoinRandomOrCreateRoom(
                roomName: NewCode(),
                roomOptions: new RoomOptions { MaxPlayers = MaxPlayers, IsVisible = true, IsOpen = true });
        }

        public void JoinRoom(string code)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length == 0) { Say("Enter a room code."); return; }
            PhotonNetwork.JoinRoom(code);
        }

        public void LeaveRoom()
        {
            countdownEnd = -1f;
            engine = null;
            View = null;
            if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
        }

        // ---- match control (host) ----

        public void StartMatch()
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (PhotonNetwork.PlayerList.Length < 2) { Say("You need at least 2 players to start."); return; }
            BeginMatch(0);
        }

        void BeginMatch(int bots)
        {
            countdownEnd = -1f;
            var players = PhotonNetwork.PlayerList;

            PhotonNetwork.CurrentRoom.IsOpen = false;
            engine = new Engine();
            var seats = players.Select(p => new KeyValuePair<int, string>(p.ActorNumber, string.IsNullOrEmpty(p.NickName) ? "Player " + p.ActorNumber : p.NickName)).ToList();
            string[] botNames = { "Bot Alex", "Bot Sam", "Bot Max", "Bot Zoe" };
            bots = Mathf.Clamp(bots, 0, MaxPlayers - seats.Count);
            for (int i = 0; i < bots; i++) seats.Add(new KeyValuePair<int, string>(-(i + 1), botNames[i % botNames.Length]));
            engine.Begin(seats, Environment.TickCount);
            botTimer = botDelay;
            lastTurnSerial = -1;
            AfterChange();
        }

        void EndMatchAsHost(string reason)
        {
            if (engine == null) return;
            string standings = reason + "\n\n" + string.Join("\n",
                engine.Players.OrderByDescending(p => p.Total)
                    .Select((p, i) => (i + 1) + ". " + p.Name + "  -  " + p.Total + " pts  (" + p.RoundsWon + " round wins)").ToArray());
            engine = null;
            if (PhotonNetwork.InRoom) PhotonNetwork.CurrentRoom.IsOpen = true;
            PhotonNetwork.RaiseEvent(EventCodes.MatchEnd, standings, new RaiseEventOptions { Receivers = ReceiverGroup.Others }, SendOptions.SendReliable);
            OnMatchEndReceived(standings);
        }

        // ---- commands (any player) ----

        public void Send(CommandDto cmd)
        {
            if (PhotonNetwork.IsMasterClient) HandleCommand(LocalActor, cmd);
            else PhotonNetwork.RaiseEvent(EventCodes.Command, JsonUtility.ToJson(cmd),
                new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient }, SendOptions.SendReliable);
        }

        public void Draw() { Send(new CommandDto { type = "draw" }); }
        public void TakeDiscard(int depth) { Send(new CommandDto { type = "take", a = depth }); }
        public void PlayMeld(int[] ids) { Send(new CommandDto { type = "play", ids = ids }); }
        public void Extend(int meldId, int[] ids) { Send(new CommandDto { type = "extend", a = meldId, ids = ids }); }
        public void DiscardCard(int id) { Send(new CommandDto { type = "discard", a = id }); }
        public void Finish(int id) { Send(new CommandDto { type = "finish", a = id }); }
        public void NextRound() { Send(new CommandDto { type = "next" }); }
        public void EndMatch() { Send(new CommandDto { type = "end" }); }

        // ---- master client: apply a command ----

        void HandleCommand(int sender, CommandDto c)
        {
            if (engine == null || c == null) return;
            Result r;
            switch (c.type)
            {
                case "draw": r = engine.DrawFromPile(sender); break;
                case "take": r = engine.TakeFromDiscard(sender, c.a); break;
                case "play": r = engine.PlayMeld(sender, c.ids); break;
                case "extend": r = engine.ExtendMeld(sender, c.a, c.ids); break;
                case "discard": r = engine.DiscardCard(sender, c.a); break;
                case "finish": r = engine.FinishRound(sender, c.a); break;
                case "next":
                    if (sender != PhotonNetwork.MasterClient.ActorNumber) r = Result.Fail("Only the host can start the next round.");
                    else if (engine.Phase != Phase.RoundOver) r = Result.Fail("The round is still running.");
                    else { engine.StartRound(); r = Result.Success; }
                    break;
                case "end":
                    if (sender != PhotonNetwork.MasterClient.ActorNumber) { SendReject(sender, "Only the host can end the match."); return; }
                    EndMatchAsHost("Match ended by the host.");
                    return;
                default: r = Result.Fail("Unknown command."); break;
            }
            if (!r.Ok) { SendReject(sender, r.Error); return; }
            AfterChange();
        }

        void AfterChange()
        {
            if (engine.TurnSerial != lastTurnSerial)
            {
                lastTurnSerial = engine.TurnSerial;
                deadline = (useTurnTimer && engine.Phase != Phase.RoundOver) ? PhotonNetwork.Time + turnSeconds : 0;
            }
            foreach (var p in PhotonNetwork.PlayerList)
            {
                string json = JsonUtility.ToJson(BuildView(p.ActorNumber));
                if (p.IsLocal) OnViewReceived(json);
                else PhotonNetwork.RaiseEvent(EventCodes.View, json,
                    new RaiseEventOptions { TargetActors = new[] { p.ActorNumber } }, SendOptions.SendReliable);
            }
        }

        void SendReject(int actor, string reason)
        {
            if (actor == LocalActor) { Say(reason); return; }
            PhotonNetwork.RaiseEvent(EventCodes.Reject, reason,
                new RaiseEventOptions { TargetActors = new[] { actor } }, SendOptions.SendReliable);
        }

        ViewDto BuildView(int actor)
        {
            var me = engine.Find(actor);
            return new ViewDto
            {
                round = engine.Round,
                phase = (int)engine.Phase,
                activeActor = engine.ActivePlayer.Actor,
                winner = engine.Winner,
                hand = me != null ? me.Hand.ToArray() : new int[0],
                required = me != null ? me.Required : -1,
                drawCount = engine.DrawPile.Count,
                discard = engine.Discard.ToArray(),
                melds = engine.Melds.Select(m => new MeldDto { id = m.Id, isRun = m.IsRun, owner = m.Owner, cards = m.Cards.ToArray() }).ToArray(),
                players = engine.Players.Select(p => new PlayerDto
                {
                    actor = p.Actor,
                    name = p.Name,
                    handCount = p.Hand.Count,
                    licensed = p.Licensed,
                    played = p.PlayedValue,
                    remaining = p.Hand.Sum(c => Cards.Value(c)),
                    baseScore = p.BaseScore,
                    roundScore = p.RoundScore,
                    total = p.Total
                }).ToArray(),
                deadline = deadline,
                log = engine.Log
            };
        }

        // ---- receiving ----

        public void OnEvent(EventData e)
        {
            switch (e.Code)
            {
                case EventCodes.Command:
                    if (PhotonNetwork.IsMasterClient) HandleCommand(e.Sender, JsonUtility.FromJson<CommandDto>((string)e.CustomData));
                    break;
                case EventCodes.View: OnViewReceived((string)e.CustomData); break;
                case EventCodes.Reject: Say((string)e.CustomData); break;
                case EventCodes.MatchEnd: OnMatchEndReceived((string)e.CustomData); break;
            }
        }

        void OnViewReceived(string json)
        {
            View = JsonUtility.FromJson<ViewDto>(json);
            if (State != NetState.Playing) SetState(NetState.Playing);
            if (ViewChanged != null) ViewChanged(View);
        }

        void OnMatchEndReceived(string standings)
        {
            View = null;
            if (State == NetState.Playing) SetState(NetState.Room);
            if (MatchEnded != null) MatchEnded(standings);
        }

        // ---- PUN callbacks ----

        public override void OnConnectedToMaster()
        {
            if (State == NetState.Connecting || State == NetState.Offline || State == NetState.Room || State == NetState.Playing)
                SetState(NetState.Lobby);
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            engine = null;
            View = null;
            SetState(NetState.Offline);
            if (cause != DisconnectCause.DisconnectByClientLogic) Say("Disconnected: " + cause);
        }

        public override void OnJoinedRoom()
        {
            // The creator waits for a human opponent; bots are added if nobody joins in time.
            countdownEnd = (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 1 && autoBots > 0)
                ? Time.unscaledTime + botWaitSeconds : -1f;
            SetState(NetState.Room);
            if (RoomChanged != null) RoomChanged();
        }

        public override void OnLeftRoom()
        {
            engine = null;
            View = null;
            // PUN returns to the master server; OnConnectedToMaster moves us back to the lobby.
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            if (returnCode == ErrorCode.GameIdAlreadyExists && createRetries++ < 3) { CreateRoom(); return; }
            Say("Could not create the room: " + message);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            if (returnCode == ErrorCode.GameDoesNotExist) Say("No room with that code.");
            else if (returnCode == ErrorCode.GameFull) Say("That room is full.");
            else if (returnCode == ErrorCode.GameClosed) Say("That match has already started.");
            else Say("Could not join: " + message);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Say("Quick match failed: " + message);
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            countdownEnd = -1f;
            if (RoomChanged != null) RoomChanged();
            Say(newPlayer.NickName + " joined.");
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            if (RoomChanged != null) RoomChanged();
            Say(otherPlayer.NickName + " left.");
            if (engine != null && PhotonNetwork.IsMasterClient)
            {
                bool enough = engine.RemovePlayer(otherPlayer.ActorNumber);
                if (!enough) EndMatchAsHost("Not enough players left.");
                else AfterChange();
            }
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            if (RoomChanged != null) RoomChanged();
            if (newMasterClient.IsLocal && PhotonNetwork.InRoom) PhotonNetwork.CurrentRoom.IsOpen = true;
            // The engine lived on the old host, so a running match cannot continue.
            if (State == NetState.Playing) OnMatchEndReceived("The host left, so the match ended.");
        }
    }
}
