# Bejeweled 3 Accessible

Accessible edition of Bejeweled 3 with full screen-reader support (NVDA and SAPI), spatial binaural audio and keyboard-only control.

This repository contains the **complete source code** (C#/.NET 4.5) and publishes **ready-to-play binary releases**. Technical documentation in [README_DEV.md](README_DEV.md).

> **About the audio assets:** the game packs its effects, voices and music into `audio.pac`. That package uses only **XOR obfuscation** (the key lives in `src/Audio/PacCipher.cs`), not cryptography: since the source code is public, anyone can extract the content. It is a cosmetic barrier for the original files, not real encryption.

## Download

Go to the [Releases](../../releases) tab and download the latest zip.

## Requirements

- Windows 10/11 (tested on Windows 11)
- NVDA installed for the best voice experience (also works with SAPI)

## How to Play

1. Unzip into any folder.
2. Run `Bejeweled3Accessible.exe`.
3. Create a player profile and pick a mode: Classic, Zen, Lightning, Poker, Butterflies, Ice Storm, Diamond Mine or Quest.

The whole game is operable with the keyboard and communicates through voice and spatial sound.
