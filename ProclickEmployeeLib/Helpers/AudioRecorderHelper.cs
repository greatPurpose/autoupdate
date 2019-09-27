using System;
using System.Linq;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;

namespace ProclickEmployeeLib.Helpers
{
    class AudioRecorderHelper
    {
        private static WasapiCapture capture;
        private static WaveWriter writer;
        private static EventHandler<DataAvailableEventArgs> writeEvent;
        private static string filePath;

        private static void Configure(string name = null)
        {
            using (MMDeviceEnumerator enumerator = new MMDeviceEnumerator())
            {
                using (MMDeviceCollection devices = enumerator.EnumAudioEndpoints(DataFlow.Capture, DeviceState.Active))
                {
                    if (devices.Count == 0)
                        throw new Exception("There are no audio devices to select from!");
                    MMDevice device = name is null ? devices.First() : devices.First(x => x.FriendlyName == name);
                    capture = new WasapiCapture
                    {
                        Device = device
                    };
                    capture.Initialize();

                    capture.DataAvailable -= writeEvent;
                    capture.DataAvailable += writeEvent;
                }
            }
        }

        /// <summary>
        /// start the audio recorder
        /// </summary>
        /// <param name="path"></param>
        /// <param name="deviceName"></param>
        public static void StartRecording(string path, string deviceName = null)
        {
            filePath = path;

            writeEvent = new EventHandler<DataAvailableEventArgs>((s, e) =>
            {
                writer.Write(e.Data, e.Offset, e.ByteCount);
            });

            Configure(deviceName);

            writer = new WaveWriter(path, capture.WaveFormat);

            capture.Start();
        }

        /// <summary>
        /// Stop the audio recorder
        /// </summary>
        /// <returns>Path to recorded audio</returns>
        public static string StopRecording()
        {
            if (capture is null)
                throw new InvalidOperationException("Audio recorder is not running!");
            capture.Stop();
            writer.Dispose();
            return filePath;
        }
    }
}
