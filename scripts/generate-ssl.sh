#!/bin/bash

# Generate self-signed SSL certificates for development
SSL_DIR="/opt/nginx/ssl"

echo "Generating self-signed SSL certificates for development..."

# Create SSL directory if it doesn't exist
mkdir -p "$SSL_DIR"

# Generate private key
openssl genrsa -out "$SSL_DIR/key.pem" 2048

# Generate certificate signing request
openssl req -new -key "$SSL_DIR/key.pem" -out "$SSL_DIR/cert.csr" -subj "/C=US/ST=Demo/L=Demo/O=Claims Processing/OU=Demo/CN=localhost"

# Generate self-signed certificate
openssl x509 -req -in "$SSL_DIR/cert.csr" -signkey "$SSL_DIR/key.pem" -out "$SSL_DIR/cert.pem" -days 365

# Set appropriate permissions
chmod 600 "$SSL_DIR/key.pem"
chmod 644 "$SSL_DIR/cert.pem"

# Clean up CSR file
rm "$SSL_DIR/cert.csr"

echo "SSL certificates generated successfully:"
echo "  Private key: $SSL_DIR/key.pem"
echo "  Certificate: $SSL_DIR/cert.pem"