using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// Simple back-and-forth patrol between two world points. Attached to the
    /// movers of a level by <see cref="TargetSpawner"/>. Moves the transform
    /// while kinematic; once the target is hit it disables itself so physics
    /// takes over.
    /// </summary>
    public class TargetMover : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public float speed = 1.6f;

        float _t;
        int _dir = 1;

        public void Configure(Vector3 a, Vector3 b, float moveSpeed)
        {
            pointA = a;
            pointB = b;
            speed = moveSpeed;
            _t = 0f;
        }

        void Update()
        {
            float length = Mathf.Max(0.01f, Vector3.Distance(pointA, pointB));
            _t += _dir * (speed / length) * Time.deltaTime;
            if (_t >= 1f) { _t = 1f; _dir = -1; }
            else if (_t <= 0f) { _t = 0f; _dir = 1; }

            Vector3 pos = Vector3.Lerp(pointA, pointB, _t);
            transform.position = pos;

            // Face the direction of travel.
            Vector3 fwd = (pointB - pointA) * _dir;
            if (fwd.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(fwd), Time.deltaTime * 6f);
        }
    }
}
