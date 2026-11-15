using Swarmforge.Infrastructure;
using Swarmforge.Simulation;
using UnityEngine;

namespace Swarmforge.Presentation
{
    /// <summary>Standalone mobile-first GDD v1.2 P2 playable trial scene.</summary>
    public sealed class V12TrialEntryPoint : MonoBehaviour
    {
        [SerializeField] private V12BalanceConfigAsset m_HP2Balance;
        [SerializeField] private V12BalanceConfigAsset m_HP3Balance;
        [SerializeField] private int m_StartingBotHP = 2;

        private V12BalanceDefinition definition;
        private V12TrialSimulation sim;
        private V12TrialView visuals;
        private SimulationClock clock;
        private long commandSequence;
        private int lastAim = int.MinValue;
        private int currentBotHP = 2;
        private bool suspended;

        public int SimulationTick { get { return sim == null ? -1 : sim.Snapshot().Tick; } }
        public V12TrialSnapshot CurrentSnapshot { get { return sim == null ? null : sim.Snapshot(); } }
        public int CurrentBotHP { get { return currentBotHP; } }
        public bool IsSuspended { get { return suspended; } }

        private void Start()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            SelectHP(m_StartingBotHP == 3 ? 3 : 2);
        }

        public void SelectHP(int hp)
        {
            if (hp != 2 && hp != 3) throw new System.ArgumentOutOfRangeException(nameof(hp));
            currentBotHP = hp;
            V12BalanceConfigAsset asset = hp == 2 ? m_HP2Balance : m_HP3Balance;
            definition = asset != null ? asset.CreateEditableCopy() : V12P2Presets.Create(hp);
            // Fail closed on accidentally assigning old three-box P1 assets.
            definition.Validate();
            if (visuals != null) visuals.Dispose();
            visuals = new V12TrialView(definition);
            RestartMatch();
        }

        public void RestartMatch()
        {
            if (definition == null) return;
            sim = new V12TrialSimulation(new V12MatchConfig(definition));
            clock = new SimulationClock(definition.TickRate);
            suspended = false;
            commandSequence = 0;
            lastAim = int.MinValue;
            sim.AdvanceOneTick();
            visuals.Render(sim.Snapshot());
        }

        private void OnDestroy()
        {
            if (visuals != null) visuals.Dispose();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Suspend();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) Suspend();
        }

        private void Suspend()
        {
            suspended = true;
            if (clock != null) clock.Paused = true;
        }

        public void ResumeFromSuspend()
        {
            if (!suspended || clock == null) return;
            clock.Reset();
            suspended = false;
        }

        private void Update()
        {
            if (sim == null || suspended) return;
            if (!sim.IsTerminal)
            {
                ReadPointer();
                clock.Advance(Time.unscaledDeltaTime, () => sim.AdvanceOneTick());
            }
            visuals.Render(sim.Snapshot());
        }

        private void ReadPointer()
        {
            Vector2 point;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended) return;
                point = touch.position;
            }
            else if (Input.GetMouseButton(0)) point = Input.mousePosition;
            else return;
            if (point.y > Screen.height * .28f) return;
            int aim = visuals.AimFromScreenX(point.x);
            if (aim == lastAim) return;
            if (sim.SubmitAim(sim.NextTick, ++commandSequence, aim)) lastAim = aim;
        }

        private void OnGUI()
        {
            if (sim == null) return;
            V12TrialSnapshot s = sim.Snapshot();
            Matrix4x4 oldMatrix = GUI.matrix;
            float scale = Mathf.Max(.35f, Mathf.Min(Screen.width / 480f, Screen.height / 850f));
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            float width = Screen.width / scale;
            float height = Screen.height / scale;
            float top = (Screen.height - Screen.safeArea.yMax) / scale;
            float bottom = Screen.safeArea.yMin / scale;
            var title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19, alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold, normal = { textColor = Color.white }
            };
            var line = new GUIStyle(title) { fontSize = 15, fontStyle = FontStyle.Normal };
            var button = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };

            if (suspended || s.Outcome != MatchOutcome.Playing)
            {
                GUI.color = new Color(.025f, .04f, .08f, .90f);
                GUI.DrawTexture(new Rect(0, 0, width, height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                float cardWidth = Mathf.Min(width - 28, 400);
                float cardX = (width - cardWidth) * .5f;
                float cardY = (height - 270) * .5f;
                GUI.Box(new Rect(cardX, cardY, cardWidth, 270), GUIContent.none);
                string result = suspended ? "PAUSED" : s.Outcome == MatchOutcome.Win ?
                    "VICTORY" : s.Outcome == MatchOutcome.LoseBase ? "BASE DESTROYED" : "TIME UP";
                GUI.Label(new Rect(cardX + 10, cardY + 25, cardWidth - 20, 40), result,
                    new GUIStyle(title) { fontSize = 29 });
                GUI.Label(new Rect(cardX + 10, cardY + 88, cardWidth - 20, 50),
                    "FORT " + s.FortressHP + "   BASE " + s.BaseHP +
                    "\nBOTS " + s.BotsProduced + "   ENEMIES " + s.EnemiesKilled, line);
                if (GUI.Button(new Rect(cardX + 28, cardY + 165, cardWidth - 56, 62),
                    suspended ? "RESUME" : "RESTART", button))
                {
                    if (suspended) ResumeFromSuspend();
                    else RestartMatch();
                }
                GUI.matrix = oldMatrix;
                return;
            }

            float y = top + 8;
            GUI.Box(new Rect(12, y, width - 24, 134), GUIContent.none);
            GUI.Label(new Rect(20, y + 2, width - 40, 26), "SWARMFORGE  |  GDD v1.2 P2", title);
            GUI.Label(new Rect(20, y + 30, width - 40, 23),
                "TIME " + Mathf.CeilToInt((definition.MatchTicks - s.NextTick) / 30f) +
                "s    BASE " + s.BaseHP + "    FORT " + s.FortressHP, line);
            GUI.Label(new Rect(20, y + 55, width - 40, 23),
                "BOX x2 HP " + s.BoxHP + "/" + s.BoxMaxHP + "  |  GROUPS " + s.WaveGroupsSpawned, line);
            string boost = s.BoostMultiplier > 1 ?
                "x" + s.BoostMultiplier + " " + Mathf.CeilToInt(s.BoostRemainingTicks / 30f) + "s" : "x1";
            GUI.Label(new Rect(20, y + 79, width - 40, 23),
                "BOOST " + boost + "   QUEUED " +
                (s.QueuedMultiplier > 1 ? "x" + s.QueuedMultiplier : "NONE") +
                "   BOTS " + s.Bots.Count, line);
            float selectWidth = Mathf.Min(145, (width - 50) / 2);
            if (GUI.Button(new Rect(width * .5f - selectWidth - 5, y + 106, selectWidth, 25),
                currentBotHP == 2 ? "HP2 (ACTIVE)" : "TRY HP2", button)) SelectHP(2);
            if (GUI.Button(new Rect(width * .5f + 5, y + 106, selectWidth, 25),
                currentBotHP == 3 ? "HP3 (ACTIVE)" : "TRY HP3", button)) SelectHP(3);

            if (s.BoxHP > 0 && Camera.main != null)
            {
                Vector3 point = Camera.main.WorldToScreenPoint(visuals.BoxLabelWorld());
                if (point.z > 0)
                {
                    float x = point.x / scale;
                    float labelY = height - point.y / scale;
                    GUI.Box(new Rect(x - 44, labelY - 16, 88, 32), GUIContent.none);
                    GUI.Label(new Rect(x - 42, labelY - 15, 84, 30), "x2  HP " + s.BoxHP, title);
                }
            }
            float hintY = height - bottom - 107;
            GUI.Box(new Rect(12, hintY, width - 24, 94), GUIContent.none);
            GUI.Label(new Rect(20, hintY + 4, width - 40, 27), "DRAG HERE TO AIM", title);
            GUI.Label(new Rect(20, hintY + 33, width - 40, 23),
                "LEFT: BREAK x2 BOX    |    DEFEND THE BASE", line);
            GUI.Label(new Rect(20, hintY + 62, width - 40, 23),
                "AUTO-FIRE: x1 1/s    x2 2/s", line);
            GUI.matrix = oldMatrix;
        }
    }
}
