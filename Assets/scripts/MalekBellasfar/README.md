# Custom Physics Engine - Energized Rigid Body Fracture

Implementation of the paper "Energized Rigid Body Fracture" by Li et al., 2018 in Unity C#.

## Overview

This is a complete custom physics engine that **does not use Unity's built-in physics** (no Rigidbody, Collider, or PhysicsEngine components). All forces, motion integration, constraints, and fracture mechanics are computed manually.

## Core Components

### 1. RigidBody.cs
Custom rigid body implementation with:
- Position & quaternion orientation
- Linear & angular velocity
- Mass & inertia tensor (diagonal)
- Newton-Euler integration:
  - `v += (F/m) * dt`
  - `ω += I⁻¹ * τ * dt`
  - `position += v * dt`
  - `rotation = Δq * rotation`

### 2. Constraint.cs
Jacobian-based distance constraint with:
- **XPBD formulation** with compliance Σ and error reduction Γ
- **Spring-damper model**: K = Γ/(h*Σ), B = (1-Γ)/Γ
- **Effective mass** including rotational terms: `m_eff = 1/(1/mA + 1/mB + angular_terms)`
- **Energy tracking**: E = 0.5 * φᵀ * K * φ
- **Fracture logic**: Breaks when |λ| > ε
- **Energy-based impulse**: Converts stored elastic energy to kinetic energy

### 3. PhysicsWorld.cs
Main simulation manager:
- Applies external forces (gravity)
- Iterative constraint solver
- Fracture processing with energy transfer
- Motion integration for all bodies
- Energy tracking (kinetic + potential + stored)

### 4. ProceduralMesh.cs
Generates meshes without Unity primitives:
- Manual vertex/triangle construction
- Inertia tensor calculation

### 5. FractureDemo.cs
Demo scene creating a pre-fractured beam that breaks under stress.

## Paper Implementation Details

### Constraint Solving (Eq. 1-2)
```
[ M  -Jᵀ ] [Δv] = [h*f_ext  ]
[ J   Σ  ] [λ ] = [-Γφ/h    ]
```

Solved iteratively using XPBD:
```csharp
Δλ = -(φ + α*λ_prev + (γ/h)*φ) / (1/m_eff + α + γ/h)
```
where α = Σ/h²

### Fracture Impulse Direction (Eq. 6)
```
l̂ = -(τ × f) / ||τ × f||
```
Uses accumulated torque and force from constraint.

### Impulse Magnitude (Eq. 8)
```
µ = sqrt(2 * α * m_G * E)
```
where:
- α = energy transfer ratio (0-1, can be >1)
- m_G = generalized inverse mass (includes rotation)
- E = stored elastic potential energy

### Energy Conversion (Eq. 5)
```
0.5 * Δvᵀ * M * Δv = α * E
```
Converts constraint potential energy to kinetic energy on fracture.

## Usage

### Quick Start
1. Create empty GameObject in scene
2. Add `FractureDemo.cs` component
3. Press Play
4. Press SPACE to apply impulse to center segment
5. Watch beam fracture under stress!

### Manual Setup
```csharp
// Create physics world
PhysicsWorld world = gameObject.AddComponent<PhysicsWorld>();
world.solverIterations = 10;
world.energyTransferAlpha = 1f;

// Create rigid body
Vector3 inertia = ProceduralMesh.CalculateBoxInertia(mass, size);
RigidBody body = new RigidBody(position, rotation, mass, inertia);
world.AddRigidBody(body);

// Create constraint
Constraint c = new Constraint(bodyA, bodyB, anchorA, anchorB);
c.compliance = 0.0001f;  // Σ
c.gamma = 0.8f;          // Γ
c.breakThreshold = 5f;   // ε
world.AddConstraint(c);

// Create visual (optional)
GameObject obj = new GameObject("Body");
obj.AddComponent<MeshFilter>().mesh = ProceduralMesh.CreateCube(size);
RigidBodyRenderer renderer = obj.AddComponent<RigidBodyRenderer>();
renderer.physicsBody = body;
```

## Parameters Tuning

### Constraint Stiffness
- **compliance (Σ)**: 0 = rigid, higher = softer
  - Recommended: 0.0001 - 0.01
- **gamma (Γ)**: Error reduction, 0-1
  - Recommended: 0.6 - 0.9

### Fracture Threshold
- **breakThreshold (ε)**: Higher = harder to break
  - Recommended: 3-10 for dramatic fracture
  - Higher values for realistic behavior

### Energy Transfer
- **energyTransferAlpha (α)**: 0-1 (can be >1 for artistic effect)
  - 1.0 = full energy transfer
  - >1 = amplified fracture (explosive)

### Solver Iterations
- More iterations = stiffer constraints
- Recommended: 8-15 iterations

## Visualization (Gizmos)

- **Green lines**: Low-stress constraints
- **Yellow lines**: Medium-stress constraints
- **Red lines**: High-stress constraints (near fracture)
- **Cyan rays**: Fracture impulse directions
- **Red spheres**: Centers of mass
- **Blue rays**: Linear velocity
- **Magenta rays**: Angular velocity

## Features Implemented

✅ Custom RigidBody with quaternion orientation  
✅ Newton-Euler integration (no Unity physics)  
✅ Jacobian-based constraint solver  
✅ XPBD formulation with compliance Σ and gamma Γ  
✅ Spring-damper model with energy tracking  
✅ Rotational effective mass in constraints  
✅ Lambda-based fracture threshold  
✅ Energy-to-kinetic conversion on fracture  
✅ Fracture direction from torque × force  
✅ Procedural mesh generation (no Unity primitives)  
✅ Pre-scored mesh handling (fragments + constraints)  
✅ Complete visualization with Gizmos  
✅ Energy tracking and debugging UI  

## Architecture

```
PhysicsWorld (MonoBehaviour)
├── RigidBody[] (pure C# class)
│   ├── position, rotation (state)
│   ├── velocity, angularVelocity
│   ├── mass, inertiaTensor
│   └── Integrate(), ApplyImpulse()
│
└── Constraint[] (pure C# class)
    ├── bodyA, bodyB
    ├── compliance, gamma, breakThreshold
    ├── lambda (accumulated impulse)
    ├── storedEnergy
    └── Solve(), ApplyFractureImpulse()

RigidBodyRenderer (MonoBehaviour)
└── Syncs GameObject transform with RigidBody.position/rotation
```

## Performance Notes

- Current implementation: O(n) for constraints per iteration
- No spatial partitioning (suitable for ~100 bodies)
- For larger scenes, implement:
  - Constraint islands
  - Broad-phase collision detection
  - Sleeping/waking system

## References

Li, Yudong, et al. "Energized Rigid Body Fracture." Symposium on Computer Animation, 2018.

Key equations implemented:
- Eq. 1-2: Constraint formulation
- Eq. 5: Energy conversion
- Eq. 6: Fracture direction
- Eq. 8: Impulse magnitude

## License

This is an educational implementation for research and learning purposes.

