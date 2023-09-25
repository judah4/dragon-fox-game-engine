﻿using Silk.NET.Maths;

namespace DragonFoxGameEngine.Core.Rendering
{
    public interface IRenderer
    {
        public void Shutdown()
        {

        }

        public void Resized(Vector2D<uint> size)
        {

        }

        public void BeginFrame(double deltaTime)
        {

        }

        public void EndFrame(double deltaTime)
        {

        }
    }
}
