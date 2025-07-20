#!/usr/bin/env python3
"""
Simple Medical Codes Import Script
Adds essential ICD-10 and CPT codes that are commonly used in healthcare claims
"""

import psycopg2
from psycopg2.extras import RealDictCursor

# Database connection 
DB_CONFIG = {
    'host': 'localhost',
    'database': 'claims_db', 
    'user': 'claims_user',
    'password': 'claims_password',
    'port': 5432
}

def create_tables(cursor):
    """Create medical code lookup tables"""
    
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS icd10_diagnosis_codes (
            id SERIAL PRIMARY KEY,
            code VARCHAR(10) NOT NULL UNIQUE,
            description TEXT NOT NULL,
            chapter_name VARCHAR(255),
            category VARCHAR(10),
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        CREATE INDEX IF NOT EXISTS idx_icd10_diag_code ON icd10_diagnosis_codes(code);
        CREATE INDEX IF NOT EXISTS idx_icd10_diag_category ON icd10_diagnosis_codes(category);
    """)
    
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS cpt_procedure_codes (
            id SERIAL PRIMARY KEY,
            code VARCHAR(10) NOT NULL UNIQUE,
            description TEXT NOT NULL,
            category VARCHAR(255),
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
        CREATE INDEX IF NOT EXISTS idx_cpt_code ON cpt_procedure_codes(code);
        CREATE INDEX IF NOT EXISTS idx_cpt_category ON cpt_procedure_codes(category);
    """)
    
    print("✅ Created medical code tables")

def insert_common_diagnosis_codes(cursor):
    """Insert commonly used ICD-10-CM diagnosis codes"""
    
    # Essential ICD-10-CM diagnosis codes
    diagnosis_codes = [
        # Z codes - Health encounters
        ('Z00.00', 'Encounter for general adult medical examination without abnormal findings', 'Health encounters', 'Z00'),
        ('Z00.01', 'Encounter for general adult medical examination with abnormal findings', 'Health encounters', 'Z00'),
        ('Z12.11', 'Encounter for screening for malignant neoplasm of colon', 'Health encounters', 'Z12'),
        ('Z51.11', 'Encounter for antineoplastic chemotherapy', 'Health encounters', 'Z51'),
        
        # Common conditions
        ('I10', 'Essential hypertension', 'Cardiovascular', 'I10'),
        ('E11.9', 'Type 2 diabetes mellitus without complications', 'Endocrine', 'E11'),
        ('J44.1', 'Chronic obstructive pulmonary disease with acute exacerbation', 'Respiratory', 'J44'),
        ('M79.3', 'Panniculitis, unspecified', 'Musculoskeletal', 'M79'),
        ('F32.9', 'Major depressive disorder, single episode, unspecified', 'Mental health', 'F32'),
        
        # Injuries
        ('S72.001A', 'Fracture of unspecified part of neck of right femur, initial encounter', 'Injuries', 'S72'),
        ('S06.0X0A', 'Concussion without loss of consciousness, initial encounter', 'Injuries', 'S06'),
        ('T14.90XA', 'Injury, unspecified, initial encounter', 'Injuries', 'T14'),
        
        # Symptoms
        ('R50.9', 'Fever, unspecified', 'Symptoms', 'R50'),
        ('R06.02', 'Shortness of breath', 'Symptoms', 'R06'),
        ('R10.9', 'Abdominal pain, unspecified', 'Symptoms', 'R10'),
        ('K59.00', 'Constipation, unspecified', 'Digestive', 'K59'),
        
        # Pregnancy
        ('O80', 'Encounter for full-term uncomplicated delivery', 'Pregnancy', 'O80'),
        ('Z34.90', 'Encounter for supervision of normal pregnancy, unspecified trimester', 'Pregnancy', 'Z34'),
        
        # Preventive care
        ('Z23', 'Encounter for immunization', 'Preventive', 'Z23'),
        ('Z71.3', 'Dietary counseling and surveillance', 'Preventive', 'Z71')
    ]
    
    for code, description, chapter, category in diagnosis_codes:
        cursor.execute("""
            INSERT INTO icd10_diagnosis_codes (code, description, chapter_name, category)
            VALUES (%s, %s, %s, %s)
            ON CONFLICT (code) DO UPDATE SET 
            description = EXCLUDED.description,
            chapter_name = EXCLUDED.chapter_name
        """, (code, description, chapter, category))
    
    print(f"✅ Inserted {len(diagnosis_codes)} ICD-10-CM diagnosis codes")

def insert_common_procedure_codes(cursor):
    """Insert commonly used CPT procedure codes"""
    
    # Essential CPT codes
    procedure_codes = [
        # Evaluation & Management
        ('99201', 'Office/outpatient visit, new patient, straightforward', 'Evaluation and Management'),
        ('99202', 'Office/outpatient visit, new patient, low complexity', 'Evaluation and Management'), 
        ('99203', 'Office/outpatient visit, new patient, moderate complexity', 'Evaluation and Management'),
        ('99211', 'Office/outpatient visit, established patient, minimal', 'Evaluation and Management'),
        ('99212', 'Office/outpatient visit, established patient, straightforward', 'Evaluation and Management'),
        ('99213', 'Office/outpatient visit, established patient, low complexity', 'Evaluation and Management'),
        ('99214', 'Office/outpatient visit, established patient, moderate complexity', 'Evaluation and Management'),
        ('99215', 'Office/outpatient visit, established patient, high complexity', 'Evaluation and Management'),
        
        # Emergency Medicine
        ('99281', 'Emergency department visit, straightforward', 'Emergency Medicine'),
        ('99282', 'Emergency department visit, low complexity', 'Emergency Medicine'),
        ('99283', 'Emergency department visit, moderate complexity', 'Emergency Medicine'), 
        ('99284', 'Emergency department visit, high complexity', 'Emergency Medicine'),
        ('99285', 'Emergency department visit, comprehensive', 'Emergency Medicine'),
        
        # Preventive Medicine
        ('99385', 'Initial comprehensive preventive medicine, 18-39 years', 'Preventive Medicine'),
        ('99386', 'Initial comprehensive preventive medicine, 40-64 years', 'Preventive Medicine'),
        ('99395', 'Periodic comprehensive preventive medicine, 18-39 years', 'Preventive Medicine'),
        ('99396', 'Periodic comprehensive preventive medicine, 40-64 years', 'Preventive Medicine'),
        
        # Laboratory
        ('80053', 'Comprehensive metabolic panel', 'Laboratory'),
        ('80061', 'Lipid panel', 'Laboratory'),
        ('85025', 'Complete blood count with differential', 'Laboratory'),
        ('85610', 'Prothrombin time', 'Laboratory'),
        ('36415', 'Routine venipuncture', 'Laboratory'),
        ('87804', 'Infectious agent detection by nucleic acid', 'Laboratory'),
        
        # Radiology
        ('71020', 'Chest X-ray, 2 views', 'Radiology'),
        ('71010', 'Chest X-ray, single view', 'Radiology'),
        ('73721', 'MRI lower extremity without contrast', 'Radiology'),
        ('74177', 'CT abdomen and pelvis with contrast', 'Radiology'),
        ('76805', 'Ultrasound pregnant uterus', 'Radiology'),
        
        # Cardiology
        ('93000', 'Electrocardiogram', 'Cardiology'),
        ('93005', 'Electrocardiogram, tracing only', 'Cardiology'),
        ('93306', 'Echocardiography complete', 'Cardiology'),
        
        # Surgery
        ('10060', 'Incision and drainage of abscess', 'Surgery'),
        ('12001', 'Simple repair of superficial wounds', 'Surgery'),
        ('29881', 'Arthroscopy, knee, with meniscectomy', 'Surgery'),
        
        # Immunizations
        ('90471', 'Immunization administration', 'Immunizations'),
        ('90630', 'Influenza vaccine', 'Immunizations'),
        ('90707', 'MMR vaccine', 'Immunizations')
    ]
    
    for code, description, category in procedure_codes:
        cursor.execute("""
            INSERT INTO cpt_procedure_codes (code, description, category)
            VALUES (%s, %s, %s)
            ON CONFLICT (code) DO UPDATE SET
            description = EXCLUDED.description,
            category = EXCLUDED.category
        """, (code, description, category))
    
    print(f"✅ Inserted {len(procedure_codes)} CPT procedure codes")

def main():
    print("🏥 Basic Medical Codes Import")
    print("=" * 35)
    
    # Connect to database
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        conn.autocommit = True
        cursor = conn.cursor(cursor_factory=RealDictCursor)
        print("✅ Connected to PostgreSQL database")
    except Exception as e:
        print(f"❌ Database connection failed: {e}")
        return
    
    try:
        # Create tables
        create_tables(cursor)
        
        # Insert codes
        insert_common_diagnosis_codes(cursor)
        insert_common_procedure_codes(cursor)
        
        # Get counts
        cursor.execute("SELECT COUNT(*) as count FROM icd10_diagnosis_codes")
        diag_count = cursor.fetchone()['count']
        
        cursor.execute("SELECT COUNT(*) as count FROM cpt_procedure_codes")
        proc_count = cursor.fetchone()['count']
        
        print("\n📊 Import Summary:")
        print(f"   ICD-10-CM Diagnosis Codes: {diag_count:,}")
        print(f"   CPT Procedure Codes: {proc_count:,}")
        print(f"   Total Medical Codes: {diag_count + proc_count:,}")
        
        print("\n✅ Basic medical codes imported successfully!")
        
    except Exception as e:
        print(f"❌ Import failed: {e}")
    finally:
        cursor.close()
        conn.close()

if __name__ == "__main__":
    main()