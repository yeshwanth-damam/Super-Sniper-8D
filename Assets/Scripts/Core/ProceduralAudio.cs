using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// Every sound in the game is synthesised at runtime — no audio files.
    /// The gunshot, the bolt cycle, the impact "ding", the reload and the wind
    /// beds are all built as <see cref="AudioClip"/>s from raw samples, so the
    /// project ships with zero downloaded assets.
    ///
    /// Playback of world sounds uses <see cref="AudioSource.PlayClipAtPoint"/>
    /// with full 3D spatial blend — that is where the "8D" positional feel comes
    /// from. Add Steam Audio later and these sources render binaurally in
    /// headphones for the full effect.
    /// </summary>
    public class ProceduralAudio : MonoBehaviour
    {
        public static ProceduralAudio Instance { get; private set; }

        const int SampleRate = 44100;

        AudioClip _shot;
        AudioClip _bolt;
        AudioClip _hit;
        AudioClip _headHit;
        AudioClip _reload;
        AudioClip _wind;
        AudioClip _heartbeat;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _shot = BuildShot();
            _bolt = BuildBolt();
            _hit = BuildImpact(880f, 0.18f);
            _headHit = BuildImpact(1400f, 0.22f);
            _reload = BuildReload();
            _wind = BuildWind();
            _heartbeat = BuildHeartbeat();
        }

        // --- Public playback helpers ---------------------------------------

        public void PlayShot(Vector3 pos) => Play(_shot, pos, 1f);
        public void PlayBolt(Vector3 pos) => Play(_bolt, pos, 0.7f);
        public void PlayReload(Vector3 pos) => Play(_reload, pos, 0.8f);
        public void PlayHit(Vector3 pos, bool headshot) => Play(headshot ? _headHit : _hit, pos, 1f);

        /// <summary>Places a looping, positional wind bed at a world point.</summary>
        public AudioSource SpawnWindBed(Vector3 pos, float volume)
        {
            var go = new GameObject("WindBed");
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = _wind;
            src.loop = true;
            src.spatialBlend = 1f;      // fully 3D
            src.volume = volume;
            src.minDistance = 4f;
            src.maxDistance = 120f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.Play();
            return src;
        }

        /// <summary>Non-positional heartbeat used by the scope/breath system.</summary>
        public AudioSource CreateHeartbeatSource()
        {
            var go = new GameObject("Heartbeat");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = _heartbeat;
            src.loop = true;
            src.spatialBlend = 0f;      // in the player's head, literally
            src.volume = 0f;
            src.playOnAwake = false;
            return src;
        }

        void Play(AudioClip clip, Vector3 pos, float volume)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, pos, volume);
        }

        // --- Synthesis ------------------------------------------------------

        // A sniper crack: a sharp noise burst with a low thump and fast decay.
        AudioClip BuildShot()
        {
            float dur = 0.5f;
            int n = (int)(SampleRate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 22f);
                float crack = (Random.value * 2f - 1f) * env;
                float thump = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 9f);
                data[i] = Mathf.Clamp(crack * 0.7f + thump * 0.6f, -1f, 1f);
            }
            return Make("Shot", data);
        }

        // Bolt cycle: two short metallic clicks.
        AudioClip BuildBolt()
        {
            float dur = 0.35f;
            int n = (int)(SampleRate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float click1 = Mathf.Exp(-Mathf.Abs(t - 0.02f) * 400f) * (Random.value * 2f - 1f);
                float click2 = Mathf.Exp(-Mathf.Abs(t - 0.20f) * 400f) * (Random.value * 2f - 1f);
                data[i] = Mathf.Clamp((click1 + click2) * 0.8f, -1f, 1f);
            }
            return Make("Bolt", data);
        }

        // Impact ding: a decaying tone (higher pitch = headshot).
        AudioClip BuildImpact(float freq, float dur)
        {
            int n = (int)(SampleRate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t * 30f);
                float body = Mathf.Sin(2f * Mathf.PI * freq * t);
                float overtone = 0.4f * Mathf.Sin(2f * Mathf.PI * freq * 2.5f * t);
                data[i] = (body + overtone) * env * 0.7f;
            }
            return Make("Impact", data);
        }

        AudioClip BuildReload()
        {
            float dur = 0.6f;
            int n = (int)(SampleRate * dur);
            var data = new float[n];
            float[] times = { 0.05f, 0.25f, 0.45f };
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float s = 0f;
                foreach (float ct in times)
                    s += Mathf.Exp(-Mathf.Abs(t - ct) * 300f) * (Random.value * 2f - 1f);
                data[i] = Mathf.Clamp(s * 0.6f, -1f, 1f);
            }
            return Make("Reload", data);
        }

        // A 2-second loopable wind bed: filtered noise slowly modulated.
        AudioClip BuildWind()
        {
            float dur = 2f;
            int n = (int)(SampleRate * dur);
            var data = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float noise = Random.value * 2f - 1f;
                prev = Mathf.Lerp(prev, noise, 0.02f); // low-pass -> airy
                float gust = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 0.3f * t);
                data[i] = prev * gust * 0.35f;
            }
            // Crossfade the seam so the loop is clickless.
            SmoothLoopSeam(data, 2000);
            return Make("Wind", data, true);
        }

        // A ~1s heartbeat loop: two low thuds ("lub-dub").
        AudioClip BuildHeartbeat()
        {
            float dur = 1f;
            int n = (int)(SampleRate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float lub = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-Mathf.Abs(t - 0.10f) * 30f);
                float dub = Mathf.Sin(2f * Mathf.PI * 45f * t) * Mathf.Exp(-Mathf.Abs(t - 0.32f) * 30f);
                data[i] = Mathf.Clamp((lub + dub) * 0.9f, -1f, 1f);
            }
            return Make("Heartbeat", data, true);
        }

        void SmoothLoopSeam(float[] data, int fade)
        {
            int n = data.Length;
            fade = Mathf.Min(fade, n / 2);
            for (int i = 0; i < fade; i++)
            {
                float k = (float)i / fade;
                data[i] = Mathf.Lerp(data[n - fade + i], data[i], k);
            }
        }

        AudioClip Make(string name, float[] data, bool loop = false)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
