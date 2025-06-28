FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/SyntheticGrassClientFinder.Api/SyntheticGrassClientFinder.Api.csproj", "src/SyntheticGrassClientFinder.Api/"]
COPY ["src/SyntheticGrassClientFinder.Application/SyntheticGrassClientFinder.Application.csproj", "src/SyntheticGrassClientFinder.Application/"]
COPY ["src/SyntheticGrassClientFinder.Communication/SyntheticGrassClientFinder.Communication.csproj", "src/SyntheticGrassClientFinder.Communication/"]
COPY ["src/SyntheticGrassClientFinder.Domain/SyntheticGrassClientFinder.Domain.csproj", "src/SyntheticGrassClientFinder.Domain/"]
COPY ["src/SyntheticGrassClientFinder.Exception/SyntheticGrassClientFinder.Exception.csproj", "src/SyntheticGrassClientFinder.Exception/"]
COPY ["src/SyntheticGrassClientFinder.Infrastructure/SyntheticGrassClientFinder.Infrastructure.csproj", "src/SyntheticGrassClientFinder.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/SyntheticGrassClientFinder.Api/SyntheticGrassClientFinder.Api.csproj"

# Copy source code
COPY . .

# Build
WORKDIR "/src/src/SyntheticGrassClientFinder.Api"
RUN dotnet build "SyntheticGrassClientFinder.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SyntheticGrassClientFinder.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SyntheticGrassClientFinder.Api.dll"]
