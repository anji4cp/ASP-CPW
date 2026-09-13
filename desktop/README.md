# ASP CPW Desktop Manager

**English** | [Bahasa Indonesia](README.id.md)

A lightweight Windows application that brings CPW server installation, client preparation, update file
selection, preview, publication, verification, and staging guidance into one place. It stores no passwords and
ships no game data.

The **Launcher Links** tab configures the stock launcher's News, Register,
Arc/Website, Support, and Forum URLs by safely updating
`patcher/skin/mainuni.xml`.

**Ubuntu address**, **SSH port**, **SSH username**, **Public patch URL**, **Game address**, and **Game port** are
passed automatically from Settings. The console asks only for action confirmation and passwords. Operations are
blocked and the user is directed to Settings when connection values are empty or invalid.

## Opening the application

Double-click `ASP-CPW-DESKTOP.cmd` in the repository root. The launcher reads `desktop/VERSION` and automatically
builds the versioned executable when it is missing. This allows a new version to be built while an older version
is still open. Windows 10/11 normally includes the required .NET Framework; no
.NET SDK or Electron runtime is required.

## Workflow

### One time

1. Use **Dashboard > Install / Update Server** to install the manager on Ubuntu. Repeat only after the server
   package in this repository changes.
2. Use **Prepare Client** for each new client. Repeat only when the client, launcher/patcher, URL, or RSA key changes.

### Every update

1. Open **Create Update (Repeat)** and select the client root.
2. Check only changed files and click **Add Checked Files**. A custom file must be inside the client's `element`,
   `launcher`, or `patcher` directory.
3. Open **Preview & Publish (Repeat)** and review paths and sizes.
4. Click **Publish Update**, complete the console confirmation, and then click **Verify Release**.
5. Test the update with a test client copy.

## Duplicate publication protection

The application compares SHA-256 against local `tools/patch-publisher/PUBLISHED` history. An identical file is
marked **Already published locally**, and publication is blocked until it is removed or replaced. If Ubuntu
staging remains occupied after a failed publication, use **Server Staging**. Publish existing staging only after
confirming every path, then use **Publish Existing Staging**; never delete it blindly.

## Client and server files

CPW distributes client files only. Never place `gshopsev.data`, `npcgen.data`, `aipolicy.data`, `domain.sev`, or
files from `gamed/config` into CPW. Selecting `gshop.data` displays a reminder to deploy and verify the matching
server-side `gshopsev.data` first.

## Manual build

Run `desktop/BUILD-DESKTOP.cmd`. Source is under `desktop/src`, and versioned output is written to `desktop/bin`.
