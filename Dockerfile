# ============================================================
# NexPOS Enterprise API - Dockerfile for Render.com Hosting
# .NET 10 SDK Preview + ASP.NET Core Runtime
# ============================================================

# ---------------- STAGE 1: BUILD ----------------
# Use Official Microsoft .NET 10 SDK Preview Image
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
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

# Publish API as Release (self-contained NOT needed - runtime in final image)
WORKDIR "/src/src/POS.API"
RUN dotnet build "POS.API.csproj" -c Release -o /app/build
RUN dotnet publish "POS.API.csproj" -c Release -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------------- STAGE 2: FINAL RUNTIME ----------------
# Use Official Microsoft ASP.NET Core 10 Preview Runtime Image (Smaller)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

# Render best practice: Listen on $PORT env var (Render injects this automatically)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_gcServer=1
ENV DOTNET_TieredCompilation=1

# Copy published output from build stage
COPY --from=build /app/publish .

# Expose port that Render expects (Render automatically routes 80/443 to this)
EXPOSE 8080

# Entrypoint: Run the API
ENTRYPOINT ["dotnet", "POS.API.dll"]
