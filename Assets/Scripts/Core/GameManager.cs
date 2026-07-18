using System.Collections;
using UnityEngine;

namespace SuperSniper8D
{
    /// <summary>
    /// The whole game state machine: authored campaign data, the home / mission
    /// select navigation, per-mission score/timer/flow, credits, upgrades and
    /// save. Everything else talks to this singleton; <see cref="GameBootstrap"/>
    /// creates and wires it at runtime with no Inspector setup.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>Runtime spawn parameters handed to the <see cref="TargetSpawner"/>.</summary>
        public struct LevelConfig
        {
            public string missionName;
            public string intel;
            public int targetCount;
            public int moverCount;
            public float moverSpeed;
            public float timeLimit;
        }

        // Collaborators (assigned by the bootstrap).
        public TargetSpawner spawner;
        public UIManager ui;
        public WeaponController weapon;

        public SaveData Profile { get; private set; }

        RegionDef[] _regions;
        int _curRegion;
        int _curMission;

        // Runtime mission state.
        public int Score { get; private set; }
        public int TargetsRemaining { get; private set; }
        public int TargetsEliminated { get; private set; }
        public bool MissionActive { get; private set; }

        int _shotsFired, _shotsHit, _headshots;
        float _bestDistance, _timeRemaining;
        bool _paused;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _regions = Campaign.Build();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------
        //  Campaign data accessors (read by the UI)
        // ------------------------------------------------------------------

        public int RegionCount => _regions.Length;
        public string RegionName(int r) => _regions[r].name;
        public string RegionTagline(int r) => _regions[r].tagline;
        public int MissionCount(int r) => _regions[r].missions.Length;
        public MissionDef Mission(int r, int m) => _regions[r].missions[m];
        public int CurrentRegion => _curRegion;

        public int GlobalIndex(int r, int m)
        {
            int idx = 0;
            for (int i = 0; i < r; i++) idx += _regions[i].missions.Length;
            return idx + m;
        }

        public int TotalMissions()
        {
            int n = 0;
            foreach (var reg in _regions) n += reg.missions.Length;
            return n;
        }

        public bool IsMissionCompleted(int r, int m) =>
            Profile != null && GlobalIndex(r, m) <= Profile.highestCompleted;

        public bool IsMissionUnlocked(int r, int m) =>
            Profile != null && GlobalIndex(r, m) <= Profile.highestCompleted + 1;

        public bool IsRegionUnlocked(int r) => IsMissionUnlocked(r, 0);

        // ------------------------------------------------------------------
        //  Navigation (called from the bootstrap and UI buttons)
        // ------------------------------------------------------------------

        public void BeginGame()
        {
            Profile = SaveSystem.Load();
            if (weapon != null) weapon.ApplyUpgrades(Profile);
            GoHome();
        }

        public void GoHome()
        {
            MissionActive = false;
            Time.timeScale = 1f;
            if (spawner != null) spawner.ClearAll(); // no leftover targets behind menus
            if (ui != null) ui.ShowHome();
        }

        public void SelectRegion(int r)
        {
            if (!IsRegionUnlocked(r)) return;
            _curRegion = r;
            if (ui != null) ui.ShowMissionSelect(r);
        }

        public void SelectMission(int r, int m)
        {
            if (!IsMissionUnlocked(r, m)) return;
            _curRegion = r;
            _curMission = m;
            StartCoroutine(StartMissionRoutine(r, m));
        }

        public void BackToSelect()
        {
            MissionActive = false;
            if (spawner != null) spawner.ClearAll();
            if (ui != null) ui.ShowMissionSelect(_curRegion);
        }

        MissionDef CurMission => _regions[_curRegion].missions[_curMission];

        LevelConfig ToConfig(MissionDef d) => new LevelConfig
        {
            missionName = d.name,
            intel = d.intel,
            targetCount = d.targets,
            moverCount = d.movers,
            moverSpeed = d.moverSpeed,
            timeLimit = d.timeLimit,
        };

        IEnumerator StartMissionRoutine(int r, int m)
        {
            MissionDef def = _regions[r].missions[m];

            MissionActive = false;
            Score = 0;
            TargetsEliminated = 0;
            _shotsFired = 0;
            _shotsHit = 0;
            _headshots = 0;
            _bestDistance = 0f;
            _timeRemaining = def.timeLimit;

            if (ui != null)
                yield return ui.PlayDossier(GlobalIndex(r, m) + 1, def.name, def.intel);

            TargetsRemaining = spawner != null ? spawner.BuildLevel(ToConfig(def)) : def.targets;

            MissionActive = true;
            if (ui != null)
            {
                ui.SetScore(Score);
                ui.SetMission(def.name, TargetsEliminated, def.targets);
            }
        }

        // ------------------------------------------------------------------
        //  Mission runtime
        // ------------------------------------------------------------------

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
            if (_paused) return;
            if (!MissionActive) return;
            if (BulletCam.Instance != null && BulletCam.Instance.IsPlaying) return;

            _timeRemaining -= Time.deltaTime;
            if (ui != null) ui.SetTimer(Mathf.Max(0f, _timeRemaining));
            if (_timeRemaining <= 0f) FailMission();
        }

        public void RegisterShot(bool hit)
        {
            _shotsFired++;
            if (hit) _shotsHit++;
        }

        public void RegisterKill(int points, bool headshot, float distance)
        {
            Score += points;
            TargetsEliminated++;
            TargetsRemaining = Mathf.Max(0, TargetsRemaining - 1);
            if (headshot) _headshots++;
            if (distance > _bestDistance) _bestDistance = distance;

            if (ui != null)
            {
                ui.SetScore(Score);
                ui.SetMission(CurMission.name, TargetsEliminated, CurMission.targets);
            }

            if (TargetsRemaining <= 0)
                StartCoroutine(CompleteMissionRoutine());
        }

        public bool IsFinalTarget => MissionActive && TargetsRemaining <= 1;

        IEnumerator CompleteMissionRoutine()
        {
            MissionActive = false;
            yield return null;
            while (BulletCam.Instance != null && BulletCam.Instance.IsPlaying)
                yield return null;
            yield return new WaitForSecondsRealtime(1.4f);

            int creditsEarned = 50 + _shotsHit * 25 + _headshots * 20;
            int gi = GlobalIndex(_curRegion, _curMission);
            if (Profile != null)
            {
                Profile.credits += creditsEarned;
                if (Score > Profile.bestScore) Profile.bestScore = Score;
                if (gi > Profile.highestCompleted) Profile.highestCompleted = gi;
                SaveSystem.Save(Profile);
            }

            bool campaignCleared = gi >= TotalMissions() - 1;
            if (ui != null)
            {
                yield return ui.ShowResults(
                    contractNo: gi + 1,
                    score: Score,
                    accuracy: _shotsFired > 0 ? (float)_shotsHit / _shotsFired : 0f,
                    headshots: _headshots,
                    bestDistance: _bestDistance,
                    creditsEarned: creditsEarned,
                    totalCredits: Profile != null ? Profile.credits : 0,
                    campaignCleared: campaignCleared);

                // Spend credits, then back to the contract board.
                yield return ui.ShowGarage(Profile);
                ui.ShowMissionSelect(_curRegion);
            }
        }

        void FailMission()
        {
            MissionActive = false;
            if (ui != null) ui.ShowFailed(CurMission.name);
        }

        // ------------------------------------------------------------------
        //  Pause / lifecycle / economy
        // ------------------------------------------------------------------

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

        /// <summary>
        /// Abandon the current mission and return home. A soft reset — no scene
        /// reload — so it works even when the scene isn't in Build Settings yet
        /// (the first editor run). Hooked to the pause menu's HOME button.
        /// </summary>
        public void RestartCampaign()
        {
            _paused = false;
            Time.timeScale = 1f;
            StopAllCoroutines();  // cancel any mission/flow coroutine in flight
            if (weapon != null) weapon.ApplyUpgrades(Profile); // fresh clip
            GoHome();             // clears spawner + shows home
        }

        /// <summary>Retry the current mission. Hooked to the fail panel.</summary>
        public void RetryLevel()
        {
            if (ui != null) ui.HidePanels();
            StartCoroutine(StartMissionRoutine(_curRegion, _curMission));
        }

        /// <summary>Buy the next level of a track. Returns true on success.</summary>
        public bool TryBuyUpgrade(UpgradeTrack track)
        {
            if (Profile == null) return false;
            int level = Profile.GetLevel(track);
            int cost = Upgrades.NextCost(track, level);
            if (cost < 0 || Profile.credits < cost) return false;

            Profile.credits -= cost;
            Profile.SetLevel(track, level + 1);
            if (weapon != null) weapon.ApplyUpgrades(Profile);
            SaveSystem.Save(Profile);
            return true;
        }
    }
}
