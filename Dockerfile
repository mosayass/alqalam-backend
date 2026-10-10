# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy CPM and project files first to cache layer dependencies
COPY Directory.Build.props Directory.Packages.props AlQalam.slnx ./
COPY src/Host/AlQalam.Host.csproj src/Host/
COPY src/Shared/AlQalam.Shared.csproj src/Shared/
COPY src/Modules/Users/Core/AlQalam.Users.Core.csproj src/Modules/Users/Core/
COPY src/Modules/Users/Infrastructure/AlQalam.Users.Infrastructure.csproj src/Modules/Users/Infrastructure/
COPY src/Modules/Memorization/Core/AlQalam.Memorization.Core.csproj src/Modules/Memorization/Core/
COPY src/Modules/Memorization/Infrastructure/AlQalam.Memorization.Infrastructure.csproj src/Modules/Memorization/Infrastructure/
COPY src/Modules/Dataset/Core/AlQalam.Dataset.Core.csproj src/Modules/Dataset/Core/
COPY src/Modules/Dataset/Infrastructure/AlQalam.Dataset.Infrastructure.csproj src/Modules/Dataset/Infrastructure/

RUN dotnet restore AlQalam.slnx

# Copy the rest of the source tree
COPY src/ src/

# Publish Host in Release mode
WORKDIR /src/src/Host
RUN dotnet publish AlQalam.Host.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

USER $APP_UID

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AlQalam.Host.dll"]
