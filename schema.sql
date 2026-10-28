CREATE DATABASE IF NOT EXISTS `expense_tracker_db` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `expense_tracker_db`;

CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

-- 1. Transactions Table
CREATE TABLE IF NOT EXISTS `Transactions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Type` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Category` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Amount` decimal(12,2) NOT NULL,
    `Date` datetime(6) NOT NULL,
    `Notes` varchar(300) CHARACTER SET utf8mb4 NULL,
    `Platform` varchar(50) CHARACTER SET utf8mb4 NULL DEFAULT 'Direct / In-Store',
    `OrderNumber` varchar(60) CHARACTER SET utf8mb4 NULL,
    `PaymentMethod` varchar(50) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Transactions` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Transactions_Platform` ON `Transactions` (`Platform`);
CREATE INDEX `IX_Transactions_Date` ON `Transactions` (`Date`);

-- 2. MonthlyBudgets Table
CREATE TABLE IF NOT EXISTS `MonthlyBudgets` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Year` int NOT NULL,
    `Month` int NOT NULL,
    `Amount` decimal(12,2) NOT NULL,
    CONSTRAINT `PK_MonthlyBudgets` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_MonthlyBudgets_Year_Month` ON `MonthlyBudgets` (`Year`, `Month`);

-- 3. Platforms Table (Section 4.A)
CREATE TABLE IF NOT EXISTS `Platforms` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `PlatformType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    CONSTRAINT `PK_Platforms` PRIMARY KEY (`Id`),
    CONSTRAINT `AK_Platforms_Name` UNIQUE (`Name`)
) CHARACTER SET=utf8mb4;

-- 4. Categories Table (Section 4.B)
CREATE TABLE IF NOT EXISTS `Categories` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Name` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `CategoryType` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `IsActive` tinyint(1) NOT NULL DEFAULT 1,
    CONSTRAINT `PK_Categories` PRIMARY KEY (`Id`),
    CONSTRAINT `AK_Categories_Name` UNIQUE (`Name`)
) CHARACTER SET=utf8mb4;

-- Seed Platforms
INSERT IGNORE INTO `Platforms` (`Id`, `Name`, `PlatformType`, `IsActive`) VALUES
(1, 'Swiggy', 'Food Delivery', 1),
(2, 'Zomato', 'Food Delivery', 1),
(3, 'Blinkit', 'Groceries & Quick Commerce', 1),
(4, 'Zepto', 'Groceries & Quick Commerce', 1),
(5, 'BigBasket', 'Groceries & Quick Commerce', 1),
(6, 'Amazon', 'Shopping', 1),
(7, 'Flipkart', 'Shopping', 1),
(8, 'Myntra', 'Shopping', 1),
(9, 'Other', 'Other', 1);

-- Seed Categories
INSERT IGNORE INTO `Categories` (`Id`, `Name`, `CategoryType`, `IsActive`) VALUES
(1, 'Food', 'Expense', 1),
(2, 'Groceries', 'Expense', 1),
(3, 'Electronics', 'Expense', 1),
(4, 'Clothing', 'Expense', 1),
(5, 'Household', 'Expense', 1),
(6, 'Health', 'Expense', 1),
(7, 'Education', 'Expense', 1),
(8, 'Travel', 'Expense', 1),
(9, 'Subscriptions', 'Expense', 1),
(10, 'Other', 'Expense', 1);

-- 5. PurchaseOrders Table (Section 4.C)
CREATE TABLE IF NOT EXISTS `PurchaseOrders` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `PlatformId` int NOT NULL DEFAULT 1,
    `ExternalOrderNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
    `OrderDate` datetime(6) NOT NULL,
    `OrderStatus` varchar(30) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Completed',
    `PaymentMethod` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ItemSubtotal` decimal(12,2) NOT NULL DEFAULT 0.00,
    `DeliveryFee` decimal(12,2) NOT NULL DEFAULT 0.00,
    `PackagingFee` decimal(12,2) NOT NULL DEFAULT 0.00,
    `PlatformFee` decimal(12,2) NOT NULL DEFAULT 0.00,
    `Taxes` decimal(12,2) NOT NULL DEFAULT 0.00,
    `OtherCharges` decimal(12,2) NOT NULL DEFAULT 0.00,
    `Discount` decimal(12,2) NOT NULL DEFAULT 0.00,
    `FinalBillAmount` decimal(12,2) NOT NULL DEFAULT 0.00,
    `RefundAmount` decimal(12,2) NOT NULL DEFAULT 0.00,
    `Notes` varchar(500) CHARACTER SET utf8mb4 NULL,
    `SourceType` varchar(50) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Manual',
    `CreatedAt` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_PurchaseOrders` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_PurchaseOrders_Platforms` FOREIGN KEY (`PlatformId`) REFERENCES `Platforms` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_PurchaseOrders_PlatformId_ExternalOrderNumber` 
    ON `PurchaseOrders` (`PlatformId`, `ExternalOrderNumber`);
CREATE INDEX `IX_PurchaseOrders_OrderDate` ON `PurchaseOrders` (`OrderDate`);
CREATE INDEX `IX_PurchaseOrders_OrderStatus` ON `PurchaseOrders` (`OrderStatus`);

-- 6. OrderItems Table (Section 4.D)
CREATE TABLE IF NOT EXISTS `OrderItems` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `PurchaseOrderId` int NOT NULL,
    `CategoryId` int NOT NULL DEFAULT 1,
    `ItemName` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Quantity` int NOT NULL DEFAULT 1,
    `UnitPrice` decimal(12,2) NOT NULL,
    `TotalPrice` decimal(12,2) NOT NULL,
    CONSTRAINT `PK_OrderItems` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_OrderItems_PurchaseOrders` FOREIGN KEY (`PurchaseOrderId`) REFERENCES `PurchaseOrders` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_OrderItems_Categories` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_OrderItems_PurchaseOrderId` ON `OrderItems` (`PurchaseOrderId`);
CREATE INDEX `IX_OrderItems_CategoryId` ON `OrderItems` (`CategoryId`);

-- 7. CategoryBudgets Table (Section 4.E)
CREATE TABLE IF NOT EXISTS `CategoryBudgets` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Year` int NOT NULL,
    `Month` int NOT NULL,
    `CategoryId` int NOT NULL,
    `Amount` decimal(12,2) NOT NULL,
    CONSTRAINT `PK_CategoryBudgets` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_CategoryBudgets_Categories` FOREIGN KEY (`CategoryId`) REFERENCES `Categories` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_CategoryBudgets_Year_Month_CategoryId` 
    ON `CategoryBudgets` (`Year`, `Month`, `CategoryId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261028135411_InitialCreate', '8.0.11')
ON DUPLICATE KEY UPDATE `ProductVersion` = VALUES(`ProductVersion`);

COMMIT;
