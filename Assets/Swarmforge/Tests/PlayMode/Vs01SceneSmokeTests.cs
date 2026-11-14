using System.Collections;
using NUnit.Framework;
using Swarmforge.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Swarmforge.Tests
{
    public sealed class Vs01SceneSmokeTests
    {
        [UnityTest]
        public IEnumerator BattleScene_HasEntryPoint_AndCreatesGreyboxCamera()
        {
            SceneManager.LoadScene("Battle");
            yield return null;
            yield return null;
            Assert.That(Object.FindFirstObjectByType<BattleEntryPoint>(), Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(GameObject.Find("Portrait_Camera"), Is.Not.Null);
            Assert.That(GameObject.Find("Fortress_Core"), Is.Not.Null);
            Assert.That(GameObject.Find("Auto_Cannon"), Is.Not.Null);
            Assert.That(GameObject.Find("Gate_x1_Bypass"), Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator PauseAndExplicitResume_DoNotAdvanceBackgroundSimulation()
        {
            SceneManager.LoadScene("Battle");
            yield return null;
            yield return null;
            BattleEntryPoint entry = Object.FindFirstObjectByType<BattleEntryPoint>();
            Assert.That(entry, Is.Not.Null);
            entry.SendMessage("OnApplicationPause", true);
            int pausedAt = entry.SimulationTick;
            Assert.That(entry.IsSuspended, Is.True);
            yield return null;
            yield return null;
            Assert.That(entry.SimulationTick, Is.EqualTo(pausedAt));
            entry.ResumeFromSuspend();
            Assert.That(entry.IsSuspended, Is.False);
        }
    }
}
