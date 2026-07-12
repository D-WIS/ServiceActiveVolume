FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DWIS.Service.ActiveVolume.CalibrationWebApp/DWIS.Service.ActiveVolume.CalibrationWebApp.csproj", "DWIS.Service.ActiveVolume.CalibrationWebApp/"]
COPY ["DWIS.Service.ActiveVolume.CalibrationWebPages/DWIS.Service.ActiveVolume.CalibrationWebPages.csproj", "DWIS.Service.ActiveVolume.CalibrationWebPages/"]
COPY ["DWIS.Service.ActiveVolume.Model/DWIS.Service.ActiveVolume.Model.csproj", "DWIS.Service.ActiveVolume.Model/"]
COPY ["DWIS.Service.ActiveVolume.ModelSharedOut/DWIS.Service.ActiveVolume.ModelSharedOut.csproj", "DWIS.Service.ActiveVolume.ModelSharedOut/"]
RUN dotnet restore "DWIS.Service.ActiveVolume.CalibrationWebApp/DWIS.Service.ActiveVolume.CalibrationWebApp.csproj"
COPY . .
WORKDIR "/src/DWIS.Service.ActiveVolume.CalibrationWebApp"
RUN dotnet build "DWIS.Service.ActiveVolume.CalibrationWebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DWIS.Service.ActiveVolume.CalibrationWebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DWIS.Service.ActiveVolume.CalibrationWebApp.dll"]
