-- Healthcare Insurance Database Schema
-- Comprehensive claims processing system for healthcare insurance workflow

-- Drop existing tables if they exist (in reverse dependency order)
DROP TABLE IF EXISTS claim_documents CASCADE;
DROP TABLE IF EXISTS claim_line_items CASCADE;
DROP TABLE IF EXISTS claims CASCADE;
DROP TABLE IF EXISTS patient_insurance_plans CASCADE;
DROP TABLE IF EXISTS insurance_plans CASCADE;
DROP TABLE IF EXISTS insurance_companies CASCADE;
DROP TABLE IF EXISTS provider_network_participation CASCADE;
DROP TABLE IF EXISTS provider_networks CASCADE;
DROP TABLE IF EXISTS healthcare_providers CASCADE;
DROP TABLE IF EXISTS patients CASCADE;
DROP TABLE IF EXISTS addresses CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS audit_logs CASCADE;
DROP TABLE IF EXISTS system_configurations CASCADE;

-- Create enum types
CREATE TYPE user_role AS ENUM ('patient', 'provider', 'admin', 'claims_adjuster', 'customer_service');
CREATE TYPE claim_status AS ENUM ('submitted', 'under_review', 'pending_info', 'approved', 'denied', 'paid', 'appealed');
CREATE TYPE claim_type AS ENUM ('medical', 'dental', 'vision', 'prescription', 'emergency', 'preventive', 'specialist');
CREATE TYPE coverage_type AS ENUM ('individual', 'family', 'employee', 'employee_spouse', 'employee_children', 'employee_family');
CREATE TYPE plan_type AS ENUM ('hmo', 'ppo', 'epo', 'pos', 'hdhp', 'catastrophic');
CREATE TYPE provider_type AS ENUM ('primary_care', 'specialist', 'hospital', 'clinic', 'lab', 'pharmacy', 'mental_health', 'emergency');
CREATE TYPE gender_type AS ENUM ('male', 'female', 'other', 'prefer_not_to_say');

-- Users table (for authentication and authorization)
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    role user_role NOT NULL DEFAULT 'patient',
    is_active BOOLEAN DEFAULT true,
    last_login TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Addresses table
CREATE TABLE addresses (
    id SERIAL PRIMARY KEY,
    street_address VARCHAR(255) NOT NULL,
    apartment_unit VARCHAR(50),
    city VARCHAR(100) NOT NULL,
    state_province VARCHAR(50) NOT NULL,
    postal_code VARCHAR(20) NOT NULL,
    country VARCHAR(50) DEFAULT 'United States',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Patients table
CREATE TABLE patients (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id),
    patient_id VARCHAR(50) UNIQUE NOT NULL, -- External patient identifier
    date_of_birth DATE NOT NULL,
    gender gender_type,
    phone_number VARCHAR(20),
    address_id INTEGER REFERENCES addresses(id),
    emergency_contact_name VARCHAR(200),
    emergency_contact_phone VARCHAR(20),
    medical_record_number VARCHAR(50),
    social_security_number VARCHAR(11), -- Encrypted in real implementation
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Healthcare Providers table
CREATE TABLE healthcare_providers (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id),
    provider_id VARCHAR(50) UNIQUE NOT NULL, -- NPI or other identifier
    provider_name VARCHAR(200) NOT NULL,
    provider_type provider_type NOT NULL,
    license_number VARCHAR(100),
    specialties TEXT[], -- Array of specialties
    phone_number VARCHAR(20),
    fax_number VARCHAR(20),
    address_id INTEGER REFERENCES addresses(id),
    is_accepting_patients BOOLEAN DEFAULT true,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Provider Networks table
CREATE TABLE provider_networks (
    id SERIAL PRIMARY KEY,
    network_name VARCHAR(200) NOT NULL,
    network_code VARCHAR(50) UNIQUE NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Provider Network Participation table
CREATE TABLE provider_network_participation (
    id SERIAL PRIMARY KEY,
    provider_id INTEGER NOT NULL REFERENCES healthcare_providers(id),
    network_id INTEGER NOT NULL REFERENCES provider_networks(id),
    effective_date DATE NOT NULL,
    termination_date DATE,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(provider_id, network_id)
);

-- Insurance Companies table
CREATE TABLE insurance_companies (
    id SERIAL PRIMARY KEY,
    company_name VARCHAR(200) NOT NULL,
    company_code VARCHAR(50) UNIQUE NOT NULL,
    phone_number VARCHAR(20),
    website VARCHAR(255),
    address_id INTEGER REFERENCES addresses(id),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Insurance Plans table
CREATE TABLE insurance_plans (
    id SERIAL PRIMARY KEY,
    insurance_company_id INTEGER NOT NULL REFERENCES insurance_companies(id),
    plan_name VARCHAR(200) NOT NULL,
    plan_code VARCHAR(100) UNIQUE NOT NULL,
    plan_type plan_type NOT NULL,
    coverage_type coverage_type NOT NULL,
    network_id INTEGER REFERENCES provider_networks(id),
    
    -- Financial details
    monthly_premium DECIMAL(10,2) NOT NULL,
    annual_deductible DECIMAL(10,2) NOT NULL,
    out_of_pocket_maximum DECIMAL(10,2) NOT NULL,
    
    -- Coverage percentages
    in_network_coverage_pct DECIMAL(5,2) DEFAULT 80.00,
    out_of_network_coverage_pct DECIMAL(5,2) DEFAULT 60.00,
    
    -- Copays
    primary_care_copay DECIMAL(8,2),
    specialist_copay DECIMAL(8,2),
    emergency_room_copay DECIMAL(8,2),
    urgent_care_copay DECIMAL(8,2),
    prescription_copay DECIMAL(8,2),
    
    effective_date DATE NOT NULL,
    termination_date DATE,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Patient Insurance Plans (enrollment) table
CREATE TABLE patient_insurance_plans (
    id SERIAL PRIMARY KEY,
    patient_id INTEGER NOT NULL REFERENCES patients(id),
    insurance_plan_id INTEGER NOT NULL REFERENCES insurance_plans(id),
    policy_number VARCHAR(100) NOT NULL,
    group_number VARCHAR(100),
    
    -- Enrollment details
    effective_date DATE NOT NULL,
    termination_date DATE,
    is_primary BOOLEAN DEFAULT true,
    
    -- Deductible tracking
    annual_deductible_met DECIMAL(10,2) DEFAULT 0.00,
    out_of_pocket_met DECIMAL(10,2) DEFAULT 0.00,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(patient_id, insurance_plan_id, effective_date)
);

-- Claims table
CREATE TABLE claims (
    id SERIAL PRIMARY KEY,
    claim_number VARCHAR(50) UNIQUE NOT NULL,
    patient_id INTEGER NOT NULL REFERENCES patients(id),
    insurance_plan_id INTEGER NOT NULL REFERENCES insurance_plans(id),
    provider_id INTEGER NOT NULL REFERENCES healthcare_providers(id),
    
    -- Claim details
    claim_type claim_type NOT NULL,
    status claim_status DEFAULT 'submitted',
    priority_level INTEGER DEFAULT 3, -- 1=urgent, 5=routine
    
    -- Financial information
    total_amount DECIMAL(12,2) NOT NULL,
    approved_amount DECIMAL(12,2),
    patient_responsibility DECIMAL(12,2),
    insurance_payment DECIMAL(12,2),
    
    -- Service details
    service_date DATE NOT NULL,
    diagnosis_codes TEXT[], -- Array of ICD-10 codes
    procedure_codes TEXT[], -- Array of CPT codes
    
    -- Processing information
    submitted_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    processed_date TIMESTAMP,
    paid_date TIMESTAMP,
    
    -- Review information
    assigned_adjuster_id INTEGER REFERENCES users(id),
    review_notes TEXT,
    denial_reason TEXT,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Claim Line Items table (for detailed breakdown)
CREATE TABLE claim_line_items (
    id SERIAL PRIMARY KEY,
    claim_id INTEGER NOT NULL REFERENCES claims(id),
    line_number INTEGER NOT NULL,
    
    -- Service details
    procedure_code VARCHAR(20) NOT NULL,
    procedure_description TEXT,
    diagnosis_code VARCHAR(20),
    service_date DATE NOT NULL,
    quantity INTEGER DEFAULT 1,
    
    -- Financial details
    unit_price DECIMAL(10,2) NOT NULL,
    total_amount DECIMAL(12,2) NOT NULL,
    allowed_amount DECIMAL(12,2),
    deductible_amount DECIMAL(10,2) DEFAULT 0.00,
    copay_amount DECIMAL(10,2) DEFAULT 0.00,
    coinsurance_amount DECIMAL(10,2) DEFAULT 0.00,
    not_covered_amount DECIMAL(10,2) DEFAULT 0.00,
    
    -- Processing
    status claim_status DEFAULT 'submitted',
    denial_reason TEXT,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(claim_id, line_number)
);

-- Claim Documents table
CREATE TABLE claim_documents (
    id SERIAL PRIMARY KEY,
    claim_id INTEGER NOT NULL REFERENCES claims(id),
    document_type VARCHAR(100) NOT NULL,
    filename VARCHAR(255) NOT NULL,
    file_path VARCHAR(500) NOT NULL,
    file_size INTEGER NOT NULL,
    mime_type VARCHAR(100),
    
    -- Metadata
    uploaded_by INTEGER REFERENCES users(id),
    upload_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Processing status
    is_processed BOOLEAN DEFAULT false,
    processing_status VARCHAR(50) DEFAULT 'pending',
    extracted_data JSONB,
    
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Audit Logs table
CREATE TABLE audit_logs (
    id SERIAL PRIMARY KEY,
    table_name VARCHAR(100) NOT NULL,
    record_id INTEGER NOT NULL,
    action VARCHAR(20) NOT NULL, -- INSERT, UPDATE, DELETE
    old_values JSONB,
    new_values JSONB,
    changed_by INTEGER REFERENCES users(id),
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ip_address INET,
    user_agent TEXT
);

-- System Configurations table
CREATE TABLE system_configurations (
    id SERIAL PRIMARY KEY,
    config_key VARCHAR(100) UNIQUE NOT NULL,
    config_value TEXT NOT NULL,
    config_description TEXT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for better performance
CREATE INDEX idx_patients_user_id ON patients(user_id);
CREATE INDEX idx_patients_patient_id ON patients(patient_id);
CREATE INDEX idx_claims_patient_id ON claims(patient_id);
CREATE INDEX idx_claims_provider_id ON claims(provider_id);
CREATE INDEX idx_claims_insurance_plan_id ON claims(insurance_plan_id);
CREATE INDEX idx_claims_status ON claims(status);
CREATE INDEX idx_claims_service_date ON claims(service_date);
CREATE INDEX idx_claims_submitted_date ON claims(submitted_date);
CREATE INDEX idx_claim_line_items_claim_id ON claim_line_items(claim_id);
CREATE INDEX idx_claim_documents_claim_id ON claim_documents(claim_id);
CREATE INDEX idx_patient_insurance_plans_patient_id ON patient_insurance_plans(patient_id);
CREATE INDEX idx_provider_network_participation_provider ON provider_network_participation(provider_id);
CREATE INDEX idx_audit_logs_table_record ON audit_logs(table_name, record_id);

-- Insert sample data for development
INSERT INTO system_configurations (config_key, config_value, config_description) VALUES
('claim_auto_approval_threshold', '500.00', 'Claims under this amount may be auto-approved'),
('max_file_upload_size', '10485760', 'Maximum file upload size in bytes (10MB)'),
('claim_review_timeout_days', '30', 'Days before claim review times out');

-- Sample addresses
INSERT INTO addresses (street_address, city, state_province, postal_code) VALUES
('123 Main St', 'Boston', 'MA', '02101'),
('456 Oak Ave', 'Boston', 'MA', '02102'),
('789 Pine Rd', 'Cambridge', 'MA', '02139'),
('321 Elm St', 'Boston', 'MA', '02103'),
('654 Maple Dr', 'Newton', 'MA', '02458');

-- Sample users
INSERT INTO users (email, password_hash, first_name, last_name, role) VALUES
('john.patient@example.com', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LeGEFtYpNrk0CzCH6', 'John', 'Smith', 'patient'),
('jane.patient@example.com', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LeGEFtYpNrk0CzCH6', 'Jane', 'Doe', 'patient'),
('dr.wilson@example.com', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LeGEFtYpNrk0CzCH6', 'Sarah', 'Wilson', 'provider'),
('admin@example.com', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LeGEFtYpNrk0CzCH6', 'System', 'Administrator', 'admin'),
('adjuster@example.com', '$2b$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LeGEFtYpNrk0CzCH6', 'Mike', 'Johnson', 'claims_adjuster');

-- Sample patients
INSERT INTO patients (user_id, patient_id, date_of_birth, gender, phone_number, address_id, emergency_contact_name, emergency_contact_phone, medical_record_number) VALUES
(1, 'PAT001', '1985-06-15', 'male', '555-0101', 1, 'Mary Smith', '555-0102', 'MRN001'),
(2, 'PAT002', '1990-03-22', 'female', '555-0201', 2, 'Bob Doe', '555-0202', 'MRN002');

-- Sample insurance companies
INSERT INTO insurance_companies (company_name, company_code, phone_number, website, address_id) VALUES
('Blue Cross Blue Shield', 'BCBS', '1-800-555-0001', 'https://bcbs.com', 3),
('Aetna Health', 'AETNA', '1-800-555-0002', 'https://aetna.com', 4),
('UnitedHealth', 'UHC', '1-800-555-0003', 'https://uhc.com', 5);

-- Sample provider networks
INSERT INTO provider_networks (network_name, network_code, description) VALUES
('Boston Metro Network', 'BMN', 'Primary network covering Boston metropolitan area'),
('Massachusetts Statewide', 'MAS', 'Statewide network covering all of Massachusetts'),
('Northeast Regional', 'NER', 'Regional network covering New England states');

-- Sample healthcare providers
INSERT INTO healthcare_providers (user_id, provider_id, provider_name, provider_type, license_number, specialties, phone_number, address_id) VALUES
(3, 'PRV001', 'Boston Family Medicine', 'primary_care', 'MD12345', '{"Family Medicine", "Internal Medicine"}', '555-0301', 1),
(NULL, 'PRV002', 'Mass General Hospital', 'hospital', 'HSP001', '{"Emergency Medicine", "Surgery", "Cardiology"}', '555-0302', 2);

-- Sample provider network participation
INSERT INTO provider_network_participation (provider_id, network_id, effective_date) VALUES
(1, 1, '2024-01-01'),
(1, 2, '2024-01-01'),
(2, 1, '2024-01-01'),
(2, 2, '2024-01-01'),
(2, 3, '2024-01-01');

-- Sample insurance plans
INSERT INTO insurance_plans (insurance_company_id, plan_name, plan_code, plan_type, coverage_type, network_id, monthly_premium, annual_deductible, out_of_pocket_maximum, primary_care_copay, specialist_copay, emergency_room_copay, effective_date) VALUES
(1, 'BCBS Gold PPO', 'BCBS-GOLD-PPO', 'ppo', 'individual', 1, 450.00, 1500.00, 8000.00, 25.00, 50.00, 300.00, '2024-01-01'),
(2, 'Aetna Silver HMO', 'AETNA-SILVER-HMO', 'hmo', 'family', 2, 320.00, 2500.00, 10000.00, 20.00, 40.00, 250.00, '2024-01-01');

-- Sample patient insurance enrollment
INSERT INTO patient_insurance_plans (patient_id, insurance_plan_id, policy_number, group_number, effective_date) VALUES
(1, 1, 'BCBS123456789', 'GRP001', '2024-01-01'),
(2, 2, 'AETNA987654321', 'GRP002', '2024-01-01');

-- Sample claims
INSERT INTO claims (claim_number, patient_id, insurance_plan_id, provider_id, claim_type, total_amount, service_date, diagnosis_codes, procedure_codes, priority_level) VALUES
('CLM-2024-000001', 1, 1, 1, 'medical', 250.00, '2024-01-15', '{"Z00.00"}', '{"99213"}', 3),
('CLM-2024-000002', 2, 2, 2, 'emergency', 1500.00, '2024-01-20', '{"S72.001A"}', '{"99284"}', 1),
('CLM-2024-000003', 1, 1, 1, 'preventive', 175.00, '2024-02-01', '{"Z00.00"}', '{"99395"}', 4);

-- Sample claim line items
INSERT INTO claim_line_items (claim_id, line_number, procedure_code, procedure_description, diagnosis_code, service_date, quantity, unit_price, total_amount) VALUES
(1, 1, '99213', 'Office visit, established patient, moderate complexity', 'Z00.00', '2024-01-15', 1, 250.00, 250.00),
(2, 1, '99284', 'Emergency department visit, high complexity', 'S72.001A', '2024-01-20', 1, 1500.00, 1500.00),
(3, 1, '99395', 'Preventive visit, age 18-39', 'Z00.00', '2024-02-01', 1, 175.00, 175.00);

-- Update claims with processing information (simulate some processing)
UPDATE claims SET 
    status = 'approved',
    approved_amount = 200.00,
    patient_responsibility = 25.00,
    insurance_payment = 175.00,
    processed_date = '2024-01-16 10:30:00'
WHERE id = 1;

UPDATE claims SET 
    status = 'under_review',
    assigned_adjuster_id = 5
WHERE id = 2;

UPDATE claims SET 
    status = 'paid',
    approved_amount = 175.00,
    patient_responsibility = 0.00,
    insurance_payment = 175.00,
    processed_date = '2024-02-02 09:15:00',
    paid_date = '2024-02-05 14:20:00'
WHERE id = 3;