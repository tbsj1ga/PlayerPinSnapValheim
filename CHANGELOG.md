# Changelog

**English** · [Русский](CHANGELOG-RU.md)

The version is set in one place — `PlayerPinSnapPlugin.Version` in `src/PlayerPinSnapPlugin.cs`.

## 0.1.0

- First release. Other players' map pins jump to them when they are further than 100 m from the
  new position (a teleport, a portal) instead of gliding at 200 m/s. Player positions are sent
  every 0.5 s instead of every 2 s (on the server: the list for everyone; on a client: its own
  position). Snap distance, glide speed and send interval configurable.
