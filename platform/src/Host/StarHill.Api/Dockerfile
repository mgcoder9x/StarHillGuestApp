# syntax=docker/dockerfile:1
# Build context = thư mục `platform/` (cần Directory.Build.props + Directory.Packages.props + mọi project ref).
# Multi-stage: SDK build/publish → runtime aspnet gọn, chạy non-root (hardening thương mại).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy toàn bộ source (đã lọc bin/obj qua .dockerignore). Copy trước file build chung để tận dụng layer cache.
COPY . .
RUN dotnet restore src/Host/StarHill.Api/StarHill.Api.csproj
RUN dotnet publish src/Host/StarHill.Api/StarHill.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Chạy bằng user non-root có sẵn trong image .NET (APP_UID) — KHÔNG chạy root (security). Kestrel nghe cổng 8080.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Secret (Jwt, ConnectionStrings) nạp qua env/secret store lúc chạy (F35) — KHÔNG bake vào image.
# Migration áp OUT-OF-BAND (dotnet ef database update / bundle) ở bước deploy — app KHÔNG tự migrate (an toàn đa-instance).
ENTRYPOINT ["dotnet", "StarHill.Api.dll"]
