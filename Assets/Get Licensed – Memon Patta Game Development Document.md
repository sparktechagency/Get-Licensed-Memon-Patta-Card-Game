# Get Licensed – Memon Patta: Game Development Document

Oct 5, 2026 · @Rashadul Islam Rony

## 1. Project summary

We are rebuilding the Memon Patta card game, currently a Lovable (vibe-coded) web app, as a native Unity mobile game for iOS and Android. The client is Khadija Gaffar; the game ships as "Get Licensed – Memon Patta". This document is the build plan for the dev team: it turns the client's GDD v1.0 and the Fiverr conversations into scope, architecture, rules, tasks and open questions.

| Item | Detail |
| --- | --- |
| Client | Khadija Gaffar (Fiverr, Australia) |
| Engine | Unity (confirmed to client, 30 Jul) |
| Multiplayer | Photon Fusion (server-authoritative) |
| Backend | PlayFab (reverted from Firebase; client must be told, see C5) |
| Push | PlayFab push notifications (APNs and FCM underneath) |
| Platforms | iOS, Android, portrait only |
| Language | English only in v1.0 (architecture must allow more) |
| Fiverr order | $1,000, 40-day delivery, due 8 Oct 2026, "Core Gameplay" |
| Original brief | $2,000 max, 45 days, full app plus store publishing |
| Client target | Live on both stores by November 2026 |
| Prototype | [memonpatta.lovable.app](https://memonpatta.lovable.app/) (reference for feel and flow) |
| Source repo | [sparktechagency/Get-Licensed-Memon-Patta-Card-Game](https://github.com/sparktechagency/Get-Licensed-Memon-Patta-Card-Game) |
| Design | [Figma file](https://www.figma.com/design/NrQ8Yhd8AFuCEWUo4JCfVm/Khadijagaf-%7C%7C-Game-UI-%7C%7C--2-000-USD-%7C%7C-App_Ninja?node-id=0-1&t=Mi1SZ5BxLYvDM7SL-1) |

**Where the project stands (5 Oct 2026).** The client has been promised a playable Practice Mode beta (deal cards, play a full game against AI) "within 2–3 weeks" from 16 Sep, and an initial app with UI functionality and basic gameplay once the real-time server and database are done. The Fiverr order is due in three days and the client has chased for updates repeatedly. The first deliverable must therefore be the rules engine plus Practice Mode, not the online features.

**Sources used.** Client GDD v1.0 (sections 1–17), the two Fiverr inbox exports, the client's original requirement screenshots, and the links file. I did not run the Lovable prototype or read the repo, so UI details should be checked against the prototype and Figma.

## 2. Scope and phasing

The client's GDD is far larger than the original Fiverr brief, so the work is split into three phases. Phase 1 matches what the client last asked to test: Memon Patta rules from start to finish against AI, with design polish later.

| Phase | Contents | Source of scope |
| --- | --- | --- |
| 1. Core gameplay | Rules engine, Hard AI, Solo and Practice Mode, Local Multiplayer (pass-and-play), card UI and animations, round summary, cumulative scoreboard, settings, local stats | GDD §3–11, 14 |
| 2. Online and backend | PlayFab auth (Apple, Google, plus email and guest per original brief), profiles, cloud saves, Photon Fusion rooms, public and private matches, reconnection, AI takeover, friends, emoji, leaderboards, push | GDD §12, 13, 15 |
| 3. Monetisation, admin and launch | Apple and Google subscriptions, free-tier limits, admin web dashboard, challenges, achievements, anti-cheat hardening, store submission, 90-day support | GDD §16, 17, original brief |

**Out of scope for v1.0** (GDD): free-text chat, tablet layouts, web version, languages other than English, loot boxes or any pay-to-win purchase, reversing or skipping turns.

**Scope risk.** On 29 Aug the client asked the team to "confirm everything on the document will be delivered for the $1,000". The visible reply was only that the file opened, then an offer was sent and accepted. The inbox shows two $1,000 orders from the client: one from July (due 14 Sep 2026) and one from August (due 8 Oct 2026), which together equal the $2,000 budget. Someone must confirm in writing which phases each order covers, and whether the 14 Sep date was extended, before more features are promised.

## 3. Architecture

The game is a Unity client talking to a Photon Fusion game server for live matches and to PlayFab for accounts and data. The rules engine is one shared C# library, so Solo, Practice and Local play run it on the device and online play runs the same code on the server.

&#91;embedded content: architecture · client, game server, PlayFab, admin\]

Solo, Practice and Local never need the network. Online, the client sends commands (draw, play, discard) and the server returns events; it never trusts a client result. Subscription receipts from the stores are verified by PlayFab (CloudScript for the store checks), and the admin dashboard reads and moderates through the PlayFab Admin API.

| Decision | Choice | Why |
| --- | --- | --- |
| Engine | Unity, portrait, one codebase | Confirmed with client; iOS and Android |
| Rules | Pure C# library, no Unity dependency | Unit-testable, shared by client and server |
| Real-time | Photon Fusion | Told to client 25 Sep; server-side validation needed for Ranked |
| Backend | PlayFab | Original choice; Firebase was proposed 25 Sep and is now dropped. Existing PlayFab scripts were removed from the repo and must be restored from history |
| Billing | Platform subscriptions only | GDD: no direct payment handling |
| Admin | Separate secure web app | GDD: not reachable from the game |

## 4. Game rules the engine must implement

The rules below are the GDD's, consolidated into one reference. The same engine code runs Solo, Practice, Local and Online; only the host differs.

### 4.1 Setup

| Players | Decks | Cards | Dealt (13 each) | Draw pile after deal and first discard |
| --- | --- | --- | --- | --- |
| 2 | 1 × 52, no Jokers | 52 | 26 | 25 |
| 3 | 2 × 52, no Jokers | 104 | 39 | 64 |
| 4 | 2 × 52 | 104 | 52 | 51 |
| 5 | 2 × 52 | 104 | 65 | 38 |

- Maximum 5 participants (humans plus Hard AI). Minimum 2.
- Remaining cards form the face-down draw pile; the top card is turned up as the first discard.
- Starting player: random in round 1; the previous round's winner afterwards (random among remaining players if the winner left).
- Play is clockwise and fixed. No reverse or skip.
- Setup is validated before play (card counts, no duplicates in one-deck games, exactly two of each card in two-deck games). If validation fails, regenerate.

### 4.2 Turn sequence

1. **Draw exactly one card** (draw pile, top discard, or any visible discard; see 4.5). No other action is allowed before drawing.
2. **Play any number of legal melds** (licensing run, runs, sets, extensions).
3. **Discard exactly one card**, or place the last card face down to finish the round.
4. Turn passes clockwise.

Turn timer is 120 seconds. Mandatory online; optional in Solo and Local. On expiry the engine draws the top draw-pile card if no draw was made, then discards a random card. Melds already played stay. The engine never builds melds for the player.

### 4.3 Licensing

A player is Not Licensed at the start of every round. Until Licensed they may only play one run of at least 3 consecutive cards of the same suit. They cannot play sets, extra runs, or extend any meld. After the licensing run is accepted they may make unlimited runs and sets and extend their own or others' melds in the same turn.

### 4.4 Runs and sets

| Meld | Rule | Valid | Invalid |
| --- | --- | --- | --- |
| Run | 3 or more consecutive cards, same suit, no maximum length | 5♥ 6♥ 7♥ 8♥ | 3♠ 4♠ 6♠; 5♥ 6♥ 7♣ |
| Set | 3 or 4 cards of the same rank; duplicates allowed in two-deck games | 8♠ 8♥ 8♦; 7♠ 7♠ 7♥ | 2 cards; 5 cards |
| Ace | Low only, can start a run (A-2-3), never wraps | A♠ 2♠ 3♠ | Q-K-A; K-A-2 |
| Extend run | Add to either end, same suit, still consecutive, multiple cards allowed | 4♣ 5♣ 6♣ → 2♣…8♣ | Wrong suit or gap |
| Extend set | Only a 3-card set, with the matching rank | Q♠ Q♥ Q♦ + Q♣ | Any card on a 4-card set |

Played melds are permanent: no splitting, merging, moving cards between melds, or returning cards to hand. An invalid play must not change game state.

### 4.5 Discard pile and Required Card

- The whole discard pile is visible and scrollable, in discard order.
- Taking a buried card also takes every card above it. Example: pile top to bottom 8♠ 3♦ K♥ 7♣ A♠; choosing K♥ takes 8♠, 3♦, K♥.
- **The chosen card becomes the Required Card.** This also applies when only the top card is taken. Cards collected above it are not required.
- The Required Card must be played this turn, from hand, as part of one of the player's own legal melds (new run, new set, or extension of their own meld). Using it only on an opponent's meld is invalid. It cannot be kept or discarded, and the turn cannot end until it is played.
- If the draw pile empties, the top discard stays and all other discards are shuffled into a new draw pile.

### 4.6 Finishing a round

A player wins by ending their turn with exactly one card left in hand and placing it face down. They cannot finish by playing every card. The first player to do so wins; online ties are broken by server timestamp.

### 4.7 Scoring

| Card | Value |
| --- | --- |
| Ace | 3 |
| King, Queen, Jack | 2 each |
| 2 to 10 | 1 each |

```latex
\text{Base} = \sum \text{value(cards played)} - \sum \text{value(cards left in hand)}
```

Final round score = Base × 10 for the round winner, Base × 5 for everyone else. Negative bases stay negative. Final scores add to a cumulative total across rounds.

GDD example: bases 18, 14, −2, 9 become 180 (winner), 70, −10, 45.

### 4.8 Sessions

There is no target score. A session ends when all human players agree to stop or only one player remains. A player who leaves keeps their cumulative score, scores 0 in later rounds, and is removed from future rounds. In Ranked, leaving early is a loss.

## 5. Game engine and AI design

Build the rules as a pure C# library with no UnityEngine references, so the identical code runs in the Unity client (Solo, Local), in unit tests, and on the online authority. Everything else (UI, animation, networking) is a thin layer that sends commands and renders events.

### 5.1 Core model

| Type | Key fields |
| --- | --- |
| Card | id (unique, includes deck index 0 or 1), rank (A=1 … K=13), suit |
| Meld | id, type (Run or Set), ordered card list, owner player id |
| PlayerState | id, isBot, hand, isLicensed, requiredCardId, roundScore, totalScore, connected |
| GameState | players in seat order, drawPile, discardPile, melds, activeSeat, turnPhase, roundNumber, turnDeadline, rng seed |
| Turn phase | AwaitDraw → Playing → (Discarded → next turn) or RoundOver |

A unique card id (not just rank and suit) is required because two-deck games contain identical cards, and the Required Card and meld extensions must refer to a specific physical card.

### 5.2 Command and event pipeline

Every player action is a command; the engine validates it against the current state and returns either a list of events or a rejection with a human-readable reason (the UI shows the reason, as the GDD requires).

| Command | Allowed when | Main checks |
| --- | --- | --- |
| DrawFromPile | AwaitDraw, active player | Pile not empty (else reshuffle rule) |
| TakeFromDiscard(index) | AwaitDraw, active player | Index exists; collects all cards above; sets Required Card |
| PlayRun(cardIds) | Playing | Cards in hand; 3+ same suit consecutive; Ace low; licensing rules |
| PlaySet(cardIds) | Playing, Licensed | 3 or 4 same rank |
| ExtendMeld(meldId, cardIds, end) | Playing, Licensed | Result still a valid run or set; 4-card set locked |
| Discard(cardId) | Playing | Not the Required Card; Required Card already played; ends turn |
| FinishRound(cardId) | Playing | Exactly one card left; Required Card played; sets round over |
| TimeoutTurn | Server timer | Auto-draw if needed, random discard, never builds melds |

Rules for the engine: reject anything out of phase or out of turn; an invalid command leaves state untouched; every accepted command emits events (CardDrawn, MeldPlayed, MeldExtended, LicensedChanged, CardDiscarded, TurnChanged, RoundEnded, ScoresUpdated) that the UI animates and the network replicates.

### 5.3 Handling the Required Card without dead ends

The GDD makes a taken discard mandatory to play, yet a player could take a card they can never legally use (for example while Not Licensed, where the only legal play is a licensing run containing it). That strands the turn, and the timeout rule (random discard) would then discard the Required Card the GDD forbids discarding. Recommended engine behaviour, pending client approval (see section 9):

- On TakeFromDiscard, check that at least one legal use of the Required Card exists using the player's hand plus all collected cards. If none exists, reject the take with a reason.
- On timeout, never discard the Required Card; if it is unplayed, discard another card.

### 5.4 Hard AI

The bot uses the same command interface as a human and sees only public information: its own hand, table melds, the full discard pile, and counts of unseen cards. It never reads the draw pile or other hands.

| Decision | Approach |
| --- | --- |
| Draw | Evaluate taking each visible discard (and everything above it) against the draw pile, using expected meld gain and the Required Card constraint |
| Plays | Enumerate all legal run, set and extension combinations from the hand; pick the sequence that maximises score while keeping a finishing path (exactly one card left) |
| Discard | Discard the card with the lowest future value to the bot and the lowest usefulness to opponents (consider melds on the table and cards already discarded) |
| Planning ahead | Track unseen cards (104 or 52 minus visible); estimate probability of completing runs and sets over the next turns |
| Finish | Prefer finishing as soon as it is legal, since the winner multiplier is ×10 |

Build order: (1) a greedy heuristic bot that is always legal, enough for Phase 1; (2) hand-potential scoring and discard safety; (3) optional sampling of opponent hands from unseen cards for short look-ahead. A "Hard" label in the UI should only be used once the bot beats the greedy version consistently in simulation.

### 5.5 Determinism and testing

Seed the shuffle per round and log commands so any game can be replayed. Run bot-vs-bot simulations (thousands of games) to catch rule violations, deadlocks, and invalid-state bugs before any UI is built on top.

## 6. Online, backend and social systems

### 6.1 Online multiplayer

- **Authority.** The GDD requires the server to validate every action and clients never to decide outcomes. In Photon Fusion that means running the shared rules library in the authoritative simulation. Host Mode (one player's phone as host) cannot meet the anti-cheat requirement for Ranked; Ranked needs a Fusion dedicated server, or Ranked should be deferred. This also affects hosting cost against a $2,000 budget.
- **Hidden information.** Hands and the draw pile must never be replicated to other clients. Send each player only their own hand; replicate hand sizes, melds, and the discard pile.
- **Match types.** Public (queue, fill empty seats with Hard AI), Private (host invites friends or shares a room code, can add or remove bots, starts with at least 2 players), Ranked (server-validated, results update leaderboards only after the match completes), Casual (no ranking).
- **Turn timer.** 120 s, server-owned, shown to all players.
- **Reconnection.** A dropped player's seat is held for 60 s while the game continues. If they return in time they resume. Otherwise a Hard AI takes over their hand, score and position permanently and they cannot rejoin. In Ranked, leaving counts as a loss.
- **Sync checks.** Every accepted command carries a sequence number; clients that fall behind resync from the authoritative state. Scores shown to all players must be identical.

### 6.2 PlayFab backend

| Concern | PlayFab feature | Notes |
| --- | --- | --- |
| Sign-in | Login (Apple, Google, email, CustomID for guest) | Apple (required on iOS if any social login exists), Google, email, guest (anonymous, upgradable) |
| Profiles, stats, friends, history | Player Data, Player Statistics, Friends API | Client reads; stats and rankings written only by server-side CloudScript or the game server, with client writes disabled in title settings |
| Match results and leaderboards | Statistics and Leaderboards + CloudScript | Triggered by the game server after a validated match |
| Subscription status | CloudScript, receipt validation (iOS and Google Play) | Verify App Store and Google Play receipts server-side, never trust the client |
| Push | PlayFab Push Notifications (APNs, FCM) and segments | Friend requests, game invites, announcements, challenges, subscription reminders |
| Crash reports and analytics | PlayStream events, Unity Cloud Diagnostics | Feeds the admin dashboard where possible |

Suggested PlayFab data: Player Data (profile, settings, blocks, subscription state), Player Statistics (XP, rank, wins, per leaderboard scope and period), Internal Data and Title Data (config, announcements), usernames (uniqueness via display name checks), reports and audit logs (Title Internal Data or an external store). Title policies must stop clients writing their own stats, XP, rank or subscription state; all such writes go through CloudScript or the server API.

### 6.3 Progression and leaderboards

- XP is awarded for finishing and winning games, Ranked play, becoming Licensed, runs, sets, and challenges. Levels, achievements and unlocks are cosmetic only.
- Statistics listed in GDD §13.8 update after each completed game and sync across devices.
- Leaderboards: Global, Friends, Country, Seasonal, All-Time (GDD) and weekly/monthly/all-time per the original brief. Compute on a schedule and on validated match results, not on client submission.

### 6.4 Social

- Username search, friend requests (send, accept, decline, cancel), mutual friendships, remove friend.
- Online status: Online, In Game, Offline, Invisible.
- Invites from friends list, profile or private lobby, delivered in-app and by push.
- Emoji reactions only (no free-text chat), shown briefly then removed.
- Privacy: hide status or country, block friend requests, friends-only or anyone invites, block players. Blocked players cannot send requests or invites.
- Player reports (offensive username, cheating, bug exploiting, invitation harassment, other) go to the admin queue without auto-blocking.

## 7. UI, subscriptions and admin

### 7.1 Screens

| Screen | Must show |
| --- | --- |
| Main menu | Play, Practice, Friends, Profile, Statistics, Leaderboards, Daily and Weekly Challenges, Subscription, Settings; username, avatar, level |
| Lobby | Players, avatars, ready state, mode, bot count, match type; host controls (start, add or remove bots, invite, share room code) |
| Game table | Own hand (fan layout), draw pile, full scrollable discard pile, all runs and sets, active-player highlight, turn timer, cumulative scores, round number, Licensed badge per player |
| Pass device (Local) | Blank "Pass Device to Next Player" screen before each hand is revealed |
| Round summary | Per player: cards played value, remaining value, base score, multiplier, final score, cumulative score, rank; winner highlighted |
| Profile and friend profile | Fields from GDD §13.4 and §15.3; no private account data |
| Settings | Music, SFX, haptics, light/dark, turn timer (Solo and Local only), animation speed, notifications, privacy, account, Restore Purchases |

Hand and card interaction: tap-to-select and drag-and-drop, sort by suit or value, manual reorder (local only), zoom, clear feedback for selected, played, rejected, discarded and Required cards. The Required Card stays highlighted until played and cannot be selected for discard. Every rejected move shows a plain-language reason. Accessibility: text size, high contrast, colour-blind card indicators, haptics, audio controls, screen reader where possible. Portrait only.

### 7.2 Subscriptions

|  | Free | Premium |
| --- | --- | --- |
| Solo, Practice, Local, Hard AI | Unlimited | Unlimited |
| Online games | Limited (GDD: 1 per day) | Unlimited |
| Ranked, XP progression, cosmetics unlocks | No | Yes |
| Leaderboards | View only (original brief) / hidden (GDD) | Global, Friends, Country, Seasonal |
| Premium badge, priority matchmaking | No | Yes |

Plans: Monthly £2.99 and Annual £24.99, each with a 7-day free trial for eligible new subscribers, priced and localised through the stores. Implement with Unity IAP or a subscription service, verify receipts through PlayFab, support Restore Purchases, and preserve all progress when a subscription lapses. v1.0 sells subscriptions only: no loot boxes, boosts or pay-to-win items. The rules engine must not read subscription status.

### 7.3 Admin dashboard

Secure web app, separate from the game, with MFA, role-based access, session timeout and an immutable audit log.

- Players: search, profile, account and subscription status, match history, discipline history.
- Moderation: suspend, ban, username review, report queue, appeals, internal notes.
- Live ops: active games, queues, connected players, bot replacements, server status; observe matches without seeing hidden hands unless permitted.
- Announcements: in-app, optional push.
- Analytics: DAU, MAU, registrations, session length, games started and completed, round and game duration, Licensing frequency, bot usage, abandonment; active subscriptions, trial conversion, renewals, cancellations, MRR.
- Admins cannot edit match results or scores.

### 7.4 Push notifications

Friend request received or accepted, game invite, match starting, daily and weekly challenge available, daily online reset (free users), challenge completed, achievement unlocked, subscription renewal and expiry. Users can toggle categories in Settings.

## 8. Plan, testing and launch

The schedule below is a proposal, not a commitment already made to the client. The client's November live date is not realistic for the full scope (online play, subscriptions, admin dashboard, and store review); a November closed beta of Phases 1 and 2 is.

&#91;embedded content: proposed schedule · 3 phases, 1 checkpoint\]

Phase 1 is playable by the client on 19 Oct. Phase 2 starts only after the client signs off the rules in section 9.1, because the online server reuses the same engine.

### 8.1 Phase 1 tasks

- [ ] Unity project with separate assemblies: Rules (pure C#), Game (Unity), Net, Backend
- [ ] Cards, seeded shuffle, deal, setup validator (13 each, deck counts, no Jokers)
- [ ] Run, set and extension validators (Ace low, duplicates in two-deck games)
- [ ] Licensing, Required Card and timeout logic
- [ ] Scoring, round summary and cumulative scoreboard
- [ ] Greedy Hard AI; 10,000-game bot simulation with zero illegal states
- [ ] Table UI: fan hand, tap and drag, scrollable discard pile, melds, rejection messages
- [ ] Practice Mode, Solo (1 to 4 bots), Local pass-device play
- [ ] Settings, local statistics, TestFlight and Android internal build for the client

### 8.2 Rule test cases

| Case | Expected result |
| --- | --- |
| Licensing run A♠ 2♠ 3♠ | Accepted; player becomes Licensed |
| Q♠ K♠ A♠ or K♥ A♥ 2♥ | Rejected (Ace is low only) |
| Not Licensed plays a set, a second run, or extends a meld | Rejected |
| 7♠ 7♠ 7♥ in a two-deck game | Accepted as a set |
| Extend a 4-card set | Rejected |
| Take K♥ from pile 8♠ 3♦ K♥ 7♣ A♠ | Player takes 8♠, 3♦, K♥; K♥ is Required; 7♣, A♠ stay |
| Discard the Required Card, or end turn holding it | Rejected |
| Use the Required Card only on an opponent's meld | Rejected |
| Draw pile empties | Top discard stays; others reshuffled into the draw pile |
| Finish with zero cards, or with two or more | Rejected; exactly one card, face down |
| Timeout after drawing | Random discard; melds already played stay |
| Bases 18, 14, −2, 9 | Final 180 (winner), 70, −10, 45 |
| Two-player and five-player setups | 52 cards, 26 dealt; 104 cards, 65 dealt |
| Online: modified client sends an illegal command | Server rejects; state unchanged |
| Online: player offline for 60 s | Hard AI takes the seat; original player cannot rejoin |

### 8.3 Launch checklist

- [ ] Client creates Apple Developer and Google Play Console accounts (needed early, see C4)
- [ ] Bundle IDs, Sign in with Apple, signing keys, provisioning profiles
- [ ] Subscription products created in App Store Connect and Play Console; sandbox purchase tested
- [ ] Privacy policy URL, App Privacy and Data Safety forms, age rating
- [ ] Store listing: screenshots, description, icons
- [ ] TestFlight and Google Play testing tracks, then production submission
- [ ] Handover: Unity source, backend source, database access, full ownership
- [ ] Support period starts at launch (length per C1)

## 9. Conflicts, gaps and questions for the client

The GDD says development must pause when a rule is unclear or conflicts with another section. Sending these as one batch avoids repeated pauses. "Default" is what we build unless the client says otherwise.

### 9.1 Gameplay rules

| # | Issue | Source | Proposed default |
| --- | --- | --- | --- |
| G1 | A player can take a discard they can never legally use (e.g. Not Licensed, card cannot join a licensing run), stranding the turn. The timeout random discard could then discard the Required Card the GDD forbids discarding. | GDD §6.3, §9.4 | Reject the take if no legal use exists; timeout never discards the Required Card |
| G2 | Does the final face-down card count as played or as remaining in the base score? | GDD §10.2, §10.4 | Count it as remaining (it is the one card left in hand) |
| G3 | What counts as a complete Ranked "match", given sessions have no end score? Number of rounds or a time limit is undefined. | GDD §4.7, §11.8, §12.9 | Fixed round count chosen in lobby (e.g. 5) for Ranked; endless for Casual and Private |
| G4 | How do online players "agree to end" a session? | GDD §4.7, §10.8 | Vote after each round; majority of humans ends it |
| G5 | Ranked uses "skill-based progression" but no rating system, season length or reset is defined. | GDD §11.8 | Elo-style rating adjusted for 2–5 players; 3-month seasons |
| G6 | Ties in cumulative score at session end. | GDD §4.2 | Shared rank, tie-break by round wins |
| G7 | A player who leaves online scores 0 in later rounds, but a bot takes over after 60 s of disconnect. Which applies to voluntary leave versus disconnect? | GDD §10.9, §12.4 | Leave = removed, scores 0; disconnect = bot takes over |
| G8 | The Lovable prototype may implement different rules from the GDD. | Links file | GDD wins; compare the prototype and list differences before coding |

### 9.2 Product, accounts and monetisation

| # | Issue | Source | Proposed default |
| --- | --- | --- | --- |
| P1 | Sign-in: brief lists Apple, Google, email and guest; GDD lists only Apple (iOS) and Google (Android). | Brief vs GDD §13.2 | Build all four; guest can be upgraded to a full account |
| P2 | Free online limit: "limited" (brief) vs one game per day (GDD); reset time and timezone undefined. | Brief vs GDD §16.2 | 1 per day, reset at 00:00 UTC, server-enforced |
| P3 | Free users "can view leaderboards" (brief); GDD makes Global, Friends, Country and Seasonal boards Premium. | Brief vs GDD §16.3 | Free users view Global all-time only; Premium gets all boards |
| P4 | Solo and Casual award XP and achievements (GDD §11.2, §11.9), but XP progression is Premium-only (§16.3). | GDD | XP for everyone; Premium gates Ranked, seasonal rewards and complete stats |
| P5 | Priority matchmaking for Premium conflicts with "premium never affects gameplay" only if it changes game rules; confirm that queue priority is acceptable. | GDD §3.8, §16.3 | Allow queue priority only |
| P6 | Daily and Weekly Challenges, achievement list rewards, XP amounts and level thresholds are not defined. | GDD §13.5–13.7 | Client supplies a table; we ship placeholder values until then |
| P7 | Cosmetic content (avatars, card backs, frames, table themes, music, SFX): who creates it? Figma shows game UI only. | GDD §13.6 | Client supplies or approves a minimal launch set |
| P8 | Leaderboard periods: brief asks weekly, monthly, all-time; GDD asks Global, Friends, Country, Seasonal, All-Time. | Brief vs GDD §13.9 | Scope × period matrix, built in Phase 2 |

### 9.3 Commercial and delivery

| # | Issue | Source | Proposed default |
| --- | --- | --- | --- |
| C1 | Post-launch support: 90 days (brief and our scope list) vs 180 days (message of 30 Jul). | Inbox | Confirm in writing; 90 days unless already agreed |
| C2 | Delivery: 45 days to live (brief) vs "within 90 days" to submission (30 Jul reply) vs client target of November live. | Inbox | Agree one date for each phase; store review is outside our control |
| C3 | Two $1,000 orders (July, due 14 Sep; August, due 8 Oct). Which phases does each cover? 14 Sep has passed. | Inbox | Map phases to orders and confirm the dates |
| C4 | On 29 Sep the client was told developer accounts are needed only at final publishing. In fact Apple Sign-In, subscription products and TestFlight builds need the Apple Developer account earlier, and a new Google Play account can require a closed-testing period before production. Verify current store rules. | Inbox, 29 Sep | Ask the client to create both accounts now |
| C5 | Running-cost estimate ($0 to $25 at 10,000 users) was given for PlayFab, which is the backend again after the Firebase proposal on 25 Sep. The client was told Firebase was chosen, so tell them the change back. CloudScript and Azure Functions usage and a Fusion dedicated server for Ranked add cost. | Inbox, 30 Jul and 25 Sep | Confirm the PlayFab estimate still holds, add Photon and server hosting, and send it |
| C6 | The client has asked for GitHub access, weekly updates and a beta date repeatedly. A repo link was shared 16 Sep, but no build date was given after "2–3 weeks". | Inbox | Send a dated beta plan from section 8 |
