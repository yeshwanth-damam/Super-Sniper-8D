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

            SpawnImpactVfx(point, direction, headshot);

            // Unfreeze and shove it in the bullet's direction.
            SetKinematic(false);
            float force = headshot ? knockbackForce * 1.6f : knockbackForce;
            Rigidbody hitBody = FindClosestBody(point);
            if (hitBody != null)
                hitBody.AddForceAtPosition(direction.normalized * force, point, ForceMode.Impulse);

            Destroy(gameObject, despawnDelay);
            return headshot ? headPoints : bodyPoints;
        }

        // A short-lived particle burst at the point of impact. Amber sparks for
        // a headshot, a darker spray for a body hit. Built entirely in code.
        void SpawnImpactVfx(Vector3 point, Vector3 direction, bool headshot)
        {
            var go = new GameObject("Impact");
            go.transform.position = point;
            // Face the burst back toward the shooter.
            if (direction.sqrMagnitude > 0.001f)
                go.transform.rotation = Quaternion.LookRotation(-direction);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.45f;
            main.startSpeed = headshot ? 6f : 4f;
            main.startSize = 0.06f;
            main.gravityModifier = 1.2f;
            main.maxParticles = 60;
            main.startColor = headshot
                ? new Color(1f, 0.72f, 0.22f)
                : new Color(0.5f, 0.06f, 0.06f);

            var emission = ps.emission;
            emission.enabled = false; // we emit a single manual burst

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.05f;

            var psr = ps.GetComponent<ParticleSystemRenderer>();
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            if (s != null) psr.material = new Material(s);

            ps.Emit(headshot ? 40 : 28);
            Destroy(go, 1.2f);
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
