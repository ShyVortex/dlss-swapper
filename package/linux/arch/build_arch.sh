#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"

BUILD_DIR="${SCRIPT_DIR}/build_root"
DIST_DIR="${SCRIPT_DIR}/dist"
VERSION="${VERSION:-$(grep -oPm1 '(?<=<Version>)[^<]+' "${REPO_ROOT}/src/LinuxUI/LinuxUI.csproj" || echo "1.2.6.1")}"

echo "=== Building Arch Linux package (.pkg.tar.zst) for DLSS Swapper v${VERSION} ==="

# Clean previous build artifacts
rm -rf "${BUILD_DIR}" "${DIST_DIR}" "${SCRIPT_DIR}/pkg" "${SCRIPT_DIR}/src"
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
exec "/usr/lib/dlss-swapper/DLSS Swapper" "$@"
EOF
chmod +x "${BUILD_DIR}/usr/bin/dlss-swapper-gui"

echo "3. Copying desktop, icon, and metainfo assets..."
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.desktop" "${BUILD_DIR}/usr/share/applications/"
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.png" "${BUILD_DIR}/usr/share/icons/hicolor/256x256/apps/"
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.metainfo.xml" "${BUILD_DIR}/usr/share/metainfo/"

echo "4. Building Arch Linux package (.pkg.tar.zst)..."
if command -v makepkg >/dev/null 2>&1; then
    mkdir -p "${SCRIPT_DIR}/src"
    ln -snf "${BUILD_DIR}" "${SCRIPT_DIR}/src/build_root"

    (cd "${SCRIPT_DIR}" && makepkg -f --nodeps)

    mv "${SCRIPT_DIR}"/*.pkg.tar.zst "${DIST_DIR}/"
    rm -rf "${SCRIPT_DIR}/pkg" "${SCRIPT_DIR}/src"
else
    echo "makepkg not found (e.g. on Ubuntu runner). Generating Arch package via .PKGINFO + tar.zst..."
    PKG_STAGING="${SCRIPT_DIR}/pkg_staging"
    rm -rf "${PKG_STAGING}"
    mkdir -p "${PKG_STAGING}"
    cp -a "${BUILD_DIR}/"* "${PKG_STAGING}/"

    BUILDDATE=$(date +%s)
    cat << EOF > "${PKG_STAGING}/.PKGINFO"
pkgname = dlss-swapper
pkgver = ${VERSION}-1
pkgdesc = Download, install, and swap DLSS, FSR, and XeSS versions in games
url = https://github.com/beeradmoore/dlss-swapper
builddate = ${BUILDDATE}
packager = beeradmoore <https://github.com/beeradmoore/dlss-swapper>
size = $(du -sb "${PKG_STAGING}" | cut -f1)
arch = x86_64
license = GPL-3.0-or-later
depend = icu
depend = openssl
depend = zlib
depend = glibc
EOF

    PKG_OUT="${DIST_DIR}/dlss-swapper-${VERSION}-1-x86_64.pkg.tar.zst"
    (cd "${PKG_STAGING}" && tar -c --owner=0 --group=0 * .PKGINFO | zstd -T0 -19 -o "${PKG_OUT}")
    rm -rf "${PKG_STAGING}"
fi

echo "=== Arch Linux package created successfully: ${DIST_DIR}/dlss-swapper-${VERSION}-1-x86_64.pkg.tar.zst ==="
