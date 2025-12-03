# ≈тап 1: «б≥рка
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

#  оп≥юЇмо проект
COPY таблиц€3/*.csproj ./таблиц€3/
RUN dotnet restore ./таблиц€3/таблиц€3.csproj

#  оп≥юЇмо весь код
COPY таблиц€3/. ./таблиц€3/

# ѕубл≥куЇмо проект
WORKDIR /app/таблиц€3
RUN dotnet publish -c Release -o out

# ≈тап 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

#  оп≥юЇмо з≥браний проект
COPY --from=build /app/таблиц€3/out .

# Ќалаштуванн€ порту (дл€ Render/Railway/Heroku)
ENV ASPNETCORE_URLS=http://+:${PORT:-5000}
ENV ASPNETCORE_ENVIRONMENT=Production

# «апуск
ENTRYPOINT ["dotnet", "таблиц€3.dll"]
