using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// The only thing you add to a scene. On <c>Start</c> it builds the entire
    /// game from code — camera + player rig, cinematic lighting and fog, a
    /// procedural rooftop-and-skyline environment, all managers (audio, UI,
    /// spawner, game flow, bullet cam) — wires them together and starts the
    /// campaign. Zero prefabs, zero Inspector wiring, zero downloaded assets.
    ///
    /// Setup: GameObject → Create Empty → Add Component → Game Bootstrap → Play.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Player placement")]
        public Vector3 nestPosition = new Vector3(0f, 12f, -6f);
        public float lookPitch = 6f;

        [Header("Atmosphere")]
        public Color skyColor = new Color(0.05f, 0.07f, 0.11f);
        public Color fogColor = new Color(0.10f, 0.13f, 0.18f);
        public Color sunColor = new Color(1f, 0.82f, 0.6f);

        Material _concrete;
        Material _building;

        void Start()
        {
            VerifyInputBackend();

            _concrete = Mat(new Color(0.12f, 0.13f, 0.15f));
            _building = Mat(new Color(0.08f, 0.09f, 0.12f));

            SetupRenderSettings();
            SetupLighting();
            BuildEnvironment();

            ProceduralAudio audio = new GameObject("ProceduralAudio").AddComponent<ProceduralAudio>();
            UIManager ui = new GameObject("UIManager").AddComponent<UIManager>();
            new GameObject("BulletCam").AddComponent<BulletCam>();

            TargetSpawner spawner = new GameObject("TargetSpawner").AddComponent<TargetSpawner>();
            spawner.transform.position = Vector3.zero; // targets spawn out in +Z

            Camera cam = BuildPlayer(ui);

            GameManager gm = new GameObject("GameManager").AddComponent<GameManager>();
            gm.spawner = spawner;
            gm.ui = ui;

            // Ambient wind beds placed around the nest for positional "8D" audio.
            audio.SpawnWindBed(nestPosition + Vector3.left * 8f, 0.4f);
            audio.SpawnWindBed(nestPosition + Vector3.right * 8f, 0.4f);
            audio.SpawnWindBed(nestPosition + Vector3.forward * 30f, 0.5f);
            audio.SpawnWindBed(nestPosition - Vector3.forward * 6f, 0.3f);

            gm.BeginGame();
        }

        Camera BuildPlayer(UIManager ui)
        {
            var go = new GameObject("Player");
            go.transform.position = nestPosition;
            go.transform.rotation = Quaternion.Euler(lookPitch, 0f, 0f);

            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1200f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = skyColor;
            cam.allowHDR = true;   // let emissive muzzle/tracer/round read bright
            cam.allowMSAA = true;
            cam.tag = "MainCamera";
            QualitySettings.antiAliasing = 4;

            go.AddComponent<AudioListener>();
            go.AddComponent<MouseLook>();
            var weapon = go.AddComponent<WeaponController>();
            weapon.cam = cam;
            weapon.Init(ui);

            return cam;
        }

        void SetupRenderSettings()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogStartDistance = 40f;
            RenderSettings.fogEndDistance = 320f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.22f);
        }

        void SetupLighting()
        {
            var go = new GameObject("Sun");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = sunColor;
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            // Low golden-hour angle raking across the city.
            go.transform.rotation = Quaternion.Euler(18f, 40f, 0f);
        }

        void BuildEnvironment()
        {
            // Ground where targets stand.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, 0f, 40f);
            ground.transform.localScale = new Vector3(30f, 1f, 30f);
            Paint(ground, _concrete);

            // The sniper's rooftop nest.
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Nest";
            roof.transform.position = new Vector3(0f, nestPosition.y - 1.6f, nestPosition.z - 1f);
            roof.transform.localScale = new Vector3(10f, 1f, 8f);
            Paint(roof, _building);
            // A low parapet to lean over.
            var parapet = GameObject.CreatePrimitive(PrimitiveType.Cube);
            parapet.transform.position = new Vector3(0f, nestPosition.y - 0.9f, nestPosition.z + 2.6f);
            parapet.transform.localScale = new Vector3(10f, 1f, 0.4f);
            Paint(parapet, _building);

            // Procedural skyline: rows of buildings of varied height/width.
            var rng = new System.Random(8);
            for (int i = 0; i < 60; i++)
            {
                float x = (float)(rng.NextDouble() * 120.0 - 60.0);
                float z = (float)(rng.NextDouble() * 130.0 + 20.0);
                // Keep the direct firing lane clearer.
                if (Mathf.Abs(x) < 18f && z < 95f) continue;

                float h = (float)(rng.NextDouble() * 40.0 + 8.0);
                float w = (float)(rng.NextDouble() * 8.0 + 5.0);
                float d = (float)(rng.NextDouble() * 8.0 + 5.0);
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = "Building";
                b.transform.position = new Vector3(x, h * 0.5f, z);
                b.transform.localScale = new Vector3(w, h, d);
                Paint(b, _building);
            }
        }

        // The scripts use the classic UnityEngine.Input API. If the project is
        // set to "Input System (New)" only, those calls throw at runtime — so we
        // probe once and print a clear, actionable message instead of a silent
        // dead game.
        void VerifyInputBackend()
        {
            try
            {
                Input.GetKeyDown(KeyCode.None);
            }
            catch (System.Exception)
            {
                Debug.LogError(
                    "[SuperSniper8D] Legacy input is disabled, so nothing will respond. " +
                    "Fix: Edit > Project Settings > Player > Other Settings > Active Input " +
                    "Handling → set to 'Both' (or 'Input Manager (Old)'), then restart the editor.");
            }
        }

        void Paint(GameObject go, Material mat)
        {
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }

        static Material Mat(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return new Material(shader) { color = color };
        }
    }
}
