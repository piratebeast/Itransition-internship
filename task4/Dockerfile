FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["UserManagement/UserManagement.csproj", "UserManagement/"]
RUN dotnet restore "UserManagement/UserManagement.csproj"
COPY . .
RUN dotnet publish "UserManagement/UserManagement.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UserManagement.dll"]