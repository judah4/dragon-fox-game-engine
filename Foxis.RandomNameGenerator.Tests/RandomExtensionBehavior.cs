using System;
using Foxis.Library.RandomNameGenerator;

namespace Foxis.Library.Tests.RandomNameGenerator
{
    [TestClass]
    public class RandomExtensionBehavior
    {
        [TestMethod]
        public void CanGetARandomPlaceNameFromARandomObject()
        {
            var rand = new Random();

            var name = rand.GenerateRandomPlaceName();

            Assert.IsNotNull(name);
        }

        [TestMethod]
        public void CanGetARandomPersonNameFromARandomObject()
        {
            var rand = new Random();

            var name = rand.GenerateRandomFirstAndLastName();

            Assert.IsNotNull(name);
        }
    }
}