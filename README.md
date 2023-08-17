# Aiursoft.Static

[![MIT licensed](https://img.shields.io/badge/license-MIT-blue.svg)](https://gitlab.aiursoft.cn/aiursoft/static/-/blob/master/LICENSE)
[![Pipeline stat](https://gitlab.aiursoft.cn/aiursoft/static/badges/master/pipeline.svg)](https://gitlab.aiursoft.cn/aiursoft/static/-/pipelines)
[![Test Coverage](https://gitlab.aiursoft.cn/aiursoft/static/badges/master/coverage.svg)](https://gitlab.aiursoft.cn/aiursoft/static/-/pipelines)
[![NuGet version (Aiursoft.Static)](https://img.shields.io/nuget/v/Aiursoft.Identity.svg)](https://www.nuget.org/packages/Aiursoft.Static/)

Static is a simple static files HTTP server, as a global tool.

## Install

Run the following command to install this tool:

```bash
dotnet tool install --global Aiursoft.Static
```

## Usage

After getting the binary, run it directly in the terminal.

```bash
$ ./static.exe --help
Description:
  A simple static files HTTP server.

Usage:
  static [options]

Options:
  --path <path>      The folder to start the server. [default: .]
  -p, --port <port>  The port to listen for the server. [default: 8080]
  --version          Show version information
  -?, -h, --help     Show help and usage information

It will start an HTTP server on http://localhost:8080.

## How to contribute

There are many ways to contribute to the project: logging bugs, submitting pull requests, reporting issues, and creating suggestions.

Even if you with push rights on the repository, you should create a personal fork and create feature branches there when you need them. This keeps the main repository clean and your workflow cruft out of sight.

We're also interested in your feedback on the future of this project. You can submit a suggestion or feature request through the issue tracker. To make this process more effective, we're asking that these include more information to help define them more clearly.