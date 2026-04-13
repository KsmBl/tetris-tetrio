# tetriorun

A 40-line sprint Tetris clone written in C# (.NET 10) using [Raylib-cs](https://github.com/chrisdill/raylib-cs).  
Targets the feel and rule-set of [TETR.IO](https://tetr.io).

## Features

- **40-line sprint** — clear 40 lines as fast as possible
- **SRS rotation** — full Super Rotation System with wall kicks (JLSTZ + dedicated I tables)
- **Hold queue** — hold one piece per placement (`Z` by default)
- **Ghost piece** — drop preview
- **DAS / ARR** — configurable auto-repeat via `handling.json`
- **Wall snap** — hold a direction for 120 ms to snap the piece to the wall
- **Progress indicator** — faded marker shows the target line when fewer than 20 remain

## Controls

| Key | Action |
|-----|--------|
| `←` / `→` | Move |
| `↓` | Soft drop (snaps to floor instantly) |
| `Space` | Hard drop |
| `X` | Rotate counter-clockwise |
| `V` | Rotate clockwise |
| `C` | Rotate 180° |
| `Z` | Hold |

All keys are rebindable in `handling.json`.

## Building

**Requires:** .NET 10 SDK

```bash
dotnet run          # dev build + launch
dotnet build        # build only
```

### Standalone binary

`PublishSingleFile` and `SelfContained` are already set in the project file:

```bash
dotnet publish -c Release -r linux-x64
```

The binary is written to `bin/Release/net10.0/<rid>/publish/tetriorun` with no external dependencies.

Common runtime identifiers: `linux-x64`, `linux-arm64`, `win-x64`, `osx-x64`, `osx-arm64`.

> **Do not add `-p:PublishTrimmed=true`.**  
> `System.Text.Json` reflection used for `handling.json` is not trim-safe — trimming silently breaks config deserialization and falls back to hardcoded defaults.

## handling.json

`handling.json` must be in **the same directory as the binary**. It is created with defaults on first run if missing.

```json
{
  "ARR": 0,
  "DAS": 10,
  "MoveLeft": "Left",
  "MoveRight": "Right",
  "SoftDrop": "Down",
  "HardDrop": "Space",
  "RotateCW": "V",
  "RotateCCW": "X",
  "Rotate180": "C",
  "Hold": "Z"
}
```

`ARR 0` = instant auto-repeat (piece snaps to wall after DAS). Key names match Raylib's `KeyboardKey` enum values.

## Dependencies

| Package | Version |
|---------|---------|
| [Raylib-cs](https://github.com/chrisdiss/raylib-cs) | 7.0.2 |
