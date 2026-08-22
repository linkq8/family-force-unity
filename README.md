# Family Force

Independent Unity 6 project for a retro-modern 2D beat-'em-up targeting Android phones, foldables, and Android TV.

## Baseline

- Unity 6.3 LTS: `6000.3.22f1`
- Internal presentation target: `640x360`
- Combat simulation: fixed `60 Hz`
- Input: Unity Input System with local player device pairing
- Rendering: URP 2D / pixel-perfect presentation
- Distribution phase 1: directly installable APK

## Safety boundary

This directory is independent. No files from the legacy Android project are referenced or linked. Approved legacy assets must be inventoried and copied only after rights and technical review.

## Bootstrap

After Unity finishes importing packages, run:

`Tools > Family Force > Build Vertical Slice Foundation`

The command creates the bootstrap scene, placeholder character data, move data, and Android-oriented project settings.
