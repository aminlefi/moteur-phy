using System.Collections.Generic;
using UnityEngine;

// Manages evaluation of constraints between fragments and triggers fracture events.
public class FractureManager : MonoBehaviour
{
    public List<SpringConstraint> constraints = new List<SpringConstraint>();

    void Update()
    {
        // Evaluate constraints each frame; if broken, the SpringConstraint handles impulse.
        foreach (var c in constraints)
        {
            if (c == null) continue;
            c.EvaluateAndMaybeBreak();
        }
    }
}
