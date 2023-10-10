namespace DragonGameEngine.Core.Rendering.Vulkan.Domain
{
    public struct PhysicalDeviceQueueFamilyInfo
    {
        public readonly uint GraphicsFamilyIndex;
        public readonly uint PresentFamilyIndex;
        public readonly uint ComputeFamilyIndex;
        public readonly uint TransferFamilyIndex;

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

        public PhysicalDeviceQueueFamilyInfo()
        {
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
