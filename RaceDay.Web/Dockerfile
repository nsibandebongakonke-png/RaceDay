# syntax=docker/dockerfile:1

# ---------- Stage 1: build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the project file first so the (slow) restore layer is cached until dependencies change.
COPY RaceDay.Web.csproj ./
RUN dotnet restore

# Now copy the rest of the source and publish a release build.
COPY . .
RUN dotnet publish RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Stage 2: runtime (small image, no SDK) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Where the MVC app finds the RaceDay API. Override at run time with -e ApiSettings__BaseUrl=...
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ApiSettings__BaseUrl=http://host.docker.internal:5100

EXPOSE 8080
COPY --from=build /app/publish .

# Run as the non-root user that ships with the .NET images.
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
