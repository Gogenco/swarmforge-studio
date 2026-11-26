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

        [Test]
        public void Stage2FinalCity_ActuallyContainsNativeRoadTreesHighrisesAndFourTextures()
        {
            // The old industrial P9 prefab must NOT pass this test.
            // Run Unity's Generate and Integrate P8 Art menu to rebuild city.
            V12Stage2ArtCatalog catalog = V12Stage2ArtCatalog.Load();
            Assert.That(catalog, Is.Not.Null, "Generate City art in Unity Editor first.");
            Assert.That(catalog.IsComplete, Is.True);
            GameObject city = catalog.Arena;
            var surface = city.transform.Find("LOD0_Surface");
            Assert.That(surface, Is.Not.Null, "City must have 3D facade/foliage surfaces.");
            MeshFilter mesh = surface.GetComponent<MeshFilter>();
            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.sharedMesh, Is.Not.Null);
            Assert.That(mesh.sharedMesh.bounds.max.y, Is.GreaterThan(15f),
                "The active generated arena must contain actual high-rise towers.");
            Assert.That(mesh.sharedMesh.bounds.max.z, Is.GreaterThan(24f),
                "The active generated arena must contain the distant skyline.");
            foreach (string layer in new[] { "Asphalt", "Grass", "Pavement", "CityGlass" })
            {
                Transform part = city.transform.Find("LOD0_" + layer);
                Assert.That(part, Is.Not.Null, layer + " must contain the city texture mesh.");
                var filter = part.GetComponent<MeshFilter>();
                var renderer = part.GetComponent<MeshRenderer>();
                Assert.That(filter, Is.Not.Null);
                Assert.That(renderer, Is.Not.Null);
                Assert.That(filter.sharedMesh.vertexCount, Is.GreaterThan(0));
                Assert.That(renderer.sharedMaterial.mainTexture, Is.Not.Null,
                    layer + " should use a generated tileable city texture.");
            }
            Assert.That(city.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(0));
            Assert.That(city.GetComponentsInChildren<Rigidbody>(true).Length, Is.EqualTo(0));
        }
        [Test]
        public void Stage2P12_NativeRobotLODsAndFourPbrMaterialMapsArePresent()
        {
            V12Stage2ArtCatalog catalog = V12Stage2ArtCatalog.Load();
            Assert.That(catalog, Is.Not.Null,
                "Run SWARMFORGE > Stage 2 > Generate and Integrate P8 Art first.");
            Assert.That(catalog.IsComplete, Is.True);
            foreach (GameObject bot in new[] { catalog.FriendlyMiniBot, catalog.RedEnemyRobot })
            {
                LODGroup lod = bot.GetComponent<LODGroup>();
                Assert.That(lod, Is.Not.Null, bot.name + " P12 LODGroup is missing.");
                Assert.That(lod.GetLODs().Length, Is.EqualTo(3));
                foreach (LOD level in lod.GetLODs())
                    Assert.That(level.renderers.Length, Is.GreaterThan(0));
            }
            foreach (string name in new[] { "Asphalt", "Grass", "Pavement", "CityGlass" })
            {
                Transform layer = catalog.Arena.transform.Find("LOD0_" + name);
                Assert.That(layer, Is.Not.Null, "P12 city layer missing: " + name);
                Material material = layer.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(material, Is.Not.Null);
                Assert.That(material.mainTexture, Is.Not.Null, "P12 albedo: " + name);
                Assert.That(material.GetTexture("_BumpMap"), Is.Not.Null,
                    "P12 normal map missing: " + name);
                Assert.That(material.GetTexture("_MetallicGlossMap"), Is.Not.Null,
                    "P12 smoothness/mask map missing: " + name);
            }
        }

    }
}
