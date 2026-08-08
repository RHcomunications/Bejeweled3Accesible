# Bejeweled 3 Accessible

> **Español** · **English**

## 🇪🇸 Español

Edición accesible de Bejeweled 3 con soporte total para lectores de pantalla (NVDA y SAPI), audio binaural espacial y control por teclado.

Este repositorio contiene el **código fuente completo** (C#/.NET 4.5) y publica **releases de binarios** listos para jugar. Documentación técnica en [README_DEV.md](README_DEV.md).

> **Sobre los assets de audio:** el juego empaqueta sus efectos, voces y música en `audio.pac`. Ese paquete usa solo **ofuscación XOR** (la clave está en `src/Audio/PacCipher.cs`), no criptografía: al publicar el código fuente, cualquiera puede extraer el contenido. Es una barrera cosmética para los archivos originales, no un cifrado.

### Descargar

Ve a la pestaña [Releases](../../releases) y descarga el zip más reciente.

### Requisitos

- Windows 10/11 (probado en Windows 11)
- NVDA instalado para la mejor experiencia de voz (también funciona con SAPI)

### Cómo jugar

1. Descomprime el zip en cualquier carpeta.
2. Ejecuta `Bejeweled3Accessible.exe`.
3. Crea un perfil de jugador y elige un modo: Clásico, Zen, Relámpago, Póker, Mariposas, Tormenta de Hielo, Mina de Diamantes o Quest.

Todo el juego es operable con el teclado y se comunica por voz y sonido espacial. En las opciones de sonido y voz puedes elegir entre tres perfiles de audio espacial: **Escenario 2D** (paisaje sonoro teatral completo), **Clásico Limpio** (por defecto, el carácter arcade nítido del original) o **Simple** (solo paneo izquierda/derecha).

## 🇬🇧 English

Accessible edition of Bejeweled 3 with full screen-reader support (NVDA and SAPI), spatial binaural audio and keyboard-only control.

This repository contains the **complete source code** (C#/.NET 4.5) and publishes **ready-to-play binary releases**. Technical documentation in [README_DEV.md](README_DEV.md).

> **About the audio assets:** the game packs its effects, voices and music into `audio.pac`. That package uses only **XOR obfuscation** (the key lives in `src/Audio/PacCipher.cs`), not cryptography: since the source code is public, anyone can extract the content. It is a cosmetic barrier for the original files, not real encryption.

### Download

Go to the [Releases](../../releases) tab and download the latest zip.

### Requirements

- Windows 10/11 (tested on Windows 11)
- NVDA installed for the best voice experience (also works with SAPI)

### How to Play

1. Unzip into any folder.
2. Run `Bejeweled3Accessible.exe`.
3. Create a player profile and pick a mode: Classic, Zen, Lightning, Poker, Butterflies, Ice Storm, Diamond Mine or Quest.

The whole game is operable with the keyboard and communicates through voice and spatial sound. In the Sound & Voice options you can pick one of three spatial audio profiles: **Stage 2D** (full theatrical soundscape), **Clean Classic** (default, the crisp original arcade character) or **Simple** (bare left/right pan).
