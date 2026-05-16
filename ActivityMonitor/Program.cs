using Confluent.Kafka;
using System.Text.Json;
using System;
using System.Collections.Generic;
using ActivityMonitor;


// Пример работы с Кафкой, еще используется код из другого файла
// https://www.youtube.com/watch?v=OvLZalwpbqQ


DateTime start = DateTime.Now;

// Вывод времени на консоль
Console.WriteLine($"Время старта: {start.Hour} часов, {start.Minute} минут, {start.Second} секунд");


string HOST = "localhost:9092";
string TOPIC = "cs_topic";


var producerConfig = new ProducerConfig(
    new Dictionary<string,string>{
        {"bootstrap.servers", HOST},

    }
);

using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
var employee = new EmployeeActivity();

employee.SubscribeSessionEvents(status => {
        var sessionData = new { 
            Event = "SessionChange", 
            Status = status, 
            Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") 
        };
        string sessionJson = JsonSerializer.Serialize(sessionData);
        
        // Отправляем событие блокировки мгновенно
        producer.Produce(TOPIC, new Message<string, string> { 
            Key = "session_event", 
            Value = sessionJson 
        });
        
        Console.WriteLine($"[SYSTEM EVENT] Экран: {status}");
    });

try 
{
    // Console.WriteLine("Координаты мыши (будут выводиться 30 секунд):");
    
    while(true)
    {
        
        var myDict = employee.GetCursorPosition();

        string jsonValue = JsonSerializer.Serialize(myDict);

        DateTime end = DateTime.Now;
        TimeSpan duration = end - start;

        producer.Produce(TOPIC, new Message<string, string> { Key = "mouse_coordinates", Value = jsonValue });

        string ActiveWindowTitle = employee.GetActiveWindowTitle();
        producer.Produce(TOPIC, new Message<string, string> { Key = "active_window", Value = ActiveWindowTitle });

        employee.SubscribeSessionEvents(status => {
    Console.WriteLine($"Система сообщила: {status}");
        producer.Produce(TOPIC, new Message<string, string> { Key = "active_window", Value = ActiveWindowTitle });
});

        if (duration.TotalSeconds >= 60)
        {
            // Перенесли вывод статистики сюда, так как здесь duration уже существует
            Console.WriteLine($"--- Статистика ---");
            Console.WriteLine($"Прошло времени: {duration.Hours} ч, {duration.Minutes} мин, {duration.Seconds} сек");
            Console.WriteLine($"Всего минут: {duration.TotalMinutes}"); 

            break;
        }

        System.Threading.Thread.Sleep(5000);

    }

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

