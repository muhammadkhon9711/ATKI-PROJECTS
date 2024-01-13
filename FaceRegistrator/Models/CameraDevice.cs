using FlashCap;
using System.Collections.ObjectModel;

namespace FaceRegistrator.Models
{
    public class CameraDevice
    {
        private CaptureDeviceDescriptor? descriptor;

        public CameraDevice(CaptureDeviceDescriptor? descriptor)
        {
            this.descriptor = descriptor;
        }

        public override string? ToString()
        {
            return $"{descriptor?.Name} ({descriptor?.DeviceType})";
        }

        public CaptureDeviceDescriptor? GetCaptureDescriptor()
        {
            return descriptor;
        }

        public ReadOnlyCollection<VideoCharacteristics>? GetVideoFormats()
        {
            if (descriptor == null)
                return null;

            return new (descriptor.Characteristics);
        }
    }
}
