CREATE TABLE IF NOT EXISTS LVV.kafka_json_dotnet
(
    session_event Tuple(
        Event String,
        Status String,
        TimeEvent DateTime
    ),
    initial_event Tuple(
        Event String,
        namePC String,
        userName String,
        osVersion String,
        TimeEvent DateTime
    ),
    mouse_coordinates Tuple(
        Event String,
        mouseCoordinates Tuple(x UInt32, y UInt32),
        TimeEvent DateTime
    ),
    active_window Tuple(
        Event String,
        ActiveWindowTitle String,
        TimeEvent DateTime
    )
) ENGINE = Kafka()
SETTINGS
    kafka_broker_list = 'kafka:29092',
    kafka_topic_list = 'cs_topic',
    kafka_group_name = 'click_kafka_dotnet',
    kafka_format = 'JSONEachRow';


CREATE TABLE IF NOT EXISTS LVV.activity_monitoring
(
    session_event Tuple(
        Event String,
        Status String,
        TimeEvent DateTime
    ),
    mouse_coordinates Tuple(
        Event String,
        mouseCoordinates Tuple(x UInt32, y UInt32),
        TimeEvent DateTime
    ),
    active_window Tuple(
        Event String,
        ActiveWindowTitle String,
        TimeEvent DateTime
    ),
    inserted_at DateTime DEFAULT now()
)
ENGINE = MergeTree()
ORDER BY (mouseCoordinates.x, mouseCoordinates.y, inserted_at); 


CREATE MATERIALIZED VIEW IF NOT EXISTS LVV.view_kafka_dotnet TO LVV.activity_monitoring AS
SELECT 
    mouse_coordinates.x AS x_coordinates,
    mouse_coordinates.y AS y_coordinates,
    mouse_coordinates.Event AS Event,
    mouse_coordinates.TimeEvent AS TimeEvent
FROM LVV.kafka_json_dotnet;

CREATE MATERIALIZED VIEW IF NOT EXISTS LVV.view_kafka_dotnet_session TO LVV.activity_monitoring AS
SELECT  
    session_event.Event AS Event,
    session_event.Status AS Status,
    session_event.TimeEvent AS TimeEvent
FROM LVV.kafka_json_dotnet;

CREATE MATERIALIZED VIEW IF NOT EXISTS LVV.view_kafka_dotnet_active_window TO LVV.activity_monitoring AS
SELECT  
    active_window.Event AS Event,
    active_window.ActiveWindowTitle AS ActiveWindowTitle,
    active_window.TimeEvent AS TimeEvent
FROM LVV.kafka_json_dotnet;

