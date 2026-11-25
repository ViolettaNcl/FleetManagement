<div align="center">

# 🚚 FleetManagement

### Fleet management platform · .NET 10 browser & API + original WPF desktop

<p>
  <img src="https://img.shields.io/badge/WPF-.NET_Framework_4.7.2-512BD4?style=flat-square" alt="WPF">
  <img src="https://img.shields.io/badge/Entity_Framework-6-68217A?style=flat-square" alt="EF6">
  <img src="https://img.shields.io/badge/MS_SQL_Server-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white" alt="MS SQL Server">
  <img src="https://img.shields.io/badge/MSTest-tests-brightgreen?style=flat-square" alt="MSTest">
</p>

</div>

---

## Interactive portfolio demo (in development)

The Blazor WebAssembly prototype retains the original lavender WPF design and runs entirely in the browser with synthetic data. It includes editable vehicle/driver registers, simulated fleet tracking, conflict-aware dispatch and rule-based maintenance.

[Demo setup, technical details and limitations](./docs/portfolio-v2.md) · [C# business rules](./src/FleetManagement.Core/Fleet.cs)

GitHub Pages publication is pending browser validation and repository Pages configuration. The original desktop app is retained.

## Modern backend (.NET 10)

The independent ASP.NET Core API reuses the same C# fleet rules and persists data using EF Core, with SQLite for local development and optional SQL Server in Docker Compose.

- [Backend quick start, endpoints and limitations](./docs/backend.md)
- [API source](./src/FleetManagement.Api)
- [HTTP integration checks](./tests/api/smoke.py)

The API is a local-development prototype, independent of the browser demo. ASP.NET Core Identity provides user login and role-based permissions; a separate automation key remains available for local integration tests. Fleet changes are recorded in an admin-only audit trail. Production deployment and migrations are not yet configured.

## 📌 Описание

WPF-приложение для управления автопарком компании. Позволяет администрировать
транспорт, водителей и рейсы, планировать маршруты и контролировать пробег с
разграничением прав доступа.

В каталоге собраны русскоязычные материалы по разработке и использованию системы
FleetManagement. Ниже представлено краткое содержание файлов.

## ⚙️ Автоматизация проекта

Для автоматизации используется PowerShell-скрипт `build.ps1`, расположенный в корневой
папке проекта. Выполните в PowerShell:
```powershell
cd путь\к\проекту\FleetManagement
.\build.ps1
```

## 📋 Основные документы

- [Постановка задачи](./FleetManagment/Документы/ОбщаяПостановка.md)
- [Техническое задание](./FleetManagment/Документы/ТЗ.md)
- [Анализ рисков](./FleetManagment/Документы/АнализРисков.md)
- [План разработки](./FleetManagment/Документы/Диаграммы_png/График.png)
- [План внедрения](./FleetManagment/Документы/Диаграммы_png/ПланВнедрения.png)

## 🏗 Архитектура и дизайн

- [Архитектура системы](./FleetManagment/Документы/АрхитектураСистемы.md)
- [Диаграмма классов](./FleetManagment/Документы/Диаграммы_png/diagram-class.png)
- [Диаграмма классов программы](./FleetManagment/Документы/Диаграммы_png/КлассыПрограммы.png)
- [Диаграммы последовательностей](./FleetManagment/Документы/Диаграммы_png/диаграмма%20последовательности.png)
- [Диаграмма состояний](./FleetManagment/Документы/Диаграммы_png/диаграммаСостояний.png)
- [Диаграмма деятельности](./FleetManagment/Документы/Диаграммы_png/ДиаграммаДеятельности.png)
- [Пакеты системы](./FleetManagment/Документы/Диаграммы_png/ДиаграммыПакетов.png)

## 👤 Пользовательские сценарии

- [Пользователи и бизнес-процессы](./FleetManagment/Документы/ПользователиБизнес-процессы.md)
- [Варианты использования системы](./FleetManagment/Документы/Диаграммы_png/Диаграмма%20вариантов%20использования.png)
- [Пример создания маршрута](./FleetManagment/Документы/Пример.md)

## 📐 Требования к системе

- [Требования к системе](./FleetManagment/Документы/ТребованияПрограмме.md)
- [Детализация требований](./FleetManagment/Документы/ДетализацияТребований.md)
- [Терминологический словарь](./FleetManagment/Документы/ТерминологическийСловарь.md)

## 🖥 Интерфейс программы

- [Интерфейс программы](./FleetManagment/Документы/Интерфейс.md)

## 📖 Документация для пользователей

- [Документация для пользователей](./FleetManagment/Документы/ДокументацияПользователей.md)
- [Инструкция: развёртывание и настройка проекта FleetManagement](./FleetManagment/Документы/Инструкция.md)

## 👨‍💻 Документация для разработчиков

- [Документация для разработчиков](./FleetManagment/Документы/ДокументацияРазработчиков.md)
- [Скрипт базы данных](./FleetManagment/Документы/script.sql)

---

**Обратите внимание**: те же файлы дублируются в каталоге `FleetManagement/Документы`.
