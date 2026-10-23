
using System;

namespace Foxis.Library
{
    /// <summary>
    /// Audio library to provide some useful utility functions.
    /// </summary>
    public static class AudioUtilityLibrary
    {

        /// <summary>
        /// Calculate final volume by converting a linear volume value to a non-linear one.
        ///
        /// This is done in order to change how responsive audio sliders appear. Generally speaking linear sliders tend
        /// to be very 'peaky' leaving only a small range of useful audio values. By using a non-linear volume value we
        /// will create a slider which has a smooth transition from quiet to loud with plenty of useful slider space.
        ///
        /// There's a complicated explanation as to why this is preferable and has to do with how humans perceive volume,
        /// the value of decibels and how much of the slider actually represents useful volume but I can't recall the
        /// entire explanation.
        /// </summary>
        private static float CalculateFinalVolume(in float volume)
        {
            /*
             * Clamp the volume range to within 0.0f and 1.0f. This is done to counteract the possibility that this
             * function gets fed a volume value outside of the expected range and we don't do something stupid like
             * blow someone's eardrums out.
             *
             * This will ensure that we can only generate values within this range as well.
             */
            var clamped = Math.Clamp(volume, 0.0f, 1.0f);

            /*
             * Multiply our clamped value to the power of 2.72.
             *
             * This is the magic number that will create smooth audio values for us to derive from our linear audio
             * sliders. Below I'll include a small table of values that this power function might produce to illustrate
             * how this will affect the perceived audio.
             *
             * 0.00f = 0.000
             * 0.10f = 0.002
             * 0.30f = 0.037
             * 0.50f = 0.151
             * 0.80f = 0.545
             * 0.90f = 0.750
             * 0.95f = 0.860
             * 1.00f = 1.000
             */
            return MathF.Pow(clamped, 2.72f);
        }

        /// <summary>
        /// Calculate the Effective Volume between the master volume and a specific volume type.
        ///
        /// The reason this function exists is because if we change the audio sliders from the current 0.0f to 1.0f
        /// scale to a 0 to 100 scale or something we would want to have any sort of conversion logic in a central place.
        /// The only reason I can see us doing so is if we want to display the audio value to the user. Right now it's
        /// a blind slider.
        /// </summary>
        private static float CalculateEffectiveVolume(in float master, in float volume)
        {
            return master * volume;
        }

        /// <summary>
        ///Helper function to quickly get a volume value given a master volume and specific volume value.
        ///
        ///This should, in most cases, be used rather than accessing the other public functions in this library directly
        ///but those are exposed in the event that manual volume nonsense needs to occur.
        /// </summary>
        public static float QuickVolume(in float master, in float volume)
        {
            float masterNormalized = Math.Clamp(master, 0.0f, 1.0f);
            float volumeNormalized = Math.Clamp(volume, 0.0f, 1.0f);


            // Calculate the effective volume.
            var effectiveVolume = CalculateEffectiveVolume(masterNormalized, volumeNormalized);

            // Return calculated final volume. 
            return CalculateFinalVolume(effectiveVolume);
        }
    }
}
