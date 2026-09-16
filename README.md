# Get Licensed: Memon Patta

A mobile card game built in Unity, based on the traditional card game **Memon Patta**.

UI/UX designed by Khadija Gaffar.

## Overview

"Get Licensed: Memon Patta" is a social card game with Practice (vs AI), local pass-and-play, and online ranked/casual multiplayer, wrapped in a progression layer (levels, XP, daily/weekly challenges, achievements) and social features (friends, leaderboards). Backend services (auth, player data, leaderboards, economy) are built on PlayFab.

## Tech Stack

- **Engine:** Unity 6000.3.10f1
- **Render pipeline:** 2D Universal Render Pipeline (URP)
- **UI:** UGUI (Canvas) + TextMeshPro
- **Input:** New Input System package (`InputSystemUIInputModule`; legacy `UnityEngine.Input` is disabled in Player Settings)
- **Backend:** PlayFab SDK (Client, Economy, Matchmaker, Progression, Experimentation, Insights, Addon modules) + PlayFab Editor Extensions
- **Solution:** `Get Licensed Memon Patta.slnx`

## Project Structure

```
Assets/
  Scenes/
    MainMenu.unity       Home screen placeholder UI (Play → Practice/Online, Daily Challenge, stats grid, bottom nav)
    SampleScene.unity    Unmodified default Unity template scene
  Scripts/
    PlayFab/             Client-side PlayFab service layer (see below)
  Settings/               URP renderer/global settings, scene templates
  PlayFabSDK/             PlayFab Unity SDK (vendored)
  PlayFabEditorExtensions/ PlayFab dashboard-linking editor tooling
```

## Current Progress

### Done
- **Main Menu / Home screen** (`Assets/Scenes/MainMenu.unity`) — built to match the PDF mockup: top bar (avatar, name, level, XP bar, notification/settings buttons), logo, Practice/Online buttons, Daily Challenge panel with progress bar and XP badge, 2×2 menu grid (Statistics, Leaderboards, Weekly Challenges, Premium), footer credit, bottom nav (Home/Rankings/Friends/Profile). Built entirely from placeholder `Image`/`Button`/`TextMeshProUGUI` elements (dark green + gold palette) on a 1080×1920 `CanvasScaler` — no art assets yet, and no UI is wired to live data.
- **PlayFab service layer** (`Assets/Scripts/PlayFab/`, namespace `MemonPatta.PlayFabServices`) — client-side wrapper around the PlayFab SDK:
  - `PlayFabManager` — session bootstrap, device-based login (`LoginWithCustomID`)
  - `PlayFabDataModels` — shared data shapes (`PlayerProfileData`, `DailyChallengeData`, `WeeklyChallengeData`, `AchievementProgressData`) and UserData/Statistic key constants
  - `PlayFabPlayerDataService` — display name, level, XP, avatar, country (stored as UserData JSON)
  - `PlayFabStatisticsService` — match result reporting (wins, win streak) + Global and Friends leaderboards; Country leaderboard is stubbed (see Known Gaps)
  - `PlayFabFriendsService` — friends list, add/remove by display name; challenge-a-friend is stubbed
  - `PlayFabChallengesService` — daily/weekly challenge progress + claim, achievement progress/unlock tracking
  - `PlayFabEconomyService` — virtual currency balance, spend/grant (room entry fees), Premium membership purchase via classic Catalog/Inventory

### Not started
Card game rules engine, AI opponents, card board rendering, remaining screens (Practice/Online setup modals, Rank Play lobby, Leaderboards, Friends, Profile), real-time matchmaking/rooms, seasons, and wiring the PlayFab services above into the actual UI.

See [TASKS.md](TASKS.md) for the full feature and to-do breakdown.

## Known Gaps / Follow-ups

- **PlayFab TitleId is not configured yet** — `Assets/PlayFabSDK/Shared/Public/Resources/PlayFabSharedSettings.asset` has an empty `TitleId`. Set it via **Window > PlayFab > Editor Extensions** (or `PlayFabSettings.staticSettings.TitleId`) before any PlayFab call will succeed.
- **All reward/progress logic is currently client-authoritative** (XP grants, challenge claims, achievement unlocks). This is fine for prototyping but should move into PlayFab CloudScript before shipping, so a modified client can't grant itself rewards.
- **No pending-request friend state** — PlayFab's `AddFriend` links accounts immediately; the mockup's Friends/Requests/Invites tabs need a custom backend store (e.g. CloudScript + a data table) to support a request/accept flow.
- **No per-country leaderboard** — PlayFab has no native country-scoped leaderboard query; `PlayFabStatisticsService.LoadCountryLeaderboard` currently returns an unfiltered sample of the global board as a placeholder.
- **No realtime transport** — the PlayFab Matchmaker SDK module is vendored but unused; challenge-a-friend and Rank Play matchmaking need a chosen realtime/matchmaking solution before they can work end-to-end.
- **MainMenu.unity UI is static** — none of the placeholder text/values are yet bound to the PlayFab service layer.

## Getting Started

1. Open the project in Unity 6000.3.10f1 (or later 6000.x).
2. Set your PlayFab Title ID in **Window > PlayFab > Editor Extensions**.
3. Open `Assets/Scenes/MainMenu.unity` and press Play to see the current placeholder Home screen.
