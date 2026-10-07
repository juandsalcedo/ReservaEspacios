FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ReservaEspacios.Api/ReservaEspacios.Api.csproj", "ReservaEspacios.Api/"]
RUN dotnet restore "ReservaEspacios.Api/ReservaEspacios.Api.csproj"
COPY . .
WORKDIR "/src/ReservaEspacios.Api"
RUN dotnet build "ReservaEspacios.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ReservaEspacios.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ReservaEspacios.Api.dll"]