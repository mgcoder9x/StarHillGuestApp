# syntax=docker/dockerfile:1
# D1-a (QR-AD): base chuẩn sống ở platform/, sản phẩm QR ở starhill/. StarHill.Api ProjectReference cross-tree vào
# platform/src qua $(PlatformSrc). Vì vậy BUILD CONTEXT = REPO ROOT (docker-compose đặt context: ..) để chứa CẢ
# hai cây; layout platform/ + starhill/ được giữ nguyên trong /src nên $(PlatformSrc)=/src/platform/src phân giải
# đúng. Multi-stage: SDK build/publish → runtime aspnet gọn, chạy non-root (hardening thương mại).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Chỉ copy hai cây cần thiết (đã lọc bin/obj qua .dockerignore ở repo root) — giữ layout tương đối platform//starhill/.
COPY platform/ platform/
COPY starhill/ starhill/
RUN dotnet restore starhill/src/Host/StarHill.Api/StarHill.Api.csproj
RUN dotnet publish starhill/src/Host/StarHill.Api/StarHill.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Chạy bằng user non-root có sẵn trong image .NET (APP_UID) — KHÔNG chạy root (security). Kestrel nghe cổng 8080.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Secret (Jwt, ConnectionStrings) nạp qua env/secret store lúc chạy (F35) — KHÔNG bake vào image.
# Migration: compose dev bật ApplyMigrationsOnStartup; production áp OUT-OF-BAND (an toàn đa-instance, AD-050).
ENTRYPOINT ["dotnet", "StarHill.Api.dll"]
