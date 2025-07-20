#!/bin/bash

# Build Frontend - Healthcare Claims Portal
# Builds the Blazor Server frontend application

set -e

echo "🏗️  Building Healthcare Claims Portal Frontend..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
FRONTEND_DIR="./services/frontend-blazor"
IMAGE_NAME="healthcare-frontend"
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

# Check if frontend directory exists
if [ ! -d "$FRONTEND_DIR" ]; then
    print_error "Frontend directory not found: $FRONTEND_DIR"
    exit 1
fi

# Build the frontend image
print_status "Building Docker image: $IMAGE_NAME:$TAG"
cd "$FRONTEND_DIR"

if docker build -t "$IMAGE_NAME:$TAG" .; then
    print_success "Frontend image built successfully: $IMAGE_NAME:$TAG"
else
    print_error "Failed to build frontend image"
    exit 1
fi

# Go back to root directory
cd - >/dev/null

# Show image info
print_status "Image details:"
docker images "$IMAGE_NAME:$TAG" --format "table {{.Repository}}\t{{.Tag}}\t{{.Size}}\t{{.CreatedAt}}"

# Optional: Run tests if they exist
if [ -f "$FRONTEND_DIR/BlazorApp.Tests.csproj" ]; then
    print_status "Running tests..."
    cd "$FRONTEND_DIR"
    if dotnet test; then
        print_success "All tests passed"
    else
        print_warning "Some tests failed, but build completed"
    fi
    cd - >/dev/null
fi

print_success "Frontend build completed successfully!"
echo ""
echo "Next steps:"
echo "  - Run './deploy-frontend.sh' to deploy the application"
echo "  - Or run 'docker-compose up -d frontend' to start with compose"
echo "  - Use 'docker run -p 3000:3000 $IMAGE_NAME:$TAG' for standalone run"