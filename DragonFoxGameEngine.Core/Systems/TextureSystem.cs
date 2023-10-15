using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Systems.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonGameEngine.Core.Systems
{
    public sealed class TextureSystem
    {
        public const string DEFAULT_TEXTURE_NAME = "default";

        public void Initialize(ulong memoryRequirement, Span<byte> state, TextureSystemConfig config)
        {
            throw new NotImplementedException();
        }

        public void Shutdown(Span<byte> state)
        {
            throw new NotImplementedException();
        }

        public Texture Acquire(string name, bool autoRelease)
        {
            throw new NotImplementedException();
        }

        public Texture Release(string name)
        {
            throw new NotImplementedException();
        }
    }
}
