# Plan 0004 — Honest wall sensing, part 1: TOUCH (rip out the god's-eye proximity field)

**Status:** Built ✓ for the `FruitFly.Living` memory fly (Play 6). The fake distance field is
gone (`World.WallProximity`/`WallFalloff` deleted); `World.Touching` reports honest contact; the
fly's wall neurons are now touch receptors driven on/off by antenna contact. Headless gate
passes: `path=3071px  collisions=168  longestStuck=53 steps → PASS` — the fly now *skims* walls
(far more contact events) and the memory latch frees each jam, exactly as predicted. The older
fly plays that still carried the fake proximity field (`BraitenbergFlyView`, `TwoFliesView`) were
since **deleted** as redundant — Play 6 is the only fly now, so no proximity field remains in the
gallery at all. Vision/looming remains deferred to 0005.
Chosen direction: do **touch first** (simple, truthful), with **vision/looming** deferred to a
later plan as the richer "eyes" answer. This plan covers touch only.
**Goal:** Replace the fly's fake "wall sensor" — a distance-to-nearest-wall field that nothing
on a real fly could measure — with an **honest contact (mechanoreceptor) sense**: the fly
learns a wall is there only when a body part *physically touches* it. Avoidance must still
**emerge** from the same cross/uncross wiring, now driven by real contact instead of a
god's-eye gradient.

## Why now

The user spotted it: of the fly's two "senses", only one is honest.

- **Smell** is real transduction. An odor field genuinely exists in the air (molecules diffuse,
  concentration falls off with distance); the antenna sampling local concentration is exactly
  what a fruit fly's chemoreceptors do. `World.Smell` models a real physical field. Keep it.
- **Wall proximity** (`World.WallProximity`) computes the *distance to the nearest wall* and
  hands it to the fly as a smooth `1/(1+r²)` field felt from ~45px away. Apply the CLAUDE.md
  fidelity test — *what physical quantity is the antenna transducing?* **Nothing.** There is no
  "wall-ness" diffusing through the air the way odor does. This number is the **bigger brain**
  (us) smuggling god's-eye knowledge into the fly and dressing it up as a sense organ. It is the
  one genuinely *faked* input in the whole creature.

This is not a new architecture decision — it's an application of the fidelity principle and
ADR 0002 (behavior emerges from real causes). We are removing a fake cause and substituting a
real one. (See memories: *fidelity-not-preference*, *tiny-brain-vs-bigger-brain*.)

## The concept

**Touch = a zero-range contact mechanoreceptor.** A real fly is covered in mechanosensory
bristles; when one is physically deflected by a surface, its neuron fires. The signal carries
**no distance information** — it is essentially "I am touching something *right now*" at the
place the bristle sits. That is the honest replacement for the proximity field:

- The cue has **zero range**. The fly gets nothing until an antenna actually reaches the wall.
  The graded "I feel the wall from 45px away" falloff — the fake part — disappears.
- The cue is **local to the sensor**. Left antenna touches → left contact neuron fires; this is
  what makes left/right asymmetry (and therefore turning) possible, exactly as before.
- It is still **transduction**, not decision: the body reports a real physical event (contact);
  the *turning* still emerges in the neurons from the existing wiring.

### What this is NOT (keep us honest)

- **Not** the nociceptor. Touch is light, informative contact at an antenna tip — it *guides*
  steering. Nociception is the body being **rammed and blocked** — *harm*. Real flies separate
  gentle mechanoreception from nociception, and so should we: touch tells the fly *which way to
  turn*; the nociceptor reports *that something went wrong*. Keep both, keep them distinct.
- **Not** a distance field with a smaller radius. Shrinking `WallFalloff` would still be the
  fake field, just nearer. The point is to delete the field, not tune it.

### Predicted emergent consequence (a thing to watch, not script around)

With distance-avoidance gone, the fly only reacts **on contact**, and an antenna touch is
**brief**. The cross/uncross reflex alone may only twitch. This is exactly where the existing
**memory latch** (Plan 0002) should start earning its keep: a brief touch charges the "I'm
stuck/blocked" interneuron, which **holds** the committed turn after the antenna has left the
wall. So touch + memory together should still escape — *reactively but truthfully*. If the fly
can no longer cope, that is real information about what the substrate needs next (e.g. vision),
**not** a cue to sneak the field back in.

## Steps (tiny pieces — assistant writes, one at a time)

- [x] **Lesson first:** contact mechanoreception vs. a distance field vs. nociception — why only
      one of the three is a "wall sensor a fly could have", and why touch carries no range.
- [x] **`World`: add an honest contact query.** Replaced `WallProximity(at)` with `Touching(at)`
      → `bool`: true when `at` has reached/crossed any of the four bounds — the **same** bounds
      `Clamp` enforces. `WallFalloff` deleted.
- [x] **`Fly`: drive the wall neurons from contact, not the field.** `_wallL/_wallR` renamed to
      `_touchL/_touchR` (touch receptors): fed a fixed `TouchCurrent` (40, = the old at-the-wall
      peak) while that antenna is `Touching`, zero otherwise. UNCROSSED touch→motor wiring kept.
- [x] **Re-check the memory coupling.** Wall→memory now charges on *contact events*. Left
      `WallToMemoryWeight` at 25; the headless gate confirms the latch still accumulates enough to
      commit escaping turns (longestStuck 53 < 240), so no tuning was needed.
- [x] **Keep the nociceptor as-is** (body ram via `Clamp(out collided)`). Touch (light antenna
      contact) and nociception (a real ram) stay cleanly separate in the brain and the viewer.
- [x] **Prove it headless.** `FlyHeadlessCheck` unchanged and passing with the fake field gone —
      now via touch + memory: `path=3071px  collisions=168  longestStuck=53 → PASS`.
- [x] **Viewer labels.** `BraitenbergFlyMemoryView` now reads `TouchL/TouchR`; the orange bars
      snap full only on contact; the text readout and header say "touch", not "wall".
- [x] **Docs:** plan status + FOUNDATIONS index updated; Fly/World header comments now describe
      touch receptors and a contact query, not a proximity field. (No new `notes/` entry — the
      intuition lives in "The concept" above; add one only if a later subtlety earns it.)

## Boundary test (keeps us honest)

For every line: *tiny brain* (neurons deciding) or *bigger brain providing* (body/world/
transduction)? `World.Touching` and the touch→current mapping are the body/world providing a
**real** event. The turn, the latch, the escape must stay in the neurons. No `if (touching)
turn` anywhere — if that temptation appears, the wiring is wrong, not the rule.

## Open questions to resolve with the user

- **Sensor placement.** Touch at the **antenna tips** (where smell already samples), or add
  dedicated forward bristles? Antennae are the simplest honest start and reuse existing geometry.
- **Contact current shape.** Pure on/off while touching, or a brief decaying pulse per contact
  (closer to a bristle's phasic burst)? Start on/off; revisit only if behaviour demands it.
- **Does this deserve an ADR?** Leaning *no* — it applies the existing fidelity principle and
  ADR 0002 rather than deciding something new. Promote to an ADR only if the touch/vision split
  becomes a structural choice worth recording.

## Done when

The fly has **no distance-based wall sense left** — `WallProximity`/`WallFalloff` are gone — and
it still seeks the banana and gets itself off walls using only **real contact + memory**, with
the headless check passing. The user can explain why the old field was the bigger brain cheating,
why touch is honest, and how contact + the memory latch produce escape without any scripted turn.

## Next (separate plan, deferred)

**0005 — Honest wall sensing, part 2: VISION / looming.** Give the fly a real eye: a wall read as
an *expanding* dark region in the visual field (optic flow / looming), wired to the motors so the
fly avoids *before* contact — the dominant mechanism in a real fruit fly, and the true answer to
the user's "eyes maybe?".
