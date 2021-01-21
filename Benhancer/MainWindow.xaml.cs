using System;
using System.Windows;
using Gma.System.MouseKeyHook;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;


namespace Benhancer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IKeyboardMouseEvents m_Events;

        Memory.Mem mem = new Memory.Mem();
        // Pointers
        public string fovPointer = "Minecraft.Windows.exe+0x02998E58,0xC8,0x120,0xF0"; // 1.12.1
        public string hidehandPointer = "Minecraft.Windows.exe+0x02998E58,0xC0,0xC50,0xE8"; // 1.12.1
        public string sensitivityPointer = "Minecraft.Windows.exe+0x02998E58,0xE8,0x18,0xBD0,0x108,0x48,0x14"; // 1.12.1

        // Init pointer variables
        public string UserFov = null;
        public string UserHideHand = null;
        public string UserSensitivity = null;

        public bool IsZoomEnabled = false;
        public MainWindow()
        {
            this.Show();
            while (MinecraftInject() == false)
            {
                MinecraftInject();
            }
            InitializeComponent();
            //Hook keyboard to make shortcuts
            m_Events = Hook.GlobalEvents();
            m_Events.KeyDown += OnKeyDown;
            m_Events.KeyUp += OnKeyUp;

            //Zoom();
        }
        public bool MinecraftInject()
        {
            bool isInjected = mem.OpenProcess(mem.GetProcIdFromName("Minecraft.Windows"));
            if (isInjected)
            {
                Console.WriteLine("Injected...");
                Console.WriteLine("Process PID: " + mem.GetProcIdFromName("Minecraft.Windows"));
                return true;
            }
            else { Console.WriteLine("Not Injected...Trying to inject after 5 sec"); Thread.Sleep(5000); return false; }
        }
        public void Zoom(bool Enable)
        {
            switch (Enable)
            {
                case true:
                    UserFov = mem.ReadInt(fovPointer).ToString();
                    UserHideHand = mem.ReadInt(hidehandPointer).ToString();
                    UserSensitivity = mem.ReadInt(sensitivityPointer).ToString();
                    bool isFovChanged = mem.WriteMemory(fovPointer, "int", "1106247680"); // '1106247680' is 30 fov
                    if (Properties.Settings.Default.hide_hand) { bool isHideHandChanged = mem.WriteMemory(hidehandPointer, "int", "1"); }
                    if (Properties.Settings.Default.change_sens) { bool isSensitivityChanged = mem.WriteMemory(sensitivityPointer, "int", "0"); }
                    break;
                case false:
                    mem.WriteMemory(fovPointer, "int", UserFov); // '1106247680' is 30 fov
                    if (Properties.Settings.Default.hide_hand) { bool isHideHandChanged = mem.WriteMemory(hidehandPointer, "int", UserHideHand); }
                    if (Properties.Settings.Default.change_sens) { bool isSensitivityChanged = mem.WriteMemory(sensitivityPointer, "int", UserSensitivity); }
                    break;
            }
        }
        private void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            switch (IsZoomEnabled)
            {
                case true:
                    break;
                case false:
                    if (e.KeyCode.ToString() == Properties.Settings.Default.zoom_button)
                    {
                        Zoom(true);
                        IsZoomEnabled = true;
                    }
                    else if (e.KeyCode.ToString() == "Escape")
                    {
                        Console.WriteLine("kkk");
                        int pid = mem.GetProcIdFromName("Minecraft.Windows");
                        var process = System.Diagnostics.Process.GetProcessById(pid);
                        //process.;

                    }
                    break;
            }
        }
        private void OnKeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode.ToString() == Properties.Settings.Default.zoom_button)
            {
                Zoom(false);
                IsZoomEnabled = false;
            }
        }
    }
}