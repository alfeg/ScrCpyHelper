using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using DeviceManager.Properties;
using SharpAdbClient;

namespace DeviceManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            RestoreConfig();
            SetScrPath();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var menu = new ContextMenu();

            menu.Popup += (sender, args) =>
            {
                var client = new SharpAdbClient.AdbClient();
                var devices = client.GetDevices();

                menu.MenuItems.Clear();

                menu.MenuItems.Add(new MenuItem("Set srccpy path", (o, eventArgs) => { ChooseScrPath(); }));

                if (CanViewDevices())
                {
                    var viewMenu = devices.Select(device =>
                            new MenuItem(device.Model, (o, eventArgs) => { ViewDevice(device); }))
                        .ToArray();

                    menu.MenuItems.Add(new MenuItem("View", viewMenu));
                }

                var restartMenu = devices.Select(device => new MenuItem(device.Model,
                    (o, eventArgs) => { RestartAction(client, device); })).ToArray();

                menu.MenuItems.Add(new MenuItem("Restart", restartMenu));

                menu.MenuItems.Add(new MenuItem("Exit", (o, eventArgs) => { Application.Exit(); }));

            };
            // Show the system tray icon.
            using (var pi = new ProcessIcon(menu))
            {
                pi.Display();

                // Make sure the application runs!
                Application.Run();
            }
        }

        private static void SetScrPath()
        {
            if (File.Exists("scrcpy-noconsole.exe") && string.IsNullOrEmpty(Settings.Default.ScrCpyPath))
            {
                Settings.Default.ScrCpyPath = Directory.GetCurrentDirectory();
                Settings.Default.Save();
            }
        }

        private static void RestartAction(AdbClient client, DeviceData device)
        {
            client.Reboot("device", device);

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    var targetDevice = client.GetDevices().SingleOrDefault(d => d.Serial == device.Serial);

                    if (targetDevice != null && targetDevice.State == DeviceState.Online)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        ViewDevice(targetDevice);
                        return;
                    }
                }
            });
        }

        private static void ChooseScrPath()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (!string.IsNullOrEmpty(Settings.Default.ScrCpyPath))
                {
                    fbd.SelectedPath = Settings.Default.ScrCpyPath;
                }

                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    if (File.Exists(Path.Combine(fbd.SelectedPath, "scrcpy-noconsole.exe")))
                    {
                        Settings.Default.ScrCpyPath = fbd.SelectedPath;
                        Settings.Default.Save();
                    }
                }
            }
        }

        private static bool CanViewDevices()
        {
            var scr = Path.Combine(Settings.Default.ScrCpyPath, "scrcpy-noconsole.exe");
            return File.Exists(scr);
        }

        private static void ViewDevice(DeviceData device)
        {
            var scr = Path.Combine(Settings.Default.ScrCpyPath, "scrcpy-noconsole.exe");
            var processStartInfo = new ProcessStartInfo(scr, "-t -s " + device.Serial)
            {
                UseShellExecute = false,
                WorkingDirectory = Settings.Default.ScrCpyPath
            };

            Process.Start(processStartInfo);
        }

        private static void RestoreConfig()
        {
            var config = Application.ExecutablePath + ".config";
            if (File.Exists(config)) return;

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DeviceManager.App.config";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return;

                using (var file = File.Create(config))
                {
                    stream.CopyTo(file);
                }
            }
        }
    }

    public class ProcessIcon : IDisposable
    {
        public NotifyIcon Icon { get; set; }

        public ProcessIcon(ContextMenu menuStrip)
        {
            Icon = new NotifyIcon
            {
                Icon = Resource.TrayIcon,
                ContextMenu = menuStrip
            };
        }

        public void Display()
        {
            Icon.Text = @"Android device manager";
            Icon.Visible = true;
        }

        public void Dispose()
        {
            Icon.Dispose();
        }
    }
}
