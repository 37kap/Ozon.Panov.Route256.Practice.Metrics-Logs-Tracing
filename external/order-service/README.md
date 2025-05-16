
## Описание сервиса
Сервис операций над заказами. Позволяет:
- создать заказ
- получить заказ списком
- отменить заказ
- выдать заказ

## Инфраструктура
Сервис взаимодействие через GRPC и два топика кафки:
- **orders_input** - входящий топик заказов
- **order_output_events** - исходящий топик событий изменения заказа
- **orders_input_errors** - исходящий dead letter queue (dlq)

Сервис имеет БД **order-service-db**.

## Настройки сервиса

Настройки можно указать через переменные окружения.

### Строка подключения к БД
Название переменной окружения: `ROUTE256_ORDER_SERVICE_DB_CONNECTION_STRING`
Допустимые значения (строка)
Connection string к БД сервиса заказов

### Адреса брокеров кафки
Название переменной окружения: `ROUTE256_KAFKA_BROKERS`
Допустимые значения (строка)
Список адресов хостов брокеров кафки, разделенных через запятую

## Сущности и связи
Статусная модель заказа:
- New - все новые заказы создаются с таким статусом
- Cancelled - этот статус заказ получает при отмене
- Delivered - этот статус заказ получает после выдачи

Заказ имеет следующие атрибуты:
- OrderId - bigint - уникальный идентификатор заказа
- RegionId - bigint - идентификатор региона заказа (см. атрибуты региона)
- Status - int - актуальный статус заказа
- CustomerId - bigint - уникальный идентификатор заказчика
- Comment - text - комментарий по заказу
- CreatedAt - tz - дата создания заказа UTC

При изменении заказ логируется в отдельной сущности, сохраняя все новые атрибуты, дополнительно сохраняется:
- метка времени изменения

Каждый заказ включает в себя набор товаров. которые заказчик просил доставить:
- ItemBarcode - text - штрихкод товара (EAM производителя)
- Quantity - int - количество товаров

Заказы поступают с разных регионов, атрибуты региона:
- RegionId - bigint - идентификатор региона
- RegionName - text - название региона

Заказы возможны в 3х регионах:
- Москва
- Санкт-Петербург
- Екатеринбург

## API-сервиса заказов
### Создание новых заказов
Новые заказы поступают в топик **orders_input** в формате json:
```json
{
  "region_id": 0,
  "customer_id": 0,
  "comment": "",
  "items": [ { "barcode":  "", "quantity": 0 } ]
}
```
Заказ проходит валидацию перед тем как будет сохранен в БД:
- указан существующий регион
- передан не пустой список товаров
- для каждого товара кол-во должно быть больше нуля

Если заказ не корректный, такое сообщение отправляется в топик **orders_input_errors** с указанием причины ошибки. Формат сообщения в DLQ топике:
```json
{
  "input_order": {
    /* входщее сообщение о новом заказе */
  },
  "error_reason": {
    "code": "", /* ExceptionName */
    "text": "" /* Краткое описание ошибки валидации */
  }
}
```

### Получение событий по заказу
После успешного сохранения нового заказа или изменения заказа отправляется сообщение в топик **order_output_events** в формате protobuf
```protobuf
message OrderOutputEventMessage {
  int64 order_id = 1;
  EventType event_type = 2;
}

enum OutputEventType {
  EVENT_TYPE_UNDEFINED = 0;
  EVENT_TYPE_CREATED = 1;
  EVENT_TYPE_UPDATED = 2;
}
```

### Метод получения информации по заказам
Возвращает информацию о заказе, позволяет отфильтровать данные по:
- order_ids - одному или нескольким идентификаторам заказа
- customer_ids - одному или нескольким заказчикам
- region_ids - одному или нескольким регионам
- limit/offset - получить данные ограниченными пачками

```protobuf
rpc V1QueryOrders (V1QueryOrdersRequest) returns (stream V1QueryOrdersResponse);

message V1QueryOrdersRequest {
  repeated int64 order_ids = 1;
  repeated int64 customer_ids = 2;
  repeated int64 region_ids = 3;
  int32 limit = 4;
  int32 offset = 5;
}

message V1QueryOrdersResponse {
  message Region {
    int64 id = 1;
    string name = 2;
  }

  int64 order_id = 1;
  Region region = 2;
  OrderStatus status = 3;
  int64 customer_id = 4;
  google.protobuf.StringValue comment = 5;
  google.protobuf.Timestamp created_at = 6;
  int64 total_count = 7;
}

enum OrderStatus {
  ORDER_STATUS_UNDEFINED = 0;
  ORDER_STATUS_NEW = 1;
  ORDER_STATUS_CANCELED = 2;
  ORDER_STATUS_DELIVERED = 3;
}
```

### Метод отмены заказа
Позволяет отменить заказ, находящийся в статусе New. Метод должен быть идемпотентным - на повторную отмену говорить ОК.
Валидация:
- некорректный статус заказа
- заказ не найден

```protobuf
rpc V1CancelOrder (V1CancelOrderRequest) returns (V1CancelOrderResponse);

message V1CancelOrderRequest {
  int64 order_id = 1;
}

message V1CancelOrderResponse {
    oneof result {
      Success ok = 1;
      Error error = 2;
    }
        
    message Success {
    }
    
    message Error {
      string code = 1;
      string text = 2;
    }
}
```

### Метод выдачи заказа
Позволяет выдать заказ, находящийся в статусе New. Метод должен быть идемпотентным - на повторную выдачу говорить ОК.
Валидация:
- некорректный статус заказа
- заказ не найден

```protobuf
rpc V1DeliveryOrder (V1DeliveryOrderRequest) returns (V1DeliveryOrderResponse);

message V1DeliveryOrderRequest {
  int64 order_id = 1;
}

message V1DeliveryOrderResponse {
    oneof result {
      Success ok = 1;
      Error error = 2;
    }
        
    message Success {
    }
    
    message Error {
      string code = 1;
      string text = 2;
    }
}
```
