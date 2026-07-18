using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// Marker placed on a target's head collider. The weapon checks for this
    /// component (rather than a string tag) to award a headshot, so no Tag
    /// setup is required in the editor.
    /// </summary>
    public class TargetHead : MonoBehaviour
    {
        public Target owner;
    }
}
