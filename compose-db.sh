#!/bin/bash

# Compose Database - Healthcare Claims Portal
# Manages PostgreSQL database using Docker Compose

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to show usage
show_usage() {
    echo "🗄️  Healthcare Claims Portal - Database Management"
    echo ""
    echo "Usage: $0 [COMMAND]"
    echo ""
    echo "Commands:"
    echo "  start     Start PostgreSQL database"
    echo "  stop      Stop PostgreSQL database"
    echo "  restart   Restart PostgreSQL database"
    echo "  status    Show database status"
    echo "  logs      Show database logs"
    echo "  connect   Connect to database CLI"
    echo "  backup    Create database backup"
    echo "  restore   Restore database from backup"
    echo "  reset     Reset database (WARNING: destroys all data)"
    echo "  import    Import medical codes"
    echo "  health    Check database health"
    echo ""
    echo "Examples:"
    echo "  $0 start               # Start the database"
    echo "  $0 logs -f             # Follow database logs"
    echo "  $0 connect             # Connect to psql"
    echo "  $0 backup mybackup     # Create backup named 'mybackup'"
}

# Configuration
DB_SERVICE="postgres"
DB_CONTAINER="claims-db"
DB_NAME="claims_db"
DB_USER="claims_user"
DB_PASSWORD="claims_password"
BACKUP_DIR="./backups"

# Check if Docker and Docker Compose are available
check_dependencies() {
    if ! command -v docker >/dev/null 2>&1; then
        print_error "Docker is not installed or not in PATH"
        exit 1
    fi

    if ! command -v docker-compose >/dev/null 2>&1; then
        print_error "Docker Compose is not installed or not in PATH"
        exit 1
    fi

    if [ ! -f "docker-compose.yml" ]; then
        print_error "docker-compose.yml not found in current directory"
        exit 1
    fi
}

# Start database
start_db() {
    print_status "Starting PostgreSQL database..."
    docker-compose up -d "$DB_SERVICE"
    
    print_status "Waiting for database to be ready..."
    sleep 5
    
    if docker-compose ps "$DB_SERVICE" | grep -q "Up"; then
        print_success "Database started successfully"
        show_connection_info
    else
        print_error "Failed to start database"
        docker-compose logs "$DB_SERVICE"
        exit 1
    fi
}

# Stop database
stop_db() {
    print_status "Stopping PostgreSQL database..."
    docker-compose stop "$DB_SERVICE"
    print_success "Database stopped"
}

# Restart database
restart_db() {
    print_status "Restarting PostgreSQL database..."
    docker-compose restart "$DB_SERVICE"
    sleep 3
    print_success "Database restarted"
    show_connection_info
}

# Show status
show_status() {
    print_status "Database status:"
    docker-compose ps "$DB_SERVICE"
    
    if docker-compose ps "$DB_SERVICE" | grep -q "Up"; then
        echo ""
        show_connection_info
    fi
}

# Show logs
show_logs() {
    shift  # Remove 'logs' from arguments
    docker-compose logs "$@" "$DB_SERVICE"
}

# Connect to database
connect_db() {
    print_status "Connecting to database..."
    docker-compose exec "$DB_SERVICE" psql -U "$DB_USER" -d "$DB_NAME"
}

# Create backup
backup_db() {
    local backup_name="${2:-backup_$(date +%Y%m%d_%H%M%S)}"
    mkdir -p "$BACKUP_DIR"
    
    print_status "Creating database backup: $backup_name"
    docker-compose exec -T "$DB_SERVICE" pg_dump -U "$DB_USER" -d "$DB_NAME" > "$BACKUP_DIR/$backup_name.sql"
    
    if [ $? -eq 0 ]; then
        print_success "Backup created: $BACKUP_DIR/$backup_name.sql"
    else
        print_error "Failed to create backup"
        exit 1
    fi
}

# Restore backup
restore_db() {
    local backup_file="$2"
    
    if [ -z "$backup_file" ]; then
        print_error "Please specify backup file to restore"
        echo "Usage: $0 restore <backup_file>"
        echo "Available backups:"
        ls -la "$BACKUP_DIR"/*.sql 2>/dev/null || echo "No backups found"
        exit 1
    fi
    
    if [ ! -f "$backup_file" ]; then
        # Try looking in backup directory
        if [ -f "$BACKUP_DIR/$backup_file" ]; then
            backup_file="$BACKUP_DIR/$backup_file"
        else
            print_error "Backup file not found: $backup_file"
            exit 1
        fi
    fi
    
    print_warning "This will replace all existing data. Are you sure? (y/N)"
    read -r confirmation
    if [[ $confirmation =~ ^[Yy]$ ]]; then
        print_status "Restoring database from: $backup_file"
        docker-compose exec -T "$DB_SERVICE" psql -U "$DB_USER" -d "$DB_NAME" < "$backup_file"
        print_success "Database restored successfully"
    else
        print_status "Restore cancelled"
    fi
}

# Reset database
reset_db() {
    print_warning "This will destroy ALL database data. Are you sure? (y/N)"
    read -r confirmation
    if [[ $confirmation =~ ^[Yy]$ ]]; then
        print_status "Resetting database..."
        docker-compose down "$DB_SERVICE"
        docker volume rm "$(basename $(pwd))_postgres_data" 2>/dev/null || true
        docker-compose up -d "$DB_SERVICE"
        sleep 5
        print_success "Database reset completed"
    else
        print_status "Reset cancelled"
    fi
}

# Import medical codes
import_codes() {
    print_status "Importing medical codes..."
    
    if [ -f "import_basic_codes.py" ]; then
        python3 import_basic_codes.py
        print_success "Basic medical codes imported"
    else
        print_warning "import_basic_codes.py not found, skipping"
    fi
}

# Check database health
check_health() {
    print_status "Checking database health..."
    
    if docker-compose exec "$DB_SERVICE" pg_isready -U "$DB_USER" -d "$DB_NAME" >/dev/null 2>&1; then
        print_success "Database is healthy and accepting connections"
        
        # Get database info
        echo ""
        print_status "Database information:"
        docker-compose exec "$DB_SERVICE" psql -U "$DB_USER" -d "$DB_NAME" -c "
            SELECT 
                current_database() as database,
                current_user as user,
                version() as version;
        " -t
        
        # Get table count
        table_count=$(docker-compose exec "$DB_SERVICE" psql -U "$DB_USER" -d "$DB_NAME" -t -c "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public';" | xargs)
        echo "Tables: $table_count"
        
    else
        print_error "Database is not healthy"
        docker-compose logs --tail 10 "$DB_SERVICE"
        exit 1
    fi
}

# Show connection information
show_connection_info() {
    echo ""
    print_status "Database connection information:"
    echo "  🔗 Host: localhost"
    echo "  🔌 Port: 5432"
    echo "  🗄️ Database: $DB_NAME"
    echo "  👤 Username: $DB_USER"
    echo "  🔑 Password: $DB_PASSWORD"
    echo ""
    echo "Connection string:"
    echo "  Host=localhost;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"
}

# Main script logic
check_dependencies

case "${1:-start}" in
    start)
        start_db
        ;;
    stop)
        stop_db
        ;;
    restart)
        restart_db
        ;;
    status)
        show_status
        ;;
    logs)
        show_logs "$@"
        ;;
    connect)
        connect_db
        ;;
    backup)
        backup_db "$@"
        ;;
    restore)
        restore_db "$@"
        ;;
    reset)
        reset_db
        ;;
    import)
        import_codes
        ;;
    health)
        check_health
        ;;
    help|--help|-h)
        show_usage
        ;;
    *)
        print_error "Unknown command: $1"
        echo ""
        show_usage
        exit 1
        ;;
esac