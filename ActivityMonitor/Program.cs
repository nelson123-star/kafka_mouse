using Confluent.Kafka;
using System.Text.Json;
using System;
using System.Collections.Generic;
using ActivityMonitor;
using IniParser;
using IniParser.Model;

// Пример работы с Кафкой, еще используется код из другого файла
// https://www.youtube.com/watch?v=OvLZalwpbqQ


internal class Program
{
    private static void Main(string[] args)
    {
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
        // !int.TryParse(config["Monitoring"]["IntervalMs"], out int intervalMs) - пытается преобразовать строку из конфигурации в целое число. Если преобразование не удается (например, если строка не является числом), то метод возвращает false, и переменная intervalMs будет установлена в 0. В этом случае мы присваиваем ей дефолтное значение 5000 миллисекунд (5 секунд).
        // out variable syntax позволяет объявить переменную прямо в условии, что упрощает код и делает его более читаемым.
        // Если в файле конфигурации указано некорректное значение для интервала или максимальной продолжительности, скрипт не упадет, а будет использовать безопасные дефолтные значения, что обеспечивает стабильную работу.
        if (!int.TryParse(config["Monitoring"]["IntervalMs"], out int intervalMs))
            intervalMs = 5000; // дефолт 5 секунд

        if (!int.TryParse(config["Monitoring"]["MaxDurationSeconds"], out int maxDurationSeconds))
            maxDurationSeconds = 7200; // дефолт 2 часа

        if (!int.TryParse(config["Kafka"]["MessageTimeoutMs"], out int MessageTimeoutMs))
            MessageTimeoutMs = 5000; // дефолт 5 секунд

        DateTime start = DateTime.Now;

        // Вывод времени на консоль
        Console.WriteLine($"Время старта: {start.Hour} часов, {start.Minute} минут, {start.Second} секунд");


        // Создаем продюсера и подписываемся на события сессии, отправляем данные в Kafka
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = host,
            // Задаем таймаут для синхронных проверок отправки (по умолчанию стоит 5 минут, снизим до 5 секунд)
            MessageTimeoutMs = MessageTimeoutMs
        };

        IProducer<string, string> producer = null;
        var employee = new EmployeeActivity();

        try
        {
            producer = new ProducerBuilder<string, string>(producerConfig)
                // ШАГ 1: Перехватываем внутренние системные ошибки rdkafka
                .SetErrorHandler((p, error) => 
                {
                    Console.WriteLine($"[KAFKA CRITICAL ERROR] Код: {error.Code}, Причина: {error.Reason}");
                    // if (error.IsFatal)
                    // {
                    //     // Тут можно залогировать критический сбой инфраструктуры
                    // }
                })
                .Build();

            // ШАГ 2: Тестовая синхронная проверка связи. 
            // Отправляем пустое сервисное сообщение. Если Кафка лежит — код упадет сразу в catch.
            Console.WriteLine("Проверка соединения с Kafka...");
            producer.ProduceAsync(topic, new Message<string, string> { Key = "ping", Value = "test_connection" })
                    .GetAwaiter().GetResult(); 
            Console.WriteLine("Соединение успешно установлено.");
        }
        catch (ProduceException<string, string> ex)
        {
            Console.WriteLine($"[ОШИБКА ПОДКЛЮЧЕНИЯ KAFKA]: Сервер {host} недоступен. Работа скрипта завершена. Тех. инфо: {ex.Error.Reason}");
            return; // Завершаем приложение, так как брокер отключен
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Критическая ошибка инициализации: {ex.Message}");
            return;
        }

        // try
        // {
        //     // Получаем и отправляем начальную информацию о ПК и пользователе в Kafka
        //     var initialData = new 
        //     {
        //         Event = "initial_info",
        //         NamePC = employee.GetNamePC(),
        //         UserName = employee.GetUserName(),
        //         OSVersion = employee.GetOSVersion(),
        //         Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        //     };
        //     string initialJson = JsonSerializer.Serialize(initialData);

        //     producer.Produce(topic, new Message<string, string> { 
        //         Key = "initial_info", 
        //         Value = initialJson 
        //     });

        //     Console.WriteLine($"[INITIAL INFO] ПК: {initialData.NamePC}, Пользователь: {initialData.UserName}, OS: {initialData.OSVersion}");
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"Ошибка при отправке начальной информации: {ex.Message}");
        // }

        try
        {
            employee.SubscribeSessionEvents(status =>
            {
                var sessionData = new
                {
                    Event = "session_event",
                    Status = status,
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                string sessionJson = JsonSerializer.Serialize(sessionData);

                // Отправляем событие в Kafka
                producer.Produce(topic, new Message<string, string>
                {
                    Key = "session_event",
                    Value = sessionJson
                });

                Console.WriteLine($"[SYSTEM EVENT] Экран: {status}");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при подписке на события сессии: {ex.Message}");
        }
        var namePC = employee.GetNamePC();
        Console.WriteLine($"Название ПК: {namePC}");

        var userName = employee.GetUserName();
        Console.WriteLine($"Имя пользователя: {userName}");

        var osVersion = employee.GetOSVersion();
        Console.WriteLine($"Версия OS: {osVersion}");



        try
        {
            // Переменная для отслеживания последней минуты, когда была выведена статистика
            int lastReportedMinute = 0;

            while (true)
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

                // Закомментировал, чтобы не засорять Кафку, дублирующий блок подписки на SubscribeSessionEvents, который приводил к утечке памяти
                // Подписываемся на события сессии и отправляем их в Kafka
                // employee.SubscribeSessionEvents(status =>
                // {
                //     Console.WriteLine($"Система сообщила: {status}");
                //     producer.Produce(topic, new Message<string, string> { Key = "session_event", Value = ActiveWindowTitle });
                // });


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
                Thread.Sleep(intervalMs);

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
    }
}