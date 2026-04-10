# Window Box Planting Interaction - Setup Guide

## Blender Model Hierarchies

### 1. Plant Prefab (Create 3 of these: Plant1.blend, Plant2.blend, Plant3.blend)

```
PlantRoot (Empty)
├── PotMesh (Mesh - the ceramic/plastic pot)
└── PlantAndSoil (Empty - THIS IS WHAT GETS PICKED UP)
    ├── PlantMesh (Mesh - leaves, stems, flowers)
    ├── SoilMesh (Mesh - dirt inside the pot)
    └── GripPoint (Empty - position this where the hand should grab)
```

**Key Points:**
- `PlantAndSoil` should be positioned so it sits inside the `PotMesh` initially
- `GripPoint` should be positioned slightly above the soil, centered (where fingers would naturally grab)
- Export each plant as a separate .blend file

### 2. Window Box Prefab (WindowBox.blend)

```
WindowBoxRoot (Empty)
└── WindowBoxMesh (Mesh - the rectangular planter box)
```

**Key Points:**
- Keep it simple - just the box mesh
- The plants will be parented to WindowBoxRoot at runtime
- Model it to fit nicely on a window sill

### 3. Compost Bag Prefab (CompostBag.blend)

```
CompostBagRoot (Empty)
└── CompostBagMesh (Mesh - bag of compost)
```

**Key Points:**
- Just a simple bag mesh
- Should be recognizable as a bag of soil/compost

## Unity Scene Setup

### Step 1: Create Empty GameObjects for Positioning

Create these empty GameObjects in your scene:

```
PlantingInteraction (Empty - attach WindowBoxPlantingController here)
├── Positions (Empty - organizer)
│   ├── WindowBoxFloorPosition (Empty - where box starts)
│   ├── WindowBoxSillPosition (Empty - where box ends up)
│   ├── Pot1DropPosition (Empty - where pot1 tips over, rotate ~90° on side)
│   ├── Pot2DropPosition (Empty - where pot2 tips over, rotate ~90° on side)
│   └── Pot3DropPosition (Empty - where pot3 tips over, rotate ~90° on side)
└── CompletionAudio (AudioSource - add your success sound clip here)
```

### Step 2: Place Your .blend Prefabs

Drag your .blend files into the scene:

1. **Plant1.blend** - place on floor/table in starting position
2. **Plant2.blend** - place next to Plant1
3. **Plant3.blend** - place next to Plant2
4. **WindowBox.blend** - initially position at exactly the same spot as `WindowBoxFloorPosition`
5. **CompostBag.blend** - place on floor near the plants

### Step 3: Add XRSimpleInteractable Components

#### On Each Plant Root:
1. Select `Plant1Root` (the root empty of Plant1.blend)
2. Add Component → XR → XR Simple Interactable
3. In the "Select Entered (SelectEnterEventArgs)" event:
   - Click `+` to add event
   - Drag the `PlantingInteraction` GameObject to the object field
   - Select `WindowBoxPlantingController` → `OnPlant1Selected`

4. Repeat for `Plant2Root` → `OnPlant2Selected`
5. Repeat for `Plant3Root` → `OnPlant3Selected`

#### On Compost Bag:
1. Select `CompostBagRoot`
2. Add Component → XR → XR Simple Interactable
3. In "Select Entered (SelectEnterEventArgs)":
   - Drag `PlantingInteraction`
   - Select `WindowBoxPlantingController` → `OnCompostBagSelected`

#### On Window Box:
1. Select `WindowBoxRoot`
2. Add Component → XR → XR Simple Interactable
3. In "Hover Enter (HoverEnterEventArgs)":
   - Drag `PlantingInteraction`
   - Select `WindowBoxPlantingController` → `OnWindowBoxHoverEnter`
4. In "Hover Exit (HoverExitEventArgs)":
   - Drag `PlantingInteraction`
   - Select `WindowBoxPlantingController` → `OnWindowBoxHoverExit`

### Step 4: Configure WindowBoxPlantingController

Select the `PlantingInteraction` GameObject and fill in the Inspector:

#### Plant 1:
- **Plant 1 Root**: Drag `Plant1Root` (from Plant1.blend)
- **Plant 1 Pot Mesh**: Drag `PotMesh` (child of Plant1Root)
- **Plant 1 Plant And Soil**: Drag `PlantAndSoil` (child of Plant1Root)
- **Plant 1 Grip Point**: Drag `GripPoint` (child of PlantAndSoil)

#### Plant 2:
- (Same pattern as Plant 1, but with Plant2 objects)

#### Plant 3:
- (Same pattern as Plant 1, but with Plant3 objects)

#### Window Box:
- **Window Box Transform**: Drag `WindowBoxRoot`
- **Window Box Floor Position**: Drag the `WindowBoxFloorPosition` empty
- **Window Box Sill Position**: Drag the `WindowBoxSillPosition` empty
- **Move Duration**: 1.5 (seconds)

#### Empty Pots Final Positions:
- **Pot 1 Drop Position**: Drag `Pot1DropPosition`
- **Pot 2 Drop Position**: Drag `Pot2DropPosition`
- **Pot 3 Drop Position**: Drag `Pot3DropPosition`
- **Pot Drop Duration**: 0.8 (seconds)

#### Compost Bag:
- **Compost Bag Root**: Drag `CompostBagRoot`

#### VR Hand:
- **Hand Ray Interactor**: Drag `XRRayInteractor` component from your right hand controller
- **Hand Line Visual**: Drag `XRInteractorLineVisual` component from your right hand controller
- **Trigger Action**: 
  - Click the dropdown
  - Select `XRI RightHand Interaction/Select` (or your trigger input action)

#### Plant Alignment:
- **Plant Rotation Offset**: 0 (adjust if plants are rotated wrong in hand)

#### Completion:
- **Completion Audio**: Drag the `CompletionAudio` AudioSource

### Step 5: Position Your Empty Transforms

#### WindowBoxFloorPosition:
- Position: On the floor where you want the window box to start
- Rotation: Facing the player

#### WindowBoxSillPosition:
- Position: On the window sill where the box should end up
- Rotation: Same as floor position (or facing outward if you prefer)

#### Pot Drop Positions (1, 2, 3):
- Position: On the floor near where each pot starts
- Rotation: Tip them on their side (rotate ~90° on X or Z axis to look dropped)

## Testing Checklist

- [ ] Can ray select Plant 1 - plant snaps to hand, pot stays behind, ray disappears
- [ ] Can hover over window box and trigger to place Plant 1 - plant goes in box, ray returns
- [ ] Repeat works for Plant 2
- [ ] Repeat works for Plant 3
- [ ] Can ray select compost bag - completion sequence starts
- [ ] Window box smoothly moves to sill
- [ ] Three empty pots tip over on the floor
- [ ] Completion sound plays
- [ ] Ray interactor returns to normal

## Troubleshooting

### Plant is rotated wrong in hand:
- Adjust `Plant Rotation Offset` in the controller (try 90, 180, 270, etc.)
- Or adjust the rotation in your Blender model

### Hand isn't gripping the plant correctly:
- Adjust the position of `GripPoint` in your Blender model
- It should be where you'd naturally hold the plant

### Window box doesn't move smoothly:
- Increase `Move Duration` for slower movement
- Check that `WindowBoxSillPosition` is positioned correctly

### Pots don't tip over nicely:
- Adjust the rotation of `Pot1DropPosition`, `Pot2DropPosition`, `Pot3DropPosition`
- Try different angles (90° on X-axis, or Z-axis, or a combination)

### Plants are positioned awkwardly in the window box:
- Open `WindowBoxPlantingController.cs`
- Find the `PlacePlantInWindowBox()` method
- Adjust the `localPlantPosition` values (line ~269-280):
  ```csharp
  case PlantingState.Plant1InHand:
      localPlantPosition = new Vector3(-0.2f, 0.1f, 0f); // Adjust these numbers
      break;
  ```
