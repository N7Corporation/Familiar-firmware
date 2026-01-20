# Contributing to Familiar Firmware

Thank you for your interest in contributing to Familiar Firmware!

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/Familiar-firmware.git`
3. Create a branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Test on hardware if possible
6. Submit a pull request

## Development Setup

### Requirements
- .NET 8 SDK
- Raspberry Pi 4 or 5 (for hardware testing)
- Optional: Meshtastic device, USB audio, Pi Camera

### Building
```bash
dotnet build
```

### Running
```bash
cd src/Familiar.Host
dotnet run
```

## Creating Issues

We use GitHub Issues to track bugs, features, and tasks. Please use the appropriate template:

### Bug Reports
Use the **Bug Report** template when:
- Something isn't working as expected
- You encounter an error or crash
- Behavior differs from documentation

Include:
- Steps to reproduce
- Expected vs actual behavior
- Hardware/software environment
- Log output

### Feature Requests
Use the **Feature Request** template when:
- You have an idea for a new feature
- You want to suggest an improvement
- You need functionality that doesn't exist

Include:
- Clear use case
- Proposed solution
- Hardware requirements

### Hardware Test Reports
Use the **Hardware Test** template when:
- You've tested on new hardware
- You've completed a testing session
- You want to document compatibility

Include:
- Complete hardware list
- Test results checklist
- Any issues found

### Development Tasks
Use the **Task** template when:
- Breaking down a feature into subtasks
- Documenting TODO items
- Tracking implementation work

## Code Style

- Follow C# conventions
- Use meaningful names
- Add XML documentation for public APIs
- Keep methods focused and small

## Pull Request Guidelines

1. **One feature per PR** - Keep PRs focused
2. **Update documentation** - If behavior changes, update docs
3. **Test on hardware** - If possible, test on actual Pi
4. **Describe changes** - Explain what and why in PR description

## Issue Labels

| Label | Description |
|-------|-------------|
| `bug` | Something isn't working |
| `enhancement` | New feature or improvement |
| `hardware` | Hardware-related issue |
| `testing` | Testing and QA |
| `task` | Development task |
| `documentation` | Documentation updates |
| `help wanted` | Extra attention needed |
| `good first issue` | Good for newcomers |

## Project Structure

```
Familiar-firmware/
├── src/
│   ├── Familiar.Audio/      # Audio playback and capture
│   ├── Familiar.Tts/        # Text-to-speech
│   ├── Familiar.Meshtastic/ # LoRa mesh networking
│   ├── Familiar.Camera/     # Pi Camera (Pi 5)
│   └── Familiar.Host/       # ASP.NET Core web app
├── tests/                   # Unit tests
├── scripts/                 # Setup and deployment
└── config/                  # Configuration templates
```

## Communication

- **Issues**: Bug reports, feature requests, tasks
- **Discussions**: Questions, ideas, general chat
- **Pull Requests**: Code contributions

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.
