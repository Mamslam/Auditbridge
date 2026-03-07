FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AuditBridge.API/AuditBridge.API.csproj", "AuditBridge.API/"]
COPY ["AuditBridge.Application/AuditBridge.Application.csproj", "AuditBridge.Application/"]
COPY ["AuditBridge.Domain/AuditBridge.Domain.csproj", "AuditBridge.Domain/"]
COPY ["AuditBridge.Infrastructure/AuditBridge.Infrastructure.csproj", "AuditBridge.Infrastructure/"]
RUN dotnet restore "AuditBridge.API/AuditBridge.API.csproj"

COPY . .
WORKDIR "/src/AuditBridge.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuditBridge.API.dll"]
