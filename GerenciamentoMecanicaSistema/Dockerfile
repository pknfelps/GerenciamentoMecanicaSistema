# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["GerenciamentoMecanicaSistema/GerenciamentoMecanicaSistema.csproj", "GerenciamentoMecanicaSistema/"]
COPY ["DependencyInjection/DependencyInjection.csproj", "DependencyInjection/"]
COPY ["Domain.Interface/Domain.Interface.csproj", "Domain.Interface/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]
COPY ["Service.Interface/Service.Interface.csproj", "Service.Interface/"]
COPY ["Service/Service.csproj", "Service/"]

RUN dotnet restore "GerenciamentoMecanicaSistema/GerenciamentoMecanicaSistema.csproj"

COPY . .
RUN dotnet publish "GerenciamentoMecanicaSistema/GerenciamentoMecanicaSistema.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "GerenciamentoMecanicaSistema.dll"]
