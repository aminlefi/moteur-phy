# 🏗️ Architecture du Système

## 📊 Diagramme de Flux

```
┌─────────────────────────────────────────────────────────┐
│                     SceneSetup.cs                       │
│                   (Setup Automatique)                   │
└────────────────────┬────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────┐
│                  FractureSystem.cs                      │
│              (Gestionnaire Principal)                   │
│  • Créer fragments (N×N×N grille)                      │
│  • Créer contraintes entre adjacents                    │
│  • Vérifier ruptures chaque frame                       │
│  • Appliquer gravité                                    │
└──┬────────────────┬─────────────────┬──────────────────┘
   │                │                 │
   ▼                ▼                 ▼
┌──────────┐  ┌──────────┐    ┌─────────────────┐
│  Mesh    │  │ Matrix   │    │  Rigid          │
│Generator │  │4x4Custom │    │  Fragment       │
└──────────┘  └──────────┘    └─────────────────┘
   │                │                 │
   └────────────────┴─────────────────┘
                     │
                     ▼
         ┌──────────────────────┐
         │  SpringConstraint    │
         │  (Entre fragments)   │
         └──────────────────────┘
```

## 🔄 Cycle de Vie

### 1️⃣ Initialisation (Start)

```
SceneSetup
    └─> FractureSystem.CreateFragments()
            ├─> MeshGenerator.CreateCubeMesh()
            │       └─> 24 vertices + 36 triangles
            │
            └─> RigidFragment (pour chaque fragment)
                    ├─> CalculateCenterOfMass()
                    └─> CalculateInertia()

FractureSystem.CreateConstraints()
    └─> SpringConstraint (pour chaque paire adjacente)
            └─> Calculer restLength
```

### 2️⃣ Boucle Physique (FixedUpdate - 50Hz)

```
POUR chaque fragment:
    1. Appliquer gravité: v += g × Δt
    2. Intégrer vélocité: pos += v × Δt
    3. Intégrer rotation: rot += ω × Δt
    4. Construire matrice: M = T × Rz × Ry × Rx
    5. Appliquer à Unity (visualisation)

POUR chaque contrainte:
    1. Mesurer violation: x = L_actuelle - L_repos
    2. SI |x| > threshold:
        a. Calculer énergie: E = ½kx²
        b. Calculer impulsion: ΔV = √(2E/m)
        c. Appliquer aux 2 fragments
        d. Marquer comme cassée
```

### 3️⃣ Rendu (OnDrawGizmos)

```
POUR chaque contrainte non-cassée:
    Calculer couleur selon violation:
        |x| > 80% threshold → Rouge
        |x| > 50% threshold → Jaune
        Sinon → Vert
    Dessiner ligne entre fragments
```

---

## 🧩 Relations Entre Classes

### MeshGenerator (Static)
```
Responsabilité: Créer geometry from scratch
Dépendances: Aucune
Utilisé par: FractureSystem

Méthodes clés:
  • CreateCubeMesh(size) → Mesh
  • CreateCubeGameObject(pos, size, mat) → GameObject
  • CalculateCubeInertia(mass, size) → Vector3
```

### Matrix4x4Custom
```
Responsabilité: Transformations spatiales
Dépendances: Aucune (math pur)
Utilisé par: RigidFragment

Méthodes clés:
  • Translation(vector) → Matrix4x4Custom
  • RotationX/Y/Z(angle) → Matrix4x4Custom
  • operator*(Matrix, Matrix) → Matrix4x4Custom
  • MultiplyPoint(point) → Vector3
```

### RigidFragment (MonoBehaviour)
```
Responsabilité: Physique d'un fragment
Dépendances: Matrix4x4Custom, MeshGenerator
Utilisé par: FractureSystem, SpringConstraint

Propriétés:
  • mass, centerOfMass, inertia
  • velocity, angularVelocity
  • currentPosition, currentRotation

Méthodes:
  • ApplyImpulse(impulse, worldPoint)
  • GetWorldPoint(localPoint) → Vector3
  • CalculateCenterOfMass()
  • CalculateInertia()
```

### SpringConstraint
```
Responsabilité: Contrainte entre 2 fragments
Dépendances: RigidFragment
Utilisé par: FractureSystem

Propriétés:
  • fragmentA, fragmentB
  • stiffness, restLength, breakThreshold
  • isBroken

Méthodes:
  • MeasureViolation(out direction) → float
  • CalculatePotentialEnergy() → float
  • ShouldBreak() → bool
  • Break()
```

### FractureSystem (MonoBehaviour)
```
Responsabilité: Orchestration du système
Dépendances: Toutes les classes ci-dessus
Utilisé par: SceneSetup

Collections:
  • List<RigidFragment> fragments
  • List<SpringConstraint> constraints

Méthodes:
  • CreateFragments(size)
  • CreateConstraints()
  • FixedUpdate() → vérifie ruptures
```

### FractureDemo (MonoBehaviour)
```
Responsabilité: Interface utilisateur
Dépendances: FractureSystem, RigidFragment
Utilisé par: Utilisateur

Méthodes:
  • ApplyRandomImpulse()
  • OnGUI() → affiche infos
```

### SceneSetup (MonoBehaviour)
```
Responsabilité: Configuration automatique
Dépendances: Toutes les classes
Utilisé par: Utilisateur

Méthodes:
  • SetupScene()
  • CreateMainCube()
  • SetupCamera()
  • CreateFloor()
```

---

## 📡 Flux de Données

### Création d'un Fragment
```
FractureSystem
    │
    ├─> MeshGenerator.CreateCubeGameObject()
    │       │
    │       └─> MeshGenerator.CreateCubeMesh()
    │               │
    │               └─> vertices[24]
    │               └─> triangles[36]
    │               └─> normals[24]
    │               └─> uvs[24]
    │
    └─> fragment.AddComponent<RigidFragment>()
            │
            ├─> CalculateCenterOfMass()
            │       └─> moyenne(vertices)
            │
            └─> CalculateInertia()
                    └─> I = (1/12)m(h²+d²)
```

### Application d'une Impulsion
```
SpringConstraint.Break()
    │
    ├─> CalculatePotentialEnergy()
    │       └─> E = ½kx²
    │
    ├─> ΔV = √(2E/m)
    │
    └─> RigidFragment.ApplyImpulse(J, point)
            │
            ├─> velocity += J/m
            │
            └─> angularVelocity += (r × J)/I
                    │
                    └─> Vector3.Cross(r, J)
```

### Transformation d'un Fragment
```
RigidFragment.FixedUpdate()
    │
    ├─> position += velocity × Δt
    │
    ├─> rotation += angularVelocity × Δt
    │
    └─> UpdateTransformMatrix()
            │
            ├─> T = Matrix4x4Custom.Translation(pos)
            ├─> Rx = Matrix4x4Custom.RotationX(rot.x)
            ├─> Ry = Matrix4x4Custom.RotationY(rot.y)
            ├─> Rz = Matrix4x4Custom.RotationZ(rot.z)
            │
            └─> M = T × Rz × Ry × Rx
                    │
                    └─> ApplyTransformToUnity()
```

---

## 🎯 Points d'Entrée

### Pour l'Utilisateur
```
1. SceneSetup.autoSetup = true (Inspector)
2. Play
3. ESPACE (impulsion)
4. R (restart)
```

### Pour le Code
```
1. SceneSetup.Start()
2. FractureSystem.Start()
3. RigidFragment.Start() × N
4. FractureSystem.FixedUpdate() (boucle)
5. RigidFragment.FixedUpdate() × N (boucle)
```

---

## 🔍 Détection de Rupture

```
┌─────────────────────────────────────┐
│    FractureSystem.FixedUpdate()     │
└─────────────────┬───────────────────┘
                  │
                  ▼
    ┌─────────────────────────────┐
    │ POUR chaque constraint      │
    └─────────────┬───────────────┘
                  │
                  ▼
    ┌──────────────────────────────────┐
    │ constraint.ShouldBreak()?        │
    └────────┬─────────────────┬───────┘
             │                 │
           OUI               NON
             │                 │
             ▼                 ▼
    ┌────────────────┐   ┌──────────┐
    │ Break()        │   │ Continue │
    │  ├─> E=½kx²    │   └──────────┘
    │  ├─> ΔV=√(2E/m)│
    │  └─> Impulses  │
    └────────────────┘
```

---

## 💾 État du Système

### Variables Globales
```
FractureSystem:
  • fragments: List<RigidFragment>        (N fragments)
  • constraints: List<SpringConstraint>   (M contraintes)

Chaque RigidFragment:
  • position: Vector3                     (3 floats)
  • rotation: Vector3                     (3 floats)
  • velocity: Vector3                     (3 floats)
  • angularVelocity: Vector3              (3 floats)
  • mass: float                           (1 float)
  • inertia: Vector3                      (3 floats)
  
  Total: 16 floats par fragment

Chaque SpringConstraint:
  • fragmentA, fragmentB: références
  • stiffness: float
  • restLength: float
  • breakThreshold: float
  • isBroken: bool
  
  Total: ~5 valeurs par contrainte
```

### Complexité
```
Fragments: N = fracturesX × fracturesY × fracturesZ
Contraintes: M ≈ 3N (approximation, dépend topologie)

Exemple 2×2×2:
  Fragments: 8
  Contraintes: ~24
  Mémoire: ~200 bytes par fragment
  Total: ~2KB
```

---

## ⚡ Performance

### Complexité Temporelle
```
CreateFragments(): O(N)
CreateConstraints(): O(N²) simplifié à O(N) avec distance check
FixedUpdate(): O(N + M)
  • Gravité: O(N)
  • Ruptures: O(M)
  • Intégration: O(N)
```

### Optimisations Possibles
1. Spatial hashing pour contraintes (O(N²) → O(N))
2. Sleeping fragments (si vélocité < epsilon)
3. Batch rendering
4. Job System Unity (parallélisation)

---

## 🎓 Principes Appliqués

### Design Patterns
- **Composition**: Matrix multiplication
- **Strategy**: Different constraint types possible
- **Observer**: Gizmos watching state
- **Factory**: MeshGenerator

### SOLID
- **S**: Chaque classe une responsabilité
- **O**: Extensible (nouveaux types contraintes)
- **L**: N/A (pas d'héritage)
- **I**: N/A (pas d'interfaces explicites)
- **D**: Dépendances vers abstractions

### Physics Principles
- Conservation énergie
- Conservation quantité mouvement
- Intégration numérique (Euler)
- Mécanique analytique

---

Cette architecture démontre une séparation claire des responsabilités tout en maintenant un flux de données logique et performant.
