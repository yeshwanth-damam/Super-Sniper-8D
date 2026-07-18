using System.Collections;
using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// Bolt-action sniper logic: fire (a raycast from screen centre), the 8×
    /// scope (FOV lerp from 60° to 7.5°), ammo + reload, recoil kick, a tracer,
    /// and breath-holding that steadies sway while a heartbeat climbs. Wires the
    /// hit into the <see cref="BulletCam"/> and <see cref="GameManager"/>.
    /// Lives on the camera; needs a <see cref="MouseLook"/> alongside it.
    /// </summary>
    [RequireComponent(typeof(MouseLook))]
    public class WeaponController : MonoBehaviour
    {
        [Header("Camera / Scope")]
        public Camera cam;
        public float hipFOV = 60f;
        public float scopeMultiplier = 8f;   // 8× -> FOV 7.5°
        public float aimSpeed = 12f;

        [Header("Ballistics")]
        public float range = 800f;
        public int clipSize = 5;
        public float reloadTime = 1.8f;
        public float boltTime = 0.7f;

        [Header("Recoil / Sway")]
        public float recoilPitch = 3.2f;
        public float recoilYaw = 0.7f;
        public float hipSway = 0.7f;
        public float scopedSway = 1.3f;      // magnified when scoped
        public float breathHoldMax = 4f;     // hold longer than this and sway spikes

        // Runtime -----------------------------------------------------------
        public int Ammo { get; private set; }
        public bool IsScoped { get; private set; }

        MouseLook _look;
        UIManager _ui;
        AudioSource _heartbeat;
        AudioLowPassFilter _lowPass;
        Light _muzzleFlash;
        LineRenderer _tracer;
        GameObject _rifle;
        float _targetFOV;
        float _breathHeldFor;
        bool _busy; // reloading or cycling the bolt
        float _scoreMul = 1f;      // Damage upgrade
        float _stabilityMul = 1f;  // Stability upgrade

        public void Init(UIManager ui)
        {
            _ui = ui;
        }

        /// <summary>Applies the persistent upgrade tree to the rifle's stats.</summary>
        public void ApplyUpgrades(SaveData p)
        {
            if (p == null) return;
            scopeMultiplier = Upgrades.ScopeMultiplier(p.GetLevel(UpgradeTrack.Zoom));
            clipSize = Upgrades.ClipSize(p.GetLevel(UpgradeTrack.Clip));
            _stabilityMul = Upgrades.SwayMultiplier(p.GetLevel(UpgradeTrack.Stability));
            _scoreMul = Upgrades.ScoreMultiplier(p.GetLevel(UpgradeTrack.Damage));
            Ammo = clipSize;
            if (_ui != null) _ui.SetAmmo(Ammo, clipSize);
        }

        void Awake()
        {
            _look = GetComponent<MouseLook>();
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            Ammo = clipSize;
            _targetFOV = hipFOV;
            if (cam != null) cam.fieldOfView = hipFOV;

            BuildRifle();
            BuildMuzzleFlash();
            BuildTracer();

            // Low-pass filter on the listener's GameObject muffles the whole
            // world when scoped (breath held, tunnel-vision focus).
            _lowPass = gameObject.GetComponent<AudioLowPassFilter>();
            if (_lowPass == null) _lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            _lowPass.cutoffFrequency = 22000f; // open = no audible filtering
        }

        void Start()
        {
            if (ProceduralAudio.Instance != null)
            {
                _heartbeat = ProceduralAudio.Instance.CreateHeartbeatSource();
                _heartbeat.transform.SetParent(transform, false);
                _heartbeat.Play();
            }
        }

        void Update()
        {
            // Freeze player agency while the bullet cam runs the show.
            if (BulletCam.Instance != null && BulletCam.Instance.IsPlaying) return;
            // ...and while any menu (dossier, results, garage, pause, fail) is up.
            if (_ui != null && _ui.MenusOpen) return;

            HandleScope();
            HandleBreath();

            if (FireInput() && !_busy)
                Shoot();
            if (ReloadInput() && !_busy && Ammo < clipSize)
                StartCoroutine(Reload());
        }

        // --- Input aggregation (desktop + mobile buttons) ------------------

        bool FireInput()
        {
            bool desktop = Input.GetMouseButtonDown(0);
            bool mobile = _ui != null && _ui.ConsumeFire();
            return desktop || mobile;
        }

        bool ScopeInput()
        {
            bool desktop = Input.GetMouseButton(1);
            bool mobile = _ui != null && _ui.MobileScopeOn;
            return desktop || mobile;
        }

        bool BreathInput()
        {
            bool desktop = Input.GetKey(KeyCode.LeftShift);
            bool mobile = _ui != null && _ui.MobileBreathHeld;
            return IsScoped && (desktop || mobile);
        }

        bool ReloadInput() => Input.GetKeyDown(KeyCode.R);

        // --- Scope ----------------------------------------------------------

        void HandleScope()
        {
            IsScoped = ScopeInput();
            _targetFOV = IsScoped ? hipFOV / scopeMultiplier : hipFOV;
            if (cam != null)
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, _targetFOV, Time.deltaTime * aimSpeed);

            // Finer aim while scoped.
            _look.sensitivityScale = IsScoped ? 0.28f : 1f;

            // Looking down the scope — hide the rifle body so the view is clean.
            if (_rifle != null) _rifle.SetActive(!IsScoped);

            // Muffle the outside world while scoped.
            if (_lowPass != null)
            {
                float target = IsScoped ? 1100f : 22000f;
                _lowPass.cutoffFrequency = Mathf.Lerp(_lowPass.cutoffFrequency, target, Time.deltaTime * 8f);
            }

            if (_ui != null) _ui.SetScoped(IsScoped);
        }

        // --- Breath hold ----------------------------------------------------

        void HandleBreath()
        {
            bool holding = BreathInput();
            float baseSway = (IsScoped ? scopedSway : hipSway) * _stabilityMul;

            if (holding)
            {
                _breathHeldFor += Time.deltaTime;
                if (_breathHeldFor <= breathHoldMax)
                {
                    // Steady: sway shrinks the longer you hold, heartbeat climbs.
                    float steady = 1f - (_breathHeldFor / breathHoldMax) * 0.85f;
                    _look.swayAmount = baseSway * steady;
                }
                else
                {
                    // Held too long — lungs give out, aim spikes.
                    float overshoot = _breathHeldFor - breathHoldMax;
                    _look.swayAmount = baseSway * (1f + overshoot * 2f);
                }
                if (_heartbeat != null)
                {
                    _heartbeat.volume = Mathf.Lerp(_heartbeat.volume, 0.6f, Time.deltaTime * 3f);
                    // Tempo climbs ~55 -> ~90 BPM the longer breath is held.
                    float f = Mathf.Clamp01(_breathHeldFor / breathHoldMax);
                    _heartbeat.pitch = Mathf.Lerp(0.9f, 1.5f, f);
                }
            }
            else
            {
                _breathHeldFor = Mathf.Max(0f, _breathHeldFor - Time.deltaTime * 2f);
                _look.swayAmount = baseSway;
                if (_heartbeat != null)
                {
                    _heartbeat.volume = Mathf.Lerp(_heartbeat.volume, IsScoped ? 0.12f : 0f, Time.deltaTime * 3f);
                    _heartbeat.pitch = Mathf.Lerp(_heartbeat.pitch, 0.9f, Time.deltaTime * 3f);
                }
            }
        }

        // --- Firing ---------------------------------------------------------

        void Shoot()
        {
            if (Ammo <= 0) { StartCoroutine(Reload()); return; }
            Ammo--;
            if (_ui != null) _ui.SetAmmo(Ammo, clipSize);

            Vector3 muzzle = cam.transform.position;
            if (ProceduralAudio.Instance != null) ProceduralAudio.Instance.PlayShot(muzzle);
            StartCoroutine(FlashMuzzle());
            _look.AddRecoil(recoilPitch, Random.Range(-recoilYaw, recoilYaw));

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 endPoint = ray.origin + ray.direction * range;
            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, range);
            if (hitSomething) endPoint = hit.point;

            ShowTracer(muzzle, endPoint);

            Target target = null;
            bool headshot = false;
            if (hitSomething)
            {
                var head = hit.collider.GetComponent<TargetHead>();
                if (head != null) { target = head.owner; headshot = true; }
                else target = hit.collider.GetComponentInParent<Target>();
            }

            bool killed = target != null && !target.IsDown;
            if (GameManager.Instance != null) GameManager.Instance.RegisterShot(killed);

            if (killed)
            {
                float distance = hit.distance;
                bool isFinal = GameManager.Instance != null && GameManager.Instance.IsFinalTarget;

                int points = target.TakeHit(hit.point, ray.direction, headshot);
                points = Mathf.RoundToInt(points * _scoreMul);
                if (GameManager.Instance != null)
                    GameManager.Instance.RegisterKill(points, headshot, distance);

                if (BulletCam.Instance != null && BulletCam.Instance.ShouldTrigger(isFinal, headshot, distance))
                    BulletCam.Instance.Play(muzzle, hit.point, headshot);
            }

            // Cycle the bolt.
            StartCoroutine(CycleBolt());
        }

        IEnumerator CycleBolt()
        {
            _busy = true;
            yield return new WaitForSeconds(boltTime);
            if (ProceduralAudio.Instance != null)
                ProceduralAudio.Instance.PlayBolt(cam.transform.position);
            _busy = false;
            if (Ammo <= 0) StartCoroutine(Reload());
        }

        IEnumerator Reload()
        {
            if (_busy && Ammo > 0) yield break;
            _busy = true;
            if (_ui != null) _ui.ShowReloading(true);
            if (ProceduralAudio.Instance != null)
                ProceduralAudio.Instance.PlayReload(cam.transform.position);
            yield return new WaitForSeconds(reloadTime);
            Ammo = clipSize;
            if (_ui != null) { _ui.SetAmmo(Ammo, clipSize); _ui.ShowReloading(false); }
            _busy = false;
        }

        // --- Cosmetics ------------------------------------------------------

        // A first-person bolt-action rifle assembled from primitives, held at
        // the lower-right of view. No colliders (the centre-screen ray must
        // ignore it). Hidden while scoped.
        void BuildRifle()
        {
            _rifle = new GameObject("Rifle");
            _rifle.transform.SetParent(transform, false);
            _rifle.transform.localPosition = new Vector3(0.17f, -0.19f, 0.28f);
            _rifle.transform.localRotation = Quaternion.Euler(0f, -3f, 0f);

            Material metal = RifleMaterial(new Color(0.09f, 0.10f, 0.12f));
            Material wood = RifleMaterial(new Color(0.20f, 0.13f, 0.07f));

            RiflePart(PrimitiveType.Cube, new Vector3(0f, 0f, 0.05f), new Vector3(0.06f, 0.08f, 0.75f), Vector3.zero, metal);        // receiver
            RiflePart(PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0.6f), new Vector3(0.03f, 0.32f, 0.03f), new Vector3(90f, 0, 0), metal); // barrel
            RiflePart(PrimitiveType.Cylinder, new Vector3(0f, 0.09f, 0.1f), new Vector3(0.038f, 0.16f, 0.038f), new Vector3(90f, 0, 0), metal); // scope tube
            RiflePart(PrimitiveType.Cube, new Vector3(0f, -0.03f, -0.42f), new Vector3(0.05f, 0.10f, 0.30f), new Vector3(6f, 0, 0), wood);  // stock
            RiflePart(PrimitiveType.Cube, new Vector3(0f, -0.11f, 0.0f), new Vector3(0.04f, 0.13f, 0.09f), new Vector3(-10f, 0, 0), metal); // magazine
        }

        void RiflePart(PrimitiveType type, Vector3 localPos, Vector3 localScale, Vector3 euler, Material mat)
        {
            var part = GameObject.CreatePrimitive(type);
            var col = part.GetComponent<Collider>();
            if (col != null) Destroy(col);
            part.transform.SetParent(_rifle.transform, false);
            part.transform.localPosition = localPos;
            part.transform.localScale = localScale;
            part.transform.localRotation = Quaternion.Euler(euler);
            var mr = part.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }

        static Material RifleMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return new Material(shader) { color = color };
        }

        void BuildMuzzleFlash()
        {
            var go = new GameObject("MuzzleFlash");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0.17f, -0.17f, 0.78f);
            _muzzleFlash = go.AddComponent<Light>();
            _muzzleFlash.type = LightType.Point;
            _muzzleFlash.color = new Color(1f, 0.75f, 0.35f);
            _muzzleFlash.range = 12f;
            _muzzleFlash.intensity = 0f;
        }

        IEnumerator FlashMuzzle()
        {
            if (_muzzleFlash == null) yield break;
            _muzzleFlash.intensity = 6f;
            float t = 0f;
            while (t < 0.06f)
            {
                t += Time.deltaTime;
                _muzzleFlash.intensity = Mathf.Lerp(6f, 0f, t / 0.06f);
                yield return null;
            }
            _muzzleFlash.intensity = 0f;
        }

        void BuildTracer()
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);
            _tracer = go.AddComponent<LineRenderer>();
            _tracer.positionCount = 2;
            _tracer.startWidth = 0.03f;
            _tracer.endWidth = 0.01f;
            _tracer.useWorldSpace = true;
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Standard");
            _tracer.material = new Material(s) { color = new Color(1f, 0.7f, 0.25f) };
            _tracer.enabled = false;
        }

        void ShowTracer(Vector3 a, Vector3 b)
        {
            if (_tracer == null) return;
            _tracer.SetPosition(0, a);
            _tracer.SetPosition(1, b);
            StartCoroutine(TracerFlash());
        }

        IEnumerator TracerFlash()
        {
            _tracer.enabled = true;
            yield return new WaitForSeconds(0.05f);
            _tracer.enabled = false;
        }
    }
}
