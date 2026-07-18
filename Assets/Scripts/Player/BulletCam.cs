using System.Collections;
using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// The signature moment. On the final kill of a mission — or any headshot
    /// past 200 m — time collapses, a dedicated camera detaches and chases the
    /// round along its path with a slow roll, the soundscape drops to a whoosh
    /// and a heartbeat, impact lands on a single white frame, and sound crashes
    /// back in. Three to four seconds, skippable after the first viewing.
    ///
    /// Implemented with coupled <c>Time.timeScale</c> / <c>fixedDeltaTime</c>
    /// dilation and a second camera lerped along the raycast path.
    /// </summary>
    public class BulletCam : MonoBehaviour
    {
        public static BulletCam Instance { get; private set; }

        [Header("Timing")]
        public float slowMoScale = 0.15f;
        public float travelSeconds = 2.6f;   // real-time length of the fly-along
        public float longShotThreshold = 200f;

        public bool IsPlaying { get; private set; }

        Camera _mainCam;
        Camera _bulletCam;
        Transform _bullet;
        TrailRenderer _trail;
        float _defaultFixedDelta;
        static bool _hasPlayedOnce;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _defaultFixedDelta = Time.fixedDeltaTime;
        }

        void EnsureRig()
        {
            if (_bulletCam != null) return;

            var camGo = new GameObject("BulletCam");
            camGo.transform.SetParent(transform, false);
            _bulletCam = camGo.AddComponent<Camera>();
            _bulletCam.enabled = false;

            // The visible round: a small emissive sphere with a trail.
            var bulletGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulletGo.name = "Round";
            Destroy(bulletGo.GetComponent<Collider>());
            bulletGo.transform.SetParent(transform, false);
            bulletGo.transform.localScale = Vector3.one * 0.12f;
            var mr = bulletGo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MakeAmberMaterial();
            _bullet = bulletGo.transform;

            _trail = bulletGo.AddComponent<TrailRenderer>();
            _trail.time = 0.35f;
            _trail.startWidth = 0.14f;
            _trail.endWidth = 0.0f;
            _trail.material = MakeAmberMaterial();
            _trail.startColor = new Color(1f, 0.75f, 0.25f, 0.9f);
            _trail.endColor = new Color(1f, 0.6f, 0.15f, 0f);

            bulletGo.SetActive(false);
        }

        public bool ShouldTrigger(bool isFinalTarget, bool headshot, float distance)
        {
            return isFinalTarget || (headshot && distance >= longShotThreshold);
        }

        public void Play(Vector3 origin, Vector3 hitPoint, bool headshot)
        {
            if (IsPlaying) return;
            EnsureRig();
            StartCoroutine(Sequence(origin, hitPoint, headshot));
        }

        IEnumerator Sequence(Vector3 origin, Vector3 hitPoint, bool headshot)
        {
            IsPlaying = true;
            _mainCam = Camera.main;

            // --- Enter slow motion, coupling the physics step. ---
            Time.timeScale = slowMoScale;
            Time.fixedDeltaTime = _defaultFixedDelta * slowMoScale;

            // --- Hand rendering to the bullet camera. ---
            AudioListener listenerToRestore = null;
            if (_mainCam != null)
            {
                _mainCam.enabled = false;
                var mainListener = _mainCam.GetComponent<AudioListener>();
                if (mainListener != null && mainListener.enabled)
                {
                    listenerToRestore = mainListener; // keep it; positional audio still works
                }
            }
            if (_bulletCam.GetComponent<AudioListener>() == null && listenerToRestore == null)
                _bulletCam.gameObject.AddComponent<AudioListener>();
            _bulletCam.enabled = true;
            _bulletCam.fieldOfView = 40f;

            // --- Drop the world's sound to a whisper. ---
            float restoreVolume = AudioListener.volume;
            AudioListener.volume = 0.25f;

            // --- Configure the round + trail. ---
            _bullet.gameObject.SetActive(true);
            _trail.Clear();
            Vector3 dir = (hitPoint - origin).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;

            var ui = GameManager.Instance != null ? GameManager.Instance.ui : null;
            if (ui != null) ui.SetLetterbox(true);

            // Scale the fly-along to the shot distance so near and far kills
            // both read well, and seat the camera before the first frame so it
            // doesn't snap in from its previous position.
            float dist = Vector3.Distance(origin, hitPoint);
            float duration = Mathf.Clamp(dist / 90f, 1.2f, travelSeconds);
            Vector3 startSide = Vector3.Cross(dir, Vector3.up).normalized;
            _bulletCam.transform.position = origin - dir * 3.2f + startSide * 1.1f + Vector3.up * 0.5f;
            _bulletCam.transform.rotation = Quaternion.LookRotation(origin - _bulletCam.transform.position);

            bool skippable = _hasPlayedOnce;
            float elapsed = 0f;
            float roll = 0f;

            while (elapsed < duration)
            {
                float u = elapsed / duration;
                Vector3 bulletPos = Vector3.Lerp(origin, hitPoint, u);
                _bullet.position = bulletPos;

                // Chase camera: sits behind and to the side with lag + slow roll.
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
                Vector3 camTarget = bulletPos - dir * 3.2f + side * 1.1f + Vector3.up * 0.5f;
                _bulletCam.transform.position = Vector3.Lerp(
                    _bulletCam.transform.position, camTarget, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
                roll += Time.unscaledDeltaTime * 6f;
                _bulletCam.transform.rotation = Quaternion.LookRotation(bulletPos - _bulletCam.transform.position)
                                                * Quaternion.Euler(0f, 0f, Mathf.Sin(roll * 0.2f) * 4f);

                if (skippable && FirePressed())
                    break;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            // --- Impact: a single white frame, then sound crashes back. ---
            _bullet.position = hitPoint;
            if (ui != null) ui.FlashWhite();
            AudioListener.volume = restoreVolume;
            if (ProceduralAudio.Instance != null)
                ProceduralAudio.Instance.PlayHit(hitPoint, headshot);

            yield return new WaitForSecondsRealtime(0.4f);

            // --- Restore everything. ---
            _bullet.gameObject.SetActive(false);
            _bulletCam.enabled = false;
            if (_mainCam != null) _mainCam.enabled = true;
            if (ui != null) ui.SetLetterbox(false);

            Time.timeScale = 1f;
            Time.fixedDeltaTime = _defaultFixedDelta;

            _hasPlayedOnce = true;
            IsPlaying = false;
        }

        static bool FirePressed()
        {
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)
                   || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        }

        static Material MakeAmberMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader) { color = new Color(1f, 0.72f, 0.22f) };
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.15f) * 2f);
            }
            return mat;
        }
    }
}
