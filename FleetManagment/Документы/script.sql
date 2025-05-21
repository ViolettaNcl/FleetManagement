USE [FleetManagementDB]
GO
IF OBJECT_ID('dbo.Routes', 'U') IS NOT NULL DROP TABLE dbo.Routes;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Drivers', 'U') IS NOT NULL DROP TABLE dbo.Drivers;
IF OBJECT_ID('dbo.Vehicles', 'U') IS NOT NULL DROP TABLE dbo.Vehicles;
GO
CREATE TABLE [dbo].[Drivers]([Id] [int] IDENTITY(1,1) NOT NULL,[FullName] [nvarchar](100) NOT NULL,[LicenseNumber] [nvarchar](50) NOT NULL,[PhoneNumber] [nvarchar](20) NOT NULL,[Experience] [int] NOT NULL,PRIMARY KEY CLUSTERED ([Id] ASC)) ON [PRIMARY]
GO
CREATE TABLE [dbo].[Roles]([Id] [int] IDENTITY(1,1) NOT NULL,[RoleName] [nvarchar](50) NOT NULL,PRIMARY KEY CLUSTERED ([Id] ASC)) ON [PRIMARY]
GO
CREATE TABLE [dbo].[Routes]([Id] [int] IDENTITY(1,1) NOT NULL,[VehicleId] [int] NOT NULL,[DriverId] [int] NOT NULL,[StartDate] [datetime] NOT NULL,[EndDate] [datetime] NOT NULL,[StartLocation] [nvarchar](100) NOT NULL,[EndLocation] [nvarchar](100) NOT NULL,[Distance] [int] NOT NULL,[UserId] [int] NULL,PRIMARY KEY CLUSTERED ([Id] ASC)) ON [PRIMARY]
GO
CREATE TABLE [dbo].[Users]([Id] [int] IDENTITY(1,1) NOT NULL,[FullName] [nvarchar](100) NOT NULL,[Email] [nvarchar](100) NOT NULL,[Password] [nvarchar](100) NOT NULL,[RoleId] [int] NULL,PRIMARY KEY CLUSTERED ([Id] ASC)) ON [PRIMARY]
GO
CREATE TABLE [dbo].[Vehicles]([Id] [int] IDENTITY(1,1) NOT NULL,[LicensePlate] [nvarchar](50) NOT NULL,[Model] [nvarchar](50) NOT NULL,[Manufacturer] [nvarchar](50) NOT NULL,[YearOfManufacture] [int] NOT NULL,[Mileage] [int] NOT NULL,[Status] [nvarchar](20) NOT NULL,[LastMaintenanceDate] [datetime] NULL,PRIMARY KEY CLUSTERED ([Id] ASC)) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Routes] ADD FOREIGN KEY([DriverId]) REFERENCES [dbo].[Drivers] ([Id]);
ALTER TABLE [dbo].[Routes] ADD FOREIGN KEY([VehicleId]) REFERENCES [dbo].[Vehicles] ([Id]);
ALTER TABLE [dbo].[Routes] ADD CONSTRAINT [FK_Routes_Users] FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([Id]);
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [FK_Users_Roles] FOREIGN KEY([RoleId]) REFERENCES [dbo].[Roles] ([Id]);
GO
INSERT INTO Roles (RoleName) VALUES ('Admin'), ('User');
INSERT INTO Users (FullName, Email, Password, RoleId) VALUES ('Admin', 'admin@admin', '123', 1),('User', 'user@user', '123', 2);
INSERT INTO Vehicles (LicensePlate, Model, Manufacturer, YearOfManufacture, Mileage, Status, LastMaintenanceDate) VALUES ('ABC123', 'Camry', 'Toyota', 2020, 50000, 'Available', '2025-01-01'),('XYZ789', 'Civic', 'Honda', 2018, 75000, 'In Repair', NULL);
INSERT INTO Drivers (FullName, LicenseNumber, PhoneNumber, Experience) VALUES ('John Doe', '123456789', '+1234567890', 5),('Jane Smith', '987654321', '+0987654321', 3);
INSERT INTO Routes (VehicleId, DriverId, StartDate, EndDate, StartLocation, EndLocation, Distance, UserId) VALUES (1, 1, '2025-05-01 08:00:00', '2025-05-01 12:00:00', 'City A', 'City B', 200, 1),(2, 2, '2025-05-02 09:00:00', '2025-05-02 15:00:00', 'City C', 'City D', 300, 2);
GO
