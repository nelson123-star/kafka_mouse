using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;

namespace ActivityMonitor
{
    class EmployeeActivity
    {
        public record MousePosition(int X, int Y);
        
        // TODO: изменить метод GetCursorPosition, чтобы возвращал MousePosition
        // Метод получения координаты мыши
        public Dictionary<string, int> GetCursorPosition()
        {
            try
            {
                var position = System.Windows.Forms.Cursor.Position;
                return new Dictionary<string, int>
                {
                    {"x_coordinates", position.X},
                    {"y_coordinates", position.Y}
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в работе скрипта: {ex.Message}");
                return new Dictionary<string, int>
                {
                    {"x_coordinates_ScriptError", -1},
                    {"y_coordinates_ScriptError", -1}
                };
            }
        }

        // Метод получения Активные окна/приложения (GetForegroundWindow(), EnumWindows)
        
        // Импорт функций Win32 API
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        // Метод получения названия активного окна
        public string GetActiveWindowTitle()
        {
            // 1. Получаем хендл активного окна
            IntPtr handle = GetForegroundWindow();

            // 2. Узнаем длину заголовка
            int length = GetWindowTextLength(handle);
            if (length == 0) return "Заголовок не найден или окно пустое";

            StringBuilder sb = new StringBuilder(length + 1);
            
            // 3. Извлекаем текст
            GetWindowText(handle, sb, sb.Capacity);
            
            return sb.ToString();
        }

        // Метод получения статуса блокировки экрана (через SystemEvents.SessionSwitch)
        // Подписка на события блокировки экрана (нужно запускать в UI-потоке или спец. окне)
        public void SubscribeSessionEvents(Action<string> onEventOccurred)
        {

            // SystemEvents.SessionSwitch генерирует события при смене сессии, таких как блокировка, разблокировка, вход и выход пользователя. В обработчике мы можем определить тип события и отправить соответствующую информацию в Kafka. Важно убедиться, что этот код выполняется в контексте, который позволяет обрабатывать события Windows (например, в WPF или WinForms приложении), так как в консольном приложении могут возникать сложности с обработкой таких событий.
            SystemEvents.SessionSwitch += (s, e) => 
            {
                // Получаем статус в виде строки
                string status = e.Reason.ToString(); 
                
                // Вызываем переданное действие (например, отправку в Кафку)
                onEventOccurred(status);
            };
        }


        // Метод получения имя ПК
        public string GetNamePC()
        {
            try
            {
                return Environment.MachineName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении имени ПК: {ex.Message}");
                return "Unknown_PC";
            }
        }

        // Метод получения имя пользователя        
        public string GetUserName()
        {
            try
            {
                return Environment.UserName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении имени пользователя: {ex.Message}");
                return "Unknown_User";
            }
        }

        // Метод получения версии OS
        public string GetOSVersion()
        {
            try
            {
                return Environment.OSVersion.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении версии операционной системы: {ex.Message}");
                return "Unknown_OS";                
            }
        }

        // TODO: текущего Wi-Fi, загрузки CPU и RAM, подключенных USB-устройств   
        // public

        // // Метод получения сбора количество кликов и нажатий клавиш
        // public int KeyStrokes { get; private set; }
        // public double MouseDistance { get; private set; }
        // private Point _lastPos;

        // // Вызывать по таймеру или через Global Hook
        // public void UpdateMouseStats()
        // {
        //     var currentPos = System.Windows.Forms.Cursor.Position;
        //     if (!_lastPos.IsEmpty)
        //     {
        //         double dist = Math.Sqrt(Math.Pow(currentPos.X - _lastPos.X, 2) + Math.Pow(currentPos.Y - _lastPos.Y, 2));
        //         MouseDistance += dist;
        //     }
        //     _lastPos = currentPos;
        // }

        // public void OnKeyPressed() => KeyStrokes++;

        // Получение имени активного Wi-Fi (через интерфейс командной строки или Managed Wifi)
        public string GetCurrentWifiSSID()
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "wlan show interfaces",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            // Далее парсим строку, содержащую "SSID"
            return output; 
        }

        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

        public float GetCpuUsage() => cpuCounter.NextValue();
        public float GetAvailableRam() => ramCounter.NextValue();

        // Получение списка подключенных USB-устройств (через WMI)
        public List<string> GetUsbDevices()
        {
            var devices = new List<string>();
            using (var searcher = new System.Management.ManagementObjectSearcher(@"Select * From Win32_USBControllerDevice"))
            {
                foreach (var device in searcher.Get())
                {
                    devices.Add(device["Dependent"].ToString());
                }
            }
            return devices;
        }

    }
    
}