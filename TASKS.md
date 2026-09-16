# Tasks — Get Licensed: Memon Patta

Status legend: `[ ]` to do · `[x]` done

Derived from the UI mockups (`Frame 1.pdf`) and current repo state. See [README.md](README.md) for the project overview and tech stack.

## Core Gameplay

- [ ] Implement Memon Patta card game rules engine (deck, dealing, turns, win conditions)
- [ ] 52-card deck model + shuffling/dealing logic
- [ ] Round/match structure (best-of-N rounds, e.g. "Win 2 Rounds")
- [ ] AI opponent logic with difficulty tiers: Easy, Medium, Hard
- [ ] Local multiplayer (pass-and-play on same device)
- [ ] Card UI/board rendering (hand, table, play area)
- [ ] Turn/game state management (Practice vs Online vs Rank)

## Screens / UI Implementation

- [ ] Splash screen ("GET LICENSED — MEMON PATTA" logo + Play button)
- [x] Home screen (avatar, level/XP bar, Practice, Online, Daily Challenge, Statistics, Leaderboards, Weekly Challenges, Premium, bottom nav) — placeholder UI in `Assets/Scenes/MainMenu.unity`
- [ ] Practice setup modal (Easy / Medium / Hard difficulty cards, deck type selector, Start Practice)
- [ ] Online setup modal (Local play / Rank Play)
- [ ] Rank Play lobby (Ranked/Casual tabs, current rank badge, Quick Match, Create Room, game room list with entry fees, season countdown + rewards)
- [ ] Leaderboards screen (Global / Friends / Country tabs, ranked list with trophies)
- [ ] Friends screen (Friends / Requests / Invites / Recent tabs, search, add friend, challenge/online status, invite friends CTA)
- [ ] Profile screen (avatar edit, level/XP, stats overview: wins/accuracy/streak/total wins, achievements grid, account section: Account Info, Achievements, Preferences, Help & Support, Log Out)
- [ ] Bottom navigation bar (Home / Rankings / Friends / Profile) shared across screens
- [ ] Notification & settings icons (top bar)

## Progression & Social

- [x] Player level & XP system — `PlayFabPlayerDataService` (profile stored as UserData JSON, level-up math client-side)
- [x] Daily challenge system (e.g. "Win 2 Rounds" with XP reward) — `PlayFabChallengesService`
- [x] Weekly challenges system — `PlayFabChallengesService`
- [x] Achievements system (e.g. First Win, Card Master, Unstoppable, Pro Player) with unlock tracking — `PlayFabChallengesService`
- [x] Friends system: add friend, recent, online/offline status — `PlayFabFriendsService` (add/remove/list only; PlayFab has no native pending-request state)
- [ ] Friend Requests/Invites tabs (needs custom pending-request store — PlayFab AddFriend links immediately, no request state)
- [ ] Challenge-a-friend flow (stubbed in `PlayFabFriendsService.ChallengeFriend` — needs a realtime invite/matchmaking transport)
- [ ] Invite friends (referral rewards)
- [x] Global / Friends leaderboard — `PlayFabStatisticsService` (PlayFab GetLeaderboard / GetFriendLeaderboard)
- [ ] Country leaderboard (stubbed as an unfiltered sample — PlayFab has no native per-country leaderboard; needs CloudScript/Azure Function aggregation)

## Online / Multiplayer

- [x] Backend auth — `PlayFabManager` (LoginWithCustomID, device-based)
- [ ] Real-time matchmaking / networking transport (PlayFab Matchmaker SDK module is installed but not wired up)
- [ ] Rank Play matchmaking (Quick Match)
- [ ] Custom room creation & join-by-room (with entry fee/stakes: Pro Room, Master Table, Challenger Arena, Ace Room)
- [ ] Ranked vs Casual play modes
- [ ] Seasons system (season timer, season-end rewards, rank tiers e.g. "Gold II")

## Monetization

- [x] Premium membership entitlement + virtual currency — `PlayFabEconomyService` (classic Catalog/Inventory model; Economy v2 SDK module installed but not used yet)
- [x] In-app currency spend/grant for game room entry fees — `PlayFabEconomyService.SpendCurrency` / `GrantCurrency`

## Technical / Infrastructure

- [x] Project architecture setup (folder structure, namespaces, core managers) — `Assets/Scripts/PlayFab/` (`MemonPatta.PlayFabServices` namespace)
- [x] Save/load system for player profile & progress — PlayFab UserData (client-authoritative for now; server validation via CloudScript is a follow-up)
- [x] Backend service integration (accounts, leaderboards, social) — PlayFab SDK wired up (see `Assets/Scripts/PlayFab/`)
- [ ] Import UI mockups/art assets into `Assets/`
- [x] Set up UI framework (Canvas/UGUI or UI Toolkit) matching mockup style (dark green + gold theme) — UGUI + TextMeshPro, see `Assets/Scenes/MainMenu.unity`
- [ ] Establish remaining scenes: Splash, Practice, Online Lobby, Match/Gameplay, Profile

## PlayFab Scripts (`Assets/Scripts/PlayFab/`)

- `PlayFabManager.cs` — session bootstrap, device login, exposes `PlayFabId` / `IsLoggedIn`
- `PlayFabDataModels.cs` — shared serializable data shapes + UserData/Statistic key constants
- `PlayFabPlayerDataService.cs` — profile (display name, level, XP, avatar, country)
- `PlayFabStatisticsService.cs` — match result reporting + Global/Friends/Country leaderboards
- `PlayFabFriendsService.cs` — friends list, add/remove, challenge-friend stub
- `PlayFabChallengesService.cs` — daily/weekly challenge progress + claim, achievement tracking
- `PlayFabEconomyService.cs` — virtual currency balance, room entry fee spend, Premium purchase

**Known gaps / follow-ups:** TitleId is not yet configured in `PlayFabSharedSettings.asset` (set it via Window > PlayFab > Editor Extensions before testing). All reward/progress writes are currently client-authoritative — move XP grants, challenge claims, and achievement unlocks into PlayFab CloudScript once available, so a modified client can't grant itself rewards. No UI is wired to these services yet — [MainMenu.unity](Assets/Scenes/MainMenu.unity) is still static placeholder text.

## Not Yet Started

Card game rules engine, AI opponents, card board UI/rendering, remaining screens (Practice/Online modals, Rank lobby, Leaderboards, Friends, Profile), real-time matchmaking, rooms, seasons, and wiring the PlayFab services above into the actual UI.
