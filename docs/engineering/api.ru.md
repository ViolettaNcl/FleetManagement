# Контракт API и руководство интегратора

[Инженерный индекс](README.ru.md) · [English](api.md) · [README проекта](../../README.ru.md)

**Базовая версия:** V6.3 Enterprise; подтверждённая публичная ревизия [`f2cea2b`](https://github.com/ViolettaNcl/FleetManagement/commit/f2cea2bfe5334fed0e056feb142e56db8fc470c0). Руководство описывает современную версию; документы предыдущих версий в docs/ сохраняются как исторические и специализированные материалы.

## 1. Транспорт и авторизация

Локальный базовый адрес: http://127.0.0.1:5095. Консоль того же origin: /console. API возвращает JSON, кроме эндпоинтов выгрузки файлов. Авторизованному клиенту доступен OpenAPI по /openapi/v1.json. Непрозрачный bearer-токен Identity действителен 15 минут и не является JWT. Используйте Authorization: Bearer либо отдельный X-Fleet-Key для автоматизации; ключи нельзя помещать в браузерный код.

| Субъект | Read | Запись автопарка | Управление аккаунтами/аудит |
| --- | --- | --- | --- |
| Admin | Да | Да | Да |
| Dispatcher | Да | Да | Нет |
| Viewer | Да | Нет | Нет |
| Automation | Да | Да | Нет |

Изменение ролей и отзыв сессий контролирует сервер. Admin управляет ролями сотрудников, но не создаёт новых администраторов через публичный API. Вход ограничен rate limit; отзыв собственных сессий меняет security stamp. Обычный выход очищает только состояние клиента.

## 2. Список эндпоинтов

Сокращённые пути после запятой используют префикс первого пути строки. Обозначения Read/Write/Admin — серверные политики доступа. Требования DTO и подробные статусы определяются реальной схемой OpenAPI и кодом эндпоинтов.

| Метод | Путь | Политика |
| --- | --- | --- |
| GET | /health, /ready | Public |
| GET | /openapi/v1.json | Read |
| POST | /api/auth/login | Public, throttled |
| GET | /api/auth/me | Read |
| POST | /api/auth/revoke-sessions | Authenticated user |
| GET | /api/fleet, /api/vehicles, /api/drivers, /api/trips | Read |
| GET | /api/vehicles/{id}, /api/drivers/{id}, /api/trips/{id} | Read |
| POST | /api/vehicles, /api/drivers, /api/trips | Write |
| PUT, DELETE | /api/vehicles/{id}, /api/drivers/{id}, /api/trips/{id} | Write |
| GET | /api/dispatch/recommend, /api/maintenance | Read |
| POST | /api/vehicles/{id}/service | Write |
| GET | /api/operations/policy, /revision, /overview, /calendar, /scheduled-review | Read |
| GET | /api/operations/maintenance, /shifts, /reservations | Read |
| POST | /api/operations/maintenance, /shifts, /reservations | Write |
| DELETE | /api/operations/shifts/{id}, /reservations/{id} | Write |
| POST | /api/operations/batch/preview, /apply | Write |
| GET | /api/reports/summary, /excel | Read |
| POST | /api/reports/finance/summary, /excel | Read |
| GET | /api/admin/users, /audit, /receipt-capacity | Admin |
| POST | /api/admin/users, /api/admin/users/{id}/revoke-sessions | Admin |
| PUT | /api/admin/users/{id}/role | Admin |
| SignalR | /hubs/fleet | Read |

Календарь: from=YYYY-MM-DD, days=1–31. Отчёты рейсов: from/to включительно по датам отправления UTC, максимум 366 дней. Для рекомендаций start/end задаются ISO 8601 со смещением. POST финансовых отчётов читает данные без изменения записей.

## 3. Безопасный пример интеграции

Запустите локальный сервер штатным лаунчером; получите разрешённый токен. В PowerShell используйте временный токен в переменной текущего процесса:

~~~powershell
$base = 'http://127.0.0.1:5095'
$headers = @{ Authorization = "Bearer $token" }
Invoke-RestMethod "$base/health"
Invoke-RestMethod "$base/api/operations/overview" -Headers $headers
Invoke-RestMethod "$base/api/operations/calendar?from=2026-11-27&days=7" -Headers $headers
~~~

Дата зафиксирована для воспроизводимости примера. Подставьте нужный период. Не публикуйте токены в логах задач и не коммитьте их.

## 4. Контракт записи

Создайте UUID до первого подтверждённого запроса. Передайте Idempotency-Key в формате D. Храните исходные типизированное тело, метод, путь и ключ до выяснения результата. Повтор того же ключа с тем же payload возвращает первоначальный успешный ответ (Idempotency-Replayed: true); изменение операции или payload вызывает конфликт. Успешные запросы без заголовка тоже расходуют квитанции, но при потере ответа клиент не узнает созданный сервером UUID. Изменения Identity и отчёты только для чтения не входят в контракт квитанций автопарка.

| HTTP-результат | Действия клиента |
| --- | --- |
| 2xx | Подтвердить успех, перечитать серверное состояние |
| 400 | Исправить ошибки валидации до нового запроса |
| 401 / 403 | Повторно войти или получить допустимую роль, не повышать привилегии локально |
| 404 | Перечитать ссылки, проверить маршрут и ID |
| 409 | Разрешить конфликт записи/устаревших данных/ёмкости квитанций; без слепого повтора |
| 429 | Соблюдать ограничение входа, уменьшить частоту |
| 503 / неопределённый сетевой исход | Повторять подтверждённую запись только с исходным ключом и телом либо проверить реестр |

Предпросмотр batch не резервирует ресурсы и не заменяет авторизацию. При подтверждении сервер перепроверяет весь переданный набор внутри блокировки; конфликт не приводит к частичному назначению.

## 5. Realtime, readiness и контракты

FleetChanged сообщает о сохранённом изменении с отметкой UTC, а не передаёт поток данных. Перечитывайте записи с авторизацией. Realtime работает best effort; опрос ревизии — запасной путь консоли. Уже установленные соединения и выполняющиеся запросы могут сохраняться до завершения/истечения. Health не подтверждает готовность БД: проверяйте /ready. Публичный Backend и промышленный SLA не заявляются.

**Исходники:** [Program](../../src/FleetManagement.Api/Program.cs), [fleet endpoints](../../src/FleetManagement.Api/FleetEndpoints.cs), [операции](../../src/FleetManagement.Api/OperationsEndpoints.cs), [Identity](../../src/FleetManagement.Api/IdentityEndpoints.cs), [финансы](../../src/FleetManagement.Api/FinancialEndpoints.cs), [overview](../../src/FleetManagement.Api/OverviewEndpoints.cs), [квитанции](../batch-receipts.md), [Backend](../backend.md).
