FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

WORKDIR /app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src"
COPY ["Ozon.Route256.CustomerService.csproj", "Ozon.Route256.CustomerService/"]
RUN dotnet restore "/src/Ozon.Route256.CustomerService/Ozon.Route256.CustomerService.csproj"
WORKDIR "/src/Ozon.Route256.CustomerService"
COPY . .
RUN dotnet build "Ozon.Route256.CustomerService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Ozon.Route256.CustomerService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ozon.Route256.CustomerService.dll"]
