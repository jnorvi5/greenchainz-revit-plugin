using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Tests
{
    [TestFixture]
    public class MaterialServiceTests
    {
        [Test]
        public async Task GetMaterialsAsync_WithMockData_ReturnsExpectedMaterials()
        {
            // Simple test to verify the mock data service
            var service = new MaterialService();
            var results = await service.GetMaterialsAsync();

            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);
            Assert.AreEqual("Recycled Steel Beam", results[0].Name);
        }

        [Test]
        public void Material_WhenPropertiesSet_ReturnsCorrectValues()
        {
            var mat = new Material
            {
                Name = "Test",
                CarbonScore = 10.5
            };

            Assert.AreEqual("Test", mat.Name);
            Assert.AreEqual(10.5, mat.CarbonScore);
        }

        // Note: Testing CreateRevitMaterial requires a running Revit instance or a heavy mocking framework
        // like RevitTestFramework. For this deliverable, we verify the data logic only.
    }
}
