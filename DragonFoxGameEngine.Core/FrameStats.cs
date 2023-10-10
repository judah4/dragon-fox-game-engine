using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DragonGameEngine.Core
{
    public class FrameStats
    {
        public const int FrameRangeSeconds = 30;

        struct FrameInfo
        {
            public long Frame;
            public double Time;
            public double DeltaTime;
        }

        //from https://www.yosoygames.com.ar/wp/2023/09/youre-calculating-framerate-percentiles-wrong/
        private List<FrameInfo> _samples = new List<FrameInfo>(100_000);
        private double _currentDelta;

        public void AddSample(in long frame, in double time, in double deltaTime)
        {
            var frameData = new FrameInfo()
            {
                Frame = frame,
                Time = time,
                DeltaTime = deltaTime,
            };
            _samples.Add(frameData);
            _currentDelta = deltaTime;

            for (int cnt = _samples.Count - 1; cnt > 0; cnt--)
            {
                if (_samples[cnt].Time < time - FrameRangeSeconds)
                {
                    _samples.RemoveAt(cnt);
                }
            }

            _samples.Sort((s1, s2) => s1.DeltaTime.CompareTo(s2.DeltaTime));
        }

        public double GetAverage()
        {
            double avg = 0.0;
            foreach (var val in _samples)
            {
                avg += val.DeltaTime;
            }

            avg /= _samples.Count;
            return avg;
        }

        public double GetPercentile95th()
        {
            if (_samples.Count == 0)
            {
                return 0.0;
            }
            var index = Math.Max(_samples.Count * 95 / 100 - 1, 0);
            return _samples[index].DeltaTime;
        }

        public double GetPercentile95thTime()
        {
            if (_samples.Count == 0)
            {
                return 0.0;
            }

            // Don't just consider the number of samples. Consider the total time taken/spent
            // (which bias towards larger samples). Consider the following extreme example:
            // Game runs at 60 fps for 1 hour. Then the next hour is spent rendering a single frame.
            //
            // The 95-p using just number of samples will say 60 fps / 16.67mspf.
            // However out of 2 hours, the user spent 1 hour at 60 fps and another hour at 0.000277778 fps.
            // The 95-p should be 0.000277778 fps (3600000 mspf), not 60 fps.
            double totalTimeTaken = 0.0;
            foreach (var sample in _samples)
            {
                totalTimeTaken += sample.DeltaTime;
            }

            double accumTimeToLookFor = totalTimeTaken * 0.95;

            totalTimeTaken = 0.0;
            foreach (var sample in _samples)
            {
                if (totalTimeTaken >= accumTimeToLookFor)
                {
                    return sample.DeltaTime;
                }
                totalTimeTaken += sample.DeltaTime;
            }

            return _samples[_samples.Count - 1].DeltaTime;  // Should not happen but it do
        }

        public void SendDebugMessage(ILogger logger)
        {
            var average = GetAverage();
            var percentileTime = GetPercentile95thTime();
            var percentile = GetPercentile95th();

            logger.LogDebug("Avg = {avg} mspf ({avgFps} FPS)\\ t95th-p(time) = {mspfFrames} mspf ({mspfFramesFps} FPS)\\ t95th-p(frames) = {mspf} mspf ({mspfFramesFps} FPS)",
                (average * 1000.0).ToString("F3"), (1.0 / average).ToString("F2"),
                (percentileTime * 1000.0).ToString("F3"), (1.0 / percentileTime).ToString("F2"),
                (percentile * 1000.0).ToString("F3"), (1.0 / percentile).ToString("F2"));
        }

        public double GetCurrentFps()
        {
            if (_currentDelta == 0.0)
            {
                return 0;
            }
            return 1.0 / _currentDelta;
        }

        public double GetMinFps()
        {
            if (_samples.Count == 0)
            {
                return 0.0;
            }

            return 1.0 / _samples[_samples.Count - 1].DeltaTime;
        }

        public double GetMaxFps()
        {
            if (_samples.Count == 0)
            {
                return 0.0;
            }

            return 1.0 / _samples[0].DeltaTime;
        }
    }
}
