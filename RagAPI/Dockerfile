# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY RagAPI.Core/RagAPI.Core.csproj RagAPI.Core/
COPY RagAPI/RagAPI.csproj RagAPI/
RUN dotnet restore RagAPI/RagAPI.csproj

# Copy everything else and build
COPY RagAPI.Core/ RagAPI.Core/
COPY RagAPI/ RagAPI/
RUN dotnet publish RagAPI/RagAPI.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RagAPI.dll"]