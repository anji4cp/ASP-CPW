#!/usr/bin/env bash
set -euo pipefail

ASSUME_YES=false
if [[ ${1:-} == "--yes" ]]; then
  ASSUME_YES=true
  shift
fi

if [[ ${EUID} -ne 0 ]]; then
  echo "Run this installer with sudo." >&2
  exit 1
fi
if [[ ! -f /etc/os-release ]]; then
  echo "Unsupported operating system." >&2
  exit 1
fi
. /etc/os-release
if [[ ${ID:-} != "ubuntu" ]]; then
  echo "This package is tested on Ubuntu Server only." >&2
  exit 1
fi

PACKAGE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CPW_SHA="30cf6ad8986cd308b4458b2ceaf8ba9a6ea86a7ea256114394aa375bd03abad6"
WEB_USER="${ASP_CPW_WEB_USER:-pwweb}"

echo "ASP CPW Manager installer"
echo "Ubuntu: ${PRETTY_NAME}"
if [[ ${ASSUME_YES} != true ]]; then
  read -r -p "Continue installation? [Y/n] " answer
  if [[ ${answer,,} == "n" ]]; then exit 0; fi
fi

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends mariadb-server mariadb-client python3 openssl ca-certificates curl

echo "${CPW_SHA}  ${PACKAGE_DIR}/bin/linux-x64/cpw" | sha256sum --check --status || {
  echo "CPW binary checksum failed. Installation stopped." >&2
  exit 1
}

install -d -m 0750 /opt/asp-cpw /opt/asp-cpw/config /opt/asp-cpw/scripts /opt/asp-cpw/log
install -d -m 0750 /etc/asp-cpw /srv/asp-cpw/work/new /srv/asp-cpw/work/CPW
install -d -m 0750 /srv/asp-cpw/staging/{element,launcher,patcher} /srv/asp-cpw/releases /srv/asp-cpw/backups
install -d -m 0750 /var/lib/asp-cpw-control /var/lib/asp-cpw-control/requests
install -m 0755 "${PACKAGE_DIR}/bin/linux-x64/cpw" /opt/asp-cpw/cpw
install -m 0755 "${PACKAGE_DIR}/scripts/asp_cpw_manager.py" /opt/asp-cpw/scripts/asp_cpw_manager.py
install -m 0755 "${PACKAGE_DIR}/scripts/asp_cpw_worker.py" /opt/asp-cpw/scripts/asp_cpw_worker.py
install -m 0755 "${PACKAGE_DIR}/scripts/asp-cpw-control" /usr/local/sbin/asp-cpw-control
install -m 0644 "${PACKAGE_DIR}/vendor/cpw_pw/config/install_mysql.sql" /opt/asp-cpw/config/install_mysql.sql

if [[ ! -L /opt/asp-cpw/files ]]; then
  if [[ -e /opt/asp-cpw/files ]]; then
    echo "/opt/asp-cpw/files already exists and is not a symlink." >&2
    exit 1
  fi
  ln -s /srv/asp-cpw/work /opt/asp-cpw/files
fi

if [[ -f /opt/asp-cpw/config/patcher.conf && -f /opt/asp-cpw/config/db.cnf ]]; then
  echo "Existing CPW database configuration found; preserving it."
else
  DB_PASSWORD="$(openssl rand -hex 24)"
  mariadb <<SQL
CREATE DATABASE IF NOT EXISTS cpw_patch CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE USER IF NOT EXISTS 'asp_cpw'@'127.0.0.1' IDENTIFIED BY '${DB_PASSWORD}';
ALTER USER 'asp_cpw'@'127.0.0.1' IDENTIFIED BY '${DB_PASSWORD}';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, DROP, INDEX, ALTER ON cpw_patch.* TO 'asp_cpw'@'127.0.0.1';
FLUSH PRIVILEGES;
SQL

  sed "s/CHANGE_ME/${DB_PASSWORD}/" "${PACKAGE_DIR}/config/patcher.conf.example" > /opt/asp-cpw/config/patcher.conf
  cat > /opt/asp-cpw/config/db.cnf <<EOF
[client]
host=127.0.0.1
port=3306
user=asp_cpw
password=${DB_PASSWORD}
EOF
fi
install -m 0644 "${PACKAGE_DIR}/config/asp-cpw.env.example" /etc/asp-cpw/asp-cpw.env
chmod 0600 /opt/asp-cpw/config/patcher.conf /opt/asp-cpw/config/db.cnf

install -m 0644 "${PACKAGE_DIR}/systemd/asp-cpw-control.service" /etc/systemd/system/asp-cpw-control.service
install -m 0644 "${PACKAGE_DIR}/systemd/asp-cpw-control.path" /etc/systemd/system/asp-cpw-control.path

if id "${WEB_USER}" >/dev/null 2>&1; then
  chown root:"${WEB_USER}" /var/lib/asp-cpw-control /var/lib/asp-cpw-control/requests
  chmod 2750 /var/lib/asp-cpw-control
  chmod 2770 /var/lib/asp-cpw-control/requests
  chown root:"${WEB_USER}" /srv/asp-cpw /srv/asp-cpw/releases
  chmod 0750 /srv/asp-cpw /srv/asp-cpw/releases
fi

if [[ ! -L /srv/asp-cpw/current ]]; then
  /usr/local/sbin/asp-cpw-control bootstrap --actor installer
fi
# Normalize an existing baseline too, so the unprivileged web service can serve it.
find /srv/asp-cpw/releases -type d -exec chmod 0755 {} +
find /srv/asp-cpw/releases -type f -exec chmod 0644 {} +
chmod 0600 /opt/asp-cpw/config/keys.json
chown root:root /opt/asp-cpw/config/keys.json
/usr/local/sbin/asp-cpw-control status > /var/lib/asp-cpw-control/snapshot.json
chmod 0644 /var/lib/asp-cpw-control/status.json /var/lib/asp-cpw-control/snapshot.json

if [[ -d /opt/pw155-web && -d "${PACKAGE_DIR}/web-integration" ]]; then
  stamp="$(date -u +%Y%m%d-%H%M%S)"
  install -d -m 0750 "/srv/asp-cpw/backups/web-${stamp}"
  cp -a /opt/pw155-web/app.py /opt/pw155-web/admin.html /opt/pw155-web/static/style.css "/srv/asp-cpw/backups/web-${stamp}/"
  install -m 0644 "${PACKAGE_DIR}/web-integration/app.py" /opt/pw155-web/app.py
  install -m 0644 "${PACKAGE_DIR}/web-integration/admin.html" /opt/pw155-web/admin.html
  install -m 0644 "${PACKAGE_DIR}/web-integration/patch_manager.html" /opt/pw155-web/patch_manager.html
  install -m 0644 "${PACKAGE_DIR}/web-integration/style.css" /opt/pw155-web/static/style.css
  install -d -m 0755 /etc/systemd/system/pw155-web.service.d
  cat > /etc/systemd/system/pw155-web.service.d/asp-cpw.conf <<EOF
[Service]
Environment=PW155_PATCH_DIR=/srv/asp-cpw/current
Environment=PW155_CPW_CONTROL_DIR=/var/lib/asp-cpw-control
ReadWritePaths=/var/lib/asp-cpw-control/requests
EOF
fi

systemctl daemon-reload
systemctl enable --now asp-cpw-control.path
if systemctl cat pw155-web.service >/dev/null 2>&1; then
  systemctl restart pw155-web.service
  sleep 2
  systemctl is-active --quiet pw155-web.service
  curl --fail --silent --show-error --max-time 10 http://127.0.0.1:8080/api/status >/dev/null
  curl --fail --silent --show-error --max-time 10 http://127.0.0.1:8080/patch/info/pid >/dev/null
fi

echo
echo "Installation complete."
echo "Staging: /srv/asp-cpw/staging/{element,launcher,patcher}"
echo "CLI:     sudo asp-cpw-control status"
echo "Web:     Admin Panel -> Patch Manager"
echo "Keys:    /opt/asp-cpw/config/keys.json (BACK THIS FILE UP SECURELY)"
