FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/LeaveManagementSystem.Domain/LeaveManagementSystem.Domain.csproj", "src/LeaveManagementSystem.Domain/"]
COPY ["src/LeaveManagementSystem.Application/LeaveManagementSystem.Application.csproj", "src/LeaveManagementSystem.Application/"]
COPY ["src/LeaveManagementSystem.Infrastructure/LeaveManagementSystem.Infrastructure.csproj", "src/LeaveManagementSystem.Infrastructure/"]
COPY ["src/LeaveManagementSystem.WebAPI/LeaveManagementSystem.WebAPI.csproj", "src/LeaveManagementSystem.WebAPI/"]

RUN dotnet restore "src/LeaveManagementSystem.WebAPI/LeaveManagementSystem.WebAPI.csproj"

COPY . .

WORKDIR "/src/src/LeaveManagementSystem.WebAPI"

RUN dotnet publish "LeaveManagementSystem.WebAPI.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["dotnet", "LeaveManagementSystem.WebAPI.dll"]
