# CreditsScene Setup Guide

## Overview
This guide walks you through creating the `CreditsScene.unity` manually in the Unity Editor.

## Prerequisites
- `CreditsTransitionController.cs` - already created
- `CreditsPageController.cs` - already created
- `CreditsMenuController.cs` - already created
- `credits.json` - in `Assets/Resources/credits.json`
- TextMesh Pro (built-in to Unity)

---

## Step-by-Step Scene Setup

### 1. Create New Scene
1. In Unity, go to **File > New Scene**
2. Save it as `CreditsScene.unity` in `Assets/Scenes/` folder
3. Choose **3D** template (or 2D if preferred)

---

### 2. Set Up Basic Scene Structure

#### Camera
1. Keep the default Main Camera in the scene
2. Position it looking at the canvas area
3. No need for XR rig in this scene (static presentation)

#### Canvas (UI Root)
1. Create a new `Canvas` under the scene
2. Set Canvas Scaler to **Scale with Screen Size** (1920x1080 reference)
3. Name it `UIRoot`

---

### 3. Add Fade-to-Black Panel

1. Create a new `Image` under `Canvas`
2. Name it `FadePanel`
3. Set RectTransform to **Stretch** (full screen)
4. Set Image color to **Black** (255, 255, 255) with **Alpha = 0** (start transparent)
5. Add **CanvasGroup** component to `FadePanel`
6. Set CanvasGroup Alpha to **0** initially
7. Create new Material or use default Image material

---

### 4. Add Credits Display Area

1. Create a new **Panel** under `Canvas`
2. Name it `CreditsPanel`
3. Set RectTransform:
   - Anchor: Middle Center
   - Offset: (0, 0)
   - Size: (1600, 1000) - adjust as needed
4. Set Image color to **Dark background** (e.g., RGB: 20, 20, 20) or transparent
5. Add **Mask** component (optional, for scrolling overflow)

#### Credits Text Display
1. Create a new **TextMeshPro - Text (UI)** under `CreditsPanel`
2. Name it `CreditsText`
3. Set RectTransform:
   - Anchor: Top Left
   - Offset: (20, -20) - left and top padding
   - Size: (1560, 1000)
4. Text component settings:
   - **Font**: Use default TMP font or import custom
   - **Font Size**: 28 (will be overridden by JSON tags)
   - **Alignment**: Top Left
   - **Overflow**: Overflow (allows content to expand)
   - **Rich Text**: **ON** (required for size/bold tags)
5. Leave text empty initially (will be populated by script)

---

### 5. Add Page Counter

1. Create a new **TextMeshPro - Text (UI)** under `Canvas`
2. Name it `PageCounterText`
3. Set RectTransform:
   - Anchor: Bottom Right
   - Offset: (-50, 50) - 50 pixels from bottom right corner
   - Size: (300, 60)
4. Text component settings:
   - Font Size: 24
   - Alignment: Bottom Right
   - Rich Text: ON
5. Set text placeholder to "Page 1 of 1"

---

### 6. Add Page Counter (Optional)

1. Create a new **TextMeshPro - Text (UI)** under `Canvas`
2. Name it `PageCounterText`
3. Set RectTransform:
   - Anchor: Bottom Right
   - Offset: (-50, 50) - 50 pixels from bottom right corner
   - Size: (300, 60)
4. Text component settings:
   - Font Size: 24
   - Alignment: Bottom Right
   - Rich Text: ON
5. Set text placeholder to "Page 1 of 1"

---

### 7. Set Up Scripts & References

#### Add CreditsPageController
1. Create empty GameObject in scene root, name it `CreditsController`
2. Add `CreditsPageController` script component
3. Assign inspector references:
   - **Credits Display Text**: Drag `CreditsText` (TextMeshPro component)
   - **Page Counter Text**: Drag `PageCounterText` (TextMeshPro component)
   - **Fade Canvas Group**: Drag `FadePanel` (select CanvasGroup component)
   - **Entries Per Page**: 45 (default)
   - **Seconds Per Page**: 8 (pages auto-advance every 8 seconds)
   - **Auto Fade Delay**: 2 (pause on final page before fading)
   - **Fade Duration**: 2 (fade-out duration)

---

### 8. Add CreditsTransitionController (in FinalGardenScene)

This script runs in `FinalGardenScene` and detects when Relax stage is complete.

1. Open `FinalGardenScene.unity`
2. Create empty GameObject, name it `CreditsTransitionController`
3. Add `CreditsTransitionController` script
4. Assign inspector references:
   - **Fade Canvas Group**: Create a new Canvas with black Image + CanvasGroup in FinalGardenScene (same as CreditsScene fade panel), or reuse existing fade system
   - **Fade Duration**: 3
5. Save scene

---

### 9. Add CreditsMenuController (Optional)

If you want extra menu buttons on the credits scene:

1. In `CreditsScene`, create empty GameObject, name it `MenuController`
2. Add `CreditsMenuController` script
3. Assign **Fade Canvas Group** to the FadePanel's CanvasGroup
4. Optionally add buttons like "Return to Title" that call `ReturnToTitle()`

---

### 10. Scene Settings

1. Add `CreditsScene` to **Build Settings**:
   - File > Build Settings
   - Drag `CreditsScene.unity` into Scenes In Build
   - Note the scene index number
   - Ensure `TitleScene` and `FinalGardenScene` are also in Build Settings

2. Verify scene names in scripts match exactly:
   - `CreditsPageController` loads "TitleScene"
   - `CreditsTransitionController` loads "CreditsScene"
   - `CreditsMenuController` can load "FinalGardenScene" or "TitleScene"

---

## Testing Checklist

- [ ] CreditsScene opens without errors
- [ ] Credits text displays on first page
- [ ] Page counter shows "Page 1 of X" (where X = actual page count)
- [ ] Page automatically advances after ~8 seconds
- [ ] Each new page displays correctly
- [ ] Final page displays without advancing
- [ ] After ~2 seconds on final page, screen fades to black
- [ ] TitleScene loads after fade completes
- [ ] Return to FinalGardenScene, reach Relax stage
- [ ] Pick up wine glass at Relax stage
- [ ] Fade-to-black triggers
- [ ] CreditsScene loads automatically
- [ ] Full credits viewing works end-to-end (auto-advance and auto-return)

---

## Troubleshooting

### Credits.json Not Loading
- **Issue**: "Could not load credits.json from Resources/credits.json"
- **Solution**: 
  - Verify `credits.json` is in `Assets/Resources/credits.json`
  - No subfolders—must be directly in Resources!
  - Restart Unity if file was just added

### Page Text Not Displaying
- **Issue**: CreditsText is blank
- **Solution**:
  - Verify CreditsPageController is assigned to CreditsText in inspector
  - Check that TextMeshPro is properly imported (usually auto-imports)
  - Enable Rich Text on TextMeshPro component

### Scene Won't Load
- **Issue**: "Scene 'CreditsScene' not found"
- **Solution**:
  - Add scene to Build Settings (File > Build Settings)
  - Verify scene name matches exactly in code

### Fade Not Working
- **Issue**: Screen doesn't fade to black
- **Solution**:
  - Verify FadePanel's CanvasGroup is assigned
  - Check FadePanel is full-screen (RectTransform set to Stretch)
  - Verify FadePanel Image color is Black

---

## Additional Notes

- **Font Size in Credits**: The JSON formatting uses TextMesh Pro size tags:
  - `<size=36>` for section titles
  - `<size=18><b>` for asset names (bold)
  - `<size=14>` for artist/source info
  - Adjust these in `CreditsPageController.cs` if needed

- **Page Count**: With 45 entries per page, ~6-7 pages estimated
  - Adjust `Entries Per Page` in inspector to fit your design

- **Performance**: JSON parsing happens on Start, so first frame may have slight delay
  - Minimal impact on most systems

---

Done! Your CreditsScene is ready to use.
