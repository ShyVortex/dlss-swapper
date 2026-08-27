#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"

BUILD_DIR="${SCRIPT_DIR}/build_root"
DIST_DIR="${SCRIPT_DIR}/dist"
VERSION="${VERSION:-$(grep -oPm1 '(?<=<Version>)[^<]+' "${REPO_ROOT}/src/LinuxUI/LinuxUI.csproj" || echo "1.2.6.1")}"

echo "=== Building Fedora / RPM package for DLSS Swapper v${VERSION} ==="

# Clean previous build artifacts
rm -rf "${BUILD_DIR}" "${DIST_DIR}"
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

echo "4. Building RPM package..."
if command -v rpmbuild >/dev/null 2>&1; then
    RPM_TOPDIR="${SCRIPT_DIR}/rpmbuild"
    rm -rf "${RPM_TOPDIR}"
    mkdir -p "${RPM_TOPDIR}"/{BUILD,RPMS,SOURCES,SPECS,SRPMS,rpmdb}
    cp "${SCRIPT_DIR}/dlss-swapper.spec" "${RPM_TOPDIR}/SPECS/"
    ln -snf "${BUILD_DIR}" "${RPM_TOPDIR}/SOURCES/build_root"

    rpmbuild -bb \
        --define "_topdir ${RPM_TOPDIR}" \
        --define "_dbpath ${RPM_TOPDIR}/rpmdb" \
        --define "__check_files %{nil}" \
        "${RPM_TOPDIR}/SPECS/dlss-swapper.spec"

    cp "${RPM_TOPDIR}"/RPMS/x86_64/*.rpm "${DIST_DIR}/"
    rm -rf "${RPM_TOPDIR}"
    echo "=== RPM package created successfully: ${DIST_DIR}/dlss-swapper-${VERSION}-1.x86_64.rpm ==="
else
    echo "rpmbuild not found on host system. Creating RPM payload tarball..."
    (cd "${SCRIPT_DIR}" && tar -czf "${DIST_DIR}/dlss-swapper-${VERSION}-rpm-payload.tar.gz" build_root dlss-swapper.spec)
    echo "=== RPM payload tarball created successfully: ${DIST_DIR}/dlss-swapper-${VERSION}-rpm-payload.tar.gz ==="
fi
