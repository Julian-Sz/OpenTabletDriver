using System;

namespace OpenTabletDriver.Plugin.Tablet
{
    public interface IGestureTouchReport : IDeviceReport
    {
        bool[] TouchGestures { set; get; }
    }
}
