using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PowerMode
{
    public static class Program
    {
        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);

        public static void Main(string[] args)
        {
            OnPowerModeChanged(null, new PowerModeChangedEventArgs(PowerModes.Resume));
            var trayIcon = new NotifyIcon();
            trayIcon.Text = "Power Mode";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);
            trayIcon.Visible = true;
            var hWnd = GetConsoleWindow();
            var visible = false;
            
            trayIcon.MouseClick += delegate(object sender, MouseEventArgs eventArgs)
            {
                visible = !visible;
                Log("Showing!");
                ShowWindow(hWnd, visible ? 1 : 0); //hide or show
            }; 
                
            StartListening();
            var hide = new Thread(() =>
            {
                ShowWindow(hWnd, 0); //hide
                Console.ReadKey();
                OnPowerModeChanged(null, new PowerModeChangedEventArgs(PowerModes.Suspend));
                Application.Exit();
            });
            hide.Start();
            Application.Run();
        }

        private static void TrayIconOnMouseUp(object sender, MouseEventArgs e)
        {
            Log("Yes!");
        }

        public static void Log(string message)
        {
            message = $"{DateTime.Now} - {message}";
            if (Environment.UserInteractive)
            {
                Console.WriteLine(message);
            }

            var path = AppDomain.CurrentDomain.BaseDirectory + "Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var filepath = AppDomain.CurrentDomain.BaseDirectory + "Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (var sw = File.CreateText(filepath))
                {
                    sw.WriteLine(message);
                }
            }
            else
            {
                using (var sw = File.AppendText(filepath))
                {
                    sw.WriteLine(message);
                }
            }
        }

        private static void StartListening()
        {
            Log("Adding listener.");
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        private static void StopListening()
        {
            Log("Removing listener.");
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        }

        private static void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            try
            {
                // Resume, StatusChange, Suspend
                var exec = $"{AppDomain.CurrentDomain.BaseDirectory}PowerMode-{e.Mode}.bat";
                Log("PowerMode changed, executing: " + exec);
                Process.Start(exec);
            }
            catch (Exception exception)
            {
                Log(exception.ToString());
            }
        }
    }
}