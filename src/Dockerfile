# FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# WORKDIR /src

# # Copia todos os csproj
# COPY ["AutoReparaAPI/AutoReparaAPI.csproj", "AutoReparaAPI/"]
# COPY ["Application/Application.csproj", "Application/"]
# COPY ["Domain/Domain.csproj", "Domain/"]
# COPY ["Infra/Infra.csproj", "Infra/"]
# COPY ["IOC/IOC.csproj", "IOC/"]

# # Copia também a solução
# COPY ["GestaoAutoRepara.slnx", "./"]

# # Restaura usando a solução
# RUN dotnet restore "AutoReparaAPI/AutoReparaAPI.csproj"

# # Copia o restante do código
# COPY . .
# WORKDIR /src/AutoReparaAPI
# RUN dotnet publish "AutoReparaAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# WORKDIR /app
# ENV ASPNETCORE_URLS=http://+:80
# COPY --from=build /app/publish .
# EXPOSE 80
# ENTRYPOINT ["dotnet", "AutoReparaAPI.dll"]


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia csproj e restaura
COPY ["OrcamentoService/OrcamentoService.csproj", "OrcamentoService/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infra/Infra.csproj", "Infra/"]
COPY ["IOC/IOC.csproj", "IOC/"]

RUN dotnet restore "OrcamentoService/OrcamentoService.csproj"

# Copia o restante e publica para linux-x64
COPY . .
WORKDIR /src/OrcamentoService
RUN dotnet publish "OrcamentoService.csproj" -c Release -r linux-x64 -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "OrcamentoService.dll"]
