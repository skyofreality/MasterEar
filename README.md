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
