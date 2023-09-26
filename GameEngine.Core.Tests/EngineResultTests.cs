using DragonFoxGameEngine.Core;

namespace GameEngine.Core.Tests
{
    [TestClass]
    public class EngineResultTests
    {
        [TestMethod]
        public void EngineResult_Ok_Test()
        {
            var result = EngineResult.Ok(10);

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.IsFailure);
            Assert.AreEqual(10, result.Value);
            Assert.AreEqual(string.Empty, result.Error);
        }

        [TestMethod]
        public void EngineResult_Ok_Simple_Test()
        {
            var result = EngineResult.Ok<bool>();

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.IsFailure);
            Assert.AreEqual(default, result.Value);
            Assert.AreEqual(string.Empty, result.Error);
        }

        [TestMethod]
        public void EngineResult_Fail_Test()
        {
            var expectedError = "This is an error.";
            var result = EngineResult.Fail<int>(expectedError);

            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(default, result.Value);
            Assert.AreEqual(expectedError, result.Error);
        }
    }
}