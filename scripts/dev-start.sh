#!/bin/bash

# Development environment startup script

set -e

PROJECT_ROOT="$(dirname "$0")/.."
cd "$PROJECT_ROOT"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

print_status() {
    echo -e "${GREEN}$1${NC}"
}

print_warning() {
    echo -e "${YELLOW}$1${NC}"
}

print_info() {
    echo -e "${BLUE}$1${NC}"
}

print_status "Claims Processing System - Development Startup"
print_status "=============================================="

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    print_warning "Docker is not running. Please start Docker first."
    exit 1
fi

# Generate SSL certificates if they don't exist
if [ ! -f "nginx/ssl/cert.pem" ] || [ ! -f "nginx/ssl/key.pem" ]; then
    print_info "Generating SSL certificates..."
    ./scripts/generate-ssl.sh
fi

# Create storage directory
mkdir -p storage

# Start services
print_info "Building and starting services..."
docker-compose up --build -d

# Wait for services to start
print_info "Waiting for services to initialize..."
sleep 15

# Check service health
print_info "Checking service health..."
./scripts/clean-containers.sh --health

print_status ""
print_status "Development environment started successfully!"
print_status "Frontend: https://localhost"
print_status "API Gateway: https://localhost/api/health" 
print_status "RabbitMQ Management: http://localhost:15672"
print_status ""
print_warning "Demo Credentials:"
print_warning "  john.doe@example.com / password"
print_warning "  jane.smith@example.com / password"
print_warning "  RabbitMQ: claims / claims123"
print_status ""
print_info "Useful commands:"
print_info "  ./scripts/clean-containers.sh --logs    # View logs"
print_info "  ./scripts/clean-containers.sh --status  # Check status"
print_info "  docker-compose down                      # Stop services"