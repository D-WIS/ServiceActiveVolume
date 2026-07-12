FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DWIS.Service.ActiveVolume.CalibrationService/DWIS.Service.ActiveVolume.CalibrationService.csproj", "DWIS.Service.ActiveVolume.CalibrationService/"]
COPY ["DWIS.Service.ActiveVolume.Model/DWIS.Service.ActiveVolume.Model.csproj", "DWIS.Service.ActiveVolume.Model/"]
RUN dotnet restore "DWIS.Service.ActiveVolume.CalibrationService/DWIS.Service.ActiveVolume.CalibrationService.csproj"
COPY . .
WORKDIR "/src/DWIS.Service.ActiveVolume.CalibrationService"
RUN dotnet build "DWIS.Service.ActiveVolume.CalibrationService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DWIS.Service.ActiveVolume.CalibrationService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
VOLUME /home
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DWIS.Service.ActiveVolume.CalibrationService.dll"]
