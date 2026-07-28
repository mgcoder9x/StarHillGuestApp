# syntax=docker/dockerfile:1
# D1-a (QR-AD): base chuẩn sống ở platform/, sản phẩm QR ở starhill/. StarHill.Api ProjectReference cross-tree vào
# platform/src qua $(PlatformSrc). Vì vậy BUILD CONTEXT = REPO ROOT (docker-compose đặt context: ..) để chứa CẢ
# hai cây; layout platform/ + starhill/ được giữ nguyên trong /src nên $(PlatformSrc)=/src/platform/src phân giải
# đúng. Multi-stage: SDK build/publish → runtime aspnet gọn, chạy non-root (hardening thương mại).

# Ghim digest @sha256: cả hai base (R3.5, build tái lập): giữ tag `:10.0` cạnh digest để người đọc biết version,
# digest để khoá đúng bit-for-bit. Đổi digest = có chủ đích, tra lại bằng `docker buildx imagetools inspect`.
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:ed034a8bf0b24ded0cbbac07e17825d8e9ebfe21e308191d0f7421eaf5ad4664 AS build
WORKDIR /src

# Chỉ copy hai cây cần thiết (đã lọc bin/obj qua .dockerignore ở repo root) — giữ layout tương đối platform//starhill/.
COPY platform/ platform/
COPY starhill/ starhill/
RUN dotnet restore starhill/src/Host/StarHill.Api/StarHill.Api.csproj
RUN dotnet publish starhill/src/Host/StarHill.Api/StarHill.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:1fa23fc4872d95fd71c2833ebe65d7e84a43b2d51a31d119516852f13d9505a7 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Chạy bằng user non-root có sẵn trong image .NET (APP_UID) — KHÔNG chạy root (security). Kestrel nghe cổng 8080.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# HEALTHCHECK cấp container (R1.1–1.4): probe chạy bằng chính DLL app qua nhánh `--healthcheck` (task 1.2) —
# KHÔNG cần curl/wget nên KHÔNG cài package OS (R1.5). retries=3 × interval=10s ⇒ unhealthy ≤ ~30s < 40s (R1.4).
HEALTHCHECK --interval=10s --timeout=3s --retries=3 --start-period=20s \
  CMD ["dotnet", "StarHill.Api.dll", "--healthcheck"]

# Secret (Jwt, ConnectionStrings) nạp qua env/secret store lúc chạy (F35) — KHÔNG bake vào image.
# Migration: compose dev bật ApplyMigrationsOnStartup; production áp OUT-OF-BAND (an toàn đa-instance, AD-050).
ENTRYPOINT ["dotnet", "StarHill.Api.dll"]
