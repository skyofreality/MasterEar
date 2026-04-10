# Master's Ear

An immersive Unity XR learning project focused on ear anatomy education.

Master's Ear combines interactive 3D anatomy models, lesson-based navigation, and a live AI teaching assistant workflow designed for VR learning experiences (Meta Quest oriented).

## What This Project Is

This repository contains the Unity client for a VR anatomy learning experience where learners can:

- Explore outer, middle, and inner ear structures in 3D.
- Switch between lessons with dedicated model + video content.
- Interact with anatomy parts using XR interactions (grab, inspect, explode views).
- Send context-aware questions to a live AI tutor over WebSocket.

## Core Features

- Lesson system
  - Dynamic lesson cards/buttons.
  - Per-lesson model loading, metadata, thumbnails, and videos.
  - Scene/skybox changes by lesson.

- Interactive anatomy exploration
  - Whole-model and small-part interaction modes.
  - Exploded view controls for anatomy models.
  - Label/context hooks for selected anatomy parts.

- Live AI tutor pipeline
  - Real-time WebSocket client for sending selected anatomy context + learner questions.
  - Optional microphone streaming for voice-first interactions.
  - Teacher personality presets (friendly, quiz, exam coach, beginner, socratic).

- XR and VR support stack
  - Meta XR Core/Interaction/Audio SDK integrations.
  - Oculus XR setup and interaction tooling.
  - URP rendering pipeline.

## Tech Stack

- Unity: 6000.0.26f1
- Render Pipeline: URP
- XR: Meta XR SDK + Oculus XR
- Networking: NativeWebSocket (UPM Git dependency)
- UI: TextMeshPro + Unity UI

## Project Structure (Important Paths)

- `Assets/Scenes/` - Main learning and demo scenes.
- `Assets/Scripts/` - Core app logic (lessons, interactions, AI client).
- `Assets/Prefabs/` - Reusable in-scene prefabs.
- `Assets/Models/` - Anatomy and environment models.
- `Assets/Videos/` - Lesson-linked media content.
- `ProjectSettings/` - Unity project configuration.
- `Packages/manifest.json` - Unity package dependencies.

## Quick Start

1. Install Unity Editor `6000.0.26f1` (Unity 6).
2. Clone this repository.
3. Open the project folder in Unity Hub.
4. Let Unity import packages and assets fully.
5. Open a main scene, for example:
   - `Assets/Scenes/inner_ear_test.unity` (currently enabled in build settings)
6. Press Play in Editor or build to Android/Quest.

## AI Tutor Backend Setup

The Unity client expects a WebSocket backend endpoint.

Default in code:

- `ws://127.0.0.1:8080/live`

To use on-device (Quest), set the backend URL to your development machine's LAN IP (same network), for example:

- `ws://192.168.x.x:8080/live`

Relevant script:

- `Assets/Scripts/AnatomyLiveClient.cs`

### Minimum Runtime Expectations

- Backend accepts context updates and question payloads over WebSocket.
- Backend can stream text and optional PCM audio chunks back to the Unity client.
- Client can reconnect automatically if enabled.

## Scenes You May Want To Check First

- `Assets/Scenes/inner_ear_test.unity`
- `Assets/Scenes/inner_ear.unity`
- `Assets/Scenes/middle_ear.unity`
- `Assets/Scenes/outer_ear_menu.unity`
- `Assets/Scenes/EarLessonF.unity`

## Notes

- This is an active development/prototype repository.
- Some experimental scripts (for earlier ChatGPT/NPC flows) are present but partially disabled/commented.
- The project currently targets Android/Quest configurations in Project Settings.

## Suggested Next Improvements

- Add architecture diagram (Unity client <-> AI backend).
- Add scene-by-scene feature matrix.
- Add screenshots/GIFs for GitHub preview.
- Add backend contract examples (JSON packets) for easier integration.

## License

No license file is currently included.
If you plan to share beyond private use, add an explicit license and asset usage notes.
