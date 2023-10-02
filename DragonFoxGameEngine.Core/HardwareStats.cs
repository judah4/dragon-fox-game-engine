using Hardware.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonFoxGameEngine.Core
{
    public static class HardwareStats
    {
        static readonly IHardwareInfo s_hardwareInfo = new HardwareInfo();

        static HardwareStats()
        {
            s_hardwareInfo.RefreshOperatingSystem();
            s_hardwareInfo.RefreshMemoryStatus();
            //hardwareInfo.RefreshBatteryList();
            //hardwareInfo.RefreshBIOSList();
            s_hardwareInfo.RefreshCPUList();
            //hardwareInfo.RefreshDriveList();
            //hardwareInfo.RefreshKeyboardList();
            //hardwareInfo.RefreshMemoryList();
            //hardwareInfo.RefreshMonitorList();
            s_hardwareInfo.RefreshMotherboardList();
            //hardwareInfo.RefreshMouseList();
            //hardwareInfo.RefreshNetworkAdapterList();
            //hardwareInfo.RefreshPrinterList();
            //hardwareInfo.RefreshSoundDeviceList();
            //s_hardwareInfo.RefreshVideoControllerList();

            //s_hardwareInfo.RefreshAll();
        }

        public static string HardwareInfo()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{s_hardwareInfo.OperatingSystem.Name} {s_hardwareInfo.OperatingSystem.VersionString}");

            var memoryGib = s_hardwareInfo.MemoryStatus.TotalPhysical / 1024f / 1024f / 1024f;
            stringBuilder.AppendLine($"Memory: {memoryGib:f2} GiB");

            foreach (var cpu in s_hardwareInfo.CpuList)
            {
                stringBuilder.AppendLine(cpu.Name);
            }

            foreach (var hardware in s_hardwareInfo.MotherboardList)
            {
                stringBuilder.AppendLine($"{hardware.Manufacturer} {hardware.Product}");

            }

            //foreach (var hardware in s_hardwareInfo.VideoControllerList)
            //{
            //    stringBuilder.AppendLine($"{hardware.Manufacturer} {hardware.Name} {hardware.DriverVersion}");
            //}

            return stringBuilder.ToString();
        }
    }
}
