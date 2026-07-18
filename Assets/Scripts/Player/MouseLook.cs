using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// Owns the camera orientation. Composites three things every frame:
    /// player look input (mouse on desktop, drag on mobile), procedural breathing
    /// sway, and transient recoil kick fed in by the <see cref="WeaponController"/>.
    /// Nothing here needs Inspector setup.
    /// </summary>
    public class MouseLook : MonoBehaviour
    {
        [Header("Sensitivity")]
        public float mouseSensitivity = 2.2f;
        public float touchSensitivity = 0.12f;
        public float pitchMin = -80f;
        public float pitchMax = 80f;

        [Header("Sway")]
        public float swayAmount = 0.6f;    // set by the weapon (grows when scoped)
        public float swaySpeed = 0.7f;

        // Set by the weapon: 1 at the hip, small (~0.25) when scoped so aim is fine.
        [HideInInspector] public float sensitivityScale = 1f;

        float _yaw;
        float _pitch;

        // Recoil kick that decays back to zero.
        Vector2 _recoil;
        Vector2 _recoilVel;
        float _seed;

        void Start()
        {
            Vector3 e = transform.localEulerAngles;
            _yaw = e.y;
            _pitch = e.x;
            _seed = Random.value * 100f;
            LockCursor(true);
        }

        void Update()
        {
            // Freeze the view entirely while a menu is up, paused, or the bullet
            // cam is running — otherwise clicking around menus silently spins the
            // aim (and sway keeps drifting) behind the panels.
            if (LookFrozen()) return;

            ReadLookInput();

            // Recoil springs back to neutral.
            _recoil = Vector2.SmoothDamp(_recoil, Vector2.zero, ref _recoilVel, 0.12f);

            // Breathing sway via Perlin noise (feels organic, not periodic).
            float t = Time.time * swaySpeed;
            float swayX = (Mathf.PerlinNoise(_seed, t) - 0.5f) * 2f * swayAmount;
            float swayY = (Mathf.PerlinNoise(t, _seed) - 0.5f) * 2f * swayAmount;

            float finalPitch = Mathf.Clamp(_pitch, pitchMin, pitchMax) - _recoil.y + swayY;
            float finalYaw = _yaw + _recoil.x + swayX;
            transform.localRotation = Quaternion.Euler(finalPitch, finalYaw, 0f);
        }

        bool LookFrozen()
        {
            if (BulletCam.Instance != null && BulletCam.Instance.IsPlaying) return true;
            var gm = GameManager.Instance;
            if (gm != null && gm.ui != null && gm.ui.MenusOpen) return true;
            return false;
        }

        void ReadLookInput()
        {
            float dx = 0f, dy = 0f;

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                // Ignore drags that start on the UI thumb-zones (right/bottom).
                if (touch.phase == TouchPhase.Moved && touch.position.x < Screen.width * 0.7f)
                {
                    dx = touch.deltaPosition.x * touchSensitivity;
                    dy = touch.deltaPosition.y * touchSensitivity;
                }
            }
            else if (Input.mousePresent)
            {
                dx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
                dy = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            }

            _yaw += dx * sensitivityScale;
            _pitch -= dy * sensitivityScale;
        }

        /// <summary>Called by the weapon when it fires.</summary>
        public void AddRecoil(float pitchKick, float yawKick)
        {
            _recoil += new Vector2(yawKick, pitchKick);
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
