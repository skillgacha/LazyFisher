using System;
using System.IO;
using System.Text.Json;

namespace AutoClicker.Core
{
    public class AppSettings
    {
        public int Hours { get; set; } = 0;
        public int Minutes { get; set; } = 0;
        public int Seconds { get; set; } = 1; // Default requested by user
        public int Milliseconds { get; set; } = 0;

        public int MouseButtonIndex { get; set; } = 0; // 0=Left
        public int ClickTypeIndex { get; set; } = 0; // 0=Single

        public bool RepeatInfinite { get; set; } = true;
        public int RepeatCount { get; set; } = 100;

        public bool PositionCurrent { get; set; } = true;
        public int FixedX { get; set; } = 0;
        public int FixedY { get; set; } = 0;

        public int ClickDuration { get; set; } = 50; // Default requested by user

        public int HotKey { get; set; } = 0x75; // VK_F6
        public int HotKeyModifiers { get; set; } = 0; // MOD_NONE

        public bool AlwaysOnTop { get; set; } = true;

        public double WindowWidth { get; set; } = 400;
        public double WindowHeight { get; set; } = 600;

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists("settings.json"))
                {
                    string json = File.ReadAllText("settings.json");
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText("settings.json", json);
            }
            catch { }
        }
    }
}
