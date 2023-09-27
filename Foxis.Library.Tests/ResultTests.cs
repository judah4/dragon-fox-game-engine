using Foxis.Library;

namespace Foxis.Library.Tests
{
    [TestClass]
    public class ResultTests
    {
        [TestMethod]
        public void Result_Ok_Test()
        {
            var result = Result.Ok(10);

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.IsFailure);
            Assert.AreEqual(10, result.Value);
            Assert.AreEqual(string.Empty, result.Error);
        }

        [TestMethod]
        public void Result_Ok_Simple_Test()
        {
            var result = Result.Ok<bool>();

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.IsFailure);
            Assert.AreEqual(default, result.Value);
            Assert.AreEqual(string.Empty, result.Error);
        }

        [TestMethod]
        public void Result_Fail_Test()
        {
            var expectedError = "This is an error.";
            var result = Result.Fail<int>(expectedError);

            Assert.IsFalse(result.Success);
            Assert.IsTrue(result.IsFailure);
            Assert.AreEqual(default, result.Value);
            Assert.AreEqual(expectedError, result.Error);
        }
    }
}