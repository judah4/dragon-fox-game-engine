namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public readonly struct PhysicalDeviceQueueFamilyInfo
    {
        public uint GraphicsFamilyIndex { get; init;}
        public uint PresentFamilyIndex { get; init; }
        public uint ComputeFamilyIndex { get; init; }
        public uint TransferFamilyIndex { get; init; }

        public struct Builder
        {
            public uint? GraphicsFamilyIndex;
            public uint? PresentFamilyIndex;
            public uint? ComputeFamilyIndex;
            public uint? TransferFamilyIndex;

            public readonly PhysicalDeviceQueueFamilyInfo Build()
            {
                return new PhysicalDeviceQueueFamilyInfo(
                    GraphicsFamilyIndex ?? 0,
                    PresentFamilyIndex ?? 0,
                    ComputeFamilyIndex ?? 0,
                    TransferFamilyIndex ?? 0
                    );
            }

        }

        private PhysicalDeviceQueueFamilyInfo(uint graphicsFamilyIndex, uint presentFamilyIndex, uint computeFamilyIndex, uint transferFamilyIndex)
        {
            GraphicsFamilyIndex = graphicsFamilyIndex;
            PresentFamilyIndex = presentFamilyIndex;
            ComputeFamilyIndex = computeFamilyIndex;
            TransferFamilyIndex = transferFamilyIndex;
        }
    }
}
