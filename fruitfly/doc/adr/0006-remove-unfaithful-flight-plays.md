# ADR 0006 — Remove the hover and 3D-fly plays (8, 9) for fidelity

**Status:** Accepted (September 2026)

## Context

CLAUDE.md's non-negotiable is that every behaviour is *caused* through neurons, and that no
parameter is ever about "what we want the fly to do". Plays 8 (hover + vertical seek, Plan 0008)
and 9 (united fly in 3D, Plan 0009) were built on top of the honest wingbeat CPG (Play 7) and
looked convincing. An audit against that rule, prompted by the user's own sense that "anything
beyond Play 6 feels fake", found that they broke it in several places:

1. **The fly was told the answer.** `Fly` called `SeekAltitude(world.BananaHeight)` every frame.
   `FlyingBody` then computed a *private* one-dimensional odour that peaked at that height, with its
   own falloff constant different from the `World`'s real smell field. The vertical smell receptors
   smelled a field that did not exist in the world. That is why it "nailed the height every trial".
2. **Outcome-tuned constants.** The code's own comments said the receptor span was widened "so the
   climb stays strong right up to the food" and the smell weight was "tuned up until the body follows
   the food down as well as up".
3. **Designed equilibrium.** `LiftGain` and the pacemaker synapse weight were sized against each
   other so the baseline beat exactly equalled body weight; the hover reflex only corrected around a
   hover point the designer set. `PacemakerCheck` then guarded that calibration.
4. **A god's-eye sense.** The motion receptors were fed the body's vertical velocity as a scalar,
   the same shortcut Plan 0004 removed for walls when it replaced a distance field with touch.
5. **Two brains drawn as one.** Play 9's "united" fly was the horizontal `Fly` network and the
   vertical `FlyingBody` network, sharing no neurons, stepped side by side and summed into one body.
   The 3D view flapped wings driven by the CPG while forward motion still came from the 2D wheel
   abstraction, so the picture attributed movement to a mechanism that did not produce it.

Point 1 alone is disqualifying. Points 2 to 5 are why patching was not the right move.

## Decision

Delete Plays 8 and 9 and everything only they used: `FlyingBody`, `HoverView`, `Fly3DView`,
`FlyBrainOverlay`, the three headless checks (`FlyingBodyCheck`, `AltitudeSeekCheck`,
`Fly3DHeadlessCheck`), the 3D mode in `Fly`, and the banana height in `World`. Drop the vigour
calibration assertion from `PacemakerCheck`. Keep Play 7 and Plan 0010's pacemaker: they are honest
and pass the rule on their own.

Plans 0008 and 0009 stay in `doc/ai/plans/` with a REMOVED status header, as a record of the failure
mode.

## Consequences

- The gallery ends at Play 7. Flight is not demonstrated; only the rhythm that will drive it is.
- Flight restarts from Play 7 under two constraints that would have prevented the above:
  - **One world, one odour field.** Smell is a function of 3D distance to the banana, and every
    receptor smells that field at its own real position. Far from the banana the vertical gradient is
    tiny, so the fly will wander in height until it is close. That is the honest result, and a check
    that expects otherwise is wrong.
  - **One network.** Real flies steer by beating one wing harder than the other and gain lift by
    beating both harder. The CPG's left and right wing cells become the motors that smell and touch
    modulate, replacing the Play 6 motor neurons rather than sitting beside them. Thrust, yaw and
    lift all come out of the same two spike trains.
- Behaviour that took months to build was thrown away. That is the intended cost of the rule: a
  demo that looks right but is not caused right is worth less than no demo.
