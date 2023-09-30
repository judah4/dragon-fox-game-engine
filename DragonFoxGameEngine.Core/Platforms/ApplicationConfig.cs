﻿using Silk.NET.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonFoxGameEngine.Core.Platforms
{
    public readonly struct ApplicationConfig
    {
        public const int WIDTH = 800;
        public const int HEIGHT = 600;
        public const int GOOD_MAX_FPS = 1025;

        public const string DEFAULT_WINDOW_TITLE = "Project Coffee Dragon Fox";

        public readonly Vector2D<int> StartingPos;
        public readonly Vector2D<int> StartingSize;

        public readonly string Title;

        /// <summary>
        /// Headless mode to not initialize the rendering engine.
        /// </summary>
        public readonly bool HeadlessMode;

        public readonly double UpdatesPerSecond;
        public readonly double FramesPerSecond;

        public ApplicationConfig(string title, Vector2D<int> startingPos, Vector2D<int> startingSize, bool headlessMode, double updatesPerSecond, double framesPerSecond)
        {
            Title = title;
            if(string.IsNullOrEmpty(title)) 
            { 
                Title = DEFAULT_WINDOW_TITLE;
            }
            StartingPos = startingPos;
            StartingSize = startingSize;
            HeadlessMode = headlessMode;
            UpdatesPerSecond = updatesPerSecond;
            FramesPerSecond = framesPerSecond;
        }

        public ApplicationConfig()
        {
            StartingPos = new Vector2D<int>(-1, -1);
            StartingSize = new Vector2D<int>(WIDTH, HEIGHT);
            Title = DEFAULT_WINDOW_TITLE;
            HeadlessMode = false;
            UpdatesPerSecond = 0;
            FramesPerSecond = 0;
        }
    }
}
