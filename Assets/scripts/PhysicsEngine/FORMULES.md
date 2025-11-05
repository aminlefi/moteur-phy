# 📐 Formules Mathématiques Implémentées

Référence rapide de toutes les formules utilisées dans le moteur physique.

## 🔄 Transformations (Matrices 4×4)

### Matrice de Translation
```
T = | 1  0  0  tx |
    | 0  1  0  ty |
    | 0  0  1  tz |
    | 0  0  0  1  |
```

### Matrice de Rotation X
```
Rx(θ) = | 1    0       0    0 |
        | 0  cos(θ) -sin(θ) 0 |
        | 0  sin(θ)  cos(θ) 0 |
        | 0    0       0    1 |
```

### Matrice de Rotation Y
```
Ry(θ) = | cos(θ)  0  sin(θ)  0 |
        |   0     1    0     0 |
        |-sin(θ)  0  cos(θ)  0 |
        |   0     0    0     1 |
```

### Matrice de Rotation Z
```
Rz(θ) = | cos(θ) -sin(θ)  0  0 |
        | sin(θ)  cos(θ)  0  0 |
        |   0       0     1  0 |
        |   0       0     0  1 |
```

### Composition
```
M_finale = T × Rz × Ry × Rx

(appliqué de droite à gauche: rotation puis translation)
```

### Application à un point
```
P' = M × P

où P = [x, y, z, 1]ᵀ (coordonnées homogènes)
```

## ⚙️ Physique des Corps Rigides

### Cinématique (Euler Explicite)
```
position(t+Δt) = position(t) + velocity × Δt
rotation(t+Δt) = rotation(t) + angularVelocity × Δt
```

### Quantité de Mouvement
```
p = m × v

où:
  p = quantité de mouvement (kg·m/s)
  m = masse (kg)
  v = vélocité (m/s)
```

### Moment Angulaire
```
L = I × ω

où:
  L = moment angulaire (kg·m²/s)
  I = moment d'inertie (kg·m²)
  ω = vélocité angulaire (rad/s)
```

## 🔧 Contraintes Type Ressort

### Déformation
```
x = L_actuelle - L_repos

où:
  x = déformation (m)
  L_actuelle = distance actuelle entre fragments
  L_repos = longueur au repos de la contrainte
```

### Force du Ressort (Loi de Hooke)
```
F = -k × x

où:
  F = force (N)
  k = rigidité/stiffness (N/m)
  x = déformation (m)
```

### Énergie Potentielle Élastique
```
E = ½ × k × x²

où:
  E = énergie potentielle (J = kg·m²/s²)
  k = rigidité (N/m)
  x = déformation (m)
```

## 💥 Rupture et Impulsions

### Condition de Rupture
```
|x| > threshold
```

### Magnitude de l'Impulsion
```
ΔV = √(2E/m)

où:
  ΔV = changement de vélocité (m/s)
  E = énergie stockée (J)
  m = masse (kg)
```

**Démonstration**:
```
E_cinétique = ½mv²
Si E_potentielle → E_cinétique:
  ½mv² = E
  v² = 2E/m
  v = √(2E/m)
```

### Impulsion Vectorielle
```
J = ΔV × m × d̂

où:
  J = impulsion (N·s = kg·m/s)
  d̂ = direction unitaire de la contrainte
```

### Application aux Fragments
```
Fragment A: v_A' = v_A - J/m_A
Fragment B: v_B' = v_B + J/m_B

(directions opposées - conservation quantité de mouvement)
```

## 🔄 Rotation et Couple

### Couple (Torque)
```
τ = r × F

où:
  τ = couple (N·m)
  r = vecteur position par rapport au centre de masse
  F = force appliquée
  × = produit vectoriel
```

### Changement de Vélocité Angulaire
```
Δω = τ / I

où:
  Δω = changement vélocité angulaire (rad/s)
  τ = couple (N·m)
  I = moment d'inertie (kg·m²)
```

### Produit Vectoriel (pour calcul du couple)
```
a × b = | i    j    k  |
        | a_x  a_y  a_z|
        | b_x  b_y  b_z|

= i(a_y·b_z - a_z·b_y) - j(a_x·b_z - a_z·b_x) + k(a_x·b_y - a_y·b_x)
```

## 📊 Propriétés Physiques du Cube

### Volume
```
V = largeur × hauteur × profondeur
```

### Masse (avec densité)
```
m = ρ × V

où:
  m = masse (kg)
  ρ = densité (kg/m³)
  V = volume (m³)
```

### Centre de Masse
```
CM = (1/n) × Σ(vertex_i)

où n = nombre de vertices
```

### Moment d'Inertie (Cube Homogène)
```
I_x = (1/12) × m × (h² + d²)
I_y = (1/12) × m × (w² + d²)
I_z = (1/12) × m × (w² + h²)

où:
  w = largeur (dimension X)
  h = hauteur (dimension Y)
  d = profondeur (dimension Z)
```

**Tenseur d'inertie complet** (simplifié pour axes principaux):
```
I = | I_x   0    0  |
    |  0   I_y   0  |
    |  0    0   I_z |
```

## 🌍 Gravité

### Force Gravitationnelle
```
F_g = m × g

où:
  g = accélération gravitationnelle ≈ 9.81 m/s² (sur Terre)
```

### Accélération
```
a = F/m = g  (indépendante de la masse!)
```

## ⏱️ Intégration Temporelle (Euler)

### Position
```
x(t + Δt) = x(t) + v(t) × Δt + ½a(t) × Δt²

Simplifié (Euler explicite):
x(t + Δt) ≈ x(t) + v(t) × Δt
```

### Vélocité
```
v(t + Δt) = v(t) + a(t) × Δt
```

## 📏 Normalisation de Vecteurs

### Vecteur Unitaire
```
û = u / |u|

où |u| = √(u_x² + u_y² + u_z²)
```

## 🎯 Conservation

### Conservation de l'Énergie
```
E_totale = E_cinétique + E_potentielle = constante

½mv² + ½kx² = constante
```

### Conservation de la Quantité de Mouvement
```
Σ(m_i × v_i) = constante

Avant rupture: p_total = 0
Après rupture: m_A × v_A + m_B × v_B = 0
```

## 🧮 Constantes Utilisées

```
π ≈ 3.14159
g ≈ 9.81 m/s²  (gravité terrestre)
Δt = 0.02 s    (Fixed Update à 50 Hz)

Valeurs par défaut:
k = 1000 N/m   (rigidité)
threshold = 0.5 m (seuil de rupture)
m = 1.0 kg     (masse par fragment)
ρ = 1.0 kg/m³  (densité)
```

## 📐 Angles

### Conversion
```
radians = degrés × (π/180)
degrés = radians × (180/π)
```

### Relations Trigonométriques
```
sin²(θ) + cos²(θ) = 1
tan(θ) = sin(θ)/cos(θ)
```

---

**Note**: Toutes ces formules sont implémentées manuellement dans le code - aucune fonction physique Unity n'est utilisée!
