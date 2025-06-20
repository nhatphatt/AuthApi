-- MySQL Database Initialization Script
-- This script will run when MySQL container starts for the first time

-- Ensure we're using the authapi database
USE authapi;

-- Create additional indexes for better performance (optional)
-- The actual tables will be created by Entity Framework migrations

-- Set MySQL specific configurations
SET GLOBAL sql_mode = 'STRICT_TRANS_TABLES,NO_ZERO_DATE,NO_ZERO_IN_DATE,ERROR_FOR_DIVISION_BY_ZERO';

-- Create a user for monitoring/backup purposes (optional)
-- CREATE USER 'monitoring'@'%' IDENTIFIED BY 'monitor_password';
-- GRANT SELECT ON authapi.* TO 'monitoring'@'%';

-- Display database info
SELECT 'AuthAPI MySQL Database initialized successfully!' AS Status;
SHOW DATABASES; 