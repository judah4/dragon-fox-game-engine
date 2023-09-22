
namespace DragonFoxGameEngine.Core.Rendering.Vulkan
{
    public struct PhysicalDeviceQueueFamilyInfoBuilder
    {
        public uint? GraphicsFamilyIndex;
        public uint? PresentFamilyIndex;
        public uint? ComputeFamilyIndex;
        public uint? TransferFamilyIndex;

        public PhysicalDeviceQueueFamilyInfoBuilder()
        {
        }

        public PhysicalDeviceQueueFamilyInfo Build()
        {
            return new PhysicalDeviceQueueFamilyInfo() {

            };
        }
    }

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

            public PhysicalDeviceQueueFamilyInfo Build()
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
