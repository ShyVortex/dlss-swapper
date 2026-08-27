#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"

BUILD_DIR="${SCRIPT_DIR}/build_root"
DIST_DIR="${SCRIPT_DIR}/dist"
FLATPAK_BUILD_DIR="${SCRIPT_DIR}/flatpak_build"
REPO_DIR="${SCRIPT_DIR}/repo"
VERSION="${VERSION:-$(grep -oPm1 '(?<=<Version>)[^<]+' "${REPO_ROOT}/src/LinuxUI/LinuxUI.csproj" || echo "1.2.6.1")}"

echo "=== Building Flatpak package for DLSS Swapper v${VERSION} ==="

# Clean previous build artifacts
rm -rf "${BUILD_DIR}" "${DIST_DIR}" "${FLATPAK_BUILD_DIR}" "${REPO_DIR}"
mkdir -p "${BUILD_DIR}/usr/lib/dlss-swapper"
mkdir -p "${BUILD_DIR}/usr/bin"
mkdir -p "${BUILD_DIR}/usr/share/applications"
mkdir -p "${BUILD_DIR}/usr/share/icons/hicolor/256x256/apps"
mkdir -p "${BUILD_DIR}/usr/share/metainfo"
mkdir -p "${DIST_DIR}"

echo "1. Publishing self-contained Linux binary..."
dotnet publish "${REPO_ROOT}/src/LinuxUI/LinuxUI.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -p:PublishSingleFile=false \
    -o "${BUILD_DIR}/usr/lib/dlss-swapper"

echo "2. Installing launcher script..."
cat << 'EOF' > "${BUILD_DIR}/usr/bin/dlss-swapper-gui"
#!/usr/bin/env bash
exec "/app/lib/dlss-swapper/DLSS Swapper" "$@"
EOF
chmod +x "${BUILD_DIR}/usr/bin/dlss-swapper-gui"

echo "3. Copying desktop, icon, and metainfo assets..."
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.desktop" "${BUILD_DIR}/usr/share/applications/"
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.png" "${BUILD_DIR}/usr/share/icons/hicolor/256x256/apps/"
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.metainfo.xml" "${BUILD_DIR}/usr/share/metainfo/"

echo "4. Building Flatpak package..."
if command -v flatpak-builder >/dev/null 2>&1; then
    flatpak-builder --user --force-clean --disable-rofiles-fuse --install-deps-from=flathub --repo="${REPO_DIR}" "${FLATPAK_BUILD_DIR}" "${SCRIPT_DIR}/com.beeradmoore.dlss-swapper.yml" || \
    flatpak-builder --force-clean --disable-rofiles-fuse --install-deps-from=flathub --repo="${REPO_DIR}" "${FLATPAK_BUILD_DIR}" "${SCRIPT_DIR}/com.beeradmoore.dlss-swapper.yml" || \
    flatpak-builder --force-clean --disable-rofiles-fuse --repo="${REPO_DIR}" "${FLATPAK_BUILD_DIR}" "${SCRIPT_DIR}/com.beeradmoore.dlss-swapper.yml" || true
    if [ -d "${REPO_DIR}" ]; then
        flatpak build-bundle "${REPO_DIR}" "${DIST_DIR}/com.beeradmoore.dlss-swapper.flatpak" com.beeradmoore.dlss-swapper
        rm -rf "${FLATPAK_BUILD_DIR}" "${REPO_DIR}"
        echo "=== Flatpak bundle created successfully: ${DIST_DIR}/com.beeradmoore.dlss-swapper.flatpak ==="
    else
        echo "Note: flatpak-builder requires org.freedesktop.Sdk 24.08 runtime."
        echo "Flatpak manifest is staged at: ${SCRIPT_DIR}/com.beeradmoore.dlss-swapper.yml"
    fi
else
    echo "flatpak-builder not found on host system."
    echo "Flatpak manifest is staged at: ${SCRIPT_DIR}/com.beeradmoore.dlss-swapper.yml"
    echo "To build manually, run: flatpak-builder --force-clean --repo=repo build_dir package/linux/flatpak/com.beeradmoore.dlss-swapper.yml"
fi
