FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Ozon.Route256.DataGenerator/Ozon.Route256.DataGenerator.csproj", "Ozon.Route256.DataGenerator/"]
COPY ["Ozon.Route256.DataGenerator.Infra/Ozon.Route256.DataGenerator.Infra.csproj", "Ozon.Route256.DataGenerator.Infra/"]
COPY ["Ozon.Route256.DataGenerator.Bll/Ozon.Route256.DataGenerator.Bll.csproj", "Ozon.Route256.DataGenerator.Bll/"]
COPY ["Ozon.Route256.DataGenerator.Proto/Ozon.Route256.DataGenerator.Proto.csproj", "Ozon.Route256.DataGenerator.Proto/"]
RUN dotnet restore "Ozon.Route256.DataGenerator/Ozon.Route256.DataGenerator.csproj"
COPY . .
WORKDIR "/src/Ozon.Route256.DataGenerator"
RUN dotnet build "Ozon.Route256.DataGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Ozon.Route256.DataGenerator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ozon.Route256.DataGenerator.dll"]
