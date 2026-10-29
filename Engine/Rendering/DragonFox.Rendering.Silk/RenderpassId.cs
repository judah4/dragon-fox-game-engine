using System;

namespace DragonGameEngine.Core.Rendering
{
    public readonly record struct RenderpassId
    {
        public static readonly RenderpassId World = new RenderpassId("World");
        public static readonly RenderpassId Ui = new RenderpassId("Ui");

        private readonly string _id;

        public RenderpassId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            _id = id;
        }

        public override string ToString()
        {
            return _id;
        }
    }
}
