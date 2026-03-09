-- ============================================
-- TicketFlow Database Initialization Script
-- ============================================

CREATE DATABASE IF NOT EXISTS TicketFlowDb;
USE TicketFlowDb;

-- --------------------------------------------
-- Users Table
-- --------------------------------------------
CREATE TABLE IF NOT EXISTS Users (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    Username    VARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FullName    VARCHAR(100) NOT NULL,
    Role        ENUM('Admin', 'User') NOT NULL DEFAULT 'User',
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    INDEX IX_Users_Role (Role)
);

-- --------------------------------------------
-- Tickets Table
-- --------------------------------------------
CREATE TABLE IF NOT EXISTS Tickets (
    Id                INT AUTO_INCREMENT PRIMARY KEY,
    TicketNumber      VARCHAR(20)  NOT NULL UNIQUE,
    CreatedByUserId   INT          NOT NULL,
    Subject           VARCHAR(200) NOT NULL,
    Description       TEXT         NOT NULL,
    Priority          ENUM('Low', 'Medium', 'High') NOT NULL DEFAULT 'Medium',
    Status            ENUM('Open', 'InProgress', 'Closed') NOT NULL DEFAULT 'Open',
    AssignedToUserId  INT          NULL,
    CreatedAt         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT FK_Tickets_CreatedBy  FOREIGN KEY (CreatedByUserId)  REFERENCES Users(Id),
    CONSTRAINT FK_Tickets_AssignedTo FOREIGN KEY (AssignedToUserId) REFERENCES Users(Id),

    INDEX IX_Tickets_CreatedBy  (CreatedByUserId),
    INDEX IX_Tickets_AssignedTo (AssignedToUserId),
    INDEX IX_Tickets_Status     (Status)
);

-- --------------------------------------------
-- Ticket Status History Table
-- --------------------------------------------
CREATE TABLE IF NOT EXISTS TicketStatusHistory (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    TicketId        INT          NOT NULL,
    OldStatus       VARCHAR(20)  NOT NULL,
    NewStatus       VARCHAR(20)  NOT NULL,
    ChangedByUserId INT          NOT NULL,
    ChangedAt       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_StatusHistory_Ticket FOREIGN KEY (TicketId)        REFERENCES Tickets(Id),
    CONSTRAINT FK_StatusHistory_User   FOREIGN KEY (ChangedByUserId) REFERENCES Users(Id),

    INDEX IX_StatusHistory_Ticket (TicketId)
);

-- --------------------------------------------
-- Ticket Comments Table
-- --------------------------------------------
CREATE TABLE IF NOT EXISTS TicketComments (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    TicketId    INT      NOT NULL,
    UserId      INT      NOT NULL,
    Comment     TEXT     NOT NULL,
    IsInternal  BOOLEAN  NOT NULL DEFAULT FALSE,
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT FK_Comments_Ticket FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
    CONSTRAINT FK_Comments_User   FOREIGN KEY (UserId)   REFERENCES Users(Id),

    INDEX IX_Comments_Ticket (TicketId)
);

-- --------------------------------------------
-- Seed Data
-- --------------------------------------------
-- Seed users are inserted by the application on startup (DbSeeder)
-- to ensure password hashes are generated correctly by BCrypt.
-- Default accounts:
--   admin / Admin@123 (Role: Admin)
--   john  / User@123  (Role: User)

