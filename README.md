# PlayerPinSnap

**English** · [Русский](README-RU.md)

A Valheim mod: other players' map pins jump to them after a teleport instead of crawling across
the map, and their positions reach the map more often than every 2 seconds. After a portal you
see on the map where your friend is right away, not half a minute later.

Version history is in `CHANGELOG.md`.

## What changes

| | In the game | PlayerPinSnap |
|---|---|---|
| A player's pin after a teleport | glides at 200 m/s: ~25 s for 5 km | jumps there at once (further than `SnapDistance`, 100 m) |
| A player's pin while walking, riding, sailing | glides at 200 m/s | the same (`GlideSpeed`) |
| How often positions are sent | every 2 s each way: up to ~4 s old on the map | every 0.5 s (`SendInterval`) |

## How it works

The map draws other players from the game's player list. Each frame `Minimap.UpdatePlayerPins`
moves a pin towards the listed position with `Vector3.MoveTowards` at 200 m/s. A transpiler
replaces that call: a pin further than `SnapDistance` from the new position is put there at once,
a closer one glides at `GlideSpeed`.

The list is refreshed in `ZNet.SendPeriodicData` every 2 seconds, both ways: every client sends
its own position to the server (`SendServerSyncPlayerData`), and the server sends everyone's
positions to every client (`SendPlayerList`). With `SendInterval` below 2 the mod makes the same
calls more often, on top of the vanilla ones — the server sends the list, a client sends its own
position. Nothing new goes over the network: these are the game's own messages, so players and
servers without the mod read them as usual.

Players who hid their position on the map stay hidden: the list only has public positions.

## Compatibility

Built against **Valheim 1.0.20** (network version 41), **BepInEx 5.4.23.5**. Other mods that
draw player markers from the player list (compasses, player markers) get the fresher positions
too.

## Who needs it

| Who | What it gives |
|---|---|
| A player | pins on their own map jump after teleports; their own position reaches the server more often |
| The server (dedicated, or the host) | the list of positions goes to every client more often |
| A player without the mod | nothing breaks; pins on their map glide as in the game |

For the fastest updates: the mod on the server and on every player. The pin jump alone needs it
only on the player who looks at the map.

## Settings

`BepInEx/config/j1ga.playerpinsnap.cfg`, or in-game with ConfigurationManager (F1). Applies at
once.

| Section | Key | Default | |
|---|---|---|---|
| 01 General | `Enabled` | `true` | off: everything as in the game |
| 02 Map | `SnapDistance` | `100` | meters; a pin further than this jumps, closer glides; 0: always jumps |
| 02 Map | `GlideSpeed` | `200` | m/s, in the game 200 |
| 03 Network | `SendInterval` | `0.5` | seconds, 0.1–2; 2 is the game's interval |

## Bugs

[GitHub issues](https://github.com/tbsj1ga/PlayerPinSnapValheim/issues).

## Building

```
powershell -ExecutionPolicy Bypass -File .\build.ps1            # build and check references
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Install   # ... and copy into plugins
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Package   # ... and make the Thunderstore zip
```

## Credits and AI assistance

This mod was developed with the help of an AI assistant (Claude by Anthropic); the design
decisions, in-game testing and releases are the author's.
