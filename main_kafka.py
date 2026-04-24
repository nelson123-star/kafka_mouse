import time
from kafka import KafkaProducer
from kafka.errors import KafkaError
import json
import pyautogui
import datetime
import time

# Настройки Kafka
KAFKA_BOOTSTRAP_SERVERS = "localhost:9092"
KAFKA_TOPIC = "test_topic"

if __name__ == '__main__':

    producer = KafkaProducer(
        bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
        value_serializer=lambda m: json.dumps(m).encode("utf-8"),
        retries=3
    )
    
    # start_time = datetime.datetime.now()  # Явно указать datetime
    start_time = time.time()
    
    try:
    
        while True:
        
            try:
                x, y = pyautogui.position()
                print(f"x_coordinates: {x}, y_coordinates: {y}")
                producer.send(KAFKA_TOPIC, {'x_coordinates': x, 'y_coordinates': y})
                producer.flush()
                
                # now = datetime.datetime.now()
                now = time.time()
                print(f"Время выполнения: {now - start_time}")
                if now - start_time >= 30:
                    print("30 секунд истекло, выход из цикла")
                    break
                
                time.sleep(1)  # Пауза 1 сек
                
                
            except pyautogui.FailSafeException:
                print("PyAutoGUI FailSafe сработал (двигайте мышь в угол экрана для остановки)")
                break
            except KafkaError as e:
                print(f"Ошибка Kafka: {e}")
                # Продолжаем цикл, не прерываем
            except Exception as e:
                print(f"Неожиданная ошибка: {e}")
                

    
    finally:
        producer.close()
        print("Producer закрыт")


# Пример из Хабра: https://habr.com/ru/companies/otus/articles/789896/
# import json
# import random
# import time
# from confluent_kafka import Producer

# # конфигурация Producer'а
# config = {
    # 'bootstrap.servers': 'localhost:9092',
    # 'client.id': 'python-producer'
# }
# producer = Producer(config)

# # функция для генерации случайных данных
# def generate_data():
    # return {
        # 'sensor_id': random.randint(1, 100),
        # 'temperature': random.uniform(20.0, 30.0),
        # 'humidity': random.uniform(30.0, 50.0),
        # 'timestamp': int(time.time())
    # }

# # функция для сериализации данных в JSON
# def serialize_data(data):
    # return json.dumps(data)

# # функция для отправки сообщения
# def send_message(topic, data):
    # producer.produce(topic, value=data)
    # producer.flush()

# # основной цикл отправки сообщений
# try:
    # while True:
        # # генерируем случайные данные
        # data = generate_data()
        
        # # сериализуем данные
        # serialized_data = serialize_data(data)

        # # отправляем данные в Kafka
        # send_message('sensor_data', serialized_data)

        # # логирование отправленного сообщения
        # print(f'Sent data: {serialized_data}')

        # # пауза между отправками
        # time.sleep(1)
# except KeyboardInterrupt:
    # print('Stopped.')

# producer.close()














# # Инициализация Kafka Producer
# producer = KafkaProducer(
    # bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS,
    # value_serializer=lambda v: json.dumps(v).encode("utf-8"),
# )

# # Модель для входных данных
# class Message(BaseModel):
    # text: str

# Эндпоинт для отправки сообщения в Kafka
# @app.post("/send/")
# async def send_message(message: Message):
    # # Отправка сообщения в Kafka
    # producer.send(KAFKA_TOPIC, value={"text": message.text})
    # producer.flush()
    # return {"status": "Message sent to Kafka", "message": message.text}