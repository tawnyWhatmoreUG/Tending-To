# Slug Pellet Interaction Setup Guide

This guide will help you set up the slug pellet interaction for your VR project.

## Overview
The system consists of three scripts:
- **SlugPelletController.cs** - Manages bottle pickup and spray mechanics
- **SlugPelletBedController.cs** - Tracks progress as pellets scatter on the garden bed
- **BlueCapsule.cs** - Physics behavior for individual pellets

## Step-by-Step Setup

### Step 1: Create the Blue Capsule Prefab
1. In your scene, create a new empty GameObject called "BlueCapsule"
2. Add a **Capsule** mesh (3D Object > Capsule)
   - Scale it to something small like (0.1, 0.15, 0.1)
3. Add a **Material** with a blue color to it
4. Add a **Rigidbody** component:
   - Mass: 0.1
   - Drag: 0.5
   - Angular Drag: 0.5
   - Constraints: None (allow free movement)
5. Add a **Collider** (Capsule Collider should be there automatically)
6. Add the **BlueCapsule.cs** script to it
7. **Drag this GameObject into your Assets/Scripts folder** to create a prefab
8. Delete it from the scene (you only need the prefab file)

### Step 2: Configure the Particle System
1. On your **SlugPellets** prefab (or the instance in your scene):
   - Create a new **ParticleSystem** as a child of the root SlugPellets object
   - Position it at the sprayanchor location or slightly below it
   
2. Configure the Particle System settings:
   - **Emission**: Set to "Burst" or continuous emission with ~50 particles/second
   - **Shape**: Set to "Sphere" with small radius (0.1)
   - **Velocity**: 
     - Linear: 5-10 m/s (adjust for spray speed)
     - Random: 1-2 m/s (for scatter effect)
   - **Lifetime**: 3-5 seconds
   - **Size**: 0.1-0.2
   - **Renderer**: 
     - Material: Create a blue material (or use your blue color)
     - Render Mode: "Mesh" if you want to use the capsule geometry, or "Billboard" for simple billboards

3. **Alternative - Use the Instantiate System**:
   - Instead of visual particles only, you can use a script that spawns BlueCapsule prefabs:
   - Add this component to the ParticleSystem or create a new script called **PelletSpawner.cs**:

```csharp
using UnityEngine;

public class PelletSpawner : MonoBehaviour
{
    public GameObject blueCapsulePrefab;
    public ParticleSystem pelletParticles;
    public float spawnInterval = 0.1f; // Spawn a capsule every 0.1 seconds
    private float lastSpawnTime;

    void Update()
    {
        if (pelletParticles.isEmitting && Time.time - lastSpawnTime > spawnInterval)
        {
            SpawnCapsuleAtSprayPoint();
            lastSpawnTime = Time.time;
        }
    }

    void SpawnCapsuleAtSprayPoint()
    {
        Vector3 spawnPos = pelletParticles.transform.position;
        Quaternion spawnRot = Random.rotation;
        Vector3 spawnVelocity = Random.insideUnitSphere * 5f; // Random spray direction
        
        GameObject capsule = Instantiate(blueCapsulePrefab, spawnPos, spawnRot);
        Rigidbody rb = capsule.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = spawnVelocity;
        }
    }
}
```

### Step 3: Set Up the SlugPellets Prefab
1. On the root **SlugPellets** object, add the **SlugPelletController.cs** script
2. In the Inspector, assign:
   - **Cap Model**: Drag the cap GameObject
   - **Spray Anchor**: Drag the sprayanchor empty
   - **Pellet Particles**: Drag the ParticleSystem you created
   - **Spray Audio Source**: Create/assign an AudioSource for spray sound (optional)
   - **Hand Ray Interactor**: Drag your right hand's XRRayInteractor
   - **Hand Line Visual**: Drag your right hand's XRInteractorLineVisual
   - **Trigger Action**: Assign the XR trigger input action
   - **Grip Point**: You may need to create an empty where you want the bottle held in the hand

### Step 4: Configure XR Interactable
1. On the **SlugPellets** root object, add an **XR Simple Interactable** component
2. Configure it:
   - **Interactable Events** → **Select Entered**: Click the "+" and assign:
     - Object: Select the SlugPellets GameObject
     - Function: SlugPelletController.PickupBottle(SelectEnterEventArgs)
   - **Interactable Events** → **Select Exited**: Click the "+" and assign:
     - Object: Select the SlugPellets GameObject
     - Function: SlugPelletController.ResetTool()

### Step 5: Set Up the Progress Bar
1. Create/locate a Canvas in world space that shows progress above your garden bed
2. Add a **Slider** component to it
3. On your **garden bed** object (or near it), add the **SlugPelletBedController.cs** script
4. In the Inspector, assign:
   - **Progress Bar**: Drag the Slider from your canvas
   - **Slug Pellet Controller**: Drag the SlugPellets object
   - **Completion Audio**: Assign an AudioSource with a completion sound (optional)
   - **Spray Speed**: Adjust how fast the progress bar fills (0.15 is a good default)

### Step 6: Input Action Setup
1. In your InputActionAsset (or InputSystem_Actions.inputactions):
   - Make sure you have a trigger action set up (e.g., "Trigger" or "Select")
2. On the **SlugPelletController** script, in the **Trigger Action** field:
   - Select your XR trigger input action from your InputActionAsset

## Audio Setup
- **Spray Sound**: Add an AudioSource to the SlugPellets object with a looping spray sound
- **Completion Sound**: Add an AudioSource to the garden bed area or anywhere, and assign it to the SlugPelletBedController

## Testing Checklist
- [ ] Ray interactor disables when bottle is picked up
- [ ] Cap disappears when bottle is picked up
- [ ] Bottle snaps to hand and orients correctly
- [ ] Particles emit when trigger is held
- [ ] Spray sound plays/stops with trigger
- [ ] Progress bar appears when spraying over garden bed
- [ ] Blue capsules appear in world space (either as visual particles or spawned prefabs)
- [ ] Progress completes and triggers sound
- [ ] Bottle returns to original position and cap reappears on completion

## Troubleshooting
- **Bottle not snapping to hand correctly**: Adjust the `bottleRotationOffset` or `gripPoint` position
- **Particles not emitting**: Ensure the ParticleSystem is set to play on awake, or that it's being enabled via ToggleSpray()
- **Progress not advancing**: Check that the garden bed object is in the right position and the distance threshold matches your scene scale
- **Capsules not staying in world space**: Make sure they have physics (Rigidbody + Collider)
