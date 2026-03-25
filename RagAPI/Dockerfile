# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RagAPI.Core/RagAPI.Core.csproj RagAPI.Core/
COPY RagAPI/RagAPI.csproj RagAPI/
RUN dotnet restore RagAPI/RagAPI.csproj

COPY RagAPI.Core/ RagAPI.Core/
COPY RagAPI/ RagAPI/
RUN dotnet publish RagAPI/RagAPI.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y \
    tesseract-ocr \
    tesseract-ocr-eng \
    tesseract-ocr-dan \
    libtesseract-dev \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Copy Linux libraries into x64 folder where Tesseract NuGet expects them
RUN LEPT_SO=$(find /usr/lib -name "liblept.so.*" -not -name "*.a" | grep -v "liblept.so$" | head -1) \
    && cp $LEPT_SO /app/x64/libleptonica-1.82.0.so \
    && TESS_SO=$(find /usr/lib -name "libtesseract.so.*" -not -name "*.a" | grep -v "libtesseract.so$" | head -1) \
    && cp $TESS_SO /app/x64/libtesseract50.so \
    && rm -f /app/x64/leptonica-1.80.0.dll \
    && rm -f /app/x64/tesseract41.dll \
    && ln -sf /lib/x86_64-linux-gnu/libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so

ENTRYPOINT ["dotnet", "RagAPI.dll"]