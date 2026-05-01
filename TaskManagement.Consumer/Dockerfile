FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore TaskManagement.Consumer/TaskManagement.Consumer.csproj
RUN dotnet publish TaskManagement.Consumer/TaskManagement.Consumer.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TaskManagement.Consumer.dll"]
