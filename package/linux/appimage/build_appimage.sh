#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"

APPDIR="${SCRIPT_DIR}/AppDir"
DIST_DIR="${SCRIPT_DIR}/dist"
VERSION="1.2.6"

echo "=== Building AppImage for DLSS Swapper v${VERSION} ==="

# Clean previous build artifacts
rm -rf "${APPDIR}" "${DIST_DIR}"
mkdir -p "${APPDIR}/usr/lib/dlss-swapper"
mkdir -p "${APPDIR}/usr/share/metainfo"
mkdir -p "${DIST_DIR}"

echo "1. Publishing self-contained Linux binary..."
dotnet publish "${REPO_ROOT}/src/LinuxUI/LinuxUI.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -p:PublishSingleFile=false \
    -o "${APPDIR}/usr/lib/dlss-swapper"

echo "2. Creating AppRun entrypoint script..."
cat << 'EOF' > "${APPDIR}/AppRun"
#!/usr/bin/env bash
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export PATH="${HERE}/usr/bin:${PATH}"
export LD_LIBRARY_PATH="${HERE}/usr/lib/dlss-swapper:${LD_LIBRARY_PATH}"
exec "${HERE}/usr/lib/dlss-swapper/DLSS Swapper" "$@"
EOF
chmod +x "${APPDIR}/AppRun"

echo "3. Copying desktop, icon, and metainfo assets..."
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.desktop" "${APPDIR}/com.beeradmoore.dlss-swapper.desktop"
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.png" "${APPDIR}/com.beeradmoore.dlss-swapper.png"
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.png" "${APPDIR}/.DirIcon"
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.metainfo.xml" "${APPDIR}/usr/share/metainfo/"

echo "4. Obtaining appimagetool..."
APPIMAGETOOL="${SCRIPT_DIR}/appimagetool"
if command -v appimagetool >/dev/null 2>&1; then
    APPIMAGETOOL="appimagetool"
elif [ ! -f "${APPIMAGETOOL}" ]; then
    echo "Downloading appimagetool binary..."
    curl -sSL -o "${APPIMAGETOOL}" "https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage" || \
    curl -sSL -o "${APPIMAGETOOL}" "https://github.com/AppImage/AppImageKit/releases/download/13/appimagetool-x86_64.AppImage"
    chmod +x "${APPIMAGETOOL}"
fi

echo "5. Generating AppImage..."
ARCH=x86_64 "${APPIMAGETOOL}" "${APPDIR}" "${DIST_DIR}/DLSS_Swapper-${VERSION}-x86_64.AppImage"

echo "=== AppImage created successfully: ${DIST_DIR}/DLSS_Swapper-${VERSION}-x86_64.AppImage ==="
