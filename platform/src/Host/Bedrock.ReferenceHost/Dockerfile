# syntax=docker/dockerfile:1
# Build context = thư mục `platform/` (cần Directory.Build.props + Directory.Packages.props + mọi project ref).
# Multi-stage: SDK build/publish → runtime aspnet gọn, chạy non-root (hardening thương mại).

FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:ed034a8bf0b24ded0cbbac07e17825d8e9ebfe21e308191d0f7421eaf5ad4664 AS build
WORKDIR /src

# Copy toàn bộ source (đã lọc bin/obj qua .dockerignore). Copy trước file build chung để tận dụng layer cache.
COPY . .
RUN dotnet restore src/Host/Bedrock.ReferenceHost/Bedrock.ReferenceHost.csproj --locked-mode
RUN dotnet publish src/Host/Bedrock.ReferenceHost/Bedrock.ReferenceHost.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:1fa23fc4872d95fd71c2833ebe65d7e84a43b2d51a31d119516852f13d9505a7 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Chạy bằng user non-root có sẵn trong image .NET (APP_UID) — KHÔNG chạy root (security). Kestrel nghe cổng 8080.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Secret (Jwt, ConnectionStrings) nạp qua env/secret store lúc chạy (F35) — KHÔNG bake vào image.
# Migration áp OUT-OF-BAND (dotnet ef database update / bundle) ở bước deploy — app KHÔNG tự migrate (an toàn đa-instance).
ENTRYPOINT ["dotnet", "Bedrock.ReferenceHost.dll"]
