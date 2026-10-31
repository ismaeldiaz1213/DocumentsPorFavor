# Redesign plan (polish pass, started 2026-10-31)

Goal: the game should feel clean and readable, with clear stakes, a real way to lose, a real way to win,
sound for everything, and one new thing to check each day.

## Days: one new rule per day

| Day | New this day | What the player checks |
|---|---|---|
| 1 | Passport only | Passport not expired; the photo matches the face at the window |
| 2 | Entry permit for non-US citizens | US citizens need none; the permit name matches the passport, and the permit is not expired |
| 3 | X-ray scan | Contraband or hidden people means IMPRISON |
| 4 | Vehicle registration | Owner matches the passport; the plate matches the plate cam |

- Each morning starts with a memo from the jefe explaining the new rule. A rulebook can be opened at any time.
- All travelers are handwritten (no random travelers). Story travelers from the old 3-day version move
  to the day whose rule fits their problem.

## Shift clock

- A clock runs from 07:00 to 19:00 (a few real minutes). Pay is per correct traveler, so slow play costs money.
- At 19:00 the traveler at the window is finished, and everyone still in line goes home.

## The family member: Memo, your older brother

- Day 2: Memo arrives with no entry permit and wants to see Abuela. The rules say reject.
  Approving him is a citation, but home that night is warmer.
- If he was rejected: "Está bien, hermano. I'll find another way."
- Later: a box truck's X-ray shows a hidden person holding a small cross (Abuela's rosary, which Memo
  mentioned). The game never says it is him.

## Win and lose

- **Fired:** the record reaches -10 points. The game ends that day.
- **Evicted:** savings below $0 at the end of two nights.
- **Abuela:** three nights without medicine.
- **Win:** survive Day 4. The home images reflect how the family is doing.
- Endings state the facts and show images of home. They never explain (see the "story is implied" rule).

## Feel

- Main menu, then an intro in typewriter text, then the first shift.
- A NEXT megaphone calls each car. Cars drive in over time and join the back of the line.
- The traveler's face appears at the booth window while they talk.
- Sound for everything, synthesized in the editor (menu: Documents Por Favor > Generate Sounds).

## Status (2026-10-31)

Steps 1-5 are written: sounds, rules by day, memo and rulebook, permit and passport photo,
portraits, shift clock, NEXT megaphone, arriving cars, José as a security guard who walks over,
the guard escort on IMPRISON, Memo's arc, the win and lose endings, and the intro text.
Run "Build Everything" to apply them to the scene. Step 6 (animation polish) is ongoing.

## Build order

1. Sound generator and `Sfx` player
2. Rules that turn on by day, the memo and rulebook, new documents (photo, permit)
3. Portraits at the window
4. Shift clock, NEXT megaphone, arriving cars
5. Memo's arc, win/lose endings, main menu and intro
6. Animation polish
