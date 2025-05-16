## Описание сервиса
Сервис операций генерирует нагрузку на [customer-service](https://gitlab.ozon.dev/cs/classroom-14/experts/customer-service/-/blob/develop/README.md) и [order-service](https://gitlab.ozon.dev/cs/classroom-14/experts/order-service/-/blob/develop/README.md).
Запускает в background-задаче генерацию заказов и заказчиков в соответствии с настройками.

(!) Важно (!): заказчики, указанные в заказах должны быть созданы в **customer_service** в рамках генерации.

## Настройки сервиса

Настройки можно указать через переменные окружения.

### Адреса брокеров кафки
Название переменной окружения: `ROUTE256_KAFKA_BROKERS`
Допустимые значения (строка)
Список адресов хостов брокеров кафки, разделенных через запятую

### Кол-во заказов в секунду 
Название переменной окружения: `ROUTE256_ORDERS_PER_SECOND`
Допустимые значения 1-100
Сколько сообщений за секунду должно быть поставлено в топик **orders_input** (подробнее [тут](https://gitlab.ozon.dev/cs/classroom-14/experts/order-service/-/blob/develop/README.md#%D1%81%D0%BE%D0%B7%D0%B4%D0%B0%D0%BD%D0%B8%D0%B5-%D0%BD%D0%BE%D0%B2%D1%8B%D1%85-%D0%B7%D0%B0%D0%BA%D0%B0%D0%B7%D0%BE%D0%B2))

### Кол-во заказчиков в секунду
Название переменной окружения: `ROUTE256_CUSTOMERS_PER_SECOND`
Допустимые значения 1-10
Сколько раз в секунду будет вызван [метод V1CreateCustomer](https://gitlab.ozon.dev/cs/classroom-14/experts/customer-service/-/tree/develop?ref_type=heads#%D1%81%D0%BE%D0%B7%D0%B4%D0%B0%D0%BD%D0%B8%D0%B5-%D0%BD%D0%BE%D0%B2%D1%8B%D1%85-%D0%B7%D0%B0%D0%BA%D0%B0%D0%B7%D1%87%D0%B8%D0%BA%D0%BE%D0%B2).

### Создавать каждый N-ый заказ с ошибкой валидации
Название переменной окружения: `ROUTE256_INVALID_ORDER_COUNTER_NUMBER` ((?) не смог придумать лучше - пишите какие будут идеи)
Допустимые значения 0-10000
С момента запуска сервиса ведется счетчик созданных заказов и, если он достиг указанного значения, то отправляется сообщение заказа, который не должен пройти валидацию ([Заказ проходит валидацию перед тем как будет сохранен в БД](https://gitlab.ozon.dev/cs/classroom-14/experts/order-service/-/blob/develop/README.md#%D1%81%D0%BE%D0%B7%D0%B4%D0%B0%D0%BD%D0%B8%D0%B5-%D0%BD%D0%BE%D0%B2%D1%8B%D1%85-%D0%B7%D0%B0%D0%BA%D0%B0%D0%B7%D0%BE%D0%B2)).

### URL-адрес customer-service
Название переменной окружения: `ROUTE256_CUSTOMER_SERVICE_URL`
URL, по которому расположен customer-service

