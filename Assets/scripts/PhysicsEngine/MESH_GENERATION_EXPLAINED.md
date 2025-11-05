# 📐 Création Manuelle de Cubes - Explication Détaillée

Ce document explique comment nous créons les cubes **from scratch** sans utiliser les primitives Unity.

## 🎯 Pourquoi From Scratch?

Objectif du cours: Comprendre comment fonctionnent réellement les objets 3D, pas juste utiliser des boîtes noires Unity.

## 📊 Structure d'un Cube

Un cube = **8 coins**, **6 faces**, **12 arêtes**

Mais pour le rendu 3D, on a besoin de **24 vertices** (pas 8!) car:
- Chaque face a besoin de ses propres normales
- Les UVs sont différentes pour chaque face

## 🔢 Les Vertices (24 au total)

```
Face AVANT (Z+): 4 vertices
  vertices[0] = (-halfX, -halfY, +halfZ)  // Bas-gauche
  vertices[1] = (+halfX, -halfY, +halfZ)  // Bas-droite
  vertices[2] = (+halfX, +halfY, +halfZ)  // Haut-droite
  vertices[3] = (-halfX, +halfY, +halfZ)  // Haut-gauche

Face ARRIÈRE (Z-): 4 vertices
  vertices[4-7] ...

Face HAUT (Y+): 4 vertices
  vertices[8-11] ...

Face BAS (Y-): 4 vertices
  vertices[12-15] ...

Face DROITE (X+): 4 vertices
  vertices[16-19] ...

Face GAUCHE (X-): 4 vertices
  vertices[20-23] ...
```

## 🔺 Les Triangles (36 indices)

Chaque face = 2 triangles = 6 indices

```
Face AVANT:
  Triangle 1: [0, 2, 1]  // Ordre anti-horaire
  Triangle 2: [0, 3, 2]
```

**Pourquoi anti-horaire?** Unity utilise le "back-face culling" - les triangles dans le sens horaire sont considérés comme "de dos" et ne sont pas rendus.

## 📏 Les Normales

Normal = vecteur perpendiculaire à une surface

```
Face AVANT (Z+) → normale = (0, 0, 1)
Face ARRIÈRE (Z-) → normale = (0, 0, -1)
Face HAUT (Y+) → normale = (0, 1, 0)
Face BAS (Y-) → normale = (0, -1, 0)
Face DROITE (X+) → normale = (1, 0, 0)
Face GAUCHE (X-) → normale = (-1, 0, 0)
```

Les normales sont essentielles pour:
- Calcul de la lumière
- Déterminer quelle face est visible

## 🗺️ Les UVs (Coordonnées de Texture)

UVs = coordonnées 2D pour mapper une texture sur une surface 3D

Pour chaque face (4 vertices):
```
UV[0] = (0, 0)  // Bas-gauche
UV[1] = (1, 0)  // Bas-droite
UV[2] = (1, 1)  // Haut-droite
UV[3] = (0, 1)  // Haut-gauche
```

Cela permet d'appliquer une texture carrée sur chaque face du cube.

## ⚙️ Moment d'Inertie

Le moment d'inertie détermine la résistance à la rotation.

Pour un cube homogène de masse `m` et dimensions `(w, h, d)`:

```
Ix = (1/12) × m × (h² + d²)
Iy = (1/12) × m × (w² + d²)
Iz = (1/12) × m × (w² + h²)
```

**Pourquoi 3 valeurs?**
- Rotation autour de X → utilise Ix
- Rotation autour de Y → utilise Iy
- Rotation autour de Z → utilise Iz

Un cube est plus difficile à faire tourner autour d'une diagonale que face-à-face.

## 📐 Volume et Masse

```
Volume = largeur × hauteur × profondeur
Masse = densité × volume

Si densité = 1 kg/m³:
  Cube de 1×1×1 → masse = 1 kg
  Cube de 0.5×0.5×0.5 → masse = 0.125 kg
```

## 🎨 Code Complet (Simplifié)

```csharp
Mesh CreateCube(Vector3 size) {
    Mesh mesh = new Mesh();
    
    // 1. Définir les 24 vertices
    Vector3[] vertices = new Vector3[24];
    // ... remplir vertices ...
    
    // 2. Définir les 36 indices (12 triangles × 3)
    int[] triangles = new int[36];
    // ... remplir triangles ...
    
    // 3. Calculer normales
    Vector3[] normals = new Vector3[24];
    // ... remplir normals ...
    
    // 4. Définir UVs
    Vector2[] uvs = new Vector2[24];
    // ... remplir uvs ...
    
    // 5. Assigner au mesh
    mesh.vertices = vertices;
    mesh.triangles = triangles;
    mesh.normals = normals;
    mesh.uv = uvs;
    
    return mesh;
}
```

## 🔬 Visualisation

```
     7 -------- 6
    /|         /|
   / |        / |
  3 -------- 2  |
  |  4 ------|--5
  | /        | /
  |/         |/
  0 -------- 1
```

Coins du cube (coordonnées):
- 0: (-x, -y, +z)
- 1: (+x, -y, +z)
- 2: (+x, +y, +z)
- 3: (-x, +y, +z)
- 4: (-x, -y, -z)
- 5: (+x, -y, -z)
- 6: (+x, +y, -z)
- 7: (-x, +y, -z)

## ✅ Vérification

Pour vérifier que le mesh est correct:
1. Toutes les faces doivent être visibles
2. Pas de "trous" noirs
3. Les normales pointent vers l'extérieur
4. Le lighting fonctionne correctement

## 🎓 Concepts Mathématiques Utilisés

- **Géométrie 3D**: Vertices, faces, arêtes
- **Algèbre linéaire**: Vecteurs, normales
- **Topologie**: Ordre des triangles (winding order)
- **Physique**: Moment d'inertie, centre de masse
- **Infographie**: UV mapping, back-face culling

---

Ce système démontre une compréhension complète de la création d'objets 3D, pas juste l'utilisation d'outils pré-faits!
