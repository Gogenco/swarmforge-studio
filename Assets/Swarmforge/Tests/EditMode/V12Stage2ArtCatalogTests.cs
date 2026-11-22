using NUnit.Framework;
using Swarmforge.Presentation;
using UnityEngine;

namespace Swarmforge.Tests
{
    public sealed class V12Stage2ArtCatalogTests
    {
        [Test]
        public void Stage2Catalog_GeneratedNativePrefabsAreCompleteAndCollisionFree()
        {
            // This test is intentionally a required Stage 2 acceptance gate.
            // Run the explicit Editor generation+binder menu first.
            V12Stage2ArtCatalog catalog = V12Stage2ArtCatalog.Load();
            Assert.That(catalog, Is.Not.Null, "Run SWARMFORGE > Stage 2 > Generate and Integrate P8 Art.");
            Assert.That(catalog.IsComplete, Is.True);
            GameObject[] models =
            {
                catalog.FriendlyMiniBot, catalog.RedEnemyRobot,
                catalog.AutoCannon, catalog.X2Chest,
                catalog.PlayerBase, catalog.EnemyFortress, catalog.Arena
            };
            foreach (GameObject model in models)
            {
                Assert.That(model, Is.Not.Null);
                Assert.That(model.GetComponentsInChildren<MeshFilter>(true).Length,
                    Is.GreaterThan(0), model.name + " must contain authored polygon meshes.");
                Assert.That(model.GetComponentsInChildren<MeshRenderer>(true).Length,
                    Is.GreaterThan(0), model.name + " must contain real renderers.");
                Assert.That(model.GetComponentsInChildren<Collider>(true).Length,
                    Is.EqualTo(0), model.name + " must not alter simulation collisions.");
                Assert.That(model.GetComponentsInChildren<Rigidbody>(true).Length,
                    Is.EqualTo(0), model.name + " must not add non-deterministic physics.");
            }
        }
    }
}
