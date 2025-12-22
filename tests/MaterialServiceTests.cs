using System;
using System.Collections.Generic;
// using NUnit.Framework; // Assuming NUnit or xUnit
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GreenChainz.Revit.Services;
using GreenChainz.Revit.Models;

namespace GreenChainz.Revit.Tests
{
    [TestClass]
    public class MaterialServiceTests
    {
        [TestMethod]
        public void GetMaterialsAsync_WithMockData_ReturnsExpectedMaterials()
        {
            // Simple test to verify the mock data service
            var service = new MaterialService();
            var task = service.GetMaterialsAsync();
            task.Wait();

            var results = task.Result;
            Assert.IsNotNull(results);
            Assert.IsTrue(results.Count > 0);
            Assert.AreEqual("Recycled Steel Beam", results[0].Name);
        }

        [TestMethod]
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
