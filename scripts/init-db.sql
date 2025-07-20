-- Claims Processing Database Schema

-- Users table
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Claims table
CREATE TABLE IF NOT EXISTS claims (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id),
    claim_number VARCHAR(50) UNIQUE NOT NULL,
    status VARCHAR(50) DEFAULT 'submitted',
    claim_type VARCHAR(100) NOT NULL,
    description TEXT,
    amount DECIMAL(10,2),
    incident_date DATE,
    submitted_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Document table
CREATE TABLE IF NOT EXISTS documents (
    id SERIAL PRIMARY KEY,
    claim_id INTEGER NOT NULL REFERENCES claims(id),
    filename VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_type VARCHAR(50) NOT NULL,
    file_size INTEGER NOT NULL,
    metadata JSONB,
    uploaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- System logs table
CREATE TABLE IF NOT EXISTS system_logs (
    id SERIAL PRIMARY KEY,
    level VARCHAR(20) NOT NULL,
    message TEXT NOT NULL,
    component VARCHAR(100) NOT NULL,
    claim_id INTEGER REFERENCES claims(id),
    user_id INTEGER REFERENCES users(id),
    metadata JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_claims_user_id ON claims(user_id);
CREATE INDEX IF NOT EXISTS idx_claims_status ON claims(status);
CREATE INDEX IF NOT EXISTS idx_claims_submitted_at ON claims(submitted_at);
CREATE INDEX IF NOT EXISTS idx_documents_claim_id ON documents(claim_id);
CREATE INDEX IF NOT EXISTS idx_system_logs_component ON system_logs(component);
CREATE INDEX IF NOT EXISTS idx_system_logs_created_at ON system_logs(created_at);

-- Insert sample data
INSERT INTO users (email, password_hash, first_name, last_name) VALUES
('john.doe@example.com', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LeGEFtYpNrk0CzCH6', 'John', 'Doe'),
('jane.smith@example.com', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LeGEFtYpNrk0CzCH6', 'Jane', 'Smith');

INSERT INTO claims (user_id, claim_number, status, claim_type, description, amount, incident_date) VALUES
(1, 'CLM-2024-001', 'submitted', 'Auto Insurance', 'Collision with another vehicle', 2500.00, '2024-01-15'),
(2, 'CLM-2024-002', 'under_review', 'Health Insurance', 'Emergency room visit', 1200.00, '2024-01-20'),
(1, 'CLM-2024-003', 'approved', 'Home Insurance', 'Water damage from burst pipe', 3500.00, '2024-01-10');