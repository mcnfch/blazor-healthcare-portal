# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Install dependencies for document processing
RUN apt-get update && apt-get install -y \
    libmagickwand-dev \
    ghostscript \
    libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Install runtime dependencies for document processing
RUN apt-get update && apt-get install -y \
    libmagickwand-6.q16-6 \
    ghostscript \
    libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

# Create a non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy the published app
COPY --from=build /app/out .

# Create directories and set permissions
RUN mkdir -p logs storage && chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose the gRPC port
EXPOSE 50052

ENTRYPOINT ["dotnet", "DocumentService.dll"]