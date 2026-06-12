# Witcher 3 Gwent Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a playable Unity implementation of the original The Witcher 3 Gwent minigame on branch `witcher3-gwent`.

**Architecture:** Implement a pure C# rules core first, then bind it to a Unity UI. Keep card/faction data separate from runtime state so missing art can be replaced without changing rules.

**Tech Stack:** Unity 6000.3.0f1, C#, Unity Test Framework, uGUI.

---

### Task 1: Core Model And Scoring

**Files:**
- Create: `Assets/Scripts/Gwent/Core/Gwent.Core.asmdef`
- Create: `Assets/Scripts/Gwent/Core/GwentCore.cs`
- Create: `Assets/Tests/EditMode/Gwent/GwentCoreTests.cs`
- Create: `Assets/Tests/EditMode/Gwent/Gwent.Tests.asmdef`

- [x] Write failing EditMode tests for row scoring, weather, Horn, Tight Bond, Morale, Scorch, and hero immunity.
- [x] Run EditMode tests and confirm they fail because `Gwent.Core` does not exist.
- [x] Implement minimal core types and scoring/effects to pass tests.
- [x] Run EditMode tests and confirm all pass.
- [x] Commit with message `feat: add gwent rules core scoring`.

### Task 2: Match Flow And Card Abilities

**Files:**
- Modify: `Assets/Scripts/Gwent/Core/GwentCore.cs`
- Modify: `Assets/Tests/EditMode/Gwent/GwentCoreTests.cs`

- [x] Add failing tests for draw, mulligan, turn alternation, pass, round end, Spy, Medic, Muster, Agile, Decoy, and faction bonuses.
- [x] Run EditMode tests and confirm the new tests fail for missing behavior.
- [x] Implement match state transitions and card abilities.
- [x] Run EditMode tests and confirm all pass.
- [x] Commit with message `feat: implement witcher 3 gwent match flow`.

### Task 3: Card Catalog

**Files:**
- Create: `Assets/Scripts/Gwent/Data/GwentCatalog.cs`
- Create: `Assets/Scripts/Gwent/Data/GwentAssetCatalog.cs`
- Create: `Assets/Scripts/Gwent/Data/Gwent.Data.asmdef`
- Modify: `Assets/Tests/EditMode/Gwent/GwentCoreTests.cs`

- [x] Add failing tests that every base faction has a playable deck, leader, and at least 22 unit cards after neutral cards are included.
- [x] Implement a data catalog for Northern Realms, Nilfgaard, Monsters, Scoia'tael, and Neutral cards.
- [x] Map existing local art to known cards where available and use placeholders for missing art.
- [x] Run EditMode tests and confirm all pass.
- [x] Commit with message `feat: add witcher 3 gwent card catalog`.

### Task 4: Unity UI Scene

**Files:**
- Create: `Assets/Scripts/Gwent/UI/GwentGameController.cs`
- Create: `Assets/Scripts/Gwent/UI/GwentCardView.cs`
- Create: `Assets/Scripts/Gwent/UI/GwentRowView.cs`
- Create: `Assets/Scripts/Gwent/UI/GwentDeckSelectionView.cs`
- Create: `Assets/Scripts/Gwent/UI/GwentViewFactory.cs`
- Modify: `Assets/Scenes/StartGame.unity`
- Modify: `Assets/Scenes/Game.unity`
- Modify: `Assets/Scenes/End.unity`

- [x] Replace the old fixed-card UI with dynamic faction selection, mulligan, board, hand, pass button, leader button, and result flow.
- [x] Render six combat rows, score totals, round gems, weather indicators, deck/discard counts, and card zoom.
- [x] Connect UI actions to the core engine.
- [x] Run EditMode tests and a Windows batchmode build.
- [x] Commit with message `feat: build gwent match UI`.

### Task 5: Animation And Polish

**Files:**
- Modify: `Assets/Scripts/Gwent/UI/GwentCardView.cs`
- Modify: `Assets/Scripts/Gwent/UI/GwentGameController.cs`
- Modify: `Assets/Scripts/Gwent/UI/GwentRowView.cs`
- Modify: `Assets/Scenes/Game.unity`

- [x] Add card slide, flip, score pulse, weather overlay, pass banner, and round result animations.
- [x] Verify the UI remains readable at desktop resolution.
- [x] Run EditMode tests and Windows batchmode build.
- [x] Commit with message `feat: add gwent board animations`.

### Task 6: Rule Parity Follow-up

**Files:**
- Modify: `Assets/Scripts/Gwent/Core/GwentCore.cs`
- Modify: `Assets/Scripts/Gwent/Data/GwentCatalog.cs`
- Modify: `Assets/Scripts/Gwent/UI/GwentCardView.cs`
- Modify: `Assets/Tests/EditMode/Gwent/GwentCoreTests.cs`
- Modify: `Assets/Tests/EditMode/Gwent/GwentCatalogTests.cs`

- [x] Add failing tests for shared Muster groups, Scorch threshold, and row-specific Scorch cards.
- [x] Implement Witcher 3-style shared Muster groups and Scorch behavior.
- [x] Update catalog flags for Villentretenmerth, Toad, and Schirru.
- [x] Run EditMode tests and Windows batchmode build.
- [x] Commit with message `feat: close witcher 3 gwent rule gaps`.
- [x] Implement functional leader abilities from the Witcher 3 decks.
- [x] Commit with message `feat: add gwent leader abilities`.

### Task 7: Local Card Art Mapping

**Files:**
- Modify: `Assets/Scripts/Gwent/Data/GwentAssetCatalog.cs`
- Modify: `Assets/Scripts/Gwent/UI/GwentCardView.cs`
- Modify: `Assets/Scripts/Gwent/UI/GwentViewFactory.cs`
- Modify: `Assets/Tests/EditMode/Gwent/GwentCatalogTests.cs`

- [x] Add failing tests for known local card art paths.
- [x] Map available local card art for known Witcher 3 Gwent cards.
- [x] Render mapped art in card views with text fallback for missing files.
- [x] Run EditMode tests and Windows batchmode build.
- [x] Commit with message `feat: map local gwent card art`.

### Task 8: Leader Ability Completeness

**Files:**
- Modify: `Assets/Scripts/Gwent/Core/GwentCore.cs`
- Modify: `Assets/Scripts/Gwent/Data/GwentCatalog.cs`
- Modify: `Assets/Tests/EditMode/Gwent/GwentCatalogTests.cs`

- [x] Add failing tests that every leader has a structured ability.
- [x] Implement discard, medic-lock, agile movement, and cancel/peek leader ability categories.
- [x] Run EditMode tests and Windows batchmode build.
- [x] Commit with message `feat: complete gwent leader ability coverage`.
