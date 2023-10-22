using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Resources.ResourceDataTypes;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonGameEngine.Core.Systems.ResourceLoaders
{
    public sealed class ImageResourceLoader : IResourceLoader
    {
        public ResourceType ResourceType => ResourceType.Image;

        public string TypePath => "Textures";

        private readonly ILogger _logger;
        private readonly string _basePath;

        public ImageResourceLoader(ILogger logger, string basePath)
        {
            _logger = logger;
            _basePath = basePath;
        }


        public Resource Load(string name)
        {
            return ImageLoad(name);
        }

        public void Unload(Resource resource)
        {
            resource.Unload();
        }
        
        private Resource ImageLoad(string name)
        {
            const int requiredChannelCount = 4;

            //todo: try different extensions
            var filePath = Path.Combine(_basePath, TypePath, $"{name}.png");

            using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);
            img.Mutate(i => i.RotateFlip(RotateMode.None, FlipMode.Vertical));

            //flip
            ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);
            var pixels = new byte[imageSize];
            img.CopyPixelDataTo(pixels);

            // Check for transparency
            bool hasTransparency = img.PixelType.AlphaRepresentation.HasValue && img.PixelType.AlphaRepresentation.Value != PixelAlphaRepresentation.None;

            var imageResourceData = new ImageResourceData(requiredChannelCount, new Silk.NET.Maths.Vector2D<uint>((uint)img.Width, (uint)img.Height), pixels, hasTransparency);

            return new Resource(ResourceType, name, filePath, imageSize, imageResourceData);
            throw new NotImplementedException();
        }
    }
}
