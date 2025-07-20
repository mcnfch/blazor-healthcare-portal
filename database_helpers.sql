-- Database Helper SQL Commands
-- Use these instead of GUI tools to avoid enum and timestamp issues

-- ========================================
-- ENUM VALUES REFERENCE
-- ========================================

-- Available user_role values:
-- 'patient'::user_role, 'provider'::user_role, 'admin'::user_role, 'claims_adjuster'::user_role, 'customer_service'::user_role

-- Available gender_type values:
-- 'male'::gender_type, 'female'::gender_type, 'other'::gender_type, 'prefer_not_to_say'::gender_type

-- Available claim_status values:
-- 'submitted'::claim_status, 'under_review'::claim_status, 'pending_info'::claim_status, 'approved'::claim_status, 'denied'::claim_status, 'paid'::claim_status, 'appealed'::claim_status

-- ========================================
-- COMMON OPERATIONS
-- ========================================

-- Create new user
-- INSERT INTO users (email, password_hash, first_name, last_name, role, is_active, created_at, updated_at) 
-- VALUES ('user@example.com', '$2a$10$hashedpassword', 'John', 'Doe', 'patient'::user_role, true, NOW(), NOW());

-- Update user role
-- UPDATE users SET role = 'admin'::user_role WHERE id = 1;

-- Create new patient
-- INSERT INTO patients (user_id, patient_id, date_of_birth, gender, created_at, updated_at) 
-- VALUES (1, 'PAT001', '1990-01-15', 'male'::gender_type, NOW(), NOW());

-- Update patient gender
-- UPDATE patients SET gender = 'female'::gender_type WHERE id = 1;

-- Update claim status
-- UPDATE claims SET status = 'approved'::claim_status, updated_at = NOW() WHERE id = 1;

-- View all enums and their values
-- SELECT typname, oid FROM pg_type WHERE typtype = 'e';
-- SELECT unnest(enum_range(NULL::gender_type)) AS gender_values;
-- SELECT unnest(enum_range(NULL::user_role)) AS user_role_values;
-- SELECT unnest(enum_range(NULL::claim_status)) AS claim_status_values;

-- ========================================
-- FILE UPLOAD TESTING
-- ========================================

-- Check if document uploads are working
-- SELECT cd.id, cd.filename, cd.file_size, cd.document_type, c.claim_number
-- FROM claim_documents cd 
-- JOIN claims c ON cd.claim_id = c.id
-- ORDER BY cd.upload_date DESC;

-- Clean up test uploads
-- DELETE FROM claim_documents WHERE filename LIKE 'test_%';