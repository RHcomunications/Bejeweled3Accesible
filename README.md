# Bejeweled 3 Accesible

Edición accesible de Bejeweled 3 con soporte total para lectores de pantalla (NVDA y SAPI), audio binaural espacial y control por teclado.

Este repositorio contiene el **código fuente completo** (C#/.NET 4.5) y publica **releases de binarios** listos para jugar. Documentación técnica en [README_DEV.md](README_DEV.md).

> **Sobre los assets de audio:** el juego empaqueta sus efectos, voces y música en `audio.pac`. Ese paquete usa solo **ofuscación XOR** (la clave está en `src/Audio/PacCipher.cs`), no criptografía: al publicar el código fuente, cualquiera puede extraer el contenido. Es una barrera cosmética para los archivos originales, no un cifrado.

## Descargar

Ve a la pestaña [Releases](../../releases) y descarga el zip más reciente.

## Requisitos

- Windows 10/11 (probado en Windows 11)
- NVDA instalado para la mejor experiencia de voz (también funciona con SAPI)

## Cómo jugar

1. Descomprime el zip en cualquier carpeta.
2. Ejecuta `Bejeweled3Accessible.exe`.
3. Crea un perfil de jugador y elige un modo: Clásico, Zen, Relámpago, Póker, Mariposas, Tormenta de Hielo, Mina de Diamantes o Quest.

Todo el juego es operable con el teclado y se comunica por voz y sonido espacial.
