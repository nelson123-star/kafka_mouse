import time
from kafka import KafkaProducer
from kafka.errors import KafkaError
import json
import pyautogui
import time

# Настройки Kafka
KAFKA_BOOTSTRAP_SERVERS = "localhost:9092"
KAFKA_TOPIC = "test_topic"
TIME_CHECK = 30

if __name__ == '__main__':

    producer = KafkaProducer(
        bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
        value_serializer=lambda m: json.dumps(m).encode("utf-8"),
        retries=3
    )
    
    # Засекаем время работы скрипта
    start_time = time.time()
    
    try:
    
        while True:
        
            try:
                x, y = pyautogui.position()
                print(f"x_coordinates: {x}, y_coordinates: {y}")
                producer.send(KAFKA_TOPIC, {'x_coordinates': x, 'y_coordinates': y})
                producer.flush()
                
                
                now = time.time()
                print(f"Время выполнения: {now - start_time}")
                if now - start_time >= TIME_CHECK:
                    print(f"{TIME_CHECK} секунд истекло, выход из цикла")
                    break
                
                time.sleep(1)  # Пауза 1 сек
                
            except KafkaError as e:
                print(f"Ошибка Kafka: {e}")
            except Exception as e:
                print(f"Неожиданная ошибка: {e}")
    
    finally:
        producer.close()
        print("Producer закрыт")

