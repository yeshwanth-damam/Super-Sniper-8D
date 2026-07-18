using System;
using System.Collections;
using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// Owns the whole game flow: level definitions, score, timer, and the
    /// win/lose state machine. Everything else (spawner, UI, weapon) talks to
    /// this singleton. No Inspector wiring required — <see cref="GameBootstrap"/>
    /// creates and connects it at runtime.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>A single mission/level, authored as data.</summary>
        [Serializable]
        public struct LevelConfig
        {
            public string missionName;   // Shown on the dossier + HUD
            public string intel;         // One line of flavour intel
            public int targetCount;      // Total targets to eliminate
            public int moverCount;       // How many of them patrol
            public float moverSpeed;     // Patrol speed for this level
            public float timeLimit;      // Seconds before the mission fails
        }

        [Header("Levels")]
        public LevelConfig[] levels = new LevelConfig[]
        {
            new LevelConfig
            {
                missionName = "ROOFTOP OVERWATCH",
                intel = "TARGET: arms courier. WINDOW: 60s. COLLATERAL: zero.",
                targetCount = 3, moverCount = 0, moverSpeed = 0f, timeLimit = 60f
            },
            new LevelConfig
            {
                missionName = "MARKET DRIFT",
                intel = "Two runners on the move. Lead your shots.",
                targetCount = 5, moverCount = 2, moverSpeed = 1.6f, timeLimit = 60f
            },
            new LevelConfig
            {
                missionName = "LAST LIGHT",
                intel = "Full cell scattering. Clear the block before dark.",
                targetCount = 7, moverCount = 5, moverSpeed = 2.4f, timeLimit = 75f
            },
        };

        // Runtime state ------------------------------------------------------
        public int Score { get; private set; }
        public int LevelIndex { get; private set; }
        public int TargetsRemaining { get; private set; }
        public int TargetsEliminated { get; private set; }
        public bool MissionActive { get; private set; }

        public LevelConfig CurrentLevel => levels[Mathf.Clamp(LevelIndex, 0, levels.Length - 1)];

        // Accuracy tracking for the results screen.
        int _shotsFired;
        int _shotsHit;
        int _headshots;
        float _bestDistance;
        float _timeRemaining;

        // Collaborators (assigned by the bootstrap).
        public TargetSpawner spawner;
        public UIManager ui;

        public event Action<int> OnScoreChanged;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void BeginGame()
        {
            Score = 0;
            LevelIndex = 0;
            StartCoroutine(StartLevelRoutine(0));
        }

        IEnumerator StartLevelRoutine(int index)
        {
            LevelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
            LevelConfig cfg = CurrentLevel;

            MissionActive = false;
            TargetsEliminated = 0;
            _timeRemaining = cfg.timeLimit;

            // Show the cinematic dossier before the mission goes live.
            if (ui != null)
                yield return ui.PlayDossier(LevelIndex + 1, cfg.missionName, cfg.intel);

            TargetsRemaining = spawner != null ? spawner.BuildLevel(cfg) : cfg.targetCount;

            MissionActive = true;
            if (ui != null) ui.SetMission(cfg.missionName, TargetsEliminated, cfg.targetCount);
        }

        bool _paused;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (_paused) return;
            if (!MissionActive) return;

            // Freeze the clock while the bullet cam runs the show.
            if (BulletCam.Instance != null && BulletCam.Instance.IsPlaying) return;

            _timeRemaining -= Time.deltaTime;
            if (ui != null) ui.SetTimer(Mathf.Max(0f, _timeRemaining));

            if (_timeRemaining <= 0f)
                FailMission();
        }

        /// <summary>Esc toggles a pause menu during an active mission.</summary>
        public void TogglePause()
        {
            if (BulletCam.Instance != null && BulletCam.Instance.IsPlaying) return;
            if (!_paused && !MissionActive) return; // nothing to pause on menus

            _paused = !_paused;
            Time.timeScale = _paused ? 0f : 1f;
            if (ui != null) { if (_paused) ui.ShowPause(); else ui.HidePause(); }
        }

        public void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Called by the weapon whenever the trigger is pulled.</summary>
        public void RegisterShot(bool hit)
        {
            _shotsFired++;
            if (hit) _shotsHit++;
        }

        /// <summary>Called when a target is confirmed down.</summary>
        public void RegisterKill(int points, bool headshot, float distance)
        {
            Score += points;
            TargetsEliminated++;
            TargetsRemaining = Mathf.Max(0, TargetsRemaining - 1);
            if (headshot) _headshots++;
            if (distance > _bestDistance) _bestDistance = distance;

            OnScoreChanged?.Invoke(Score);
            if (ui != null)
            {
                ui.SetScore(Score);
                ui.SetMission(CurrentLevel.missionName, TargetsEliminated, CurrentLevel.targetCount);
            }

            if (TargetsRemaining <= 0)
                StartCoroutine(CompleteLevelRoutine());
        }

        /// <summary>True when only one target is left — used to trigger the bullet cam.</summary>
        public bool IsFinalTarget => MissionActive && TargetsRemaining <= 1;

        IEnumerator CompleteLevelRoutine()
        {
            MissionActive = false;
            // Let the final bullet cam finish, then let the knockback breathe.
            yield return null;
            while (BulletCam.Instance != null && BulletCam.Instance.IsPlaying)
                yield return null;
            yield return new WaitForSecondsRealtime(1.4f);

            bool lastLevel = LevelIndex >= levels.Length - 1;
            if (ui != null)
            {
                yield return ui.ShowResults(
                    levelCleared: LevelIndex + 1,
                    score: Score,
                    accuracy: _shotsFired > 0 ? (float)_shotsHit / _shotsFired : 0f,
                    headshots: _headshots,
                    bestDistance: _bestDistance,
                    isFinalLevel: lastLevel);
            }

            if (lastLevel)
            {
                // Campaign complete — the UI results screen offers a restart.
                yield break;
            }

            StartCoroutine(StartLevelRoutine(LevelIndex + 1));
        }

        void FailMission()
        {
            MissionActive = false;
            if (ui != null) ui.ShowFailed(CurrentLevel.missionName);
        }

        /// <summary>Reload the whole run from level one. Hooked to UI buttons.</summary>
        public void RestartCampaign()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Retry just the current level. Hooked to the fail panel.</summary>
        public void RetryLevel()
        {
            if (ui != null) ui.HidePanels();
            StartCoroutine(StartLevelRoutine(LevelIndex));
        }
    }
}
