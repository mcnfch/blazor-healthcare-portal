#!/bin/bash

# Bash script for container cleanup and maintenance

set -e

PROJECT_ROOT="$(dirname "$0")/.."
cd "$PROJECT_ROOT"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}$1${NC}"
}

print_warning() {
    echo -e "${YELLOW}$1${NC}"
}

print_error() {
    echo -e "${RED}$1${NC}"
}

print_info() {
    echo -e "${BLUE}$1${NC}"
}

# Help function
show_help() {
    echo "Claims Processing System - Container Management"
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -c, --clean     Stop and remove all containers and volumes"
    echo "  -l, --logs      Show container logs"
    echo "  -s, --status    Show container status"
    echo "  -r, --restart   Restart all services"
    echo "  -h, --help      Show this help message"
    echo "  --health        Run health checks"
    echo "  --backup        Backup database"
    echo "  --prune         Remove unused Docker resources"
    echo ""
}

# Function to check container health
check_health() {
    print_info "Checking container health..."
    
    services=("postgres" "rabbitmq" "claims-processor" "doc-service" "api-gateway" "frontend" "nginx")
    
    for service in "${services[@]}"; do
        container_id=$(docker-compose ps -q "$service" 2>/dev/null)
        if [ -n "$container_id" ]; then
            status=$(docker inspect --format='{{.State.Status}}' "$container_id" 2>/dev/null)
            if [ "$status" = "running" ]; then
                print_status "✓ $service is running"
            else
                print_error "✗ $service is $status"
            fi
        else
            print_error "✗ $service container not found"
        fi
    done
}

# Function to show container status
show_status() {
    print_info "Container Status:"
    docker-compose ps
    
    echo ""
    print_info "Resource Usage:"
    docker stats --no-stream --format "table {{.Container}}\t{{.CPUPerc}}\t{{.MemUsage}}\t{{.NetIO}}\t{{.BlockIO}}"
}

# Function to backup database
backup_database() {
    print_info "Creating database backup..."
    
    timestamp=$(date +"%Y%m%d_%H%M%S")
    backup_file="backups/claims_db_backup_$timestamp.sql"
    
    mkdir -p backups
    
    docker-compose exec postgres pg_dump -U claims_user claims_db > "$backup_file"
    
    if [ $? -eq 0 ]; then
        print_status "Database backup created: $backup_file"
    else
        print_error "Database backup failed"
        return 1
    fi
}

# Function to clean up resources
cleanup_resources() {
    print_warning "Stopping and removing containers..."
    docker-compose down -v --remove-orphans
    
    print_warning "Removing unused Docker resources..."
    docker system prune -f
    
    print_status "Cleanup completed"
}

# Function to show logs
show_logs() {
    if [ $# -gt 0 ]; then
        print_info "Showing logs for: $1"
        docker-compose logs -f "$1"
    else
        print_info "Showing logs for all services:"
        docker-compose logs -f
    fi
}

# Function to restart services
restart_services() {
    print_info "Restarting services..."
    docker-compose restart
    print_status "Services restarted"
}

# Parse command line arguments
case "${1:-}" in
    -c|--clean)
        cleanup_resources
        ;;
    -l|--logs)
        show_logs "$2"
        ;;
    -s|--status)
        show_status
        ;;
    -r|--restart)
        restart_services
        ;;
    --health)
        check_health
        ;;
    --backup)
        backup_database
        ;;
    --prune)
        docker system prune -f
        print_status "Docker system pruned"
        ;;
    -h|--help)
        show_help
        ;;
    "")
        check_health
        ;;
    *)
        print_error "Unknown option: $1"
        show_help
        exit 1
        ;;
esac