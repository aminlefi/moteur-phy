# 🚀 Quick Start - Moteur Physique de Fracture

Guide rapide pour démarrer le projet en 2 minutes.

## ⚡ Méthode Ultra-Rapide

1. **Ouvrir Unity** (le projet est déjà configuré)

2. **Créer une nouvelle scène**:
   - File → New Scene
   - Template: Basic (Built-in)

3. **Ajouter le système**:
   - GameObject → Create Empty
   - Renommer en "Physics Manager"
   - Add Component → `SceneSetup`
   - Cliquer **Play** ▶️

**C'EST TOUT!** Le système se configure automatiquement.

## 🎮 Contrôles

- **ESPACE**: Fracture (applique impulsion aléatoire)
- **R**: Restart la scène
- **Gizmos ON**: Voir les contraintes (bouton en haut à droite)

## 🔧 Personnalisation

Dans l'Inspector du GameObject "Physics Manager":

```
SceneSetup:
  ✓ Auto Setup
  Cube Position: (0, 5, 0)    ← Hauteur de départ
  Cube Size: (2, 2, 2)        ← Taille du cube
  
FractureSystem (créé auto):
  Fractures X/Y/Z: 2          ← Nombre de fragments
  Fragment Mass: 1.0          ← Masse de chaque fragment
  Constraint Stiffness: 1000  ← Rigidité des ressorts
  Break Threshold: 0.5        ← Seuil de rupture
```

## 📊 Paramètres Recommandés

### Pour Apprentissage (stable):
```
Fractures: 2×2×2
Mass: 1.0
Stiffness: 1000
Threshold: 0.5
```

### Pour Démo Spectaculaire:
```
Fractures: 3×3×3
Mass: 0.5
Stiffness: 500
Threshold: 0.3
Apply Force On Start: ✓
```

### Pour Fracture Violente:
```
Fractures: 2×2×2
Mass: 0.3
Stiffness: 2000
Threshold: 0.2
Force Magnitude: 20
```

## 👀 Visualisation Gizmos

Les contraintes entre fragments sont visualisées:

- 🟢 **Vert**: Contrainte stable
- 🟡 **Jaune**: Sous tension (50-80%)
- 🔴 **Rouge**: Près de la rupture (>80%)
- ⚫ **Disparu**: Contrainte rompue

Les petites sphères rouges = centres de masse

## 📁 Structure du Code

```
Assets/Scripts/PhysicsEngine/
├── MeshGenerator.cs          ← Création cubes manuels
├── Matrix4x4Custom.cs        ← Matrices transformations
├── RigidFragment.cs          ← Physique des fragments
├── SpringConstraint.cs       ← Contraintes ressort
├── FractureSystem.cs         ← Gestionnaire principal
├── FractureDemo.cs           ← Interface utilisateur
├── SceneSetup.cs             ← Setup automatique
└── README.md                 ← Documentation complète
```

## 🧪 Tests Suggérés

1. **Test Gravité**:
   - Position Y = 10
   - Laisser tomber sans force
   - Observer les contraintes

2. **Test Impulsion**:
   - ESPACE plusieurs fois
   - Observer propagation de l'énergie

3. **Test Force Directionnelle**:
   - Apply Force On Start: ✓
   - Force Direction: (1, 0, 0)
   - Force Magnitude: 15

4. **Test Nombreux Fragments**:
   - Fractures: 4×4×4
   - Observer performance

## ⚠️ Troubleshooting

### Rien ne se passe au Play
- Vérifier que "Auto Setup" est coché
- Vérifier Console pour erreurs
- Vérifier que les scripts sont dans Assets/Scripts/PhysicsEngine/

### Fragments traversent le sol
- C'est normal! On n'a pas de collision sol (hors scope)
- Pour visualiser: augmenter "Floor Position Y"

### Pas de Gizmos visibles
- Cliquer bouton "Gizmos" en haut à droite de Scene view
- S'assurer d'être en mode Scene (pas Game)

### Contraintes se cassent immédiatement
- Augmenter Break Threshold (ex: 1.0)
- Diminuer Force Magnitude

## 📚 Documentation Détaillée

- `README.md`: Documentation complète du système
- `MESH_GENERATION_EXPLAINED.md`: Comment les cubes sont créés
- Commentaires dans le code: Chaque fonction expliquée

## 🎯 Objectifs du Projet Validés

✅ Objets 3D créés from scratch (vertices/triangles)  
✅ Transformations manuelles (matrices 4×4)  
✅ Contraintes type ressort avec mesure violation  
✅ Calcul énergie potentielle: E = ½kx²  
✅ Impulsions lors rupture: ΔV = √(2E/m)  
✅ Centre de masse et moment d'inertie calculés  
✅ ZÉRO fonction Unity physique utilisée  

## 💡 Extensions Possibles

- Ajouter collision avec le sol
- Patterns de fracture (Voronoi)
- Sons lors des ruptures
- Particules/débris
- Amortissement (damping)
- Contraintes plastiques (déformation permanente)

---

**Bon travail! 🎓**
