namespace DragonFoxGameEngine.Core
{
    public struct SimpleFpsCounter
    {
        public int CurrentFps { get; private set; }
        public int MinFps {get; private set;}
        public int MaxFps { get; private set; }

        int tickDelay = 10; //hacky but works

        public SimpleFpsCounter()
        {
            CurrentFps = 0;
            MinFps = int.MaxValue;
            MaxFps = 0;
        }

        public void SetFps(int fps)
        {
            if(tickDelay > 0)
            {
                tickDelay--;
                return;
            }

            CurrentFps = fps;
            if(fps < MinFps)
            {
                MinFps = fps;
            }
            if(fps > MaxFps)
            {
                MaxFps = fps;
            }
        }

        public void SetFpsFromDeltaTime(double deltaTime)
        {
            var fps = (int)(1.0 / deltaTime);
            SetFps(fps);
        }
    }
}
