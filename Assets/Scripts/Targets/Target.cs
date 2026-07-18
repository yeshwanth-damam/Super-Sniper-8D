using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// A single enemy dummy. Built entirely from primitives by
    /// <see cref="TargetSpawner"/>. Stands still (kinematic) until hit, then
    /// unfreezes and takes physics knockback so it falls convincingly, and
    /// reports the kill back to the <see cref="GameManager"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Target : MonoBehaviour
    {
        [Header("Scoring")]
        public int bodyPoints = 50;
        public int headPoints = 150;

        [Header("Feel")]
        public float knockbackForce = 14f;
        public float despawnDelay = 4f;

        [HideInInspector] public TargetMover mover;

        Rigidbody[] _bodies;
        bool _isDown;

        void Awake()
        {
            _bodies = GetComponentsInChildren<Rigidbody>();
            SetKinematic(true);
        }

        void SetKinematic(bool value)
        {
            foreach (var rb in _bodies)
                rb.isKinematic = value;
        }

        /// <summary>Applied by the weapon on a confirmed hit.</summary>
        /// <returns>Points awarded (already scaled for headshots).</returns>
        public int TakeHit(Vector3 point, Vector3 direction, bool headshot)
        {
            if (_isDown) return 0;
            _isDown = true;

            if (mover != null) mover.enabled = false;

            if (ProceduralAudio.Instance != null)
                ProceduralAudio.Instance.PlayHit(point, headshot);

            // Unfreeze and shove it in the bullet's direction.
            SetKinematic(false);
            float force = headshot ? knockbackForce * 1.6f : knockbackForce;
            Rigidbody hitBody = FindClosestBody(point);
            if (hitBody != null)
                hitBody.AddForceAtPosition(direction.normalized * force, point, ForceMode.Impulse);

            Destroy(gameObject, despawnDelay);
            return headshot ? headPoints : bodyPoints;
        }

        Rigidbody FindClosestBody(Vector3 point)
        {
            Rigidbody best = null;
            float bestDist = float.MaxValue;
            foreach (var rb in _bodies)
            {
                float d = (rb.worldCenterOfMass - point).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = rb; }
            }
            return best;
        }

        public bool IsDown => _isDown;
    }
}
