using Foxis.Library;

namespace Foxis.Library.Tests
{
    [TestClass]
    public class AudioUtilityLibraryTests
    {
        [DataTestMethod]
        [DataRow(1.0f, 1.0f)]
        [DataRow(0.0f, 1.0f)]
        [DataRow(1.0f, 0.0f)]
        [DataRow(0.5f, 0.5f)]
        [DataRow(1.0f, 0.9f)]
        public void AudioUtilityLibrary_Test(float masterVolume, float volume)
        {
            var result = AudioUtilityLibrary.QuickVolume(masterVolume, volume);

            var expectedLessOrEqual = masterVolume * volume;
            //For a simple calculation, the value should always be less or equal to the inputs.

            Assert.IsTrue(result <= expectedLessOrEqual, $"Actual volume level {result} was not expected.");
        }
    }
}