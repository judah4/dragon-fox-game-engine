using DragonGameEngine.Core.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonGameEngine.Core.Systems.Domain
{
    public class TextureSystemState
    {
        public TextureSystemConfig Config { get; init; }
        public Texture DefaultTexture { get; init; }
        public Dictionary<string, TextureReference> Textures { get; init; }

        public TextureSystemState(TextureSystemConfig config, Texture defaultTexture)
        {
            Config = config;
            DefaultTexture = defaultTexture;
            Textures = new Dictionary<string, TextureReference>((int)config.MaxTextureCount);
        }
    }
}
