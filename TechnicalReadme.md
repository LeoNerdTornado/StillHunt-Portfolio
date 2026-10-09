# Still Hunt --- Technical Case Study

> A Unity 3D tactical hunting prototype focused on perception,
> deliberate movement, concealment, and player-versus-AI encounters.

**Engine:** Unity 2022.3.62f3\
**Language:** C#\
**Platform:** Windows\
**Project type:** Personal game-development portfolio project

This document explains the main gameplay systems, how they work
together, and what the screenshots demonstrate. It is a focused
technical walkthrough, not a line-by-line explanation of every script.

------------------------------------------------------------------------

## 1. System Overview

Still Hunt connects two sides of a tactical encounter.

-   **Player systems** let the player choose a destination, enter and
    switch cover positions, change posture, peek from cover, and aim.
-   **Guard AI systems** let the guard perceive the player, coordinate a
    tactical response, evaluate cover options, and execute navigation
    and combat behaviors.

``` text
PLAYER
Mouse + keyboard
      |
      v
CoverSystem --------------------> PlayerMovement
(destination / cover selection)   (movement / crouch / posture)
                                        |
                         +--------------+--------------+
                         |                             |
                         v                             v
                CoverStanceSystem          ADS aiming components
                (lean / peek)              (ADS / head / hand target)

GUARD AI
GuardVision --> GuardAI --> GuardCover
(perception)   (decisions)  (cover evaluation)
                    |
                    v
              GuardMovement
        (navigation / state behavior)
```

The scripts have separate responsibilities, but they cooperate through
component references, shared state, and method calls.

------------------------------------------------------------------------

## 2. Guard AI --- Four Cooperating Responsibilities

The four guard scripts form the main AI case study. They should be
presented as cooperating components, not as four completely independent
systems.

### `GuardVision.cs` --- Perception and evidence

`GuardVision` handles visibility checks and maintains information used
by the AI, including detection state and the player's last-known
position.

-   **`guardCanSeeMe`** checks whether the guard's visibility logic can
    see the player.
-   **`playerCanSeeMe`** checks whether the player's view can see the
    guard. It is **not** the guard's peripheral-vision check.
-   **`playerInsideVisionCone`** is a separate check associated with
    whether the player is inside the guard's vision cone.
-   **`playerDetected`** records whether the guard's detection logic has
    registered the player.

This distinction supports Still Hunt's focus on concealment and
exposure: the guard seeing the player and the player being able to see
the guard are different questions. When a sighting is registered, the
vision logic can update detection, observation/investigation
information, and last-known-position data. The AI's response remains
state-dependent; detection does not guarantee an immediate shot in every
situation.

### `GuardAI.cs` --- Tactical decision coordinator

`GuardAI` coordinates tactical decisions and state transitions based on
the current situation and selected tactical goal. It is the
decision-and-coordination layer---not simply a script that fires
whenever the player is detected.

For example, during `Observe`, the selected `observeTacticalGoal` can
lead to different responses:

-   **Survive / GainBetterPosition:** choose cover and transition toward
    `MoveToCover`.
-   **PinPlayer:** transition to `AimFire`.
-   **HuntPlayer:** transition to `Investigate`.
-   **Patrol:** return to `Patrol`.
-   **EscapeRoute:** transition to `EscapeRoute`.

The selected goal and current state determine the path; not every
encounter follows the same sequence.

### `GuardCover.cs` --- Tactical cover evaluation

`GuardCover` evaluates candidate positions and supports the guard's
cover-related behavior. `ChooseBestCover()` evaluates candidate cover
points, while the scoring logic considers factors such as reachability,
danger, visibility/occlusion, concealment, prior cover experience, and
tactical opportunities.

The goal is not merely to move behind the nearest rock. The system
compares possible positions and selects a suitable reachable option
based on its scoring logic. The script also contains cover-related
exposure and peeking behavior.

### `GuardMovement.cs` --- Navigation and behavior execution

`GuardMovement` contains navigation and movement/state behavior. Its
states include `Patrol`, `Observe`, `MoveToCover`, `AimFire`,
`Investigate`, and `EscapeRoute`, as well as additional cover, search,
and recovery states.

`GuardAI` can change `guardMovement.currentState`; `GuardMovement`
implements the corresponding movement and state behavior. The decision
logic and movement execution are connected rather than wholly
independent.

### Example encounter flow

``` text
Patrol
  -> perception registers evidence of the player
  -> GuardAI evaluates the situation and tactical goal
  -> Observe becomes a decision point
  -> choose a response:
       move to cover
       aim / engage
       investigate a suspected location
       resume patrol
       follow an escape route
  -> GuardMovement carries out the selected behavior
```

**Engineering concepts:** modular component design, perception checks,
state-based behavior, tactical decision coordination, scoring-based
cover selection, and navigation.

------------------------------------------------------------------------

## 3. Player Click-to-Move --- `CoverSystem.cs` + `PlayerMovement.cs`

![Still Hunt click-to-move destination
selection](Portfolio/Screenshots/Technical/StillHunt-ClickToMove.png)

`CoverSystem` handles destination selection and the preview. While Space
is held, a camera ray passes through the mouse position to identify a
valid ground location, and the preview follows that point. A left-click
commits the ground destination through
`PlayerMovement.SetDestination()`.

`PlayerMovement` calculates the direction and moves the
`CharacterController` toward the recorded destination. This separates
**choosing where to go** from **executing movement**. When a ground
destination is selected, the click-to-move path also clears the current
cover references so the player can leave cover.

**Interview explanation:** "I separated world-space destination
selection from character movement. `CoverSystem` identifies the intended
point, then `PlayerMovement` moves the controller toward it."

------------------------------------------------------------------------

## 4. Player Cover Interaction --- `CoverSystem.cs`

![Still Hunt cover
interaction](Portfolio/Screenshots/Technical/StillHunt-Cover-Scene.png)

Pressing **C** requests a cover interaction.

-   If the player is not in cover, `CoverSystem` searches for a usable
    cover point.
-   The selection logic evaluates a point associated with the cover in
    front of the player rather than blindly snapping to the nearest
    rock.
-   The selected cover and cover point are stored in `PlayerMovement`,
    and `SetDestination()` initiates movement toward that point.
-   If the player is already in cover, the system can attempt to switch
    to another usable point belonging to the current cover.

This is the **player cover interaction system**. It is distinct from
`GuardCover.cs`, which evaluates tactical cover choices for the AI
guard.

**Interview explanation:** "The player and AI both use cover, but their
responsibilities differ: `CoverSystem` handles player input and
cover-point selection, while `GuardCover` evaluates cover for the
guard's tactical behavior."

------------------------------------------------------------------------

## 5. Lean and Peek --- `CoverStanceSystem`

![Still Hunt leaning over
cover](Portfolio/Screenshots/Technical/StillHunt-Lean-Over-Cover.png)

While the player is in cover, `CoverStanceSystem` calculates a body
offset from the neutral position:

-   **A:** lean left.
-   **D:** lean right.
-   **W:** peek upward.

The `UpdateBodyStance()` method smoothly moves `bodyStanceAnchor` toward
the target offset. When no lean or peek key is held, the target returns
to zero, bringing the body back toward its neutral position.

This lets the player expose part of the body for observation or aiming
without instantly repositioning the whole character.

**Engineering concepts:** local-space transform offsets, state-dependent
input, and smoothed positional transitions.

------------------------------------------------------------------------

## 6. Crouch and Posture --- `PlayerMovement.cs`

![Still Hunt before
crouching](Portfolio/Screenshots/Technical/StillHunt-Pre-Crouch.png)

![Still Hunt after
crouching](Portfolio/Screenshots/Technical/StillHunt-PostCrouch.png)

**Left Shift** toggles the crouch state. `PlayerMovement` coordinates
more than the character's appearance:

-   Smoothly adjusts the `CharacterController` height and center toward
    the configured posture.
-   Adjusts the soldier model's local position.
-   Updates the Animator's `IsCrouching` parameter.
-   Updates the aiming offsets for the current posture.
-   Selects `crouchSpeed` or `walkSpeed` according to the crouch state.

The transition uses interpolation to avoid an abrupt change. The
controller, model position, animation, movement speed, and aiming setup
are coordinated by the movement component.

**Interview explanation:** "Crouching is a gameplay state that
coordinates the controller, model, animation, movement speed, and aiming
offsets---not just an animation trigger."

------------------------------------------------------------------------

## 7. Aim Down Sights (ADS) --- Coordinated Aiming Components

![Still Hunt before aiming down
sights](Portfolio/Screenshots/Technical/StillHunt-PreAim.png)

![Still Hunt aiming down
sights](Portfolio/Screenshots/Technical/StillHunt-PostAim.png)

Aiming is split across cooperating components rather than handled by one
script.

-   **`WeaponADSController`** reads the right-mouse-button aiming input
    and coordinates the ADS state, camera field of view, weapon-rig
    weight, and related aiming controllers.
-   **`ADSHeadAimController`** adjusts the character's head-aim behavior
    in response to the aiming state.
-   **`WeaponAimTargetFollower`** updates the right-hand/weapon aim
    target so the hand and weapon can follow the intended aiming target.
-   **`WeaponSightDebug`** is a supporting debugging tool that helps
    inspect the weapon sight direction; it is not the main ADS
    coordinator.

### ADS interaction flow

``` text
Player holds right mouse button
        |
        v
WeaponADSController
  - updates aiming state
  - adjusts camera field of view / rig weight
  - notifies supporting controllers
        |
        +-----------------------+
        |                       |
        v                       v
ADSHeadAimController    WeaponAimTargetFollower
(head aim response)     (right-hand / weapon target)
```

Separating ADS coordination from detailed head and hand adjustments
makes the responsibilities easier to understand and tune.

**Interview explanation:** "I split ADS coordination from the head and
hand target behavior. The main controller manages the aiming state and
communicates it to the supporting controllers, which adjust their
respective targets."

------------------------------------------------------------------------

## 8. Suggested Technical Demo

Use one short recording or live demonstration to connect the systems:

1.  Hold **Space**, select a ground destination with the mouse, and
    left-click.
2.  Press **C** to enter cover or switch to another usable cover point.
3.  Use **A / D / W** to lean or peek while in cover.
4.  Toggle crouch with **Left Shift** and show the posture transition.
5.  Hold the **right mouse button** to aim down sights.
6.  Show the guard patrolling, registering evidence of the player, and
    choosing a tactical response.

The goal is to show the main player systems, then explain one observable
guard-AI decision sequence. Do not claim that every possible AI path
occurs in every encounter.

------------------------------------------------------------------------

## 9. Technical Highlights

-   Four-script guard AI architecture: perception, tactical
    coordination, cover evaluation, and movement execution.
-   Separate checks for guard visibility of the player and player
    visibility of the guard.
-   State-dependent tactical responses rather than assuming every
    detection immediately causes firing.
-   Scoring-based selection of reachable AI cover positions.
-   Mouse-ray destination preview and click-to-move movement.
-   Cover-point selection and cover switching.
-   Smoothed body lean/peek offsets.
-   Crouch state coordinating controller dimensions, model position,
    animation, movement speed, and aim offsets.
-   ADS coordination separated from head-aim and right-hand
    target-following behavior.

## 10. Technology

-   Unity 2022.3.62f3
-   C#
-   Unity `CharacterController` and `Animator`
-   Raycasting and layer masks
-   Transform and vector calculations
-   State-based gameplay logic
-   Git and GitHub

## 11. Scope and Accuracy

Still Hunt is a personal prototype and an evolving game project. The
explanations here reflect the supplied scripts and project notes. Exact
tactical responses depend on runtime state, configuration, and the code
path; not every encounter follows the same sequence.

The public portfolio repository contains selected source code and
presentation material. The full Unity development project and
third-party source assets are kept separately in the private development
repository.

------------------------------------------------------------------------

## Screenshot Setup

Place these seven screenshots in the public repository under
`Portfolio/Screenshots/Technical/`:

``` text
StillHunt-ClickToMove.png
StillHunt-Cover-Scene.png
StillHunt-Lean-Over-Cover.png
StillHunt-PreAim.png
StillHunt-PostAim.png
StillHunt-Pre-Crouch.png
StillHunt-PostCrouch.png
```

The image links above use those exact filenames and paths. If your files
have different names, either rename them to match or update the Markdown
links. Add this document to the repository root as `TechnicalReadme.md`,
then add a link to it from the main `README.md`.
