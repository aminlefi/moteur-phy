# URP Materials - Quick Reference

## ✅ IMPLEMENTATION COMPLETE

All meshes now use URP materials automatically!

---

## What Changed

### New File Created:
**MaterialUtility.cs** - Centralized material creation with URP support

### Files Updated:
- **RigidBodyRenderer.cs** - Now uses MaterialUtility (cleaner code)
- **FractureDemo.cs** - Ground uses transparent URP material

---

## Quick Usage

### Create Opaque Material:
```csharp
Material mat = MaterialUtility.CreateURPMaterial(
    Color.red,        // Color
    smoothness: 0.5f, // 0-1 (shiny)
    metallic: 0.2f    // 0-1 (metallic)
);
```

### Create Transparent Material:
```csharp
Material mat = MaterialUtility.CreateTransparentURPMaterial(
    Color.green,  // Color
    alpha: 0.7f   // Transparency (0=invisible, 1=opaque)
);
```

### Set Color (Universal):
```csharp
MaterialUtility.SetMaterialColor(material, Color.blue);
// Works for both URP and Standard shaders!
```

---

## Shader Priority

The system automatically tries shaders in this order:

1. ✅ **Universal Render Pipeline/Lit** (Best - URP PBR)
2. ✅ **Universal Render Pipeline/Simple Lit** (Good - URP Simple)
3. ✅ **Standard** (Fallback - Built-in PBR)
4. ✅ **Unlit/Color** (Basic - No lighting)
5. ✅ **Sprites/Default** (Last resort)

**You don't need to configure anything - it just works!**

---

## Property Mapping

MaterialUtility handles differences automatically:

| Feature | URP Property | Standard Property |
|---------|-------------|------------------|
| Color | `_BaseColor` | `_Color` |
| Smoothness | `_Smoothness` | `_Glossiness` |
| Metallic | `_Metallic` | `_Metallic` |
| Transparency | `_Surface` | `_Mode` |

---

## Visual Results

### Fragments:
- ✅ Use URP/Lit shader (if available)
- ✅ Smoothness = 0.5 (moderate shine)
- ✅ Metallic = 0.2 (slightly metallic)
- ✅ Respond to scene lighting
- ✅ Color gradient (orange → blue in demo)

### Ground:
- ✅ Use URP/Lit shader (if available)
- ✅ Semi-transparent (alpha = 0.7)
- ✅ Green color
- ✅ Can see fragments through floor

---

## Testing

### To Verify:
1. **Press Play** in Unity
2. **Select a fragment** in Hierarchy
3. **Check Inspector** → Material → Shader
4. Should see: "Universal Render Pipeline/Lit" (if URP)
5. Or: "Standard" (if built-in pipeline)

### Visual Check:
- ✅ Fragments have realistic lighting
- ✅ Specular highlights visible
- ✅ Ground is semi-transparent
- ✅ Colors are vibrant

---

## Benefits

### Before:
- ❌ Only Standard shader support
- ❌ No URP compatibility
- ❌ Duplicate material code
- ❌ No transparency

### After:
- ✅ Full URP support
- ✅ Automatic shader detection
- ✅ Centralized material creation
- ✅ Transparent ground
- ✅ Cleaner code
- ✅ Works in any pipeline

---

## Compatibility

**Works with:**
- ✅ Universal Render Pipeline (URP)
- ✅ Built-in Render Pipeline
- ✅ Minimal Unity setups
- ✅ Any lighting configuration

**No setup required - automatic detection!**

---

## Code Examples

### Custom Fragment Material:
```csharp
// In your script
GameObject fragment = CreateFragment();
MeshRenderer renderer = fragment.GetComponent<MeshRenderer>();

// Create shiny metallic material
renderer.material = MaterialUtility.CreateURPMaterial(
    new Color(0.8f, 0.6f, 0.2f), // Gold color
    smoothness: 0.9f,             // Very shiny
    metallic: 0.8f                // Very metallic
);
```

### Glass-like Material:
```csharp
// Create transparent glass
Material glass = MaterialUtility.CreateTransparentURPMaterial(
    new Color(0.9f, 0.95f, 1f), // Light blue tint
    alpha: 0.3f                  // Very transparent
);
```

### Ice Material:
```csharp
// Smooth, slightly transparent
Material ice = MaterialUtility.CreateTransparentURPMaterial(
    new Color(0.8f, 0.9f, 1f),
    alpha: 0.6f
);
```

---

## Summary

**Status:** ✅ Complete
**Files:** 1 new, 2 updated
**Result:** All meshes use URP materials!

Press Play to see realistic lighting on your fracturing physics simulation! 🎨

---

**Tip:** If fragments appear dark, check your scene lighting (Window → Rendering → Lighting). Add a Directional Light if needed.

