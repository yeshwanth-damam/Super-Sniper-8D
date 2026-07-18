using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SuperSniper8D
{
    /// <summary>
    /// Builds the entire interface in code — HUD, generated scope reticle,
    /// letterbox bars, white-flash, the cinematic dossier brief, the results
    /// screen and mobile touch buttons. No Canvas, prefab or font asset needs
    /// to exist in the project; everything is created at runtime with the
    /// premium "quiet" language from the design doc: near-black panels, hairline
    /// borders and a single amber accent.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // Design-doc palette.
        static readonly Color Amber = new Color(0.93f, 0.70f, 0.24f);
        static readonly Color Ink = new Color(0.04f, 0.05f, 0.06f);
        static readonly Color Paper = new Color(0.86f, 0.86f, 0.84f);

        Font _font;
        Canvas _canvas;

        Text _scoreText, _missionText, _timerText, _ammoText, _reloadText;
        GameObject _crosshair, _scopeRoot;
        RectTransform _scopeSquare, _maskL, _maskR, _maskT, _maskB;
        RectTransform _letterTop, _letterBottom;
        Image _flash;
        Vector2 _lastScreen;
        RawImage _grain;

        GameObject _pausePanel;

        GameObject _garagePanel;
        Text _garageCredits;
        readonly Text[] _garageRowLabel = new Text[4];
        readonly Button[] _garageBuyBtn = new Button[4];
        readonly Text[] _garageBuyLabel = new Text[4];
        bool _continuePressed;
        bool _deployPressed;

        GameObject _homePanel, _selectPanel;
        readonly System.Collections.Generic.List<GameObject> _homeCards = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<GameObject> _selectCards = new System.Collections.Generic.List<GameObject>();

        /// <summary>True when any full-screen menu is up (weapon input is gated on this).</summary>
        public bool MenusOpen =>
            (_dossier != null && _dossier.activeSelf) ||
            (_resultsPanel != null && _resultsPanel.activeSelf) ||
            (_failPanel != null && _failPanel.activeSelf) ||
            (_pausePanel != null && _pausePanel.activeSelf) ||
            (_garagePanel != null && _garagePanel.activeSelf) ||
            (_homePanel != null && _homePanel.activeSelf) ||
            (_selectPanel != null && _selectPanel.activeSelf);

        GameObject _dossier;
        Text _dossierTitle, _dossierName, _dossierIntel;

        GameObject _resultsPanel, _failPanel;
        Text _resultsBody, _failBody;

        // Mobile input state (read by the weapon).
        bool _fireQueued;
        public bool MobileScopeOn { get; private set; }
        public bool MobileBreathHeld { get; private set; }

        public bool ConsumeFire()
        {
            if (!_fireQueued) return false;
            _fireQueued = false;
            return true;
        }

        void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Build();
        }

        // ------------------------------------------------------------------
        //  Build
        // ------------------------------------------------------------------

        void Build()
        {
            // EventSystem for UI clicks/touches.
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("HUD");
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            BuildCinematic();   // filmic grade under everything else
            BuildScopeOverlay();
            BuildLetterbox();
            BuildHud();
            BuildFlash();
            BuildDossier();
            BuildResults();
            BuildFail();
            BuildPause();
            BuildGarage();
            BuildMetaPanels();
            if (Application.isMobilePlatform || Application.platform == RuntimePlatform.Android)
                BuildMobileControls();

            HidePanels();
            SetScoped(false);
        }

        void BuildHud()
        {
            _scoreText = Label("Score", TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-30, -24), 40, Amber);
            _scoreText.text = "0";

            _missionText = Label("Mission", TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(30, -24), 26, Paper);
            _timerText = Label("Timer", TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -24), 44, Paper);
            _ammoText = Label("Ammo", TextAnchor.LowerRight, new Vector2(1, 0), new Vector2(-30, 28), 34, Paper);
            _ammoText.text = "5 / 5";

            _reloadText = Label("Reload", TextAnchor.LowerCenter, new Vector2(0.5f, 0), new Vector2(0, 120), 30, Amber);
            _reloadText.text = "RELOADING";
            _reloadText.gameObject.SetActive(false);

            // Hip crosshair: a thin amber plus at centre.
            _crosshair = new GameObject("Crosshair");
            _crosshair.transform.SetParent(_canvas.transform, false);
            var crt = _crosshair.AddComponent<RectTransform>();
            Center(crt, new Vector2(28, 28));
            Bar(_crosshair.transform, new Vector2(28, 2));
            Bar(_crosshair.transform, new Vector2(2, 28));
        }

        void Bar(Transform parent, Vector2 size)
        {
            var go = new GameObject("Bar");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = Amber;
            var rt = img.rectTransform;
            Center(rt, size);
        }

        // A pipeline-agnostic filmic grade drawn as screen-space overlays: a
        // cool shadow tint, a soft radial vignette, and animated film grain.
        // Delivers the design doc's near-black, framed look on URP or Built-in
        // with no volume framework or package dependency.
        void BuildCinematic()
        {
            var tint = new GameObject("Grade");
            tint.transform.SetParent(_canvas.transform, false);
            var timg = tint.AddComponent<Image>();
            timg.color = new Color(0.10f, 0.14f, 0.20f, 0.06f);
            timg.raycastTarget = false;
            Stretch(timg.rectTransform);

            var vig = new GameObject("Vignette");
            vig.transform.SetParent(_canvas.transform, false);
            var vimg = vig.AddComponent<Image>();
            vimg.sprite = MakeVignetteSprite(256);
            vimg.color = new Color(0f, 0f, 0f, 0.55f);
            vimg.raycastTarget = false;
            Stretch(vimg.rectTransform);

            var grain = new GameObject("Grain");
            grain.transform.SetParent(_canvas.transform, false);
            _grain = grain.AddComponent<RawImage>();
            _grain.texture = MakeNoiseTexture(256);
            _grain.color = new Color(1f, 1f, 1f, 0.05f);
            _grain.raycastTarget = false;
            Stretch(_grain.rectTransform);
        }

        Sprite MakeVignetteSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (size * 0.5f);
                    float a = Mathf.SmoothStep(0.55f, 1.15f, d);
                    px[y * size + x] = new Color32(0, 0, 0, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        Texture2D MakeNoiseTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++)
            {
                byte v = (byte)Random.Range(0, 256);
                px[i] = new Color32(v, v, v, 255);
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        // A procedurally-drawn scope reticle. Circular clear centre, opaque
        // surround, crosshair + mil-dots. The square circle sprite is sized to
        // the screen's shorter side and four black masks fill the remainder, so
        // the reticle stays round and fully masked on any aspect ratio.
        void BuildScopeOverlay()
        {
            _scopeRoot = new GameObject("ScopeOverlay");
            _scopeRoot.transform.SetParent(_canvas.transform, false);
            var root = _scopeRoot.AddComponent<RectTransform>();
            Stretch(root);

            var sq = new GameObject("ScopeCircle");
            sq.transform.SetParent(_scopeRoot.transform, false);
            var img = sq.AddComponent<Image>();
            img.sprite = MakeScopeSprite(512);
            img.color = Color.white;
            img.raycastTarget = false;
            _scopeSquare = img.rectTransform;
            _scopeSquare.anchorMin = _scopeSquare.anchorMax = new Vector2(0.5f, 0.5f);
            _scopeSquare.pivot = new Vector2(0.5f, 0.5f);

            _maskL = Mask("LeftMask", new Vector2(0f, 0.5f));
            _maskR = Mask("RightMask", new Vector2(1f, 0.5f));
            _maskT = Mask("TopMask", new Vector2(0.5f, 1f));
            _maskB = Mask("BottomMask", new Vector2(0.5f, 0f));

            LayoutScope();
        }

        RectTransform Mask(string name, Vector2 anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_scopeRoot.transform, false);
            var img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = Vector2.zero;
            return rt;
        }

        // Sizes the reticle + masks from the canvas' logical rect (scaler-aware).
        void LayoutScope()
        {
            var canvasRT = _canvas.transform as RectTransform;
            if (canvasRT == null || _scopeSquare == null) return;
            float w = canvasRT.rect.width;
            float h = canvasRT.rect.height;
            float side = Mathf.Min(w, h);

            _scopeSquare.sizeDelta = new Vector2(side, side);
            float sx = Mathf.Max(0f, (w - side) * 0.5f);
            float sy = Mathf.Max(0f, (h - side) * 0.5f);
            _maskL.sizeDelta = new Vector2(sx, h);
            _maskR.sizeDelta = new Vector2(sx, h);
            _maskT.sizeDelta = new Vector2(w, sy);
            _maskB.sizeDelta = new Vector2(w, sy);

            _lastScreen = new Vector2(Screen.width, Screen.height);
        }

        void Update()
        {
            if (_lastScreen.x != Screen.width || _lastScreen.y != Screen.height)
                LayoutScope();

            // Jitter the grain UVs so the noise shimmers like film.
            if (_grain != null)
                _grain.uvRect = new Rect(Random.value, Random.value, 1.5f, 1.5f);
        }

        Sprite MakeScopeSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            float rClear = size * 0.46f;
            float rRing = size * 0.47f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    Color32 col;
                    if (d > rRing) col = new Color32(0, 0, 0, 255);               // outer black
                    else if (d > rClear) col = new Color32(237, 179, 61, 255);    // amber ring
                    else
                    {
                        // Clear centre with crosshair + mil-dots.
                        bool line = Mathf.Abs(x - c) < 1.2f || Mathf.Abs(y - c) < 1.2f;
                        bool dot = false;
                        for (int m = 1; m <= 4; m++)
                        {
                            float off = m * (rClear / 6f);
                            if ((Mathf.Abs(Mathf.Abs(x - c) - off) < 2f && Mathf.Abs(y - c) < 2f) ||
                                (Mathf.Abs(Mathf.Abs(y - c) - off) < 2f && Mathf.Abs(x - c) < 2f))
                                dot = true;
                        }
                        if (line || dot) col = new Color32(20, 20, 22, 220);
                        else col = new Color32(0, 0, 0, 0);
                    }
                    px[y * size + x] = col;
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        void BuildLetterbox()
        {
            _letterTop = LetterBar(true);
            _letterBottom = LetterBar(false);
        }

        RectTransform LetterBar(bool top)
        {
            var go = new GameObject(top ? "LetterTop" : "LetterBottom");
            go.transform.SetParent(_canvas.transform, false);
            var img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0, top ? 1 : 0);
            rt.anchorMax = new Vector2(1, top ? 1 : 0);
            rt.pivot = new Vector2(0.5f, top ? 1 : 0);
            rt.sizeDelta = new Vector2(0, 0);
            return rt;
        }

        void BuildFlash()
        {
            var go = new GameObject("Flash");
            go.transform.SetParent(_canvas.transform, false);
            _flash = go.AddComponent<Image>();
            _flash.color = new Color(1, 1, 1, 0);
            _flash.raycastTarget = false;
            Stretch(_flash.rectTransform);
        }

        void BuildDossier()
        {
            _dossier = Panel("Dossier", new Color(0.02f, 0.02f, 0.03f, 0.97f));
            _dossierTitle = Label("DTitle", TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(160, -180), 30, Amber, _dossier.transform);
            _dossierName = Label("DName", TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(160, -230), 70, Paper, _dossier.transform);
            _dossierIntel = Label("DIntel", TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(160, -330), 32, Paper, _dossier.transform);
            _dossierIntel.rectTransform.sizeDelta = new Vector2(1200, 200);
            _dossierIntel.horizontalOverflow = HorizontalWrapMode.Wrap;
        }

        void BuildResults()
        {
            _resultsPanel = Panel("Results", new Color(0.02f, 0.02f, 0.03f, 0.94f));
            Label("RTitle", TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -220), 60, Amber, _resultsPanel.transform)
                .text = "MISSION COMPLETE";
            _resultsBody = Label("RBody", TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -340), 38, Paper, _resultsPanel.transform);
            _resultsBody.rectTransform.sizeDelta = new Vector2(900, 400);
        }

        void BuildFail()
        {
            _failPanel = Panel("Fail", new Color(0.05f, 0.01f, 0.01f, 0.95f));
            Label("FTitle", TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -240), 60, new Color(0.85f, 0.2f, 0.15f), _failPanel.transform)
                .text = "MISSION FAILED";
            _failBody = Label("FBody", TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -360), 34, Paper, _failPanel.transform);
            MakeButton("RETRY", _failPanel.transform, new Vector2(0, -520),
                () => { if (GameManager.Instance != null) GameManager.Instance.RetryLevel(); });
            MakeButton("ABORT", _failPanel.transform, new Vector2(0, -630),
                () => { if (GameManager.Instance != null) GameManager.Instance.BackToSelect(); });
        }

        void BuildPause()
        {
            _pausePanel = Panel("Pause", new Color(0.02f, 0.02f, 0.03f, 0.92f));
            Label("PTitle", TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -260), 60, Amber, _pausePanel.transform)
                .text = "PAUSED";
            MakeButton("RESUME", _pausePanel.transform, new Vector2(0, -420),
                () => { if (GameManager.Instance != null) GameManager.Instance.TogglePause(); });
            MakeButton("RESTART", _pausePanel.transform, new Vector2(0, -530),
                () => { if (GameManager.Instance != null) GameManager.Instance.RestartCampaign(); });
            MakeButton("QUIT", _pausePanel.transform, new Vector2(0, -640),
                () => { if (GameManager.Instance != null) GameManager.Instance.QuitGame(); });
            _pausePanel.SetActive(false);
        }

        // ------------------------------------------------------------------
        //  Home / region map + mission-select "case files"
        // ------------------------------------------------------------------

        void BuildMetaPanels()
        {
            _homePanel = Panel("Home", new Color(0.02f, 0.03f, 0.04f, 0.98f));
            _homePanel.SetActive(false);
            _selectPanel = Panel("MissionSelect", new Color(0.02f, 0.03f, 0.04f, 0.98f));
            _selectPanel.SetActive(false);
        }

        public void ShowHome()
        {
            var gm = GameManager.Instance;
            HideMenuPanels();
            _homePanel.SetActive(true);
            MouseLook.LockCursor(false);

            foreach (var c in _homeCards) { if (c) { c.SetActive(false); Destroy(c); } }
            _homeCards.Clear();

            var titleText = Label("HTitle", TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -110), 72, Amber, _homePanel.transform);
            titleText.text = "SUPER SNIPER 8D";
            _homeCards.Add(titleText.gameObject);
            var sub = Label("HSub", TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -200), 28, Paper, _homePanel.transform);
            sub.text = gm != null ? $"SELECT REGION    ·    CREDITS {gm.Profile.credits:N0}" : "SELECT REGION";
            _homeCards.Add(sub.gameObject);

            if (gm == null) return;
            float y = -300f;
            for (int r = 0; r < gm.RegionCount; r++)
            {
                int region = r;
                bool unlocked = gm.IsRegionUnlocked(r);
                string body = $"{gm.RegionName(r)}\n{gm.RegionTagline(r)}";
                string stamp = unlocked ? null : "LOCKED";
                var card = MakeCard(_homePanel.transform, new Vector2(0, y), new Vector2(1000, 150), body, unlocked, stamp,
                    () => { if (GameManager.Instance != null) GameManager.Instance.SelectRegion(region); });
                _homeCards.Add(card);
                y -= 180f;
            }
        }

        public void ShowMissionSelect(int r)
        {
            var gm = GameManager.Instance;
            HideMenuPanels();
            _selectPanel.SetActive(true);
            MouseLook.LockCursor(false);

            foreach (var c in _selectCards) { if (c) { c.SetActive(false); Destroy(c); } }
            _selectCards.Clear();
            if (gm == null) return;

            var title = Label("STitle", TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(120, -90), 48, Amber, _selectPanel.transform);
            title.text = gm.RegionName(r);
            _selectCards.Add(title.gameObject);
            var cred = Label("SCred", TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(-320, -95), 30, Paper, _selectPanel.transform);
            cred.text = $"CREDITS {gm.Profile.credits:N0}";
            _selectCards.Add(cred.gameObject);

            var back = MakeButton("BACK", _selectPanel.transform, new Vector2(0, 0), () => { if (GameManager.Instance != null) GameManager.Instance.GoHome(); });
            var brt = back.GetComponent<RectTransform>();
            brt.anchorMin = brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(1, 1);
            brt.anchoredPosition = new Vector2(-120, -70);
            brt.sizeDelta = new Vector2(170, 66);
            _selectCards.Add(back.gameObject);

            float y = -190f;
            for (int m = 0; m < gm.MissionCount(r); m++)
            {
                int mission = m;
                MissionDef def = gm.Mission(r, m);
                bool unlocked = gm.IsMissionUnlocked(r, m);
                bool done = gm.IsMissionCompleted(r, m);
                int gi = gm.GlobalIndex(r, m);
                string body = $"CONTRACT #{gi + 1:00}   {def.name}\n{def.intel}\nTARGETS {def.targets}  ·  {def.timeLimit:0}s";
                string stamp = done ? "CLEARED" : (unlocked ? null : "LOCKED");
                Color stampCol = done ? Amber : new Color(0.5f, 0.5f, 0.55f);
                var card = MakeCard(_selectPanel.transform, new Vector2(0, y), new Vector2(1200, 118), body, unlocked, stamp,
                    () => { if (GameManager.Instance != null) GameManager.Instance.SelectMission(r, mission); }, stampCol);
                _selectCards.Add(card);
                y -= 135f;
            }
        }

        // A clickable multi-line card with an optional corner stamp.
        GameObject MakeCard(Transform parent, Vector2 pos, Vector2 size, string body, bool enabled,
            string stamp, UnityEngine.Events.UnityAction onClick, Color? stampColor = null)
        {
            var go = new GameObject("Card");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = enabled ? new Color(0.10f, 0.11f, 0.13f, 0.95f) : new Color(0.06f, 0.06f, 0.07f, 0.9f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = enabled ? Amber : new Color(0.3f, 0.3f, 0.32f);
            outline.effectDistance = new Vector2(1, 1);

            var txt = Label("CardText", TextAnchor.MiddleLeft, new Vector2(0, 0.5f), new Vector2(30, 0), 27,
                enabled ? Paper : new Color(0.55f, 0.55f, 0.58f), go.transform);
            txt.rectTransform.anchorMin = new Vector2(0, 0.5f);
            txt.rectTransform.anchorMax = new Vector2(0, 0.5f);
            txt.rectTransform.sizeDelta = new Vector2(size.x - 220, size.y - 16);
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.raycastTarget = false;
            txt.text = body;

            if (!string.IsNullOrEmpty(stamp))
            {
                var st = Label("Stamp", TextAnchor.MiddleRight, new Vector2(1, 0.5f), new Vector2(-30, 0), 30,
                    stampColor ?? new Color(0.5f, 0.5f, 0.55f), go.transform);
                st.rectTransform.anchorMin = new Vector2(1, 0.5f);
                st.rectTransform.anchorMax = new Vector2(1, 0.5f);
                st.rectTransform.sizeDelta = new Vector2(200, size.y);
                st.raycastTarget = false;
                st.text = stamp;
            }

            var btn = go.AddComponent<Button>();
            btn.interactable = enabled;
            btn.onClick.AddListener(onClick);
            return go;
        }

        void HideMenuPanels()
        {
            if (_dossier) _dossier.SetActive(false);
            if (_resultsPanel) _resultsPanel.SetActive(false);
            if (_failPanel) _failPanel.SetActive(false);
            if (_pausePanel) _pausePanel.SetActive(false);
            if (_garagePanel) _garagePanel.SetActive(false);
            if (_homePanel) _homePanel.SetActive(false);
            if (_selectPanel) _selectPanel.SetActive(false);
        }

        // The between-mission safehouse: spend credits on the rifle upgrade tree.
        void BuildGarage()
        {
            _garagePanel = Panel("Garage", new Color(0.02f, 0.02f, 0.03f, 0.96f));
            Label("GTitle", TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -140), 56, Amber, _garagePanel.transform)
                .text = "SAFEHOUSE";
            _garageCredits = Label("GCredits", TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -215), 34, Paper, _garagePanel.transform);

            float y = -320f;
            for (int i = 0; i < 4; i++)
            {
                var track = (UpgradeTrack)i;

                _garageRowLabel[i] = Label("GRow" + i, TextAnchor.MiddleLeft, new Vector2(0.5f, 1), new Vector2(-320, y), 30, Paper, _garagePanel.transform);
                _garageRowLabel[i].rectTransform.sizeDelta = new Vector2(560, 80);

                var btn = MakeButton(track.ToString(), _garagePanel.transform, new Vector2(360, y), () => { });
                var brt = btn.GetComponent<RectTransform>();
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 1f);
                brt.pivot = new Vector2(0.5f, 1f);
                brt.anchoredPosition = new Vector2(360, y);
                brt.sizeDelta = new Vector2(240, 76);
                // Rebind the click to buy this specific track.
                UpgradeTrack captured = track;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null && GameManager.Instance.TryBuyUpgrade(captured))
                        RefreshGarage(GameManager.Instance.Profile);
                });
                _garageBuyBtn[i] = btn;
                _garageBuyLabel[i] = btn.GetComponentInChildren<Text>();

                y -= 100f;
            }

            MakeButton("DEPLOY", _garagePanel.transform, new Vector2(0, y - 40f), () => _deployPressed = true);
            _garagePanel.SetActive(false);
        }

        void RefreshGarage(SaveData profile)
        {
            if (profile == null) return;
            if (_garageCredits != null) _garageCredits.text = $"CREDITS  {profile.credits:N0}";

            for (int i = 0; i < 4; i++)
            {
                var track = (UpgradeTrack)i;
                int lvl = profile.GetLevel(track);
                if (_garageRowLabel[i] != null)
                    _garageRowLabel[i].text = $"{Upgrades.Names[i]}   Lv {lvl}/{Upgrades.MaxLevel}   ·   {Upgrades.EffectLabel(track, lvl)}";

                int cost = Upgrades.NextCost(track, lvl);
                if (_garageBuyLabel[i] != null)
                    _garageBuyLabel[i].text = cost < 0 ? "MAX" : $"{cost} cr";
                if (_garageBuyBtn[i] != null)
                    _garageBuyBtn[i].interactable = cost >= 0 && profile.credits >= cost;
            }
        }

        public IEnumerator ShowGarage(SaveData profile)
        {
            _deployPressed = false;
            _garagePanel.SetActive(true);
            RefreshGarage(profile);
            MouseLook.LockCursor(false);
            while (!_deployPressed) yield return null;
            _garagePanel.SetActive(false);
        }

        public void ShowPause()
        {
            if (_pausePanel != null) _pausePanel.SetActive(true);
            MouseLook.LockCursor(false);
        }

        public void HidePause()
        {
            if (_pausePanel != null) _pausePanel.SetActive(false);
            MouseLook.LockCursor(true);
        }

        void BuildMobileControls()
        {
            MakeHoldButton("FIRE", new Vector2(-160, 160), new Vector2(1, 0),
                onDown: () => _fireQueued = true, onUp: null);
            MakeHoldButton("SCOPE", new Vector2(-160, 360), new Vector2(1, 0),
                onDown: () => MobileScopeOn = !MobileScopeOn, onUp: null);
            MakeHoldButton("HOLD", new Vector2(160, 160), new Vector2(0, 0),
                onDown: () => MobileBreathHeld = true, onUp: () => MobileBreathHeld = false);
        }

        // ------------------------------------------------------------------
        //  Public HUD API
        // ------------------------------------------------------------------

        public void SetScore(int score) { if (_scoreText) _scoreText.text = score.ToString("N0"); }

        public void SetMission(string name, int eliminated, int total)
        {
            if (_missionText) _missionText.text = $"{name}\nTARGETS  {eliminated} / {total}";
        }

        public void SetTimer(float seconds)
        {
            if (!_timerText) return;
            int s = Mathf.CeilToInt(seconds);
            _timerText.text = $"{s / 60:00}:{s % 60:00}";
            _timerText.color = s <= 10 ? new Color(0.85f, 0.2f, 0.15f) : Paper;
        }

        public void SetAmmo(int ammo, int clip) { if (_ammoText) _ammoText.text = $"{ammo} / {clip}"; }
        public void ShowReloading(bool on) { if (_reloadText) _reloadText.gameObject.SetActive(on); }

        public void SetScoped(bool scoped)
        {
            if (_scopeRoot) _scopeRoot.SetActive(scoped);
            if (_crosshair) _crosshair.SetActive(!scoped);
        }

        public void SetLetterbox(bool on) => StartCoroutine(LerpLetterbox(on ? 140f : 0f));

        IEnumerator LerpLetterbox(float target)
        {
            float start = _letterTop.sizeDelta.y;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 4f;
                float h = Mathf.Lerp(start, target, t);
                _letterTop.sizeDelta = new Vector2(0, h);
                _letterBottom.sizeDelta = new Vector2(0, h);
                yield return null;
            }
            _letterTop.sizeDelta = new Vector2(0, target);
            _letterBottom.sizeDelta = new Vector2(0, target);
        }

        public void FlashWhite() => StartCoroutine(FlashRoutine());

        IEnumerator FlashRoutine()
        {
            _flash.color = new Color(1, 1, 1, 0.9f);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 3f;
                _flash.color = new Color(1, 1, 1, Mathf.Lerp(0.9f, 0f, t));
                yield return null;
            }
            _flash.color = new Color(1, 1, 1, 0f);
        }

        // ------------------------------------------------------------------
        //  Cinematic screens
        // ------------------------------------------------------------------

        public IEnumerator PlayDossier(int contractNo, string missionName, string intel)
        {
            HidePanels();
            _dossier.SetActive(true);
            _dossierTitle.text = $"CONTRACT #{contractNo:00}  //  CLASSIFIED";
            _dossierName.text = missionName;
            _dossierIntel.text = "";

            // Typewriter reveal.
            var sb = new StringBuilder();
            foreach (char ch in intel)
            {
                sb.Append(ch);
                _dossierIntel.text = sb.ToString();
                yield return new WaitForSecondsRealtime(0.018f);
            }

            yield return new WaitForSecondsRealtime(1.6f);
            _dossier.SetActive(false);
        }

        public IEnumerator ShowResults(int contractNo, int score, float accuracy,
            int headshots, float bestDistance, int creditsEarned, int totalCredits, bool campaignCleared)
        {
            SetLetterbox(true);
            _resultsPanel.SetActive(true);

            // Count the score up slowly.
            int shown = 0;
            int step = Mathf.Max(1, score / 40);
            while (shown < score)
            {
                shown = Mathf.Min(score, shown + step);
                _resultsBody.text = ResultsText(shown, accuracy, headshots, bestDistance, creditsEarned, totalCredits, campaignCleared);
                yield return new WaitForSecondsRealtime(0.03f);
            }
            _resultsBody.text = ResultsText(score, accuracy, headshots, bestDistance, creditsEarned, totalCredits, campaignCleared);

            // Clear any button left over from a previous mission, then add ours.
            foreach (Transform child in _resultsPanel.transform)
                if (child.name.StartsWith("Btn_")) Destroy(child.gameObject);

            _continuePressed = false;
            MakeButton("CONTINUE", _resultsPanel.transform, new Vector2(0, -600), () => _continuePressed = true);

            MouseLook.LockCursor(false);

            // Wait for the player to acknowledge before moving on. The panel is
            // left active (the garage renders on top) so a menu is always up
            // until the next screen takes over — no input-gate gap.
            while (!_continuePressed) yield return null;
        }

        string ResultsText(int score, float accuracy, int headshots, float bestDistance,
            int creditsEarned, int totalCredits, bool campaignCleared)
        {
            string header = campaignCleared ? "CAMPAIGN CLEARED\n\n" : "";
            return $"{header}SCORE  {score:N0}\n\n" +
                   $"ACCURACY  {accuracy * 100f:0}%\n" +
                   $"HEADSHOTS  {headshots}\n" +
                   $"BEST SHOT  {bestDistance:0} m\n\n" +
                   $"CREDITS EARNED  +{creditsEarned:N0}\n" +
                   $"BALANCE  {totalCredits:N0}";
        }

        public void ShowFailed(string mission)
        {
            _failPanel.SetActive(true);
            _failBody.text = $"{mission}\nThe window closed.";
            MouseLook.LockCursor(false);
        }

        public void HidePanels()
        {
            HideMenuPanels();
            SetLetterbox(false);
            MouseLook.LockCursor(true);
        }

        // ------------------------------------------------------------------
        //  Widget helpers
        // ------------------------------------------------------------------

        Text Label(string name, TextAnchor anchor, Vector2 anchorPoint, Vector2 pos,
            int size, Color color, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : _canvas.transform, false);
            var txt = go.AddComponent<Text>();
            txt.font = _font;
            txt.fontSize = size;
            txt.color = color;
            txt.alignment = anchor;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = txt.rectTransform;
            rt.anchorMin = rt.anchorMax = anchorPoint;
            rt.pivot = anchorPoint;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(700, 120);
            return txt;
        }

        GameObject Panel(string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvas.transform, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            Stretch(img.rectTransform);
            return go;
        }

        Button MakeButton(string label, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(360, 90);

            var outline = go.AddComponent<Outline>();
            outline.effectColor = Amber;
            outline.effectDistance = new Vector2(1, 1);

            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            var t = Label("Label", TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, 34, Amber, go.transform);
            Stretch(t.rectTransform);
            t.raycastTarget = false;
            return btn;
        }

        // A press-and-hold capable button for mobile controls.
        void MakeHoldButton(string label, Vector2 pos, Vector2 anchor,
            System.Action onDown, System.Action onUp)
        {
            var go = new GameObject("Touch_" + label);
            go.transform.SetParent(_canvas.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.12f, 0.6f);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(240, 240);

            var t = Label("Label", TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, 34, Amber, go.transform);
            Stretch(t.rectTransform);
            t.raycastTarget = false;

            var trigger = go.AddComponent<EventTrigger>();
            if (onDown != null) AddTrigger(trigger, EventTriggerType.PointerDown, _ => onDown());
            if (onUp != null) AddTrigger(trigger, EventTriggerType.PointerUp, _ => onUp());
        }

        void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> cb)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(new UnityEngine.Events.UnityAction<BaseEventData>(cb));
            trigger.triggers.Add(entry);
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void Center(RectTransform rt, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }
    }
}
