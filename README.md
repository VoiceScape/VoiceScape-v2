# VOXR - Meditative Singing Design Spec

## Overview
VOXR is designed to motivate people to sing in a meditative way, yielding therarpuetic benefits, such as a relaxed and euphoric state by slowing respiration, resonating the body and stimulating the vagus nerve.

## Core Interaction

Two inputs control the character movement.

1. Audio of the user's voice  
    a. Sensor: device microphone  
    b. Controls: amplitude and frequency
    c. Mappings:  
    1. Amplitude drives elevation on the y axis with an envelope follower, feature a fairly rapid attack and very slow logarithmic release </i>
    2. Frequency modulates the y axis elevation within the "pitch fly zone", with a more responsive envelope.
2. Tilt of the headset 
    a. Sensor: gyroscope  
    b. Controls: azimuth plane  
    c. Mappings: forward, backward, left, right moves character slowly in that direction, 360 degrees on the azimuth plane </i>

## Narrative Overview
1. Vocal Calibration / Onboarding - guide and measure player's voice
2. 3rd POV MR - more familiarization and practice, big transition
3. 1st POV MR - fly with your voice to follow the guided melody

## Scene 1: 3rd Person [MR] Vocal Calibration / Onboarding

Colliders fly at you, each emitting a target sound and displaying at the corresponding pitch/frequency elevation.  Your goal is to match the golden snitch to collide with it. 

We measure and capture to adjust gameplay to the vocal range for later stages.

### Your voice guided "golden snitch" 

Objectives
- Gently introduce the interaction and world
- Fun interaction for exploring your vocal range ability
- Capture ability to hit notes, to establish and calibrate vocal range for following levels

The experience guides players through an exercise to initialize familarization with voice control and head tilt, by guiding a "golden snitch"-like object through space to collide with objects in a musical way.

The voice and head tilt controls the currentPitch sphere as the main player.  In this 3rd person POV, we guide a player object freely through the world, within the MR room's bounds on the X and Z axis.  The height is limited in Y axis by the Height Calculation parameters driven by the amplitude and pitch of the voice.  X and Z axis and driven by head tilt.

### Pickup Feedback
A ding will sound if you can hit the pitch at the right timing. 

### Scene Hierarchy and Components
```
UnityAudioProto2
├── AudioManager
│   └── MPMAudioAnalyzer (Pitch detection and voice processing)
├── [BuildingBlock] Camera Rig
├──PitchVisualizer3POV
│   ├── CurrentPitchSphere
│   ├── TargetPitchSphere
│   ├── Labels
│   ├── PlayerControl
├── TerrainManager
├── PickupManager3POV
├── Range Report Canvas
├── Event System

```

## Scene 2: 3rd Person [MR] Player Interaction

The player follows a spline for more complex motion.

Details TBD  @Berkay

## Scene 3: 1st Person [VR] Player Interaction

Objectives
- Apex and payoff
- Give sensation of flying with voice and head tilt steering
- Stimulate and motivate singing

## Scene Hierarchy and Components

### Voice-Height Mapping

Players control their height through voice pitch:
1. Voice frequency is analyzed in real-time using the McLeod Pitch Method
2. Detected frequency is mapped to a target height using logarithmic scaling
3. Player's vertical position smoothly transitions to match their voice pitch
4. Visual feedback shows current and target heights
5. Confidence metrics ensure stable pitch detection


### Musical Pickup Collection

Musical pickups are placed at heights corresponding to their frequencies:
1. Each pickup represents a note in the sequence
2. Pickups are positioned ahead of the player in a path
3. Players must match the pickup's pitch to reach its height
4. Visual and audio feedback indicate proximity to correct pitch
5. Successful collection requires maintaining pitch for a brief duration

## Visual Feedback

### Pickup Visualization
- Pickup color indicates sequence position
- Opacity changes based on current/target status
- Size pulses subtly to draw attention
- Forward path shows upcoming sequence
- Height lines show pitch targets

### Player Feedback
- Current pitch shown as floating sphere
- Color gradient indicates pitch accuracy
- Particle effects for successful matches
- Distance markers show height targets
- Frequency and confidence displays

### Scene Hierarchy
```
UnityAudioProto2
├── AudioManager
│   └── MPMAudioAnalyzer (Pitch detection and voice processing)
├── [BuildingBlock] Camera Rig
│   ├── Player (Movement controller)
│   └── TrackingSpace
│       ├── CenterEyeAnchor
│       │   └── PitchVisualizer
│       ├── LeftEyeAnchor
│       └── RightEyeAnchor
├── TerrainManager (Procedural terrain generation)
├── LandmarkManager (Dynamic reference objects)
├── ReferenceGrid
├──PickupManager (Musical progression system) [In progress]
│   ├── PickupManager 
```

## Implementation Details


### Musical Pickup Collection

Musical pickups are placed at heights corresponding to their frequencies:
1. Each pickup represents a note in the sequence
2. Players must match the pickup's pitch to reach its height
3. Visual and audio feedback indicate proximity to correct pitch
4. Successful collection requires maintaining pitch for a brief duration

### Key Components

1. **MPMAudioAnalyzer**
   - Real-time pitch detection
   - Frequency confidence calculation
   - Amplitude envelope following

2. **PlayerMovementController**
   - Voice-to-height mapping
   - Smooth height transitions
   - Ground tracking
   - Collision detection

3. **PickupManager**
   - Musical sequence management
   - Pickup spawning and positioning
   - Collection detection
   - Progress tracking

4. **Height Calculation**

```
Height = baseHeight + (log(frequency) - log(minFreq)) / (log(maxFreq) - log(minFreq)) * (maxHeight - baseHeight)

Where:
- baseHeight = 4 meters (height at 100 Hz)
- maxHeight = 20 meters (height at maximum frequency)
- minFreq = 80 Hz (lowest supported note)
- maxFreq = 300 Hz (highest supported note)
```

### Core Systems

1. **Voice Analysis (MPMAudioAnalyzer)**
   - Real-time pitch detection using McLeod Pitch Method
   - Amplitude envelope following
   - Frequency and clarity analysis

2. **Movement System (PlayerMovementController)**
   - Voice-driven elevation control
   - Pitch-based height modulation
   - Head-tilt based directional control
   - ADSR envelope for smooth transitions
   - PlayerMovementController3rdPOV.cs is for the 3rdPOV scene
   - PlayerMovementController.cs is for the 1stPOV scene

3. **Terrain Generation (TerrainManager)**
   - Procedural mesh generation (perlin noise displacement)
   - Dynamic chunk loading
   - Collision detection
   - Performance-optimized updates

4. **Landmark System (LandmarkManager)**
   - Dynamic spawn system
   - Distance-based visual feedback
   - Cardinal direction markers
   - Spatial reference points

5. **Musical Progression (PickupManager)**
   - C minor meditation sequence
   - Visual waypoint system
   - Audio feedback (each pickup emits the target note)
   - Progress tracking (unclear how this is to be implemented)

## Setup Requirements

### Hardware
- Meta Quest or similar VR headset
- Microphone input
- Stereo audio output

### Software Dependencies
- Unity 2022.3 or later
- Meta XR SDK
- Universal Render Pipeline (URP)
- TextMeshPro

## Audio System Architecture

```mermaid
graph TD
    subgraph Audio Analysis
        MA[Microphone Audio] --> MPM[MPM Analyzer]
        MPM --> FA[Frequency Analysis]
        MPM --> AA[Amplitude Analysis]
        FA --> PC[Pitch Confidence]
        FA --> FD[Frequency Detection]
        AA --> EF[Envelope Follower]
    end

    subgraph Visual Feedback
        FD --> PS[Pitch Spheres]
        PC --> VC[Visual Confidence]
        PS --> CM[Color Mapping]
        PS --> PA[Pitch Accuracy]
        PA --> PFX[Particle Effects]
    end

    subgraph Movement System
        EF --> VM[Vertical Movement]
        FD --> PM[Pitch Modulation]
        VM --> PH[Physics Handler]
        PM --> PH
    end

    subgraph Gameplay
        PH --> PC[Player Controller]
        PC --> PP[Pickup Processing]
        PP --> MS[Musical Sequence]
        MS --> GM[Game Manager]
    end
```


## Core Systems Detail

### MPM Audio Analysis System

```mermaid
flowchart TB
    subgraph Audio Pipeline
        direction TB
        A[Audio Input] --> B[Buffer]
        B --> C[NSDF Calculation]
        C --> D[Peak Detection]
        D --> E[Frequency Estimation]
        E --> F[Confidence Check]
    end

    subgraph Debug Visualization
        direction TB
        G[Buffer View] --> H[NSDF Plot]
        H --> I[Peak Markers]
        I --> J[Confidence Display]
    end

    F --> G
```

### Visual Feedback System

```mermaid
stateDiagram-v2
    [*] --> OutOfRange
    OutOfRange --> NearTarget: Within 50 cents
    NearTarget --> CloseToTarget: Within 25 cents
    CloseToTarget --> OnTarget: Within 5 cents
    
    OnTarget --> CloseToTarget: Drift > 5 cents
    CloseToTarget --> NearTarget: Drift > 25 cents
    NearTarget --> OutOfRange: Drift > 50 cents

    state OutOfRange {
        [*] --> Blue
    }
    state NearTarget {
        [*] --> Orange
    }
    state CloseToTarget {
        [*] --> Yellow
    }
    state OnTarget {
        [*] --> Green
    }
```

## C Minor Meditation Sequence

```mermaid
graph LR
    Start --> C3
    C3 --> Eb3
    Eb3 --> G3
    G3 --> C4
    C4 --> G3_2[G3]
    G3_2 --> Eb3_2[Eb3]
    Eb3_2 --> C3_End[C3]
    C3_End --> End
```

## Implementation Priorities

1. **Phase 1: Audio Analysis Foundation**
   - MPM implementation debugging
   - Real-time visualization tools
   - Frequency detection validation
   - Amplitude envelope system

2. **Phase 2: Visual Feedback System**
   - Pitch sphere behavior
   - Color transition system
   - Accuracy visualization
   - Musical note display

3. **Phase 3: Movement System**
   - Amplitude-based hovering
   - Pitch-based modulation
   - Physics integration
   - Movement bounds

4. **Phase 4: Gameplay Elements**
   - Pickup system
   - Musical sequence implementation
   - Progress tracking
   - Success feedback

## Testing Strategy

```mermaid
graph TD
    subgraph Unit Tests
        UT1[MPM Algorithm] --> UT2[Frequency Detection]
        UT2 --> UT3[Amplitude Analysis]
    end

    subgraph Integration Tests
        IT1[Audio-Visual Sync] --> IT2[Movement Response]
        IT2 --> IT3[Pickup Interaction]
    end

    subgraph User Testing
        US1[Pitch Feedback] --> US2[Movement Feel]
        US2 --> US3[Musical Flow]
    end

    UT3 --> IT1
    IT3 --> US1
```

## Debug Visualization Tools

- NSDF Buffer View
- Frequency Spectrum Display
- Pitch Confidence Meter
- Amplitude Envelope Monitor
- Physics Debug View

## Visual Feedback Elements

1. **Pitch Accuracy Indicators**
   - Expanding/contracting aura
   - Particle system intensity
   - Color transitions
   - Distance lines

2. **Musical Information**
   - Current note name
   - Target note
   - Cents deviation
   - Octave indicator

3. **Performance Feedback**
   - Pitch history trail
   - Success particles
   - Collection effects
   - Level progress

## Implementation Phases

```mermaid
gantt
    title Development Timeline
    dateFormat  YYYY-MM-DD
    section Audio Analysis
    MPM Debug & Fix            :a1, 2025-01-05, 2d
    Frequency Detection        :a2, 2025-01-05, 2d
    Amplitude Analysis         :a3, 2025-01-05, 2d

    section Visual Feedback
    Basic Pitch Display        :v4, 2025-01-05, 2d
    Color System              :v5, 2025-01-05, 2d
    Accuracy Visualization    :v6, 2025-01-05, 2d

    section Movement
    Envelope Follower         :m1, after a3, 1d
    Physics Integration       :m2, after m1, 1d
    Movement Controls         :m3, after m1, 1d

    section Gameplay
    Pickup System             :g1, after m1, 1d
    Musical Sequence          :g2, after g1, 1d

    section Final Detail
    Polish & Testing          :g3, after g2, 3d
    Visuals                   :g4, after g2, 3d
    Scene Sequencing          :g5, after g2, 2d
    Calibration               :g6, after g5, 1d

    section Submission
    Video                     :g7, after g6, 1d
    DevPost                   :g8, after g6, 1d
```
