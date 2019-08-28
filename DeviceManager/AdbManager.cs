using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DeviceManager
{
    public class AdbManager
    {
        private readonly string scrScpPath;
        private readonly string adb;
        public AdbManager(string scrScpPath)
        {
            this.scrScpPath = scrScpPath;
            adb = Path.Combine(this.scrScpPath, "adb.exe");
        }

        public List<DeviceData> GetDevices()
        {
            var result = ConsoleExeRunner.Execute(adb, "devices");

            bool skipping = true;
            var deviceSerials = new List<string>();
            foreach (var line in result)
            {
                if (line.StartsWith("List of devices"))
                {
                    skipping = false;
                    continue;
                }

                if(!line.Contains("device")) continue;

                if(skipping) continue;

                if(string.IsNullOrWhiteSpace(line)) continue;

                deviceSerials.Add(line.Split('\t')[0]);
            }

            var devices = new List<DeviceData>();
            foreach (var serial in deviceSerials)
            {
                var data = GetDeviceData(serial);

                devices.Add(data);
            }

            return devices;
        }

        public DeviceData GetDeviceData(string serial)
        {
            var result = ConsoleExeRunner.Execute(adb, $"-s {serial} shell getprop");

            /*
             *
             *[ro.product.model]: [Nexus 9]
                [ro.product.name]: [volantis]
             *
             */
            var deviceData = new DeviceData
            {
                Serial = serial,
                State = DeviceState.Offline

            };

            bool TryParse(string line, out (string key, string value) res)
            {
                var match = Regex.Match(line, @"\[(?<key>.*)\]: \[(?<value>.*)\]");
                if (match.Success)
                {
                    res = (match.Groups["key"].Value, match.Groups["value"].Value);
                    return true;
                }

                res = (String.Empty, String.Empty);
                return false;
            }
            
            foreach (var line in result)
            {
                if (TryParse(line, out var kv))
                {
                    deviceData.State = DeviceState.Online;
                    if (kv.key == "ro.product.model") { deviceData.Model = kv.value; }
                    if (kv.key == "ro.product.name") { deviceData.Name = kv.value; }
                }
            }

            return deviceData;
        }

        public void Reboot(DeviceData device)
        {
            ConsoleExeRunner.Execute(adb, $"-s {device.Serial} reboot");
        }
    }
}