using Foxis.Library;

namespace Foxis.Library.Tests
{
    [TestClass]
    public class StringUtilsTests
    {
        [DataTestMethod]
        [DataRow("thisIsAString", "this Is A String")]
        [DataRow("This_Is_String", "This _ Is _ String")]
        [DataRow("USA", "USA")]
        public void ToStringNameFormat_Test(string str, string expected)
        {
            var formatted = str.ToStringNameFormat();
            Assert.AreEqual(expected, formatted);
        }
    }
}