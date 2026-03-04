-- ========================================
-- Sample script to demonstrate transaction isolation anomalies
-- ========================================

-- ========================================
-- Setup: Create tables and sample data
-- ========================================
-- Drop tables if they exist for a clean start
IF OBJECT_ID('dbo.accounts', 'U') IS NOT NULL
    DROP TABLE dbo.accounts;
IF OBJECT_ID('dbo.employees', 'U') IS NOT NULL
    DROP TABLE dbo.employees;

-- Create accounts table
CREATE TABLE dbo.accounts (
    id INT PRIMARY KEY,
    balance INT
);

-- Create employees table
CREATE TABLE dbo.employees (
    id INT PRIMARY KEY,
    name VARCHAR(100),
    department INT
);

-- Insert initial data
INSERT INTO dbo.accounts (id, balance) VALUES (1, 1000);
INSERT INTO dbo.employees (id, name, department) VALUES (1, 'Alice', 2);
INSERT INTO dbo.employees (id, name, department) VALUES (2, 'Bob', 2);

-- ========================================
-- Example 1: Dirty Read (READ UNCOMMITTED)
-- ========================================
-- In this example:
-- Session 1 updates the account balance without committing.
-- Session 2 reads with READ UNCOMMITTED to see dirty data.

-- --------- Session 1 -----------
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;  -- default level for updates
BEGIN TRANSACTION;
UPDATE dbo.accounts
SET balance = balance + 500
WHERE id = 1;
-- At this point, balance is 1500 in this transaction, but not yet committed.

-- (Do NOT commit or rollback yet; keep the transaction open.)

-- --------- Session 2 -----------
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
BEGIN TRANSACTION;
SELECT id, balance
FROM dbo.accounts
WHERE id = 1;
-- With READ UNCOMMITTED, Session 2 reads the uncommitted balance = 1500.
COMMIT TRANSACTION;  -- End the read transaction.

-- --------- Session 1 -----------
ROLLBACK TRANSACTION;  -- Undo the update; balance returns to 1000.

-- --------- Session 2 -----------
SELECT id, balance
FROM dbo.accounts
WHERE id = 1;
-- Now balance is back to 1000. The first read was "dirty" because it saw uncommitted data.
-- ========================================

-- Example 2: Non-Repeatable Read (READ COMMITTED)
-- ========================================
-- In this example:
-- Session 1 reads a value, then Session 2 updates and commits.
-- Session 1 reads again and sees a different value.

-- --------- Session 1 -----------
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
BEGIN TRANSACTION;
SELECT id, balance
FROM dbo.accounts
WHERE id = 1;
-- Assume the result is balance = 1000.

-- --------- Session 2 -----------
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;  -- default level
BEGIN TRANSACTION;
UPDATE dbo.accounts
SET balance = balance + 200
WHERE id = 1;
COMMIT TRANSACTION;
-- Balance is now updated to 1200.

-- --------- Session 1 -----------
SELECT id, balance
FROM dbo.accounts
WHERE id = 1;
-- Now returns balance = 1200, changed from first read.
COMMIT TRANSACTION;
-- ========================================

-- Example 3: Phantom Read (READ COMMITTED)
-- ========================================
-- In this example:
-- Session 1 queries employees in department 2.
-- Session 2 inserts a new employee in department 2 and commits.
-- Session 1 queries again and sees the new "phantom" row.

-- --------- Session 1 -----------
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
BEGIN TRANSACTION;
SELECT id, name, department
FROM dbo.employees
WHERE department = 2;
-- Returns 2 rows (Alice, Bob).

-- --------- Session 2 -----------
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;  -- default level
BEGIN TRANSACTION;
INSERT INTO dbo.employees (id, name, department)
VALUES (3, 'Carol', 2);
COMMIT TRANSACTION;
-- A new row (Carol) in department 2 is added.

-- --------- Session 1 -----------
SELECT id, name, department
FROM dbo.employees
WHERE department = 2;
-- Now returns 3 rows (Alice, Bob, Carol). Carol's row is a "phantom".
COMMIT TRANSACTION;
-- ========================================

-- Cleanup (optional)
-- DROP TABLE dbo.accounts;
-- DROP TABLE dbo.employees;
