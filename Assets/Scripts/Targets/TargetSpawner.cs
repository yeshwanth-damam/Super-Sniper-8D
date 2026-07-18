using System.Collections.Generic;
using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// Builds a level's worth of humanoid target dummies out of Unity
    /// primitives — no imported models. Each dummy is a stack of a body,
    /// limbs and a head; the head gets a <see cref="TargetHead"/> marker so it
    /// registers as a headshot. Some are made to patrol via <see cref="TargetMover"/>.
    /// </summary>
    public class TargetSpawner : MonoBehaviour
    {
        [Header("Play area (relative to spawner origin)")]
        public float areaWidth = 34f;
        public float minDistance = 25f;
        public float maxDistance = 90f;
        public float groundY = 0f;

        readonly List<GameObject> _live = new List<GameObject>();
        Material _bodyMat;
        Material _headMat;

        void Awake()
        {
            _bodyMat = MakeMaterial(new Color(0.16f, 0.17f, 0.2f));   // dark tactical grey
            _headMat = MakeMaterial(new Color(0.85f, 0.62f, 0.18f));  // amber accent = headshot zone
        }

        /// <summary>Clears the old level and builds the new one. Returns target count.</summary>
        public int BuildLevel(GameManager.LevelConfig cfg)
        {
            ClearAll();

            for (int i = 0; i < cfg.targetCount; i++)
            {
                bool moving = i < cfg.moverCount;
                Vector3 pos = RandomStand();
                GameObject dummy = BuildDummy(pos, moving, cfg.moverSpeed);
                _live.Add(dummy);
            }
            return cfg.targetCount;
        }

        public void ClearAll()
        {
            foreach (var go in _live)
                if (go != null) Destroy(go);
            _live.Clear();
        }

        Vector3 RandomStand()
        {
            float x = Random.Range(-areaWidth * 0.5f, areaWidth * 0.5f);
            float z = Random.Range(minDistance, maxDistance);
            return transform.position + new Vector3(x, groundY, z);
        }

        GameObject BuildDummy(Vector3 basePos, bool moving, float moverSpeed)
        {
            var root = new GameObject("Target");
            root.transform.position = basePos;
            root.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // face the shooter

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 60f;

            var target = root.AddComponent<Target>();

            // Torso ----------------------------------------------------------
            var torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            torso.name = "Torso";
            torso.transform.SetParent(root.transform, false);
            torso.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            torso.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            Paint(torso, _bodyMat);

            // Head ------------------------------------------------------------
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.85f, 0f);
            head.transform.localScale = Vector3.one * 0.34f;
            Paint(head, _headMat);
            var headMarker = head.AddComponent<TargetHead>();
            headMarker.owner = target;

            // Legs ------------------------------------------------------------
            AddLimb(root.transform, new Vector3(-0.15f, 0.4f, 0f), new Vector3(0.18f, 0.4f, 0.18f));
            AddLimb(root.transform, new Vector3(0.15f, 0.4f, 0f), new Vector3(0.18f, 0.4f, 0.18f));
            // Arms ------------------------------------------------------------
            AddLimb(root.transform, new Vector3(-0.42f, 1.15f, 0f), new Vector3(0.14f, 0.35f, 0.14f));
            AddLimb(root.transform, new Vector3(0.42f, 1.15f, 0f), new Vector3(0.14f, 0.35f, 0.14f));

            if (moving)
            {
                var mover = root.AddComponent<TargetMover>();
                Vector3 a = basePos + Vector3.left * 6f;
                Vector3 b = basePos + Vector3.right * 6f;
                mover.Configure(a, b, moverSpeed);
                target.mover = mover;
            }

            return root;
        }

        void AddLimb(Transform parent, Vector3 localPos, Vector3 scale)
        {
            var limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            limb.transform.SetParent(parent, false);
            limb.transform.localPosition = localPos;
            limb.transform.localScale = scale;
            Paint(limb, _bodyMat);
        }

        void Paint(GameObject go, Material mat)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }

        // Uses whichever pipeline shader is available (URP or Built-in).
        static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            return mat;
        }
    }
}
