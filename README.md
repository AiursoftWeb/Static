# Aiursoft.Static

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.cn/aiursoft/static/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.cn/aiursoft/static/badges/master/pipeline.svg)](https://gitlab.aiursoft.cn/aiursoft/static/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.cn/aiursoft/static/badges/master/coverage.svg)](https://gitlab.aiursoft.cn/aiursoft/static/-/pipelines)
[![NuGet version (Aiursoft.Static)](https://img.shields.io/nuget/v/Aiursoft.Static.svg)](https://www.nuget.org/packages/Aiursoft.Static/)
[![ManHours](https://manhours.aiursoft.cn/r/gitlab.aiursoft.cn/aiursoft/Static.svg)](https://gitlab.aiursoft.cn/aiursoft/Static/-/commits/master?ref_type=heads)

Static is a simple static files HTTP server, as a global tool.

## Install

Requirements:

1. [.NET 7 SDK](http://dot.net/)

Run the following command to install this tool:

```bash
dotnet tool install --global Aiursoft.Static
```

## Usage

After getting the binary, run it directly in the terminal.

```bash
$ ./static.exe  --help
Description:
  Start a static file server.

Usage:
  static [options]

Options:
  -p, --port <port>           The port to listen for the server. [default: 8080]
  --path <path>               The folder to start the server. [default: .]
  --allow-directory-browsing  Allow directory browsing the server files under the path. This options if conflict with --mirror. [default: False]
  --mirror <mirror>           The website to mirror. Automatically proxy the file if the file is not found in the server. This option if conflict with --allow-directory-browsing.
  --cache-mirror              Cache the mirrored files. This will save the mirrored files to the servers disk. [default: True]
  --enable-webdav             Enable WebDAV for the server. This is a read-only WebDAV server. [default: False]
  --version                   Show version information
  -?, -h, --help              Show help and usage information

It will start an HTTP server on http://localhost:8080.
```

## Install for all users

You can install this tool for all users by running the following command:

```bash
sudo apt install -y dotnet7
sudo dotnet tool install Aiursoft.Static --tool-path /opt/static || sudo dotnet tool update Aiursoft.Static --tool-path /opt/static
sudo chmod +x /opt/static/static
```

Then you can run the tool by `/opt/static/static`.

## Install for systemd

You can make it a systemd service by creating a file `/etc/systemd/system/static.service` with the following content:

```ini
[Unit]
Description=Serves static files for '/mnt/data' on port 48466
After=network.target
Wants=network.target

# Before starting, run:
# find /mnt/data -type d -print0 | sudo xargs -0 chmod 0755
# find /mnt/data -type f -print0 | sudo xargs -0 chmod 0644
# So www-data user can read the files
# Also owner user can write to the files
[Service]
User=www-data
Type=simple
Restart=on-failure
RestartSec=5s
ExecStart=/opt/static/static --path /mnt/data -p 48466 --allow-directory-browsing
WorkingDirectory=/mnt/data
LimitNOFILE=1048576
KillSignal=SIGINT
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="DOTNET_PRINT_TELEMETRY_MESSAGE=false"
Environment="DOTNET_CLI_TELEMETRY_OPTOUT=1"
Environment="ASPNETCORE_FORWARDEDHEADERS_ENABLED=true"

[Install]
WantedBy=multi-user.target
```

Then you can start the service by running:

```bash
sudo systemctl enable static
sudo systemctl start static
sudo systemctl status static
```

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you with push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.
