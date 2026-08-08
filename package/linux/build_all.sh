#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "======================================================="
echo " Building DLSS Swapper Release Packages for All Distros"
echo "======================================================="

echo ""
echo ">>> 1. Building Debian / Ubuntu (.deb) Package..."
"${SCRIPT_DIR}/debian/build_deb.sh"

echo ""
echo ">>> 2. Building Fedora / RHEL (.rpm) Package..."
"${SCRIPT_DIR}/rpm/build_rpm.sh"

echo ""
echo ">>> 3. Building Arch Linux (.pkg.tar.zst) Package..."
"${SCRIPT_DIR}/arch/build_arch.sh"

echo ""
echo ">>> 4. Building Portable AppImage (.AppImage)..."
"${SCRIPT_DIR}/appimage/build_appimage.sh"

echo ""
echo ">>> 5. Building Flatpak Package (.flatpak)..."
"${SCRIPT_DIR}/flatpak/build_flatpak.sh"

echo ""
echo "======================================================="
echo " All Linux Packages Built Successfully!"
echo "======================================================="
