# Pre-fractured fragments and manual transforms

This set of scripts demonstrates a simple pipeline to create pre-fractured 3D objects (a cube subdivided into box shards), compute fragment centers of inertia, simulate spring-like constraints between fragments, detect breakage, compute stored energy and apply impulses at rupture.

Files added (location: `Assets/Scripts`):

- `CustomMatrix4x4.cs` — small 4x4 matrix helper: translation, rotations (X/Y/Z), multiply, point transform and converting rotation part to a `Quaternion`. All math is computed manually; Unity matrix helpers are not used.
- `PreFracturedGenerator.cs` — subdivides a parent-sized cube into smaller box shards. Positions shards by computing a manual matrix for each shard and applying the computed position/rotation/scale to the GameObject.
- `Fragment.cs` — component holding `mass` and computes center of inertia from the attached `MeshFilter`. Provides `ApplyImpulse(Vector3)` to apply an impulse to the fragment's `Rigidbody`.
- `SpringConstraint.cs` — models a rigid spring-like constraint between two `Fragment` objects. Measures extension x, computes potential energy E = 1/2 k x^2. If extension exceeds the `breakThreshold`, the constraint breaks and applies impulses to both fragments.
- `FractureManager.cs` — simple manager that evaluates a list of `SpringConstraint` instances each frame.

Usage notes & assumptions:

- The generator currently creates box-shaped shards by subdividing the parent cube into an axis-aligned grid. It's intentionally simple so you can inspect and customize the fracture pattern.
- Transform calculations (positions/rotations) are computed with `CustomMatrix4x4`. The resulting `Vector3` and `Quaternion` are then applied to `Transform` properties — the computations themselves avoid Unity's built-in transform/rotation helper functions.
- When a `SpringConstraint` breaks we split the stored energy equally between the two fragments (assumption). For fragment i we compute velocity magnitude from E_i by v_i = sqrt(2 E_i / m_i), then impulse = m_i * v_i in the corresponding direction. This is a simple, physically-motivated heuristic but not guaranteed to perfectly conserve all momentum/energy for complex setups.
- `Fragment.ComputeCenterOfInertiaFromMesh()` computes the centroid of the mesh vertices as the center of inertia (simple point-mass approximation). It sets the Rigidbody.centerOfMass accordingly.

How to try it quickly in the Editor:

1. Create an empty GameObject in your scene and attach `PreFracturedGenerator`.
2. Set `size`, `subdivisions` and `shardMass` in the inspector and call the `GenerateShards` context menu action.
3. Create a `FractureManager` and add `SpringConstraint` instances (point them to pairs of generated `Shard` objects). Configure `k` and `breakThreshold`.
4. Play the scene — constraints are evaluated every frame; when a constraint breaks, impulses are applied to shards.

Next improvements you might want:

- Replace the equal-energy split with a momentum/energy-conserving solver.
- Use arbitrary fracture meshes (Voronoi fragments) rather than axis-aligned subdivisions.
- Compute a proper inertia tensor for each fragment for rotational impulses.
- Add editor helpers to auto-wire constraints between neighboring shards.
