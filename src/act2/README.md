# env0.adventure

A deliberately minimal, old-school Choose Your Own Adventure engine written in C#.

This project is built in small, frozen versions.
Each version defines a clear endpoint, is completed, and then left alone.

If it feels boring, it’s working.

---

## v0 — Frozen

### What v0 is
v0 is a proof of viability.

It demonstrates that:
- a minimal CYOA loop can exist,
- the engine can be reasoned about end-to-end,
- state, choices, and transitions are explicit,
- and the game terminates cleanly.

v0 is complete and must not be extended.

### v0 Success Criteria (met)
- One navigable area
- One simple activity
- Explicit start and explicit end
- Numbered choices only
- Disabled choices shown with reasons
- State consists only of:
  - currentSceneId
  - boolean flags

Canonical v0 example:
- A hallway
- A locked door
- A kitchen
- The game ends

### v0 Constraints (non-negotiable)
- No AI
- No parser / free-text input
- No inventory
- No counters or timers
- No conditional scene text
- No save/load
- No UI
- No engine awareness of success or failure

v0 exists to prove the shape, not the scale.

---

## v0.1 — Implemented

v0.1 extends v0 horizontally, not vertically.

It adds enough structure to make JSON authoring worthwhile,
without introducing new systems or abstractions.

### What v0.1 adds
- Story content authored in JSON (story.json)
- All scenes, choices, and effects loaded at startup
- A small navigable space of 4–5 scenes
- Basic navigation between scenes
- One simple gated interaction using flags
- One explicit end scene (IsEnd = true)

Example scope:
- Hallway
- Kitchen
- Living room
- Bedroom
- Cupboard or utility space

Navigation should feel like moving through a mundane physical space.

### What v0.1 keeps the same
- Numbered choices only
- Choices always visible
- Disabled choices shown with a reason
- State remains:
  - currentSceneId
  - boolean flags only
- Effects remain limited to:
  - SetFlag
  - ClearFlag
  - GotoScene
- Exactly one scene transition per choice
- Fail fast on invalid data
- The engine remains intentionally dumb

### v0.1 Success Criteria (met)
- The player can navigate between at least 4 scenes
- At least one route is gated by a flag
- The objective can be completed, reaching an end scene
- All story content is loaded from JSON
- Invalid or malformed JSON crashes loudly with a clear error
- Behaviour is predictable and traceable

If adding JSON changes engine behaviour, v0.1 has failed.

### What v0.1 explicitly does NOT add
- No parser or verb/noun input
- No inventory system
- No conditional scene text
- No counters, timers, or progression systems
- No save/load
- No UI or rendering work
- No Godot integration
- No attempt at extensibility or tooling

v0.1 exists to prove authorability, not flexibility.

---

## v0.2 — In Progress

v0.2 introduces a minimal front-end and loosens authoring assumptions,
without adding new gameplay systems.

The purpose of v0.2 is to get the engine on screen.

### What v0.2 is
- The same dumb engine
- The same story model
- The same constraints
- Presented through a minimal Godot UI

v0.2 is about integration, not expansion.

### What v0.2 adds

#### 1. Godot Front-End (MVP)
- Scene title and scene text rendered in Godot
- Numbered choices rendered as UI elements
- Disabled choices visibly disabled with their reason
- Player selection routed through the engine
- End scenes handled cleanly (input disabled, end text shown)

The engine does not know it is being rendered in Godot.

#### 2. Non-Spatial Scenes (Authoring Only)
Scenes are no longer assumed to be physical locations.

A scene may represent:
- a moment
- a decision
- an internal thought
- a transition

Mechanically, scenes remain identical.
This is an authoring freedom, not a new system.

### What v0.2 keeps the same
- Numbered choices only
- Choices always visible
- Disabled choices shown with reasons
- State remains:
  - currentSceneId
  - boolean flags only
- Effects remain limited to:
  - SetFlag
  - ClearFlag
  - GotoScene
- Exactly one scene transition per choice
- One story, one JSON file

### v0.2 Success Criteria
- The existing story logic runs unchanged under Godot
- Godot acts purely as a view layer
- No story logic exists in the UI
- State changes are predictable and traceable
- The game reaches an end scene cleanly

If Godot needs to know game rules, v0.2 has failed.

### What v0.2 explicitly does NOT add
- No inventory
- No conditional scene text
- No counters or timers
- No parser or free-text input
- No save/load
- No multiple stories
- No tooling
- No engine awareness of success or failure

v0.2 exists to prove presentation, not power.

---

## Current state (v0.2 work-in-progress)

- Console app now builds as an executable and copies `story/*.json` alongside the binary.
- Temporary preloader lists available `story/*.json` files and prompts for a numeric selection before loading.
- xUnit test project added to cover scene validation, choice gating, and effect execution.
- Story content lives in `story/`; original `story/story.json` plus the new `story/newcalifornia.json`.

Next up: front-end integration for v0.2 (Godot view layer using the existing engine rules). The current preloader is a temporary console aid for story switching during authoring.
