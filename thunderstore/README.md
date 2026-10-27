# PlayerPinSnap

**Find each other after a teleport.** In the game another player's map pin glides towards them at
200 m/s — after a portal across the world it takes half a minute to get there, and the positions
themselves are refreshed only every 2 seconds. PlayerPinSnap makes the pin jump straight to the
player after a teleport and sends positions more often.

- Pins further than 100 m from the new position jump there; closer ones glide as in the game.
- Positions are sent every 0.5 s instead of every 2 s.
- Uses the game's own messages: players and servers without the mod are not affected.
- **Who needs it:** the pin jump — the player who looks at the map. Faster updates — the server
  (sends everyone's positions) and each player (sends their own). Best: everyone.

## Settings

`BepInEx/config/j1ga.playerpinsnap.cfg`, or ConfigurationManager (F1):

- `01 General / Enabled` — `true`
- `02 Map / SnapDistance` — `100` m; 0: pins always jump
- `02 Map / GlideSpeed` — `200` m/s (as in the game)
- `03 Network / SendInterval` — `0.5` s, 0.1–2 (2 is the game's)
