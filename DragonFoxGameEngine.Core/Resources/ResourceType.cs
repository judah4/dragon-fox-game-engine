using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonGameEngine.Core.Resources
{
    /// <summary>
    /// pre-defined resource types
    /// </summary>
    public readonly record struct ResourceType
    {
        public static readonly ResourceType Text = new ResourceType("Text");
        public static readonly ResourceType Binary = new ResourceType("Binary");
        public static readonly ResourceType Image = new ResourceType("Image");
        public static readonly ResourceType Material = new ResourceType("Material");
        public static readonly ResourceType StaticMesh = new ResourceType("StaticMesh");

        public string Name { get; }

        public ResourceType(string name)
        {
            if(string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
