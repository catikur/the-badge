#!/bin/bash
# The Badge — Claude Code BULUT oturumu başlangıç hook'u.
#
# Amaç: zorunlu kapı (CLAUDE.md: `dotnet run --project shared/TheBadge.Sim.Checks -c Release`)
# her yeni bulut oturumunda koşabilsin. Bulut konteynerleri .NET'siz başlayabiliyor ve
# dot.net / builds.dotnet.microsoft.com ağ politikasında engelli (proxy 403) — SDK bu yüzden
# Ubuntu'nun kendi deposundan kurulur (dotnet-sdk-8.0). Çekirdekte NuGet paketi YOK
# (bağımlılıksız kural), yani SDK'nın kendisi tek bağımlılıktır.
#
# Yerel oturumlarda (ör. macOS) HİÇBİR ŞEY yapmaz: CLAUDE_CODE_REMOTE yalnız bulutta "true".
# İdempotent: SDK varsa kurulum atlanır.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

SUDO=""
if [ "$(id -u)" -ne 0 ]; then SUDO="sudo"; fi

# `dotnet` ikilisi var ama SDK yoksa (yalnız host/runtime) da kurulum gerekir — o yüzden
# komutun varlığına değil, 8.x SDK listesine bakılır. Here-string: pipefail + `grep -q` SIGPIPE
# yarışına düşmemek için boru kullanılmaz.
sdkler="$(command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null || true)"
if grep -q '^8\.' <<< "$sdkler"; then
  echo "[session-start] .NET 8 SDK zaten kurulu: $(dotnet --version)"
else
  echo "[session-start] .NET 8 SDK kuruluyor (Ubuntu deposu)..."
  # Bazı üçüncü taraf PPA'lar proxy'de 403 döner; apt bunları uyarı olarak geçer.
  $SUDO apt-get update -qq || true
  DEBIAN_FRONTEND=noninteractive $SUDO apt-get install -y -qq dotnet-sdk-8.0
  echo "[session-start] kuruldu: $(dotnet --version)"
fi

# Gürültüsüz, telemetrisiz CLI — oturum boyunca kalıcı.
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
    echo 'export DOTNET_NOLOGO=1'
    echo 'export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1'
  } >> "$CLAUDE_ENV_FILE"
fi

# Proje varlıklarını kur (paket indirmez; konteyner önbelleğine girer).
cd "${CLAUDE_PROJECT_DIR:-$(pwd)}"
dotnet restore shared/TheBadge.Sim.Checks/TheBadge.Sim.Checks.csproj -v quiet
dotnet restore server/TheBadge.SimWorker/TheBadge.SimWorker.csproj -v quiet
echo "[session-start] hazır — zorunlu kapı: dotnet run --project shared/TheBadge.Sim.Checks -c Release"
