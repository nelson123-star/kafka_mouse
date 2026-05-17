using Confluent.Kafka;
using System.Text.Json;
using System;
using System.Collections.Generic;
using ActivityMonitor;
using IniParser;
using IniParser.Model;

// Пример работы с Кафкой, еще используется код из другого файла
// https://www.youtube.com/watch?v=OvLZalwpbqQ


// Чтение конфигурации из INI файла
var parser = new FileIniDataParser();
IniData config;

string configPath = "settings.ini";


// todo: ДОБАВИТЬ ПРОВЕРКУ НАЛИЧИЯ ФАЙЛА И ОБРАБОТКУ ОТСУТСТВИЯ
// Проверяем наличие файла конфигурации
if (!File.Exists(configPath))
{
    Console.WriteLine($"Ошибка: Файл конфигурации {configPath} не найден!");
    return; 
}

// Создаем парсер и считываем конфигурацию из файла
config = parser.ReadFile(configPath);

// Получаем параметры из конфигурации 
string host = config["Kafka"]["Host"];
string topic = config["Kafka"]["Topic"];

// Проверяем и парсим числовые параметры, если не удается - используем дефолтные значения
if (!int.TryParse(config["Monitoring"]["IntervalMs"], out int intervalMs)) 
    intervalMs = 5000; // дефолт 5 секунд

if (!int.TryParse(config["Monitoring"]["MaxDurationSeconds"], out int maxDurationSeconds)) 
    maxDurationSeconds = 7200; // дефолт 2 часа



DateTime start = DateTime.Now;

// Вывод времени на консоль
Console.WriteLine($"Время старта: {start.Hour} часов, {start.Minute} минут, {start.Second} секунд");

// Конфигурация продюсера Kafka
var producerConfig = new ProducerConfig(
    new Dictionary<string,string>{
        {"bootstrap.servers", host},

    }
);

// Создаем продюсера и подписываемся на события сессии, отправляем данные в Kafka
using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
var employee = new EmployeeActivity();

employee.SubscribeSessionEvents(status => {
        var sessionData = new { 
            Event = "session_event", 
            Status = status, 
            Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") 
        };
        string sessionJson = JsonSerializer.Serialize(sessionData);
        
        // Отправляем событие в Kafka
        producer.Produce(topic, new Message<string, string> { 
            Key = "session_event", 
            Value = sessionJson 
        });
        
        Console.WriteLine($"[SYSTEM EVENT] Экран: {status}");
    });

try 
{
    // Переменная для отслеживания последней минуты, когда была выведена статистика
    int lastReportedMinute = 0;

    while(true)
    {
        
        var myDict = employee.GetCursorPosition();

        string jsonValue = JsonSerializer.Serialize(myDict);

        
        DateTime end = DateTime.Now;
        TimeSpan duration = end - start;

        int currentMinute = (int)duration.TotalMinutes;

        // Отправляем данные о координатах мыши в Kafka
        producer.Produce(topic, new Message<string, string> { Key = "mouse_coordinates", Value = jsonValue });

        // Получаем название активного окна и отправляем в Kafka
        string ActiveWindowTitle = employee.GetActiveWindowTitle();
        producer.Produce(topic, new Message<string, string> { Key = "active_window", Value = ActiveWindowTitle });

        // Подписываемся на события сессии и отправляем их в Kafka
        employee.SubscribeSessionEvents(status => {
    Console.WriteLine($"Система сообщила: {status}");
        producer.Produce(topic, new Message<string, string> { Key = "session_event", Value = ActiveWindowTitle });
    });


        // Вывод статистики каждые 60 секунд
        if (currentMinute > lastReportedMinute)
        {
            // Вывод статистики на консоль
            Console.WriteLine($"--- Статистика ---");
            Console.WriteLine($"Прошло времени: {duration.Hours} ч, {duration.Minutes} мин, {duration.Seconds} сек");
            Console.WriteLine($"Всего минут: {duration.TotalMinutes}"); 

            lastReportedMinute = currentMinute;
            Console.WriteLine($"lastReportedMinute: {lastReportedMinute}"); 
        }

        // Останавливаем скрипт после заданного времени для демонстрации
        if (duration.TotalSeconds >= maxDurationSeconds) 
        {
            break;
        }

        // Пауза между итерациями (например, 5 секунд)
        System.Threading.Thread.Sleep(intervalMs);

    }

    // Гарантируем отправку всех сообщений перед завершением
    producer.Flush(TimeSpan.FromSeconds(10));

}
catch (Exception ex) 
{
    Console.WriteLine($"Ошибка в работе скрипта: {ex.Message}");
}
finally 
{
    Console.WriteLine("Завершение работы скрипта");
}

