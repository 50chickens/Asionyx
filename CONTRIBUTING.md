# Contributing

Thank you for contributing to Asionyx — we appreciate your help.

This document explains the project's conventions, how to run tests and format the code, and what we expect from contributors.

## Branches & PRs

- Work on feature branches off `master` named like `feature/short-description` or `fix/short-description`.
- Open a pull request against `master` and include a short description of the change and any verification steps.
- Include unit/integration tests for behavioral changes.

## Commit messages

We follow a lightweight Conventional Commits style:

- `feat: ...` for new features
- `fix: ...` for bug fixes
- `chore: ...` for tooling/deps changes
- `docs: ...` for documentation
- `test: ...` for test-only changes

Example: `feat(deployment): add /status endpoint`

## Code style and formatting

- Run `dotnet format` to format code before committing.
- Follow the rules in `.editorconfig` and `docs/CODING_GUIDELINES.md`.

## Running locally

From the repository root (this file is next to `src/`):

- Restore and build:

```pwsh
Set-Location -LiteralPath './src'; ./build-test-and-deploy.ps1
```

- Run the orchestrator (build + image + container-based tests):

```pwsh
Set-Location -LiteralPath './src'; ./build-test-and-deploy.ps1
```

- Format check (CI uses this):

```bash
# Install dotnet-format if you don't have it
dotnet tool install -g dotnet-format
# Run format check
dotnet format --verify-no-changes
```

## Tests

- Unit tests can be run with `dotnet test` in the `src` folder or from solution level. Integration tests are marked `Explicit` or require Docker; the orchestrator drives them.

## Code review checklist

- Is the code formatted? (`dotnet format`)
- Are there tests for new behavior?
- Are secrets excluded from the commit?
- Does the change have a clear description in the PR?

Thanks again — contributions are welcome!