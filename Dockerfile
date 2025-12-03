# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file if exists
COPY *.sln* ./ 2>/dev/null || true

# Copy project file using wildcard to avoid UTF-8 issues
COPY Tablitsya3/*.csproj ./Tablitsya3/

# Restore
RUN dotnet restore ./Tablitsya3/Tablitsya3.csproj

# Copy everything else
COPY Tablitsya3/. ./Tablitsya3/

# Publish - dotnet will find the project automatically
WORKDIR /src/Tablitsya3
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:${PORT:-5000}
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the app
ENTRYPOINT ["dotnet", "Tablitsya3.dll"]
