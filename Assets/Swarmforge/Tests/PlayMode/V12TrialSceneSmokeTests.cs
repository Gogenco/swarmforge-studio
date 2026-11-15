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
    }
}
