using DragonGameEngine.Core.Exceptions;
using DragonGameEngine.Core.Resources;
using DragonGameEngine.Core.Resources.ResourceDataTypes;
using Microsoft.Extensions.Logging;
using Silk.NET.Maths;
using System;
using System.IO;

namespace DragonGameEngine.Core.Systems.ResourceLoaders
{
    public sealed class MaterialResourceLoader : IResourceLoader
    {
        public ResourceType ResourceType => ResourceType.Material;

        public string TypePath => "Materials";

        private readonly ILogger _logger;
        private readonly string _basePath;

        public MaterialResourceLoader(ILogger logger, string basePath)
        {
            _logger = logger;
            _basePath = basePath;
        }

        public Resource Load(string name)
        {
            try
            {
                return MaterialLoad(name);
            }
            catch (DirectoryNotFoundException ex)
            {
                throw new ResourceException(name, ex.Message, ex);
            }
        }

        public void Unload(Resource resource)
        {
            resource.Unload();
        }

        private Resource MaterialLoad(string name)
        {
            var filePath = Path.Combine(_basePath, TypePath, $"{name}.kmt");

            string matName = string.Empty;
            string diffuseMapName = string.Empty;
            Vector4D<float>? diffuseColor = default;

            int lineNumber = 0;
            using (var reader = File.OpenText(filePath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    line = line.Trim();
                    if (line.Length < 1 || line[0] == '#')
                    {
                        continue;
                    }

                    var splitData = line.Split('=');
                    if (splitData.Length < 2)
                    {
                        _logger.LogWarning("Potential formatting issue found in file '{path}': '=' token not found. Skipping line {lineNum}", filePath, lineNumber);
                        continue;
                    }

                    var valName = splitData[0].Trim();

                    var value = splitData[1].Trim();

                    if (valName.Equals("version", StringComparison.InvariantCultureIgnoreCase))
                    {
                        //TODO: version
                    }
                    else if (valName.Equals("name", StringComparison.InvariantCultureIgnoreCase))
                    {
                        matName = value;
                    }
                    else if (valName.Equals("diffuse_map_name", StringComparison.InvariantCultureIgnoreCase))
                    {
                        diffuseMapName = value;
                    }
                    else if (valName.Equals("diffuse_color", StringComparison.InvariantCultureIgnoreCase))
                    {
                        diffuseColor = ParseColor(value, filePath);
                    }
                }
            }

            if (string.IsNullOrEmpty(matName))
            {
                throw new ResourceException(name, "Material has no name.");
            }
            if (string.IsNullOrEmpty(diffuseMapName))
            {
                _logger.LogWarning("diffuse_map_name for material {name} not set. Defaulting to default texture.", matName);
                diffuseMapName = TextureSystem.DEFAULT_TEXTURE_NAME;
            }
            if (!diffuseColor.HasValue)
            {
                _logger.LogWarning("No parsed diffuse_color in file '{file}'. Using default of white instead.", filePath);
                diffuseColor = Vector4D<float>.One;
            }

            var config = new MaterialConfig(matName, true, diffuseColor.Value, diffuseMapName);

            return new Resource(ResourceType, name, filePath, (uint)config.Name.Length, config);
        }

        private Vector4D<float> ParseColor(string value, string path)
        {
            var colorSplit = value.Split(' ');
            if (colorSplit.Length < 4)
            {
                _logger.LogWarning("Error parsing diffuse_color in file '{file}'. Using default of white instead.", path);
                return Vector4D<float>.One;
            }

            var color = new Vector4D<float>();

            color.X = ParseColorValueOrLog(colorSplit[0], path, 0);
            color.Y = ParseColorValueOrLog(colorSplit[1], path, 1);
            color.Z = ParseColorValueOrLog(colorSplit[2], path, 2);
            color.W = ParseColorValueOrLog(colorSplit[3], path, 3);
            return color;
        }

        private float ParseColorValueOrLog(string value, string path, int index)
        {
            if (!float.TryParse(value, out var result))
            {
                _logger.LogWarning("Error parsing diffuse_color in file '{file}', color index {index}", path, index);
                return 1.0f;
            }
            return result;
        }

    }
}
