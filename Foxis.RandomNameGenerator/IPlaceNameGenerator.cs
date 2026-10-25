using System.Collections.Generic;

namespace Foxis.Library.RandomNameGenerator
{
    public interface IPlaceNameGenerator
    {
        string GenerateRandomPlaceName();

        IEnumerable<string> GenerateMultiplePlaceNames(int numberOfNames);
    }
}