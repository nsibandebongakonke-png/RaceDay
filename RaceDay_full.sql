-- ============================================
-- RaceDay Database — SQL Server (T-SQL) Schema
-- Version: Part 1 PoE (1:1 User <-> Role)
-- ============================================

-- Create the database
CREATE DATABASE RaceDay;
GO

USE RaceDay;
GO

-- ============================================
-- 1. TABLES
-- ============================================

-- Users (shared table for both roles)
-- NOTE: role_id has been removed. The role now lives in the role table,
-- which is linked 1:1 to the user.
CREATE TABLE [user] ( -- 'user' is a reserved keyword, so we bracket it
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL, -- Store BCrypt/Argon2 hashes here
    phone VARCHAR(20) NULL,
    profile_pic VARCHAR(500) NULL -- Increased length for cloud URLs
);
GO

-- Roles (Organiser / Participant) - strict 1:1 with [user]
-- user_id is both the PRIMARY KEY and the FOREIGN KEY, which is what
-- guarantees that a user has exactly one role row and a role row
-- belongs to exactly one user.
CREATE TABLE role (
    user_id INT PRIMARY KEY,
    role_name VARCHAR(50) NOT NULL,

    CONSTRAINT FK_Role_User FOREIGN KEY (user_id) REFERENCES [user](user_id) ON DELETE CASCADE,
    CONSTRAINT CHK_Role_Name CHECK (role_name IN ('Organiser', 'Participant'))
);
GO

-- Events (created by Organisers)
CREATE TABLE event (
    event_id INT IDENTITY(1,1) PRIMARY KEY,
    organiser_id INT NOT NULL,
    event_name VARCHAR(100) NOT NULL,
    description VARCHAR(MAX) NULL, -- VARCHAR(MAX) = TEXT in SQL Server
    event_date DATETIME2 NOT NULL, -- More precise than DATETIME
    [location] VARCHAR(255) NOT NULL, -- Bracket because it's a keyword
    distance VARCHAR(50) NULL, -- e.g. "42.2km", "21.1km"
    event_type VARCHAR(50) NULL, -- e.g. "Road", "Trail", "Cycle"
    banner_image VARCHAR(500) NULL,

    CONSTRAINT FK_Event_Organiser FOREIGN KEY (organiser_id) REFERENCES [user](user_id)
);
GO

-- Categories (e.g., 42km Senior, 21km Junior) - linked to an Event
CREATE TABLE category (
    category_id INT IDENTITY(1,1) PRIMARY KEY,
    event_id INT NOT NULL,
    category_name VARCHAR(50) NOT NULL,
    description VARCHAR(MAX) NULL,

    CONSTRAINT FK_Category_Event FOREIGN KEY (event_id) REFERENCES event(event_id) ON DELETE CASCADE
);
GO

-- Enrolments (Participant enters an Event in a specific Category)
CREATE TABLE enrolment (
    enroll_id INT IDENTITY(1,1) PRIMARY KEY,
    participant_id INT NOT NULL,
    event_id INT NOT NULL,
    category_id INT NOT NULL, -- Which distance/category they entered
    enrol_date DATETIME2 NOT NULL DEFAULT GETDATE(),
    [status] VARCHAR(20) NOT NULL DEFAULT 'pending',

    CONSTRAINT FK_Enrolment_Participant FOREIGN KEY (participant_id) REFERENCES [user](user_id),
    CONSTRAINT FK_Enrolment_Event FOREIGN KEY (event_id) REFERENCES event(event_id) ON DELETE CASCADE,
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (category_id) REFERENCES category(category_id),
    -- Enforce valid statuses
    CONSTRAINT CHK_Enrolment_Status CHECK ([status] IN ('pending', 'confirmed', 'cancelled', 'disqualified'))
);
GO

-- Supporting Documents (Proof of payment, medical certs, etc.)
CREATE TABLE supporting_document (
    document_id INT IDENTITY(1,1) PRIMARY KEY,
    enroll_id INT NOT NULL,
    file_name VARCHAR(100) NOT NULL,
    file_path VARCHAR(500) NOT NULL, -- Cloud storage path
    [status] VARCHAR(20) NOT NULL DEFAULT 'pending',
    uploaded_date DATETIME2 NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Document_Enrolment FOREIGN KEY (enroll_id) REFERENCES enrolment(enroll_id) ON DELETE CASCADE,
    CONSTRAINT CHK_Document_Status CHECK ([status] IN ('pending', 'approved', 'rejected'))
);
GO

-- Race Results (Captured by Organiser after the event)
CREATE TABLE race_result (
    result_id INT IDENTITY(1,1) PRIMARY KEY,
    enroll_id INT NOT NULL,
    finish_time TIME NULL, -- e.g. '03:45:12' (supports up to 23:59:59)
    [position] INT NULL, -- Overall position
    published BIT NOT NULL DEFAULT 0, -- 0 = Draft, 1 = Published

    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (enroll_id) REFERENCES enrolment(enroll_id) ON DELETE CASCADE
);
GO

-- ============================================
-- 2. INDEXES (Performance Boost)
-- ============================================
-- (No role index needed: role.user_id is the primary key)
CREATE INDEX IX_Event_Organiser ON event(organiser_id);
CREATE INDEX IX_Category_Event ON category(event_id);
CREATE INDEX IX_Enrolment_Participant ON enrolment(participant_id);
CREATE INDEX IX_Enrolment_Event ON enrolment(event_id);
CREATE INDEX IX_Enrolment_Category ON enrolment(category_id);
CREATE INDEX IX_Document_Enrolment ON supporting_document(enroll_id);
CREATE INDEX IX_Result_Enrolment ON race_result(enroll_id);
GO

-- ============================================
-- 3. INSERT SAMPLE DATA (to test with)
-- ============================================
-- Users must be inserted BEFORE roles, because role.user_id references [user]

-- Insert a test Organiser (password_hash would be hashed in real life)
INSERT INTO [user] (first_name, last_name, email, password_hash, phone)
VALUES ('John', 'Doe', 'john.organiser@raceday.co.za', 'hashed_pw_123', '0821234567');
GO

-- Insert a test Participant
INSERT INTO [user] (first_name, last_name, email, password_hash, phone)
VALUES ('Jane', 'Smith', 'jane.runner@email.com', 'hashed_pw_456', '0839876543');
GO

-- Assign each user their single role (1:1)
INSERT INTO role (user_id, role_name) VALUES (1, 'Organiser'), (2, 'Participant');
GO

-- Insert an Event
INSERT INTO event (organiser_id, event_name, description, event_date, [location], distance, event_type)
VALUES (1, 'Cape Town Cycle Tour 2026', 'The worlds largest timed cycle race.', '2026-03-08 06:00:00', 'Cape Town', '109km', 'Cycle');
GO

-- Insert Categories for that Event
INSERT INTO category (event_id, category_name, description)
VALUES
(1, 'Elite Men', 'Competitive licensed riders'),
(1, 'Elite Women', 'Competitive licensed riders'),
(1, 'Open', 'General public');
GO

-- Insert an Enrolment (Jane enters the Open category)
INSERT INTO enrolment (participant_id, event_id, category_id, [status])
VALUES (2, 1, 3, 'confirmed');
GO

-- Insert a Supporting Document for that Enrolment
INSERT INTO supporting_document (enroll_id, file_name, file_path, [status])
VALUES (1, 'Medical_Cert.pdf', '/uploads/1_medical.pdf', 'approved');
GO

-- Insert a Race Result for Jane
INSERT INTO race_result (enroll_id, finish_time, [position], published)
VALUES (1, '05:12:34', 156, 1);
GO

-- ============================================
-- 4. USEFUL SELECT QUERIES (for your API)
-- ============================================

-- 4.1 All Users with their Role Names (join is now user_id = user_id)
SELECT u.user_id, u.first_name, u.last_name, u.email, r.role_name
FROM [user] u
JOIN role r ON r.user_id = u.user_id;

-- 4.2 All Events with Organiser Names
SELECT e.event_id, e.event_name, e.event_date, e.location,
       u.first_name + ' ' + u.last_name AS organiser_full_name
FROM event e
JOIN [user] u ON e.organiser_id = u.user_id;

-- 4.3 Categories for a specific Event (replace 1 with your event_id)
SELECT category_id, category_name, description
FROM category
WHERE event_id = 1;

-- 4.4 All Enrolments for an Event, including Category chosen
SELECT en.enroll_id, u.first_name, u.last_name, cat.category_name, en.status
FROM enrolment en
JOIN [user] u ON en.participant_id = u.user_id
JOIN category cat ON en.category_id = cat.category_id
WHERE en.event_id = 1;

-- 4.5 A Participant's Personal Enrolment History
SELECT en.enroll_id, ev.event_name, cat.category_name, ev.event_date, en.status
FROM enrolment en
JOIN event ev ON en.event_id = ev.event_id
JOIN category cat ON en.category_id = cat.category_id
WHERE en.participant_id = 2
ORDER BY ev.event_date DESC;

-- 4.6 Published Leaderboard for an Event (replace 1)
SELECT u.first_name, u.last_name, rr.finish_time, rr.[position]
FROM race_result rr
JOIN enrolment en ON rr.enroll_id = en.enroll_id
JOIN [user] u ON en.participant_id = u.user_id
WHERE en.event_id = 1 AND rr.published = 1
ORDER BY rr.[position] ASC;

-- 4.7 Count of Enrolments per Event per Status
SELECT ev.event_name, en.status, COUNT(*) AS total
FROM event ev
JOIN enrolment en ON en.event_id = ev.event_id
GROUP BY ev.event_name, en.status;
GO
