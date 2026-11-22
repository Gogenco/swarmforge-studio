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
