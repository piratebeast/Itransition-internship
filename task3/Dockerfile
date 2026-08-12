FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Task3/Task3.csproj Task3/
RUN dotnet restore Task3/Task3.csproj
COPY . .
RUN dotnet publish Task3/Task3.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Task3.dll"]