#!/bin/bash

# Deploy Frontend - Healthcare Claims Portal
# Deploys the Blazor Server frontend application

set -e

echo "🚀 Deploying Healthcare Claims Portal Frontend..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
IMAGE_NAME="healthcare-frontend"
CONTAINER_NAME="healthcare-app"
NETWORK_NAME="claims-network"
DATABASE_URL="Host=claims-db;Database=claims_db;Username=claims_user;Password=claims_password"
UPLOAD_VOLUME="/opt/uploads:/app/uploads"
PORT="3000:3000"
TAG="${1:-latest}"

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

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    print_error "Docker is not running. Please start Docker first."
    exit 1
fi

# Check if image exists
if ! docker images -q "$IMAGE_NAME:$TAG" >/dev/null; then
    print_warning "Frontend image not found. Building first..."
    if ./build-frontend.sh "$TAG"; then
        print_success "Image built successfully"
    else
        print_error "Failed to build image"
        exit 1
    fi
fi

# Create network if it doesn't exist
if ! docker network ls | grep -q "$NETWORK_NAME"; then
    print_status "Creating Docker network: $NETWORK_NAME"
    docker network create "$NETWORK_NAME"
fi

# Stop and remove existing container if it exists
if docker ps -a | grep -q "$CONTAINER_NAME"; then
    print_status "Stopping existing container: $CONTAINER_NAME"
    docker stop "$CONTAINER_NAME" >/dev/null 2>&1 || true
    
    print_status "Removing existing container: $CONTAINER_NAME"
    docker rm "$CONTAINER_NAME" >/dev/null 2>&1 || true
fi

# Create uploads directory if it doesn't exist
mkdir -p /opt/uploads

# Deploy the new container
print_status "Deploying new container: $CONTAINER_NAME"
docker run -d \
    --name "$CONTAINER_NAME" \
    --network "$NETWORK_NAME" \
    -e DATABASE_URL="$DATABASE_URL" \
    -v "$UPLOAD_VOLUME" \
    -p "$PORT" \
    "$IMAGE_NAME:$TAG"

# Wait a moment for container to start
sleep 3

# Check if container is running
if docker ps | grep -q "$CONTAINER_NAME"; then
    print_success "Frontend deployed successfully!"
    
    # Show container status
    print_status "Container status:"
    docker ps --filter "name=$CONTAINER_NAME" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    
    # Show logs
    print_status "Recent logs:"
    docker logs --tail 10 "$CONTAINER_NAME"
    
    echo ""
    print_success "🎉 Deployment completed!"
    echo ""
    echo "Access the application:"
    echo "  🌐 Frontend: http://localhost:3000"
    echo "  📊 Health: http://localhost:3000/health"
    echo ""
    echo "Demo accounts:"
    echo "  👤 Patient: patient@example.com / password"
    echo "  👨‍💼 Admin: admin@example.com / password"
    echo "  🔍 Adjuster: adjuster@example.com / password"
    echo "  🏥 Provider: provider@example.com / password"
    echo ""
    echo "Management commands:"
    echo "  📋 View logs: docker logs -f $CONTAINER_NAME"
    echo "  🔄 Restart: docker restart $CONTAINER_NAME"
    echo "  🛑 Stop: docker stop $CONTAINER_NAME"
    
else
    print_error "Container failed to start"
    print_status "Checking logs for errors:"
    docker logs "$CONTAINER_NAME"
    exit 1
fi