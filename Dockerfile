# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Force rebuild - version 2.7 - FIX PORT PARSING
# Set UTF-8 environment
ENV LANG=C.UTF-8 \
    LC_ALL=C.UTF-8 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /src

# Copy project file
COPY Tablitsya3/*.csproj ./Tablitsya3/

# Restore packages
RUN dotnet restore ./Tablitsya3/Tablitsya3.csproj

# Copy all source code
COPY Tablitsya3/. ./Tablitsya3/

# Build and publish
WORKDIR /src/Tablitsya3
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Set UTF-8 environment
ENV LANG=C.UTF-8 \
    LC_ALL=C.UTF-8

WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:${PORT:-5000}
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the app
ENTRYPOINT ["dotnet", "Tablitsya3.dll"]
