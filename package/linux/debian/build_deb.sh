#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"

BUILD_DIR="${SCRIPT_DIR}/build_root"
DIST_DIR="${SCRIPT_DIR}/dist"
VERSION="1.2.5"
PKG_NAME="dlss-swapper_${VERSION}_amd64"

echo "=== Building Debian package for DLSS Swapper v${VERSION} ==="

# Clean previous build artifacts
rm -rf "${BUILD_DIR}" "${DIST_DIR}"
mkdir -p "${BUILD_DIR}/usr/lib/dlss-swapper"
mkdir -p "${BUILD_DIR}/usr/bin"
mkdir -p "${BUILD_DIR}/usr/share/applications"
mkdir -p "${BUILD_DIR}/usr/share/icons/hicolor/256x256/apps"
mkdir -p "${BUILD_DIR}/usr/share/metainfo"
mkdir -p "${BUILD_DIR}/DEBIAN"
mkdir -p "${DIST_DIR}"

echo "1. Publishing self-contained Linux binary..."
dotnet publish "${REPO_ROOT}/src/LinuxUI/LinuxUI.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained \
    -p:PublishSingleFile=false \
    -o "${BUILD_DIR}/usr/lib/dlss-swapper"

echo "2. Installing launcher script & symlink..."
cat << 'EOF' > "${BUILD_DIR}/usr/bin/dlss-swapper-gui"
#!/usr/bin/env bash
exec "/usr/lib/dlss-swapper/DLSS Swapper" "$@"
EOF
chmod +x "${BUILD_DIR}/usr/bin/dlss-swapper-gui"

echo "3. Copying desktop, icon, and metainfo assets..."
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.desktop" "${BUILD_DIR}/usr/share/applications/"
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.png" "${BUILD_DIR}/usr/share/icons/hicolor/256x256/apps/"
cp "${REPO_ROOT}/package/linux/assets/com.beeradmoore.dlss-swapper.metainfo.xml" "${BUILD_DIR}/usr/share/metainfo/"

echo "4. Creating DEBIAN/control file..."
cat << EOF > "${BUILD_DIR}/DEBIAN/control"
Package: dlss-swapper
Version: ${VERSION}
Architecture: amd64
Maintainer: beeradmoore <https://github.com/beeradmoore/dlss-swapper>
Section: utils
Priority: optional
Depends: libc6, libgcc-s1, libstdc++6
Description: Download, install, and swap DLSS, FSR, and XeSS versions in games
 DLSS Swapper is an open-source application that allows you to manage,
 update, and swap NVIDIA DLSS, AMD FSR, and Intel XeSS libraries for your
 installed games across Steam, Heroic Games Launcher, and custom game paths.
EOF

echo "5. Building Debian package..."
if command -v dpkg-deb >/dev/null 2>&1; then
    dpkg-deb --build "${BUILD_DIR}" "${DIST_DIR}/${PKG_NAME}.deb"
else
    echo "dpkg-deb not found. Packaging .deb via standard ar + tar..."
    TMP_DEB="${SCRIPT_DIR}/tmp_deb"
    rm -rf "${TMP_DEB}"
    mkdir -p "${TMP_DEB}"
    echo "2.0" > "${TMP_DEB}/debian-binary"

    (cd "${BUILD_DIR}/DEBIAN" && tar --owner=0 --group=0 -czf "${TMP_DEB}/control.tar.gz" .)
    (cd "${BUILD_DIR}" && tar --exclude='DEBIAN' --owner=0 --group=0 -czf "${TMP_DEB}/data.tar.gz" .)

    (cd "${TMP_DEB}" && ar rcs "${DIST_DIR}/${PKG_NAME}.deb" debian-binary control.tar.gz data.tar.gz)
    rm -rf "${TMP_DEB}"
fi

echo "=== Debian package created successfully: ${DIST_DIR}/${PKG_NAME}.deb ==="
