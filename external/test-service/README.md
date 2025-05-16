## Описание сервиса
Сервис для тестирования сервисов и инфраструктуры. 
Проверяет, что за указанный интервал времени были созданы заказы, созданы заказчики, а невалидные заказы попали в DLQ-топик и не записались в БД.

Сервис консьюмит топики:
- события заказа **order_output_events**
- ошибки создания заказа **orders_input_errors**

Сервис подключается к базам данных:
- **order-service-db**
- **customer-service-db**

## Настройки сервиса

Настройки можно указать через переменные окружения.

### Адреса брокеров кафки
Название переменной окружения: `ROUTE256_KAFKA_BROKERS`
Допустимые значения (строка)
Список адресов хостов брокеров кафки, разделенных через запятую

### Таймаут теста
Название переменной окружения: `ROUTE256_TEST_SERVICE_TIMEOUT_SECONDS`
Допустимые значения 1-1000
Количество времени, сколько мы собираем данные для теста

### Строка подключения к БД order-service
Название переменной окружения: `ROUTE256_ORDER_SERVICE_DB_CONNECTION_STRING`
Допустимые значения (строка)
Connection string к БД сервиса заказов

### Строка подключения к БД customer-service
Название переменной окружения: `ROUTE256_CUSTOMER_SERVICE_DB_CONNECTION_STRING`
Допустимые значения (строка)
Connection string к БД сервиса заказчиков

## Алгоритм работы

Сервис при запуске начинает слушать топики order_output_events и orders_input_errors.
Но без выставления флага регистрации событий расхождений, не будут детектироваться проблемы.

Если нужно начать отслеживание расхождений, то нужно вызвать метод API для начала сбора.
Время выставляется либо через вызов, либо берется из `ROUTE256_TEST_SERVICE_TIMEOUT_SECONDS`.

Для получения деталей по расхождения можно вызвать отдельный метод API, который вернет детальную статистику.

Для прекращения работы есть отдельный метод API для завершения сбора.

Работать с API сервиса можно через Swagger.

### Асинхронный сбор расхождений

Асинхронно осуществляются следующие проверки корректность генерации:
1. В топики **order_output_events**, **orders_input_errors** насыпало сообщений
1. Все заказы, по которым пришли события в **order_output_events** должны быть в БД
1. Все заказы, по которым пришли события в **orders_input_errors** не должно быть в БД
1. Для заказов из **order_output_events** из БД **order-service-db** достаем customer_id и проверяем, что заказчики есть в **customer-service-db**

### Работа с API
1. Начать сбор расхождений POST /start
```json
{
  "duration": "01:00:00" // время отслеживания ROUTE256_TEST_SERVICE_TIMEOUT_SECONDS
}
```
2. Запрос статистики расхождений GET /statistics

Пример ответа
```json
{
    "mismatchDistribution": {
        "None": 1,
        "OrderNotExist": 1,
        "OrderCreatedOnError": 1,
        "CustomerNotExist": 1
    },
    "validCount": 1,
    "invalidCount": 3,
    "totalCount": 4,
    "validPercent": 25,
    "mismatches": [
        {
            "type": "OrderNotExist",
            "key": "3",
            "payload": {
                "orderId": 3,
                "eventType": "Created"
            },
            "storedData": {},
            "createAt": "2024-08-20T11:30:00.9752677+00:00"
        },
        {
            "type": "CustomerNotExist",
            "key": "2",
            "payload": {
                "orderId": 2,
                "eventType": "Created"
            },
            "storedData": {
                "orderId": 2,
                "regionId": 1,
                "customerId": 2,
                "status": 1,
                "comment": "comment text",
                "createdAt": "2024-08-11T20:36:42.031472+00:00",
                "items": [
                    {
                        "id": 1,
                        "orderId": 2,
                        "barcode": "BAR-123",
                        "quantity": 1
                    }
                ]
            },
            "createAt": "2024-08-20T11:29:34.8148011+00:00"
        },
        {
            "type": "OrderCreatedOnError",
            "key": "5",
            "payload": {
                "customerId": 1,
                "regionId": 1,
                "orderItems": [
                    {
                        "barcode": "BAR-123",
                        "quantity": 1
                    }
                ]
            },
            "storedData": {
                "orders": [
                    {
                        "orderId": 2,
                        "regionId": 1,
                        "customerId": 1,
                        "status": 1,
                        "comment": "comment text",
                        "createdAt": "2024-08-11T20:36:42.031472+00:00",
                        "items": [
                            {
                                "id": 1,
                                "orderId": 2,
                                "barcode": "BAR-123",
                                "quantity": 1
                            }
                        ]
                    }
                ]
            },
            "createAt": "2024-08-20T11:27:39.7912107+00:00"
        }
    ]
}
```
3. Завершение работы сбора расхождений POST /stop
4. Очистка накопленной статистики DELETE /clear
