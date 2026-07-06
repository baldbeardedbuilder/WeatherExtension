# Contributing to Weather Extension

Thank you for your interest in contributing to the Weather Extension for
Microsoft Command Palette! This guide will help you get started.

## Code of Conduct

Please be respectful and constructive in all interactions. We're all here to
build something great together.

## Getting Started

### Prerequisites

- Windows 10 or 11
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (main
  project)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (test
  project)
- [PowerToys](https://github.com/microsoft/PowerToys) with Command Palette
  enabled (for manual testing)

### Building

The project requires a Windows runtime identifier due to MSIX packaging:

```bash
dotnet build WeatherExtension/WeatherExtension.csproj -r win-x64
```

### Running Tests

```bash
dotnet test WeatherExtension.Tests/WeatherExtension.Tests.csproj -r win-x64
```

## How to Contribute

### Reporting Bugs

- Search [existing issues](https://github.com/baldbeardedbuilder/WeatherExtension/issues)
  before opening a new one
- Include steps to reproduce, expected behavior, and actual behavior
- Screenshots are very helpful for UI issues

### Suggesting Features

Open an issue describing the feature, why it would be useful, and any
implementation ideas you have.

### Submitting Changes

1. Fork the repository
2. Create a feature branch from `main` (`git checkout -b feature/my-change`)
3. Make your changes
4. Ensure the project builds and tests pass
5. Commit using [conventional commits](#commit-messages)
6. Push to your fork and open a pull request against `main`

### Commit Messages

This project uses [Conventional Commits](https://www.conventionalcommits.org/).
Please format your commit messages as:

```
type: short description

Optional longer description.
```

Common types:

| Type | Description |
|------|-------------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation changes |
| `ci` | CI/CD changes |
| `chore` | Maintenance tasks |
| `refactor` | Code changes that don't fix a bug or add a feature |
| `test` | Adding or updating tests |

### Pull Requests

- Keep PRs focused — one logical change per PR
- Ensure CI passes (build + tests run automatically)
- Provide a clear description of what changed and why
- Link any related issues (e.g., "Fixes #123")

## Project Structure

```
WeatherExtension/
├── Assets/              # App icons and images
├── Commands/            # Command implementations (pin, unpin, refresh)
├── DockBands/           # Dock band items (current weather, pinned locations)
├── Models/              # Data models (weather, geocoding, forecasts)
├── Pages/               # UI pages (weather list, detail, hourly, settings)
├── Properties/          # Resources and assembly info
├── Services/            # API clients and business logic
└── Icons.cs             # Weather condition emoji mappings

WeatherExtension.Tests/  # Unit tests (MSTest + Moq)
```

## License

By contributing, you agree that your contributions will be licensed under the
[MIT License](LICENSE).
