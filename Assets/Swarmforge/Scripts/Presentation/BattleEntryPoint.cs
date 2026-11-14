using Swarmforge.Infrastructure;
using Swarmforge.Simulation;
using UnityEngine;

namespace Swarmforge.Presentation
{
    /// <summary>Attach to Battle.unity scene root. Restarts create a fresh MatchSimulation.</summary>
    public sealed class BattleEntryPoint : MonoBehaviour
    {
        [SerializeField] private LevelConfigAsset m_LevelAsset;
        private LevelDefinition config;
        private MatchSimulation sim;
        private SimulationClock clock;
        private PrimitiveBattleView visuals;
        private long matchSequence;
        private long inputSequence;
        private int lastSubmittedX = int.MinValue;
        private bool suspended;

        public bool IsSuspended { get { return suspended; } }
        public int SimulationTick { get { return sim == null ? -1 : sim.GetSnapshot().Tick; } }

        private void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            config = m_LevelAsset != null ? m_LevelAsset.CreateDefinition() : new LevelDefinition();
            config.Validate();
            visuals = new PrimitiveBattleView(config);
            RestartMatch();
        }

        private void OnDestroy()
        {
            if (visuals != null) visuals.Dispose();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Suspend();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Suspend();
        }

        private void Suspend()
        {
            suspended = true;
            if (clock != null) clock.Paused = true;
        }

        public void ResumeFromSuspend()
        {
            if (sim == null || !suspended) return;
            // Discard wall time spent in the background; never batch-simulate it.
            clock.Reset();
            suspended = false;
        }

        private void RestartMatch()
        {
            matchSequence++;
            sim = new MatchSimulation(config, matchSequence);
            clock = new SimulationClock(config.TickRate);
            suspended = false;
            inputSequence = 0;
            lastSubmittedX = int.MinValue;
            // Tick 0 runs at the beginning of the match (shot at t=0).
            sim.AdvanceOneTick();
            visuals.Render(sim.GetSnapshot());
        }

        private void Update()
        {
            if (sim == null) return;
            if (suspended) return;
            if (sim.IsTerminal) { visuals.Render(sim.GetSnapshot()); return; }
            PollPointer();
            clock.Advance(Time.unscaledDeltaTime, () => sim.AdvanceOneTick());
            visuals.Render(sim.GetSnapshot());
        }

        private void PollPointer()
        {
            Vector2 point;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) return;
                point = touch.position;
            }
            else if (Input.GetMouseButton(0)) point = Input.mousePosition;
            else return;
            if (point.y > Screen.height * .25f) return; // bottom-quarter one-thumb zone
            int x = Mathf.RoundToInt(Mathf.Lerp(config.CannonMinX, config.CannonMaxX,
                Mathf.Clamp01(point.x / Mathf.Max(1, Screen.width))));
            if (x == lastSubmittedX) return;
            if (sim.Submit(new InputCommand(sim.NextTick, ++inputSequence, CommandKind.MoveCannon, x)))
                lastSubmittedX = x;
        }

        private void OnGUI()
        {
            if (sim == null) return;
            MatchSnapshot s = sim.GetSnapshot();
            Matrix4x4 originalMatrix = GUI.matrix;
            // Scale against BOTH axes. Keep every label inside the phone safe area.
            float scale = Mathf.Max(.35f, Mathf.Min(Screen.width / 480f, Screen.height / 850f));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            float safeTop = (Screen.height - Screen.safeArea.yMax) / scale;
            float safeBottom = Screen.safeArea.yMin / scale;
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            var lineStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20, fontStyle = FontStyle.Bold
            };

            if (suspended || s.Outcome != MatchOutcome.Playing)
            {
                // Terminal/pause presentation replaces the HUD entirely.
                Color original = GUI.color;
                GUI.color = new Color(.02f, .04f, .10f, .85f);
                GUI.DrawTexture(new Rect(0, 0, width, height), Texture2D.whiteTexture);
                GUI.color = original;
                float cardWidth = Mathf.Min(width - 34, 390);
                float cardHeight = 235;
                float x = (width - cardWidth) * .5f;
                float y = Mathf.Clamp((height - cardHeight) * .5f,
                    safeTop + 16, Mathf.Max(safeTop + 16, height - safeBottom - cardHeight - 16));
                GUI.Box(new Rect(x, y, cardWidth, cardHeight), GUIContent.none);
                string headline = suspended ? "PAUSED" :
                    s.Outcome == MatchOutcome.Win ? "VICTORY!" :
                    s.Outcome == MatchOutcome.LoseBase ? "BASE DESTROYED" : "TIME UP";
                GUI.Label(new Rect(x + 12, y + 20, cardWidth - 24, 44), headline,
                    new GUIStyle(titleStyle) { fontSize = 28 });
                GUI.Label(new Rect(x + 12, y + 77, cardWidth - 24, 46),
                    "BASE " + s.BaseHP + "   |   FORT " + s.FortressHP +
                    "\nSHOTS " + s.RawShots + "   |   KILLS " + s.EnemiesDestroyed, lineStyle);
                if (GUI.Button(new Rect(x + 36, y + 149, cardWidth - 72, 58),
                    suspended ? "RESUME" : "RESTART", buttonStyle))
                {
                    if (suspended) ResumeFromSuspend();
                    else RestartMatch();
                }
                GUI.matrix = originalMatrix;
                return;
            }

            const float padding = 12;
            float hudY = safeTop + 10;
            GUI.Box(new Rect(padding, hudY, width - padding * 2, 84), GUIContent.none);
            GUI.Label(new Rect(padding + 8, hudY + 4, width - (padding + 8) * 2, 23),
                "SWARMFORGE  •  VS-01", titleStyle);
            GUI.Label(new Rect(padding + 8, hudY + 30, width - (padding + 8) * 2, 22),
                "BASE " + s.BaseHP + "    FORT " + s.FortressHP + "    TIME " +
                Mathf.CeilToInt(Mathf.Max(0, config.MatchTicks - s.Tick) / (float)config.TickRate) + "s", lineStyle);
            GUI.Label(new Rect(padding + 8, hudY + 55, width - (padding + 8) * 2, 22),
                "SHOTS " + s.RawShots + "    BOTS " + s.ProducedTotal +
                "    KILLS " + s.EnemiesDestroyed, lineStyle);

            // GUI overlays use built-in IMGUI shaders, not legacy 3D TextMesh materials.
            Camera worldCamera = Camera.main;
            if (worldCamera != null && visuals != null)
            {
                for (int lane = -1; lane <= 1; lane++)
                {
                    Vector3 point = worldCamera.WorldToScreenPoint(visuals.GateLabelWorld(lane));
                    if (point.z <= 0) continue;
                    float gx = point.x / scale;
                    float gy = height - point.y / scale;
                    string label = lane == -1 ? "×2" : lane == 0 ? "×1" : "×3";
                    GUI.Box(new Rect(gx - 25, gy - 13, 50, 27), GUIContent.none);
                    GUI.Label(new Rect(gx - 25, gy - 13, 50, 27), label, titleStyle);
                }
            }

            float hintsY = height - safeBottom - 102;
            GUI.Box(new Rect(padding, hintsY, width - padding * 2, 90), GUIContent.none);
            GUI.Label(new Rect(padding + 6, hintsY + 6, width - padding * 2 - 12, 25),
                "DRAG HERE TO AIM", titleStyle);
            GUI.Label(new Rect(padding + 6, hintsY + 32, width - padding * 2 - 12, 25),
                "LEFT ×2    CENTER ×1    RIGHT ×3", lineStyle);
            GUI.Label(new Rect(padding + 6, hintsY + 58, width - padding * 2 - 12, 25),
                "AUTO-FIRE • ATTACK", lineStyle);
            GUI.matrix = originalMatrix;
        }
    }
}
