# Product

<!-- impeccable:product-schema 1 -->

## Platform

android

## Users

Families and friends playing locally on Android TV, phones, and foldable Android devices. The primary session is a shared-screen game controlled by a TV remote or a paired controller; a second local player may join without being required.

## Product Purpose

Family Force Unity is an original retro-modern 2D beat-'em-up. It turns a family cast into a clear, approachable local co-op action game with selectable heroes and independent Link companions.

## Positioning

The game is built as its own family-focused, data-driven combat experience rather than a copy of Streets of Rage. Customer Packs will later let each customer replace roster identity, name, logo, and colors without changing game code.

## Operating Context

Sessions run on Android TV, phones, and foldables. Players navigate every menu with D-pad, Confirm, and Back, and may connect, disconnect, or reconnect controllers during play. The fixed internal presentation target is 640x360 with precise pixel art.

## Capabilities and Constraints

- Unity 6 project with the modern Input System and a fixed 60 Hz combat simulation.
- One or two local players; P2 is optional.
- Four current characters: Essa (177 cm), Adam (5 years / 108 cm), Shaikha (5 years / 108 cm), and Sulaiman (8 years / 124 cm).
- Planned move set: idle, walk, punch, kick, heavy punch, heavy kick, jump, special, link, hurt, and knockdown; grab, throw, and get-up follow later.
- Combat is data-driven with state machines, hitboxes/hurtboxes, startup/active/recovery, hit pause, and knockback.
- Runtime needs Android TV remote, DualSense, Xbox Controller, Nintendo Switch Joy-Con/Joy-Con 2 support, controller assignment, hot-plug, and reconnect.
- Performance target includes Xiaomi Stick, Nvidia Shield, and Sony/Skyworth TVs; stage atlases load per stage rather than all at once.
- Stage-one distribution is a direct APK, not an app-store release.

## Brand Commitments

The title is **Family Force Unity**. The game uses original retro-modern 2D pixel art at 640x360; oversized pixel art, AI-generated video, and imitation of Streets of Rage are out of scope. Temporary original placeholder art is permitted during the vertical slice.

## Evidence on Hand

The repository contains an original Unity vertical-slice foundation, character ScriptableObjects, placeholder geometry, a development APK, and controller diagnostics. Final sprites, logo, customer art packs, and production audio are not yet available and must not be represented as final.

## Product Principles

1. A TV player can complete essential flows without touch input.
2. Controller compatibility is observable and recoverable, not assumed.
3. Combat and content data remain separable from customer identity.
4. Temporary art proves interaction and scale without pretending to be final production art.
5. The first slice stays small enough to run reliably on low-power Android TV hardware.

## Accessibility & Inclusion

All menus require an obvious, persistent focus state and D-pad/Confirm/Back operation. The optional second player must not prevent a solo player from starting a session.
