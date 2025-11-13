# Custom Physics Engine - File Index

Quick reference to all files in the implementation.

---

## 🎯 Start Here

### For Users
**→ [README.md](README.md)** - Quick start guide, parameters, usage  
**→ [QuickStart.cs](QuickStart.cs)** - One-click demo setup (attach to GameObject)

### For Developers
**→ [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md)** - Technical details, equations  
**→ [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md)** - Complete feature list  
**→ [VALIDATION.md](VALIDATION.md)** - Testing checklist

---

## 📦 Core Physics Files

### RigidBody.cs
**What:** Custom rigid body state and integration  
**Key Methods:**
- `Integrate(force, torque, dt)` - Newton-Euler integration
- `ApplyImpulse(impulse, worldPoint)` - Impulse application
- `LocalToWorld(localPoint)` - Transform local to world space

**Lines:** 136  
**Dependencies:** None (pure physics)

---

### Constraint.cs
**What:** XPBD constraint solver with fracture logic  
**Key Methods:**
- `Solve(dt)` - Iterative XPBD constraint projection
- `ComputeEffectiveMass(n, out rA, out rB)` - Includes rotation
- `ComputeFractureDirection(out force, out torque)` - Paper Eq. 6
- `ApplyFractureImpulse(energyTransferRatio)` - Paper Eq. 5, 8
- `DrawGizmo()` - Visualization

**Lines:** 283  
**Dependencies:** RigidBody  
**Paper Equations:** 1, 2, 5, 6, 8

---

### PhysicsWorld.cs
**What:** Main simulation manager (MonoBehaviour)  
**Key Methods:**
- `FixedUpdate()` - Main simulation loop
- `ApplyExternalForces(dt)` - Gravity
- `SolveConstraints(dt)` - Iterative solver
- `ProcessFractures()` - Break detection and impulse
- `IntegrateMotion(dt)` - Update all bodies
- `UpdateEnergyTracking()` - Energy monitoring

**Lines:** 203  
**Dependencies:** RigidBody, Constraint  
**Attach to:** GameObject in scene

---

### ProceduralMesh.cs
**What:** Manual mesh generation (static utility)  
**Key Methods:**
- `CreateCube(size)` - Generate cube mesh from scratch
- `CalculateBoxInertia(mass, size)` - Inertia tensor

**Lines:** 127  
**Dependencies:** None  
**Note:** No Unity primitives used

---

### RigidBodyRenderer.cs
**What:** Syncs GameObject transform with physics simulation  
**Key Methods:**
- `Update()` - Sync transform.position/rotation
- `OnDrawGizmos()` - Visualize COM, velocities

**Lines:** 60  
**Dependencies:** RigidBody  
**Attach to:** GameObject with MeshFilter/MeshRenderer

---

### FractureDemo.cs
**What:** Demo scene creator (MonoBehaviour)  
**Key Methods:**
- `CreateFractureDemo()` - Build pre-fractured beam
- `Update()` - Handle input (SPACE, R)
- `OnGUI()` - Show controls and stats

**Lines:** 118  
**Dependencies:** PhysicsWorld, RigidBody, Constraint  
**Attach to:** GameObject in scene  
**Controls:** SPACE = impulse, R = restart

---

### QuickStart.cs
**What:** One-click demo setup (MonoBehaviour)  
**Key Methods:**
- `CreateDemo()` - Auto-create FractureDemo
- `Start()` - Auto-run on Play

**Lines:** 48  
**Dependencies:** FractureDemo  
**Attach to:** Empty GameObject  
**Note:** Simplest way to test!

---

## 📄 Documentation Files

### README.md
**Content:**
- Overview of the implementation
- Core features list
- Quick start guide
- Parameter reference
- Usage examples
- Visualization guide

**Best for:** First-time users

---

### IMPLEMENTATION_GUIDE.md
**Content:**
- Step-by-step implementation details
- All paper equations with code
- XPBD solver explanation
- Fracture mechanics deep dive
- Parameter tuning guide
- Advanced usage examples
- Debugging tips
- Testing checklist

**Best for:** Developers, students, researchers

---

### PROJECT_SUMMARY.md
**Content:**
- Complete feature list
- Paper equations verification
- Comparison with Unity physics
- Performance notes
- Known limitations
- Future enhancements
- Quick reference commands

**Best for:** Project overview, presentations

---

### VALIDATION.md
**Content:**
- Requirements checklist (all ✅)
- Equation verification
- Functional testing results
- Code quality metrics
- Success criteria
- Final verification

**Best for:** Validation, quality assurance

---

### INDEX.md (this file)
**Content:**
- File navigation
- Quick reference
- Dependencies map

**Best for:** Finding the right file quickly

---

## 🗂️ File Dependencies

```
QuickStart.cs
    └── FractureDemo.cs
            ├── PhysicsWorld.cs
            │       ├── RigidBody.cs
            │       └── Constraint.cs
            │               └── RigidBody.cs
            ├── ProceduralMesh.cs
            └── RigidBodyRenderer.cs
                    └── RigidBody.cs
```

---

## 🎓 Learning Path

### Beginner
1. Read **README.md** - Understand what it does
2. Attach **QuickStart.cs** - See it work
3. Press Play and SPACE - Test fracture
4. Adjust parameters in Inspector - Experiment

### Intermediate
1. Read **IMPLEMENTATION_GUIDE.md** - Understand how it works
2. Study **RigidBody.cs** - Newton-Euler integration
3. Study **Constraint.cs** - XPBD solver
4. Study **PhysicsWorld.cs** - Simulation loop
5. Modify **FractureDemo.cs** - Create custom scenes

### Advanced
1. Read paper: Li et al. 2018
2. Verify equations in **IMPLEMENTATION_GUIDE.md**
3. Extend **Constraint.cs** - Add new constraint types
4. Optimize **PhysicsWorld.cs** - Add islands, broadphase
5. Add features: collision, sleeping, plasticity

---

## 🔍 Finding Specific Features

### "How do I..."

#### ...start the demo?
→ **QuickStart.cs** - Attach to GameObject, press Play

#### ...adjust stiffness?
→ **Constraint.compliance** - Lower = stiffer  
→ **PhysicsWorld.solverIterations** - More = stiffer

#### ...make it break easier?
→ **Constraint.breakThreshold** - Lower value

#### ...add custom objects?
→ **FractureDemo.cs** - Study CreateFractureDemo()  
→ Create RigidBody, add to PhysicsWorld

#### ...visualize forces?
→ **RigidBodyRenderer.OnDrawGizmos()** - Already implemented  
→ **Constraint.DrawGizmo()** - Already implemented

#### ...understand the math?
→ **IMPLEMENTATION_GUIDE.md** - All equations explained

#### ...verify it works?
→ **VALIDATION.md** - Complete testing checklist

---

## 📊 File Statistics

| File | Type | Lines | Purpose |
|------|------|-------|---------|
| RigidBody.cs | Physics | 136 | State & integration |
| Constraint.cs | Physics | 283 | Solver & fracture |
| PhysicsWorld.cs | Manager | 203 | Simulation loop |
| ProceduralMesh.cs | Utility | 127 | Mesh generation |
| RigidBodyRenderer.cs | Visual | 60 | Transform sync |
| FractureDemo.cs | Demo | 118 | Scene setup |
| QuickStart.cs | Setup | 48 | Quick start |
| **Total Code** | | **975** | |
| README.md | Docs | - | Quick start |
| IMPLEMENTATION_GUIDE.md | Docs | - | Technical |
| PROJECT_SUMMARY.md | Docs | - | Overview |
| VALIDATION.md | Docs | - | Testing |
| INDEX.md | Docs | - | Navigation |
| **Total Files** | | **12** | |

---

## 🎯 Common Tasks

### Task: Run Demo
1. Open Unity project
2. Create empty GameObject
3. Add Component → **QuickStart**
4. Press Play
5. Press SPACE to fracture

### Task: Adjust Parameters
1. Select **FractureDemo** in Hierarchy
2. Inspector → Adjust values:
   - `segmentCount` = number of pieces
   - `compliance` = softness
   - `breakThreshold` = fracture difficulty
3. Press Play to test

### Task: Create Custom Scene
```csharp
// In your script:
PhysicsWorld world = gameObject.AddComponent<PhysicsWorld>();

RigidBody body = new RigidBody(pos, rot, mass, inertia);
world.AddRigidBody(body);

Constraint c = new Constraint(bodyA, bodyB, anchorA, anchorB);
world.AddConstraint(c);
```

### Task: Debug Issues
1. Check **VALIDATION.md** - Known issues
2. Check Console - Error messages
3. Enable gizmos - See constraint stress
4. Check **PhysicsWorld** UI - Energy values
5. Reduce timestep - Edit → Project Settings → Time

---

## 🚀 Next Steps

After understanding the basics:

1. **Experiment** - Change parameters and observe
2. **Extend** - Add new constraint types
3. **Optimize** - Add constraint islands
4. **Visualize** - Add particle effects on fracture
5. **Learn** - Study the paper equations
6. **Share** - Show your cool fractures!

---

## 📞 Quick Reference

**Want to:** → **Look at:**
- Understand project → README.md
- Learn implementation → IMPLEMENTATION_GUIDE.md
- See all features → PROJECT_SUMMARY.md
- Verify correctness → VALIDATION.md
- Find a file → INDEX.md (this file)
- Start demo → QuickStart.cs
- Understand physics → RigidBody.cs, Constraint.cs
- Understand solver → PhysicsWorld.cs
- Create custom scene → FractureDemo.cs (example)

---

**Author:** AI Implementation  
**Date:** November 2025  
**Based on:** Li et al., "Energized Rigid Body Fracture", SCA 2018  
**Status:** ✅ Complete and validated

