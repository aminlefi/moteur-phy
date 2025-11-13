using UnityEngine;

/// <summary>
/// Utility for creating materials with URP support and fallbacks.
/// </summary>
public static class MaterialUtility
{
    /// <summary>
    /// Create a material with URP Lit shader and fallbacks.
    /// </summary>
    public static Material CreateURPMaterial(Color color, float smoothness = 0.5f, float metallic = 0.2f)
    {
        // Try URP shaders first, then fallback to Standard, then Unlit
        Shader shader = FindBestShader();
        Material mat = new Material(shader);
        
        // Set color (works for both URP and Standard)
        SetMaterialColor(mat, color);
        
        // Set surface properties
        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", smoothness);
        }
        else if (mat.HasProperty("_Glossiness"))
        {
            mat.SetFloat("_Glossiness", smoothness);
        }
        
        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", metallic);
        }
        
        return mat;
    }
    
    /// <summary>
    /// Create a transparent URP material.
    /// </summary>
    public static Material CreateTransparentURPMaterial(Color color, float alpha = 0.7f)
    {
        Shader shader = FindBestShader();
        Material mat = new Material(shader);
        
        Color transparentColor = color;
        transparentColor.a = alpha;
        
        // Configure for transparency based on shader type
        if (IsURPShader(shader))
        {
            SetupURPTransparency(mat, transparentColor);
        }
        else if (IsStandardShader(shader))
        {
            SetupStandardTransparency(mat, transparentColor);
        }
        else
        {
            // Unlit shader - just set color
            SetMaterialColor(mat, transparentColor);
        }
        
        return mat;
    }
    
    /// <summary>
    /// Find the best available shader with priority: URP > Standard > Unlit
    /// </summary>
    public static Shader FindBestShader()
    {
        // Try URP shaders
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader != null) return shader;
        
        shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader != null) return shader;
        
        // Try Standard shader
        shader = Shader.Find("Standard");
        if (shader != null) return shader;
        
        // Fallback to Unlit
        shader = Shader.Find("Unlit/Color");
        if (shader != null) return shader;
        
        // Last resort
        shader = Shader.Find("Sprites/Default");
        return shader ?? Shader.Find("Hidden/InternalErrorShader");
    }
    
    /// <summary>
    /// Set material color for both URP and Standard shaders.
    /// </summary>
    public static void SetMaterialColor(Material mat, Color color)
    {
        // URP uses _BaseColor
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }
        // Standard uses _Color
        else if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }
        
        // Also set the material.color property
        mat.color = color;
    }
    
    /// <summary>
    /// Setup URP material for transparency.
    /// </summary>
    static void SetupURPTransparency(Material mat, Color color)
    {
        // URP transparency setup
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1); // 1 = Transparent
        }
        
        if (mat.HasProperty("_Blend"))
        {
            mat.SetFloat("_Blend", 0); // 0 = Alpha blend
        }
        
        // Set blend mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        
        // Disable keywords
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        
        mat.renderQueue = 3000;
        
        SetMaterialColor(mat, color);
    }
    
    /// <summary>
    /// Setup Standard shader for transparency.
    /// </summary>
    static void SetupStandardTransparency(Material mat, Color color)
    {
        // Standard shader transparency setup
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 3); // 3 = Transparent
        }
        
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        
        mat.renderQueue = 3000;
        
        SetMaterialColor(mat, color);
    }
    
    /// <summary>
    /// Check if shader is a URP shader.
    /// </summary>
    static bool IsURPShader(Shader shader)
    {
        if (shader == null) return false;
        string name = shader.name;
        return name.Contains("Universal Render Pipeline") || name.Contains("URP");
    }
    
    /// <summary>
    /// Check if shader is Standard shader.
    /// </summary>
    static bool IsStandardShader(Shader shader)
    {
        if (shader == null) return false;
        return shader.name == "Standard";
    }
}

