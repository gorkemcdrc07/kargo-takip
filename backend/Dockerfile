# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY KargoTakip.sln ./
COPY backend/src ./backend/src

RUN dotnet restore ./backend/src/KargoTakip.Api/KargoTakip.Api.csproj
RUN dotnet publish ./backend/src/KargoTakip.Api/KargoTakip.Api.csproj -c Release -o /app/publish

# --- run ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "KargoTakip.Api.dll"]
