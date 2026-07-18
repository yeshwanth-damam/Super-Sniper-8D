using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// Simple back-and-forth patrol between two world points. Attached to the
    /// movers of a level by <see cref="TargetSpawner"/>. Drives a kinematic
    /// <see cref="Rigidbody"/> via <c>MovePosition</c> so motion interpolates
    /// smoothly and plays well with the physics raycasts; once the target is
    /// hit this component is disabled and physics takes over.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TargetMover : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public float speed = 1.6f;

        float _t;
        int _dir = 1;
        Rigidbody _rb;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        public void Configure(Vector3 a, Vector3 b, float moveSpeed)
        {
            pointA = a;
            pointB = b;
            speed = moveSpeed;
            _t = 0f;
        }

        void FixedUpdate()
        {
            float length = Mathf.Max(0.01f, Vector3.Distance(pointA, pointB));
            _t += _dir * (speed / length) * Time.fixedDeltaTime;
            if (_t >= 1f) { _t = 1f; _dir = -1; }
            else if (_t <= 0f) { _t = 0f; _dir = 1; }

            Vector3 pos = Vector3.Lerp(pointA, pointB, _t);
            Vector3 fwd = (pointB - pointA) * _dir;
            Quaternion rot = fwd.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(fwd)
                : transform.rotation;

            if (_rb != null && _rb.isKinematic)
            {
                _rb.MovePosition(pos);
                _rb.MoveRotation(rot);
            }
            else
            {
                transform.position = pos;
                transform.rotation = rot;
            }
        }
    }
}
