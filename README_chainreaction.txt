Chain Reaction Physics Simulation
Author: Jebri Oussama
Branch: chainreaction

Overview
--------
This project implements a complete chain physics simulation without using Unity’s built-in physics engine.
All physical behavior—movement, constraints, damping, and fracture—is computed manually using:

- Mécanique du point matériel (Chapitre 2)
- Contraintes géométriques (Chapitre 3)
- Energy-based fracture model from “Energized Rigid Body Fracture, I3D 2018”

The chain:
- swings under gravity
- respects distance constraints
- dissipates energy through damping
- produces a realistic snap when the bottom link breaks

Features
--------
✔ Mass–spring particle system
✔ Distance constraints using Gauss–Seidel projection
✔ Viscous damping (internal and external)
✔ Explicit Euler integration
✔ Energy-based fracture impulse
✔ Custom chain rendering using Gizmos
✔ Fully independent from Unity’s Rigidbody / Joints

Mathematical Model
------------------
Newton’s Law:
    a_i = g - λ_air v_i

Euler Integration:
    v_i(t+Δt) = v_i(t) + a_i Δt
    r_i(t+Δt) = r_i(t) + v_i(t) Δt

Distance Constraint:
    C_ij = ||r_j - r_i|| - L0

Gauss–Seidel Projection:
    r_i ← r_i + ω (w_i/(w_i+w_j)) C_ij n_ij

Internal Damping:
    Δv_i = c v_rel (w_i/(w_i+w_j)) n_ij

Elastic Energy:
    E = 1/2 k (ΔL)^2

Energy Scaling (Alpha):
    E_c = α E

Fracture Impulse:
    μ = sqrt(2 E_c / (w_i + w_j))

Role of Alpha (α)
-----------------
Coefficient controlling how much elastic energy is converted into kinetic energy when a link breaks.

    α = 0   → no reaction
    α = 0.5 → light reaction
    α = 1   → realistic reaction
    α = 2   → exaggerated snap

System Architecture
-------------------
Points (Particles):
    position r_i, velocity v_i, mass m_i, can be fixed

Links (Constraints):
    L0, k, c, breakable

Simulation Steps:
    1. Apply gravity and air drag
    2. Integrate positions
    3. Solve constraints
    4. Apply damping
    5. Break last link
    6. Apply impulse
    7. Render

Experiments
-----------
4 scenarios with varying α:
    0, 0.5, 1, 2

Files Included
--------------
- ChainManager.cs
- README
- Unity project settings

Author
------
Jebri Oussama
Branch: chainreaction
Email: jebrioussama5@gmail.com
