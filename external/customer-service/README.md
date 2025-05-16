
## Описание сервиса
Сервис операций над заказчика (клиентами) 
- создать клиента
- получить заказчиков списком

## Инфраструктура
Сервис взаимодействие через GRPC.
Сервис имеет базу данных **customer-service-db**.


## Настройки сервиса

Настройки можно указать через переменные окружения.

### Строка подключения к БД customer-service
Название переменной окружения: `ROUTE256_CUSTOMER_SERVICE_DB_CONNECTION_STRING`
Допустимые значения (строка)
Connection string к БД сервиса заказчиков

## Сущности и связи
Заказчик имеет следующие атрибуты:
- CustomerId - bigint - уникальный идентификатор заказчика
- RegionId - int - идентификатор региона (см. атрибуты региона)
- Name - int - название заказчика
- CreatedAt - tz - дата регистрации заказчика UTC

Заказчик привязан к региону, атрибуты региона:
- RegionId - bigint - идентификатор региона
- RegionName - text - название региона

Заказчики могут быть привязаны к 3м регионам:
- Москва
- Санкт-Петербург
- Екатеринбург

## API-сервиса заказчиков
### Создание новых заказчиков
Новых заказчиков можно создать с помощью ручки. Валидация:
- не может быть 2х заказчиков с одинаковым full_name
- заказчик может быть добавлен только в существующий регион 

```protobuf
rpc V1CreateCustomer (V1CreateCustomerRequest) returns (V1CreateCustomerResponse);

message V1CreateCustomerRequest {
  int64 region_id = 1;
  string full_name = 2;
}

message V1CreateCustomerResponse {
  oneof result {
    Success ok = 1;
    Error error = 2;
  }

  message Success {
    int64 customer_id = 1;
  }

  message Error {
    string code = 1; /* ExceptionName */
    string text = 2; /* Краткое описание ошибки валидации */
  }
}
```

### Метод получения списка заказчиков
Возвращает информацию о заказчиках, позволяет отфильтровать данные по:
- customer_ids - одному или нескольким заказчикам*
- region_ids - одному или нескольким регионам*
- limit/offset - получить данные ограниченными пачками
Список сортируется по customer_id.

* - если передан пустой массив, то фильтрация по параметру не производится

```protobuf
rpc V1QueryCustomers (V1QueryCustomersRequest) returns (stream V1QueryCustomersResponse);

message V1QueryCustomersRequest {
  repeated int64 customer_ids = 1;
  repeated int64 region_ids = 2;
  int32 limit = 3;
  int32 offset = 4;
}

message V1QueryCustomersResponse {
  Customer customer = 1;
  int64 total_count = 2;
  
  message Customer {
    int64 customer_id = 1;
    Region region = 2;
    string full_name = 3;
    google.protobuf.Timestamp created_at = 4;
  }
  
  message Region {
    int64 id = 1;
    string name = 2;
  }
}
```