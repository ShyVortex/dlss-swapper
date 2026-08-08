%global __os_install_post %{nil}
%define _build_id_links none
%define _unpackaged_files_terminate_build 0
%define _check_files %{nil}

Name:           dlss-swapper
Version:        1.2.5
Release:        1%{?dist}
Summary:        Download, install, and swap DLSS, FSR, and XeSS versions in games
License:        GPLv3+
URL:            https://github.com/beeradmoore/dlss-swapper
AutoReqProv:    no

%description
DLSS Swapper is an open-source application that allows you to manage,
update, and swap NVIDIA DLSS, AMD FSR, and Intel XeSS libraries for your
installed games across Steam, Heroic Games Launcher, and custom game paths.

%install
mkdir -p %{buildroot}/usr/lib/dlss-swapper
mkdir -p %{buildroot}/usr/bin
mkdir -p %{buildroot}/usr/share/applications
mkdir -p %{buildroot}/usr/share/icons/hicolor/256x256/apps
mkdir -p %{buildroot}/usr/share/metainfo

cp -a %{_sourcedir}/build_root/usr/lib/dlss-swapper/* %{buildroot}/usr/lib/dlss-swapper/
cp -a %{_sourcedir}/build_root/usr/bin/dlss-swapper-gui %{buildroot}/usr/bin/dlss-swapper-gui
cp -a %{_sourcedir}/build_root/usr/share/applications/* %{buildroot}/usr/share/applications/
cp -a %{_sourcedir}/build_root/usr/share/icons/hicolor/256x256/apps/* %{buildroot}/usr/share/icons/hicolor/256x256/apps/
cp -a %{_sourcedir}/build_root/usr/share/metainfo/* %{buildroot}/usr/share/metainfo/

%files
%attr(755, root, root) /usr/bin/dlss-swapper-gui
/usr/lib/dlss-swapper
/usr/share/applications/com.beeradmoore.dlss-swapper.desktop
/usr/share/icons/hicolor/256x256/apps/com.beeradmoore.dlss-swapper.png
/usr/share/metainfo/com.beeradmoore.dlss-swapper.metainfo.xml

%changelog
* Sat Aug 08 2026 beeradmoore <https://github.com/beeradmoore/dlss-swapper> - 1.2.5-1
- Release version 1.2.5 with cross-platform Linux support.
