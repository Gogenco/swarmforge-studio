using System.Collections;
using NUnit.Framework;
using Swarmforge.Presentation;
using Swarmforge.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Swarmforge.Tests
{
    public sealed class V12TrialSceneSmokeTests
    {
        [UnityTest]
        public IEnumerator V12Trial_Stage2SevenNativePrefabsAreBoundWithoutChangingCombat()
        {
            // Artist-generated native Mesh/Material/Prefab assets must first be
            // bound using SWARMFORGE > Stage 2 > Generate and Integrate P8 Art.
            SceneManager.LoadScene("V12_Trial");
            yield return null;
            yield return null;
            V12Stage2ArtCatalog art = V12Stage2ArtCatalog.Load();
            Assert.That(art, Is.Not.Null, "Run Stage 2 Generate and Integrate P8 Art in Unity before tests.");
            Assert.That(art.IsComplete, Is.True, "All seven actual Art Director prefabs must be assigned.");
            GameObject arena = GameObject.Find("Trial_Art_Arena");
            GameObject cannon = GameObject.Find("P2_Auto_Cannon");
            GameObject chest = GameObject.Find("X2_Destructible_Box");
            GameObject friend = GameObject.Find("P2_MiniBot");
            GameObject fortress = GameObject.Find("P2_Enemy_Fortress");
            GameObject baseRoot = GameObject.Find("Player_Base");
            Assert.That(arena, Is.Not.Null);
            Assert.That(cannon, Is.Not.Null);
            Assert.That(chest, Is.Not.Null);
            Assert.That(friend, Is.Not.Null);
            Assert.That(fortress, Is.Not.Null);
            Assert.That(baseRoot, Is.Not.Null);
            Assert.That(arena.GetComponentsInChildren<MeshFilter>().Length, Is.GreaterThan(0));
            Assert.That(cannon.GetComponentsInChildren<MeshFilter>().Length, Is.GreaterThan(0));
            Assert.That(chest.GetComponentsInChildren<MeshFilter>().Length, Is.GreaterThan(0));
            Assert.That(friend.GetComponentsInChildren<MeshFilter>().Length, Is.GreaterThan(0));
            // Reworked P9 cannon local muzzle +Z .9535 still ends at true shot Z=0.
            float muzzleZ = cannon.transform.position.z + .9535f * cannon.transform.localScale.z;
            Assert.That(Mathf.Abs(muzzleZ), Is.LessThan(.025f));
            Assert.That(Mathf.Abs(friend.transform.position.x - cannon.transform.position.x),
                Is.LessThan(.0001f), "Green bot must appear from the same muzzle X without lateral fan-out.");
            var entry = Object.FindFirstObjectByType<V12TrialEntryPoint>();
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.CurrentSnapshot.RawShots, Is.EqualTo(1));
            Assert.That(entry.CurrentSnapshot.BoxHP, Is.EqualTo(5));
            Assert.That(entry.CurrentSnapshot.FortressHP, Is.EqualTo(40));
        }

        [UnityTest]
        public IEnumerator V12Trial_P9PortraitFramingAndHeroScalePreserveLogicalMuzzle()
        {
            SceneManager.LoadScene("V12_Trial");
            yield return null;
            yield return null;
            var art = V12Stage2ArtCatalog.Load();
            Assert.That(art, Is.Not.Null,
                "Generate and Integrate P8 Art in Unity before Stage 2 acceptance tests.");
            Assert.That(art.IsComplete, Is.True);
            var cameraObject = GameObject.Find("V12_Trial_Camera");
            var cannon = GameObject.Find("P2_Auto_Cannon");
            var green = GameObject.Find("P2_MiniBot");
            Assert.That(cameraObject, Is.Not.Null);
            Assert.That(cannon, Is.Not.Null);
            Assert.That(green, Is.Not.Null);
            Camera cam = cameraObject.GetComponent<Camera>();
            Assert.That(cam, Is.Not.Null);
            Assert.That(cam.orthographic, Is.True);
            Assert.That(cam.transform.eulerAngles.x, Is.InRange(60f, 67f),
                "P9 art must expose 3D silhouettes rather than the P8 85-degree view.");
            Assert.That(cam.orthographicSize, Is.GreaterThanOrEqualTo(12.2f));
            Assert.That(cam.orthographicSize * (9f / 16f), Is.GreaterThan(6f),
                "At 9:16 the full authored arena width needs room.");
            Assert.That(green.transform.localScale.x, Is.InRange(.75f, .85f));
            Assert.That(cannon.transform.localScale.x, Is.GreaterThanOrEqualTo(.8f));
            Assert.That(Mathf.Abs(cannon.transform.position.z +
                .9535f * cannon.transform.localScale.z), Is.LessThan(.025f));
            Assert.That(Mathf.Abs(green.transform.position.x - cannon.transform.position.x),
                Is.LessThan(.0001f), "P8 x2 and solo paths must remain at captured aim X.");
            var fillLight = GameObject.Find("V12_Trial_Cool_Fill");
            Assert.That(fillLight, Is.Not.Null);
            Assert.That(fillLight.GetComponent<Light>().shadows, Is.EqualTo(LightShadows.None));
        }

        [UnityTest]
        public IEnumerator V12Trial_FirstSoloMiniBotRendersAtExactCannonMuzzleX()
        {
            SceneManager.LoadScene("V12_Trial");
            yield return null;
            yield return null;
            var entry = Object.FindFirstObjectByType<V12TrialEntryPoint>();
            GameObject cannon = GameObject.Find("P2_Auto_Cannon");
            GameObject bot = GameObject.Find("P2_MiniBot");
            Assert.That(entry, Is.Not.Null);
            Assert.That(cannon, Is.Not.Null);
            Assert.That(bot, Is.Not.Null);
            Assert.That(entry.CurrentSnapshot.Bots.Count, Is.GreaterThan(0));
            Assert.That(Mathf.Abs(bot.transform.position.x - cannon.transform.position.x),
                Is.LessThan(.00001f), "The initial green MiniBot must emerge at barrel center, never at ID-based ±X.");
        }

        [UnityTest]
        public IEnumerator V12Trial_SceneAndDeterministicX2BothUseOneMuzzleLaneWithRealGap()
        {
            SceneManager.LoadScene("V12_Trial");
            yield return null;
            yield return null;
            var entry = Object.FindFirstObjectByType<V12TrialEntryPoint>();
            GameObject cannon = GameObject.Find("P2_Auto_Cannon");
            Assert.That(entry, Is.Not.Null);
            Assert.That(cannon, Is.Not.Null);
            Assert.That(entry.CurrentSnapshot.CannonX, Is.EqualTo(0));
            Assert.That(V12TrialSimulation.BoostFollowupDelayTicks, Is.EqualTo(8));

            // PlayMode runtime simulation on the actual P2 scene profile;
            // no frame-time-dependent visual tween can mask overlapping bots.
            foreach (int hp in new[] { 2, 3 })
            {
                var sim = new V12TrialSimulation(new V12MatchConfig(V12P2Presets.Create(hp)));
                Assert.That(sim.SubmitAim(0, 1, -950), Is.True);
                Assert.That(sim.SubmitAim(180, 2, 0), Is.True);
                for (int tick = 0; tick <= 188; tick++) sim.AdvanceOneTick();
                int count = 0, front = 0, back = 0;
                foreach (V12TrialUnitView bot in sim.Snapshot().Bots)
                    if (bot.X == 0)
                    {
                        if (count == 0) front = bot.Position;
                        else if (count == 1) back = bot.Position;
                        count++;
                        Assert.That(V12FormationLayout.ForBot(bot.Position, 51000),
                            Is.EqualTo(Vector2.zero), "Green bots cannot have lateral/fake depth offsets.");
                    }
                Assert.That(count, Is.EqualTo(2));
                Assert.That(Mathf.Abs(front - back), Is.GreaterThanOrEqualTo(7 * 340),
                    "The two real bots must be separated by actual authoritative forward distance.");
                Assert.That(sim.Snapshot().BoxHP, Is.EqualTo(0));
                Assert.That(sim.Snapshot().BoostMultiplier, Is.EqualTo(2));
                Assert.That(GameObject.Find("V12_Trial_Camera"), Is.Not.Null);
                Assert.That(GameObject.Find("X2_Destructible_Box"), Is.Not.Null);
            }
        }

        [UnityTest]
        public IEnumerator V12Trial_StandaloneSceneLoadsWithBoxAndCorrectHP()
        {
            SceneManager.LoadScene("V12_Trial");
            yield return null;
            yield return null;
            var entry = Object.FindFirstObjectByType<V12TrialEntryPoint>();
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.CurrentBotHP, Is.EqualTo(2));
            Assert.That(entry.CurrentSnapshot, Is.Not.Null);
            Assert.That(entry.CurrentSnapshot.BoxHP, Is.EqualTo(5));
            Assert.That(entry.CurrentSnapshot.FortressHP, Is.EqualTo(40),
                "Owner-approved P2 HP40 must be loaded from the Unity HP2 asset.");
            Assert.That(entry.CurrentSnapshot.Outcome, Is.EqualTo(MatchOutcome.Playing));
            Assert.That(entry.CurrentSnapshot.RawShots, Is.EqualTo(1),
                "Scene should fire one MiniBot immediately at tick zero.");
            Assert.That(entry.CurrentSnapshot.ConfigHash,
                Is.EqualTo(new V12MatchConfig(V12P2Presets.Create(2)).ConfigHash),
                "Authored HP2 ScriptableObject must match the approved one-per-second fallback.");
            Assert.That(GameObject.Find("V12_Trial_Camera"), Is.Not.Null);
            Assert.That(GameObject.Find("X2_Destructible_Box"), Is.Not.Null);
            Assert.That(GameObject.Find("P2_Auto_Cannon"), Is.Not.Null);
            Assert.That(GameObject.Find("Gate_x3"), Is.Null,
                "A separate P2 scene must not accidentally instantiate the old VS-01 gates.");
        }

        [UnityTest]
        public IEnumerator V12Trial_Hp3SwitchAndRestartReplaceSimulationWithoutLosingScene()
        {
            SceneManager.LoadScene("V12_Trial");
            yield return null;
            yield return null;
            var entry = Object.FindFirstObjectByType<V12TrialEntryPoint>();
            Assert.That(entry, Is.Not.Null);
            entry.SelectHP(3);
            yield return null;
            Assert.That(entry.CurrentBotHP, Is.EqualTo(3));
            Assert.That(entry.CurrentSnapshot.ConfigHash, Is.Not.EqualTo(0));
            Assert.That(entry.CurrentSnapshot.ConfigHash,
                Is.EqualTo(new V12MatchConfig(V12P2Presets.Create(3)).ConfigHash),
                "Authored HP3 ScriptableObject must match the approved one-per-second fallback.");
            Assert.That(entry.CurrentSnapshot.BoxMaxHP, Is.EqualTo(5));
            Assert.That(entry.CurrentSnapshot.FortressHP, Is.EqualTo(40),
                "Owner-approved P2 HP40 must be loaded from the Unity HP3 asset.");
            entry.RestartMatch();
            Assert.That(entry.SimulationTick, Is.EqualTo(0));
            Assert.That(entry.CurrentSnapshot.BoxHP, Is.EqualTo(5));
            Assert.That(entry.CurrentSnapshot.RawShots, Is.EqualTo(1));
            Assert.That(GameObject.Find("X2_Destructible_Box"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator V12Trial_BackgroundPauseDoesNotAdvanceAndResumeWorks()
        {
            SceneManager.LoadScene("V12_Trial");
            yield return null;
            yield return null;
            var entry = Object.FindFirstObjectByType<V12TrialEntryPoint>();
            Assert.That(entry, Is.Not.Null);
            entry.SendMessage("OnApplicationPause", true);
            int paused = entry.SimulationTick;
            yield return null;
            yield return null;
            Assert.That(entry.IsSuspended, Is.True);
            Assert.That(entry.SimulationTick, Is.EqualTo(paused));
            entry.ResumeFromSuspend();
            Assert.That(entry.IsSuspended, Is.False);
        }

        [UnityTest]
        public IEnumerator V12Trial_SideBoxVisualIsClearOfCentralCombatAndSceneKeepsOneChest()
        {
            SceneManager.LoadScene("V12_Trial");
            yield return null;
            yield return null;
            var entry = Object.FindFirstObjectByType<V12TrialEntryPoint>();
            GameObject cannon = GameObject.Find("P2_Auto_Cannon");
            GameObject rewardBox = GameObject.Find("X2_Destructible_Box");
            Assert.That(entry, Is.Not.Null);
            Assert.That(cannon, Is.Not.Null);
            Assert.That(rewardBox, Is.Not.Null);
            Assert.That(rewardBox.transform.position.x,
                Is.LessThan(cannon.transform.position.x - 2.5f),
                "The functional chest must be clearly to the left of center in the real 3D scene.");
            Assert.That(Object.FindObjectsByType<V12TrialEntryPoint>(FindObjectsSortMode.None).Length,
                Is.EqualTo(1));
            Assert.That(entry.CurrentSnapshot.BoxHP, Is.EqualTo(5));
            // The simulated enemy X is authoritative, independent of any
            // red visual formation and without waiting six wall-clock seconds.
            var sim = new V12TrialSimulation(new V12MatchConfig(V12P2Presets.Create(2)));
            for (int tick = 0; tick <= 180; tick++) sim.AdvanceOneTick();
            Assert.That(sim.Snapshot().Enemies.Count, Is.EqualTo(4));
            foreach (V12TrialUnitView enemy in sim.Snapshot().Enemies)
                Assert.That(enemy.X, Is.EqualTo(0));
        }

    }
}
