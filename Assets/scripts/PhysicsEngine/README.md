# Moteur Physique - Système de Fracture d'Objets Rigides

Implémentation from scratch d'un système de fracture basé sur l'article "Rigid Body Fracture" (I3D 2018).

## 📋 Objectifs Réalisés

### ✅ 1. Objets 3D Pré-Fracturés
- Découpage automatique d'un cube en fragments (shards)
- Chaque fragment = objet indépendant avec masse et centre d'inertie
- Configurable via `fracturesX`, `fracturesY`, `fracturesZ`

### ✅ 2. Transformations Manuelles (Matrices 4×4)
- Classe `Matrix4x4Custom` - AUCUNE fonction Unity utilisée
- Matrices de rotation (X, Y, Z) et translation
- Composition de matrices pour mouvement complet
- Formules mathématiques pures

### ✅ 3. Contraintes entre Fragments
- Modélisation type ressort rigide
- Mesure de la déformation/violation: `x = currentLength - restLength`
- Seuil de rupture configurable (`breakThreshold`)

### ✅ 4. Énergie et Impulsions
- Calcul énergie potentielle: **E = ½kx²**
- Calcul impulsion lors de rupture: **ΔV = √(2E/m)**
- Direction basée sur vecteur reliant les fragments
- Application automatique aux deux fragments

## 🏗️ Architecture

```
MeshGenerator.cs         → Création MANUELLE de cubes (vertices/triangles)
Matrix4x4Custom.cs       → Matrices de transformation manuelles
RigidFragment.cs         → Fragment avec masse, vélocité, rotation
SpringConstraint.cs      → Contrainte type ressort avec rupture
FractureSystem.cs        → Gestionnaire de fracture
FractureDemo.cs          → Démonstration interactive
SceneSetup.cs            → Setup automatique de la scène
```

### 🔧 Génération Mesh Custom

**IMPORTANT**: Aucune primitive Unity n'est utilisée! Tout est créé manuellement:

- **24 vertices** (4 par face × 6 faces)
- **36 indices** pour triangles (2 triangles par face)
- **Normales** calculées pour chaque face
- **UVs** pour textures
- **Moment d'inertie** calculé: `I = (1/12) × m × (h² + d²)`

## 🎮 Utilisation

### Setup RAPIDE (Recommandé):
1. Créer un GameObject vide dans la scène
2. Ajouter le component `SceneSetup`
3. Cliquer "Play" → tout se setup automatiquement!

### Setup MANUEL:
1. Créer un GameObject vide
2. Ajouter le component `FractureSystem`
3. Ajouter le component `FractureDemo`
4. Configurer les paramètres:
   - Fractures (2x2x2 recommandé pour début)
   - Mass (1.0)
   - Stiffness (1000)
   - Break Threshold (0.5)

**Note**: Les cubes sont créés 100% manuellement - ZÉRO primitive Unity!

### Contrôles:
- **ESPACE**: Appliquer impulsion aléatoire
- **R**: Restart la scène

## 📐 Formules Implémentées

### Transformation
```
T = Translation * RotationZ * RotationY * RotationX
```

### Énergie Potentielle du Ressort
```
E = ½ k x²
où:
  k = stiffness (rigidité)
  x = deformation (longueur - repos)
```

### Impulsion lors de Rupture
```
ΔV = √(2E/m)
J = ΔV × m (impulsion)
```

### Application au Fragment
```
v_new = v_old + J/m        (vélocité linéaire)
ω_new = ω_old + τ/I        (vélocité angulaire)
τ = r × J                  (couple)
```

## 🔍 Visualisation

- **Gizmos verts**: Contraintes stables
- **Gizmos jaunes**: Contraintes sous tension (50-80% du seuil)
- **Gizmos rouges**: Contraintes près de la rupture (>80%)
- **Sphères rouges**: Centres de masse

## 📊 Paramètres Recommandés

```csharp
// Pour démonstration stable:
fracturesX/Y/Z = 2
fragmentMass = 1.0
constraintStiffness = 1000
breakThreshold = 0.5

// Pour fracture dramatique:
fracturesX/Y/Z = 3
fragmentMass = 0.5
constraintStiffness = 500
breakThreshold = 0.3
```

## 🧪 Tests

1. **Test statique**: Observer les contraintes sans force
2. **Test gravité**: Laisser tomber l'objet
3. **Test impulsion**: ESPACE pour impulsion aléatoire
4. **Test force externe**: Activer `applyForceOnStart`

## 📚 Concepts Physiques

- **Corps rigide**: Pas de déformation interne
- **Contrainte**: Lien mécanique entre deux corps
- **Ressort**: Force = -k × déformation
- **Impulsion**: Changement instantané de quantité de mouvement
- **Conservation énergie**: Énergie potentielle → cinétique

## 🎯 Extensions Possibles

- [ ] Collision avec le sol/murs
- [ ] Amortissement (damping)
- [ ] Moments d'inertie précis (tenseur 3×3)
- [ ] Contraintes non-linéaires
- [ ] Patterns de fracture (Voronoi)
- [ ] Sons de fracture

---
**Note**: Ce système N'utilise PAS le moteur physique Unity - tout est calculé manuellement selon les principes du paper I3D 2018.
