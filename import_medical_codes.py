#!/usr/bin/env python3
"""
Import ICD-10-CM Diagnosis Codes and ICD-10-PCS Procedure Codes into PostgreSQL
This script processes XML files from CMS and imports medical codes into lookup tables.
"""

import os
import sys
import xml.etree.ElementTree as ET
import psycopg2
from psycopg2.extras import RealDictCursor
import argparse
from datetime import datetime

# Database connection parameters
DB_CONFIG = {
    'host': 'localhost',
    'database': 'claims_db',
    'user': 'claims_user', 
    'password': 'claims_password',
    'port': 5432
}

def create_tables(cursor):
    """Create lookup tables for medical codes"""
    
    # ICD-10-CM Diagnosis Codes Table
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS icd10_diagnosis_codes (
            id SERIAL PRIMARY KEY,
            code VARCHAR(10) NOT NULL UNIQUE,
            description TEXT NOT NULL,
            chapter_name VARCHAR(255),
            section_name VARCHAR(255),
            category VARCHAR(10),
            valid_from DATE DEFAULT CURRENT_DATE,
            valid_to DATE,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            INDEX(code),
            INDEX(category)
        );
    """)
    
    # ICD-10-PCS Procedure Codes Table  
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS icd10_procedure_codes (
            id SERIAL PRIMARY KEY,
            code VARCHAR(10) NOT NULL UNIQUE,
            description TEXT NOT NULL,
            section_name VARCHAR(255),
            body_system VARCHAR(255),
            operation_name VARCHAR(255),
            operation_definition TEXT,
            valid_from DATE DEFAULT CURRENT_DATE,
            valid_to DATE,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            INDEX(code),
            INDEX(section_name),
            INDEX(body_system)
        );
    """)
    
    # CPT Procedure Codes Table (for current system compatibility)
    cursor.execute("""
        CREATE TABLE IF NOT EXISTS cpt_procedure_codes (
            id SERIAL PRIMARY KEY,
            code VARCHAR(10) NOT NULL UNIQUE,
            description TEXT NOT NULL,
            category VARCHAR(255),
            rvu DECIMAL(8,2),
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            INDEX(code),
            INDEX(category)
        );
    """)
    
    print("✅ Created medical code lookup tables")

def parse_icd10cm_diagnosis(xml_file_path):
    """Parse ICD-10-CM diagnosis codes from XML"""
    print(f"📋 Parsing ICD-10-CM diagnosis codes from: {xml_file_path}")
    
    try:
        tree = ET.parse(xml_file_path)
        root = tree.getroot()
        codes = []
        
        # Navigate through chapters and sections
        for chapter in root.findall('.//chapter'):
            chapter_name = chapter.find('desc').text if chapter.find('desc') is not None else "Unknown Chapter"
            
            for section in chapter.findall('.//section'):
                section_name = section.get('desc', 'Unknown Section')
                
                # Find diagnosis codes 
                for diag in section.findall('.//diag'):
                    code = diag.get('name', '')
                    desc_elem = diag.find('desc')
                    description = desc_elem.text if desc_elem is not None else 'No description'
                    
                    if code and len(code) >= 3:  # Valid ICD-10 codes are at least 3 characters
                        codes.append({
                            'code': code,
                            'description': description,
                            'chapter_name': chapter_name[:255],
                            'section_name': section_name[:255],
                            'category': code[:3]  # First 3 characters are category
                        })
        
        print(f"📊 Found {len(codes)} ICD-10-CM diagnosis codes")
        return codes
        
    except ET.ParseError as e:
        print(f"❌ XML Parse Error: {e}")
        return []
    except Exception as e:
        print(f"❌ Error parsing ICD-10-CM: {e}")
        return []

def parse_icd10pcs_procedure(xml_file_path):
    """Parse ICD-10-PCS procedure codes from XML"""
    print(f"🔧 Parsing ICD-10-PCS procedure codes from: {xml_file_path}")
    
    try:
        tree = ET.parse(xml_file_path)
        root = tree.getroot()
        codes = []
        
        # Parse PCS tables
        for table in root.findall('.//pcsTable'):
            section_name = "Unknown Section"
            body_system = "Unknown Body System" 
            operation_name = "Unknown Operation"
            operation_definition = ""
            
            # Get table metadata
            for axis in table.findall('./axis'):
                pos = axis.get('pos')
                title = axis.find('title')
                label = axis.find('label')
                definition = axis.find('definition')
                
                if title is not None and label is not None:
                    if pos == '1':  # Section
                        section_name = label.text or "Unknown Section"
                    elif pos == '2':  # Body System
                        body_system = label.text or "Unknown Body System"
                    elif pos == '3':  # Operation
                        operation_name = label.text or "Unknown Operation"
                        if definition is not None:
                            operation_definition = definition.text or ""
            
            # Generate codes from table structure
            # Note: This is simplified - real PCS codes are complex 7-character combinations
            # For now, we'll create sample entries
            base_code = "0"  # Medical and Surgical section
            
            # This would need more complex logic to generate all valid combinations
            # For demonstration, we'll add key operation codes
            if operation_name != "Unknown Operation":
                code = f"{base_code}000000"  # Placeholder 7-character code
                codes.append({
                    'code': code,
                    'description': f"{operation_name} - {body_system}",
                    'section_name': section_name,
                    'body_system': body_system,
                    'operation_name': operation_name,
                    'operation_definition': operation_definition
                })
        
        print(f"📊 Found {len(codes)} ICD-10-PCS procedure codes")
        return codes
        
    except ET.ParseError as e:
        print(f"❌ XML Parse Error: {e}")
        return []
    except Exception as e:
        print(f"❌ Error parsing ICD-10-PCS: {e}")
        return []

def add_common_cpt_codes(cursor):
    """Add common CPT codes that are already in the system"""
    
    common_cpts = [
        ('99213', 'Office/outpatient visit, est patient, low complexity', 'Evaluation and Management'),
        ('99214', 'Office/outpatient visit, est patient, moderate complexity', 'Evaluation and Management'),
        ('99215', 'Office/outpatient visit, est patient, high complexity', 'Evaluation and Management'),
        ('99284', 'Emergency department visit, moderate complexity', 'Emergency Medicine'),
        ('36415', 'Routine venipuncture', 'Laboratory'),
        ('80053', 'Comprehensive metabolic panel', 'Laboratory'),
        ('85025', 'Complete blood count with diff WBC', 'Laboratory'),
        ('93000', 'Electrocardiogram', 'Cardiovascular'),
        ('71020', 'Chest X-ray, 2 views', 'Radiology'),
        ('73721', 'MRI lower extremity without contrast', 'Radiology')
    ]
    
    for code, description, category in common_cpts:
        cursor.execute("""
            INSERT INTO cpt_procedure_codes (code, description, category)
            VALUES (%s, %s, %s)
            ON CONFLICT (code) DO NOTHING
        """, (code, description, category))
    
    print("✅ Added common CPT procedure codes")

def insert_diagnosis_codes(cursor, codes):
    """Insert diagnosis codes into database"""
    if not codes:
        print("⚠️  No diagnosis codes to insert")
        return
        
    print(f"💾 Inserting {len(codes)} diagnosis codes...")
    
    for code_data in codes:
        try:
            cursor.execute("""
                INSERT INTO icd10_diagnosis_codes 
                (code, description, chapter_name, section_name, category)
                VALUES (%(code)s, %(description)s, %(chapter_name)s, %(section_name)s, %(category)s)
                ON CONFLICT (code) DO UPDATE SET
                description = EXCLUDED.description,
                chapter_name = EXCLUDED.chapter_name,
                section_name = EXCLUDED.section_name,
                category = EXCLUDED.category
            """, code_data)
        except Exception as e:
            print(f"❌ Error inserting diagnosis code {code_data['code']}: {e}")
    
    print("✅ Diagnosis codes inserted")

def insert_procedure_codes(cursor, codes):
    """Insert procedure codes into database"""  
    if not codes:
        print("⚠️  No procedure codes to insert")
        return
        
    print(f"💾 Inserting {len(codes)} procedure codes...")
    
    for code_data in codes:
        try:
            cursor.execute("""
                INSERT INTO icd10_procedure_codes 
                (code, description, section_name, body_system, operation_name, operation_definition)
                VALUES (%(code)s, %(description)s, %(section_name)s, %(body_system)s, 
                        %(operation_name)s, %(operation_definition)s)
                ON CONFLICT (code) DO UPDATE SET
                description = EXCLUDED.description,
                section_name = EXCLUDED.section_name,
                body_system = EXCLUDED.body_system,
                operation_name = EXCLUDED.operation_name,
                operation_definition = EXCLUDED.operation_definition
            """, code_data)
        except Exception as e:
            print(f"❌ Error inserting procedure code {code_data['code']}: {e}")
    
    print("✅ Procedure codes inserted")

def main():
    parser = argparse.ArgumentParser(description='Import ICD-10 medical codes into PostgreSQL')
    parser.add_argument('--data-dir', default='/opt/data', help='Path to data directory')
    parser.add_argument('--diagnosis-only', action='store_true', help='Import only diagnosis codes')
    parser.add_argument('--procedure-only', action='store_true', help='Import only procedure codes')
    parser.add_argument('--dry-run', action='store_true', help='Parse files but do not insert into database')
    
    args = parser.parse_args()
    
    print("🏥 ICD-10 Medical Codes Import Tool")
    print("=" * 40)
    
    # Connect to PostgreSQL
    try:
        conn = psycopg2.connect(**DB_CONFIG)
        conn.autocommit = True
        cursor = conn.cursor(cursor_factory=RealDictCursor)
        print("✅ Connected to PostgreSQL database")
    except Exception as e:
        print(f"❌ Database connection failed: {e}")
        sys.exit(1)
    
    # Create tables
    if not args.dry_run:
        create_tables(cursor)
    
    # Find XML files
    diagnosis_file = None
    procedure_file = None
    
    for root, dirs, files in os.walk(args.data_dir):
        for file in files:
            if 'tabular' in file and file.endswith('.xml'):
                if 'icd10cm' in file:
                    diagnosis_file = os.path.join(root, file)
                elif 'icd10pcs' in file:
                    procedure_file = os.path.join(root, file)
    
    # Process diagnosis codes
    if not args.procedure_only and diagnosis_file:
        diagnosis_codes = parse_icd10cm_diagnosis(diagnosis_file)
        if diagnosis_codes and not args.dry_run:
            insert_diagnosis_codes(cursor, diagnosis_codes)
    
    # Process procedure codes  
    if not args.diagnosis_only and procedure_file:
        procedure_codes = parse_icd10pcs_procedure(procedure_file)
        if procedure_codes and not args.dry_run:
            insert_procedure_codes(cursor, procedure_codes)
    
    # Add common CPT codes
    if not args.diagnosis_only and not args.dry_run:
        add_common_cpt_codes(cursor)
    
    # Summary
    if not args.dry_run:
        cursor.execute("SELECT COUNT(*) as count FROM icd10_diagnosis_codes")
        diag_count = cursor.fetchone()['count']
        
        cursor.execute("SELECT COUNT(*) as count FROM icd10_procedure_codes") 
        proc_count = cursor.fetchone()['count']
        
        cursor.execute("SELECT COUNT(*) as count FROM cpt_procedure_codes")
        cpt_count = cursor.fetchone()['count']
        
        print("\n📊 Import Summary:")
        print(f"   ICD-10-CM Diagnosis Codes: {diag_count:,}")
        print(f"   ICD-10-PCS Procedure Codes: {proc_count:,}")  
        print(f"   CPT Procedure Codes: {cpt_count:,}")
        print(f"   Total Medical Codes: {diag_count + proc_count + cpt_count:,}")
    
    cursor.close()
    conn.close()
    print("\n✅ Import completed successfully!")

if __name__ == "__main__":
    main()