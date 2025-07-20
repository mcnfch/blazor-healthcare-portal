#!/usr/bin/env python3
"""
Full ICD-10 XML Import Script
Processes the official CMS ICD-10-CM and ICD-10-PCS XML files
"""

import os
import sys
import xml.etree.ElementTree as ET
import psycopg2
from psycopg2.extras import RealDictCursor
import argparse
import re

DB_CONFIG = {
    'host': 'localhost',
    'database': 'claims_db',
    'user': 'claims_user',
    'password': 'claims_password',
    'port': 5432
}

def parse_icd10cm_xml(xml_file):
    """Parse ICD-10-CM diagnosis codes from CMS XML format"""
    print(f"📋 Parsing ICD-10-CM file: {xml_file}")
    
    try:
        tree = ET.parse(xml_file)
        root = tree.getroot()
        
        diagnosis_codes = []
        current_chapter = "Unknown Chapter"
        
        # Navigate through the XML structure
        for element in root.iter():
            # Track current chapter
            if element.tag == 'chapter':
                chapter_desc = element.find('desc')
                if chapter_desc is not None:
                    current_chapter = chapter_desc.text or "Unknown Chapter"
            
            # Extract diagnosis codes
            elif element.tag == 'diag':
                # Get code from name child element
                name_elem = element.find('name')
                code = name_elem.text.strip() if name_elem is not None else ''
                
                # Get description
                desc_elem = element.find('desc')
                description = desc_elem.text.strip() if desc_elem is not None else 'No description'
                
                # Validate ICD-10 code format
                if code and len(code) >= 3 and re.match(r'^[A-Z][0-9][0-9A-Z]', code):
                    diagnosis_codes.append({
                        'code': code,
                        'description': description,
                        'chapter_name': current_chapter[:255],
                        'category': code[:3]
                    })
        
        print(f"📊 Parsed {len(diagnosis_codes)} ICD-10-CM diagnosis codes")
        return diagnosis_codes
        
    except Exception as e:
        print(f"❌ Error parsing ICD-10-CM XML: {e}")
        return []

def parse_icd10pcs_xml(xml_file):
    """Parse ICD-10-PCS procedure codes from CMS XML format"""
    print(f"🔧 Parsing ICD-10-PCS file: {xml_file}")
    
    try:
        tree = ET.parse(xml_file)
        root = tree.getroot()
        
        procedure_codes = []
        
        # ICD-10-PCS has a complex table structure
        # For now, extract what we can from the axis definitions
        for table in root.findall('.//pcsTable'):
            section_name = ""
            body_system = ""
            operation_name = ""
            operation_definition = ""
            
            # Extract table metadata from axes
            for axis in table.findall('axis'):
                pos = axis.get('pos')
                title_elem = axis.find('title')
                
                if title_elem is not None:
                    title = title_elem.text or ""
                    
                    # Get the first label for this axis
                    label_elem = axis.find('label')
                    if label_elem is not None:
                        label_text = label_elem.text or ""
                        
                        if pos == '1' and 'Section' in title:
                            section_name = label_text
                        elif pos == '2' and 'Body System' in title:
                            body_system = label_text
                        elif pos == '3' and 'Operation' in title:
                            operation_name = label_text
                            # Look for definition
                            def_elem = axis.find('definition')
                            if def_elem is not None:
                                operation_definition = def_elem.text or ""
            
            # Create a representative entry for this table
            if operation_name and section_name:
                # Generate a sample 7-character PCS code (this is simplified)
                base_code = "0000000"  # Placeholder
                if section_name:
                    base_code = "0" + base_code[1:]  # Medical and Surgical
                
                procedure_codes.append({
                    'code': base_code,
                    'description': f"{operation_name} - {body_system}".strip(' - '),
                    'section_name': section_name,
                    'body_system': body_system,
                    'operation_name': operation_name,
                    'operation_definition': operation_definition
                })
        
        # Remove duplicates based on description
        unique_codes = {}
        for code in procedure_codes:
            desc = code['description']
            if desc not in unique_codes:
                unique_codes[desc] = code
        
        procedure_codes = list(unique_codes.values())
        
        print(f"📊 Parsed {len(procedure_codes)} unique ICD-10-PCS procedure concepts")
        return procedure_codes
        
    except Exception as e:
        print(f"❌ Error parsing ICD-10-PCS XML: {e}")
        return []

def insert_codes(cursor, table_name, codes, code_fields):
    """Generic function to insert codes into database"""
    if not codes:
        return 0
    
    print(f"💾 Inserting {len(codes)} codes into {table_name}...")
    
    inserted = 0
    for code_data in codes:
        try:
            # Build dynamic insert query
            fields = list(code_fields.keys())
            placeholders = ', '.join(['%s'] * len(fields))
            values = [code_data.get(field, '') for field in fields]
            
            # Create ON CONFLICT clause
            update_clause = ', '.join([f"{field} = EXCLUDED.{field}" for field in fields if field != 'code'])
            
            query = f"""
                INSERT INTO {table_name} ({', '.join(fields)})
                VALUES ({placeholders})
                ON CONFLICT (code) DO UPDATE SET {update_clause}
            """
            
            cursor.execute(query, values)
            inserted += 1
            
        except Exception as e:
            print(f"❌ Error inserting code {code_data.get('code', 'unknown')}: {e}")
    
    print(f"✅ Inserted {inserted} codes into {table_name}")
    return inserted

def main():
    parser = argparse.ArgumentParser(description='Import ICD-10 codes from CMS XML files')
    parser.add_argument('--data-dir', default='/opt/data', help='Data directory path')
    parser.add_argument('--dry-run', action='store_true', help='Parse only, do not insert')
    parser.add_argument('--diagnosis-only', action='store_true', help='Import only diagnosis codes')
    parser.add_argument('--procedure-only', action='store_true', help='Import only procedure codes')
    
    args = parser.parse_args()
    
    print("🏥 Full ICD-10 XML Import Tool")
    print("=" * 40)
    
    # Find XML files
    diagnosis_files = []
    procedure_files = []
    
    for root, dirs, files in os.walk(args.data_dir):
        for file in files:
            full_path = os.path.join(root, file)
            if file.endswith('.xml'):
                if 'icd10cm' in file.lower() and 'tabular' in file.lower():
                    diagnosis_files.append(full_path)
                elif 'icd10pcs' in file.lower() and 'tabular' in file.lower():
                    procedure_files.append(full_path)
    
    print(f"📁 Found {len(diagnosis_files)} ICD-10-CM files")
    print(f"📁 Found {len(procedure_files)} ICD-10-PCS files")
    
    if not diagnosis_files and not procedure_files:
        print("❌ No suitable XML files found")
        return
    
    # Connect to database
    if not args.dry_run:
        try:
            conn = psycopg2.connect(**DB_CONFIG)
            conn.autocommit = True
            cursor = conn.cursor(cursor_factory=RealDictCursor)
            print("✅ Connected to database")
        except Exception as e:
            print(f"❌ Database connection failed: {e}")
            return
    
    total_inserted = 0
    
    # Process diagnosis codes
    if not args.procedure_only and diagnosis_files:
        for file_path in diagnosis_files:
            diagnosis_codes = parse_icd10cm_xml(file_path)
            
            if diagnosis_codes and not args.dry_run:
                diag_fields = {
                    'code': str,
                    'description': str,
                    'chapter_name': str,
                    'category': str
                }
                inserted = insert_codes(cursor, 'icd10_diagnosis_codes', diagnosis_codes, diag_fields)
                total_inserted += inserted
    
    # Process procedure codes  
    if not args.diagnosis_only and procedure_files:
        for file_path in procedure_files:
            procedure_codes = parse_icd10pcs_xml(file_path)
            
            if procedure_codes and not args.dry_run:
                # First ensure the table exists with correct schema
                cursor.execute("""
                    CREATE TABLE IF NOT EXISTS icd10_procedure_codes (
                        id SERIAL PRIMARY KEY,
                        code VARCHAR(10) NOT NULL UNIQUE,
                        description TEXT NOT NULL,
                        section_name VARCHAR(255),
                        body_system VARCHAR(255),
                        operation_name VARCHAR(255),
                        operation_definition TEXT,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );
                """)
                
                proc_fields = {
                    'code': str,
                    'description': str,
                    'section_name': str,
                    'body_system': str,
                    'operation_name': str,
                    'operation_definition': str
                }
                inserted = insert_codes(cursor, 'icd10_procedure_codes', procedure_codes, proc_fields)
                total_inserted += inserted
    
    # Show summary
    if not args.dry_run:
        cursor.execute("SELECT COUNT(*) as count FROM icd10_diagnosis_codes")
        diag_count = cursor.fetchone()['count']
        
        try:
            cursor.execute("SELECT COUNT(*) as count FROM icd10_procedure_codes")
            proc_count = cursor.fetchone()['count']
        except:
            proc_count = 0
        
        print("\n📊 Final Database Summary:")
        print(f"   ICD-10-CM Diagnosis Codes: {diag_count:,}")
        print(f"   ICD-10-PCS Procedure Codes: {proc_count:,}")
        print(f"   Total Medical Codes: {diag_count + proc_count:,}")
        
        cursor.close()
        conn.close()
    
    print(f"\n✅ Import completed! Processed {total_inserted} new codes.")

if __name__ == "__main__":
    main()