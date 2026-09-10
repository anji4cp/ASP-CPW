#!/usr/bin/env bash
set -euo pipefail

SOURCE_DIR="${1:-}"
ASSUME_YES="${2:-}"
STAGING=/srv/asp-cpw/staging
IGNORE_NAME=PLACE-FILES-HERE.txt

if [[ ${EUID} -ne 0 ]]; then
  echo "This helper must run through sudo." >&2
  exit 1
fi
if [[ ! -d ${SOURCE_DIR} ]]; then
  echo "Patch bundle is missing PATCH-FILES." >&2
  exit 1
fi
if [[ ! -x /usr/local/sbin/asp-cpw-control ]]; then
  echo "ASP CPW Manager is not installed." >&2
  exit 1
fi

for channel in element launcher patcher; do
  [[ -d "${SOURCE_DIR}/${channel}" ]] || { echo "Missing channel folder: ${channel}" >&2; exit 1; }
done

if find "${SOURCE_DIR}" -type l -print -quit | grep -q .; then
  echo "Symbolic links are not allowed in a patch bundle." >&2
  exit 1
fi

mapfile -d '' files < <(find "${SOURCE_DIR}" -type f ! -name "${IGNORE_NAME}" -print0)
if (( ${#files[@]} == 0 )); then
  echo "No patch files found." >&2
  exit 1
fi

for channel in element launcher patcher; do
  if find "${STAGING}/${channel}" -type f -print -quit | grep -q .; then
    echo "Server staging is not empty. Run 'sudo asp-cpw-control preview' and review it first." >&2
    exit 1
  fi
done

echo "Receiving ${#files[@]} patch file(s)..."
for channel in element launcher patcher; do
  while IFS= read -r -d '' source; do
    relative="${source#"${SOURCE_DIR}/${channel}/"}"
    [[ ${relative} != "${source}" ]] || { echo "Unsafe source path." >&2; exit 1; }
    if [[ ${relative} == .* || ${relative} == */.* || ${relative} == *..* ]]; then
      echo "Hidden or unsafe path rejected: ${relative}" >&2
      exit 1
    fi
    target="${STAGING}/${channel}/${relative}"
    install -D -m 0644 -- "${source}" "${target}"
  done < <(find "${SOURCE_DIR}/${channel}" -type f ! -name "${IGNORE_NAME}" -print0)
done

/usr/local/sbin/asp-cpw-control preview
if [[ ${ASSUME_YES} != "--yes" ]]; then
  read -r -p "Publish this patch? Type PUBLISH: " answer
  [[ ${answer} == "PUBLISH" ]] || { echo "Cancelled; files remain in staging."; exit 2; }
fi

/usr/local/sbin/asp-cpw-control publish --actor "${SUDO_USER:-pwadmin}"
echo "ASP CPW patch published and verified."

