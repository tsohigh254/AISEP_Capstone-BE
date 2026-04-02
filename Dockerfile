# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first for layer caching
COPY src/AISEP.Domain/AISEP.Domain.csproj src/AISEP.Domain/
COPY src/AISEP.Application/AISEP.Application.csproj src/AISEP.Application/
COPY src/AISEP.Infrastructure/AISEP.Infrastructure.csproj src/AISEP.Infrastructure/
COPY src/AISEP.WebAPI/AISEP.WebAPI.csproj src/AISEP.WebAPI/
COPY AISEP.sln .

RUN dotnet restore AISEP.sln

# Copy everything else and publish
COPY . .
RUN dotnet publish src/AISEP.WebAPI/AISEP.WebAPI.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create uploads & logs directories
RUN mkdir -p /app/uploads /app/logs

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

EXPOSE 5000

ENTRYPOINT ["dotnet", "AISEP.WebAPI.dll"]
