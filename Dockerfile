# ============================================================
# NexPOS Enterprise API - Dockerfile for Render.com Hosting
# .NET 10 Stable + ASP.NET Core Runtime
# ============================================================

# ---------------- STAGE 1: BUILD ----------------
# Use Official Microsoft .NET 10 SDK Stable Image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution/project files FIRST (Better Docker layer caching)
COPY ["src/POS.Domain/POS.Domain.csproj", "src/POS.Domain/"]
COPY ["src/POS.Application/POS.Application.csproj", "src/POS.Application/"]
COPY ["src/POS.Infrastructure/POS.Infrastructure.csproj", "src/POS.Infrastructure/"]
COPY ["src/POS.API/POS.API.csproj", "src/POS.API/"]

# Restore NuGet packages (only when csproj changes = Fast!)
RUN dotnet restore "src/POS.API/POS.API.csproj"

# Now copy ALL source code
COPY . .

# Publish API as Release
WORKDIR "/src/src/POS.API"
RUN dotnet build "POS.API.csproj" -c Release -o /app/build
RUN dotnet publish "POS.API.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------------- STAGE 2: FINAL RUNTIME ----------------
# Use Official Microsoft ASP.NET Core 10 Runtime Image (Smaller)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Render best practice: Listen on $PORT env var (Render injects this automatically)
# Also support direct PORT variable and fallback
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENV DOTNET_gcServer=1
ENV DOTNET_TieredCompilation=1

# Copy published output from build stage
COPY --from=build /app/publish .

# Expose port that Render expects
EXPOSE 8080
EXPOSE 80

# Entrypoint: Run the API - listen on Render PORT or default 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet POS.API.dll"]
