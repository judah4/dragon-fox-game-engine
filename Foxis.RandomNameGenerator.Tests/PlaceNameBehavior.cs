using System;
using Foxis.Library.RandomNameGenerator;

namespace Foxis.Library.Tests.RandomNameGenerator
{
    [TestClass]
    public class PlaceNameBehavior
    {
        private readonly PlaceNameGenerator _placeGenerator;

        public PlaceNameBehavior()
        {
            _placeGenerator = new PlaceNameGenerator();

        }

        [TestMethod]
        public void ShouldGenerateRandomName()
        {
            var name = _placeGenerator.GenerateRandomPlaceName();

            Assert.IsFalse(string.IsNullOrWhiteSpace(name));
        }

        [TestMethod]
        public void ShouldGenerateSameNameIfSameRandomGenerator()
        {
            var personNameGenerator1 = new PersonNameGenerator(new Random(42));
            var personNameGenerator2 = new PersonNameGenerator(new Random(42));

            var firstName = personNameGenerator1.GenerateRandomFirstAndLastName();
            var secondName = personNameGenerator2.GenerateRandomFirstAndLastName();

            Assert.AreEqual(firstName, secondName);
        }
    }
}