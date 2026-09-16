# Tasks — Get Licensed: Memon Patta

Status legend: `[ ]` to do · `[x]` done

Derived from the UI mockups (`Frame 1.pdf`) and current repo state (blank Unity 2D URP project, no gameplay code yet).

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
- [ ] Home screen (avatar, level/XP bar, Practice, Online, Daily Challenge, Statistics, Leaderboards, Weekly Challenges, Premium, bottom nav)
- [ ] Practice setup modal (Easy / Medium / Hard difficulty cards, deck type selector, Start Practice)
- [ ] Online setup modal (Local play / Rank Play)
- [ ] Rank Play lobby (Ranked/Casual tabs, current rank badge, Quick Match, Create Room, game room list with entry fees, season countdown + rewards)
- [ ] Leaderboards screen (Global / Friends / Country tabs, ranked list with trophies)
- [ ] Friends screen (Friends / Requests / Invites / Recent tabs, search, add friend, challenge/online status, invite friends CTA)
- [ ] Profile screen (avatar edit, level/XP, stats overview: wins/accuracy/streak/total wins, achievements grid, account section: Account Info, Achievements, Preferences, Help & Support, Log Out)
- [ ] Bottom navigation bar (Home / Rankings / Friends / Profile) shared across screens
- [ ] Notification & settings icons (top bar)

## Progression & Social

- [ ] Player level & XP system
- [ ] Daily challenge system (e.g. "Win 2 Rounds" with XP reward)
- [ ] Weekly challenges system
- [ ] Achievements system (e.g. First Win, Card Master, Unstoppable, Pro Player) with unlock tracking
- [ ] Friends system: add friend, requests, invites, recent, online/offline status
- [ ] Challenge-a-friend flow
- [ ] Invite friends (referral rewards)
- [ ] Global / Friends / Country leaderboard ranking logic

## Online / Multiplayer

- [ ] Backend/networking setup (auth, matchmaking, real-time play)
- [ ] Rank Play matchmaking (Quick Match)
- [ ] Custom room creation & join-by-room (with entry fee/stakes: Pro Room, Master Table, Challenger Arena, Ace Room)
- [ ] Ranked vs Casual play modes
- [ ] Seasons system (season timer, season-end rewards, rank tiers e.g. "Gold II")

## Monetization

- [ ] Premium membership system & rewards
- [ ] In-app currency / entry-fee economy for game rooms

## Technical / Infrastructure

- [ ] Project architecture setup (folder structure, namespaces, core managers)
- [ ] Save/load system for player profile & progress
- [ ] Backend service integration (accounts, leaderboards, social)
- [ ] Import UI mockups/art assets into `Assets/`
- [ ] Set up UI framework (Canvas/UGUI or UI Toolkit) matching mockup style (dark green + gold theme)
- [ ] Establish scenes: Splash, Home/Main Menu, Practice, Online Lobby, Match/Gameplay, Profile

## Not Yet Started

Everything above — the project currently contains only the default Unity 2D URP template (blank sample scene, default render settings, no custom code or assets).
