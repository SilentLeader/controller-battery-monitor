# GitHub Flow Workflow

This repository uses GitHub Flow for development and release management.

## Workflow Overview

### Development
1. **Feature Development**: Create feature branches from `main`
   ```bash
   git checkout main
   git pull origin main
   git checkout -b feature/my-new-feature
   ```

2. **Pull Requests**: Create PRs to merge features into `main`
   - CI workflow runs automatically on PRs
   - Builds and tests are verified before merge
   - All changes go through code review

3. **Main Branch**: Always deployable, production-ready code
   - Direct pushes trigger CI workflow
   - Version is automatically calculated by GitVersion

### Releases
1. **Create Release Tag**: Tag commits on `main` to trigger releases
   ```bash
   git checkout main
   git pull origin main
   git tag v1.2.3
   git push origin v1.2.3
   ```

2. **Automatic Publishing**: Tagged versions trigger the publish workflow
   - Builds for Windows and Linux
   - Creates GitHub release with artifacts
   - Packages for multiple Linux distributions

### Hotfixes
1. **Hotfix Branches**: Create from `main` for urgent fixes
   ```bash
   git checkout main
   git checkout -b hotfix/critical-bug-fix
   ```

2. **Fast-track**: Can be merged directly to `main` after review
   - Creates new patch version
   - Can be tagged immediately for release

## GitVersion Configuration

- **Main Branch**: Increments minor version on merge
- **Feature Branches**: Uses branch name as pre-release tag
- **Hotfix Branches**: Increments patch version
- **Release Branches**: Used for release candidates (beta tag)

## Workflows

### CI Workflow (`.github/workflows/ci.yml`)
- **Triggers**: PRs and pushes to `main`
- **Actions**: Build, test, cross-platform verification
- **Purpose**: Ensure code quality and compatibility

### Publish Workflow (`.github/workflows/publish.yml`)
- **Triggers**: Version tags (`v*`), manual dispatch
- **Actions**: Build, package, create GitHub release
- **Purpose**: Automated release management

## Branch Protection

Recommended settings for `main` branch:
- Require pull request reviews
- Require status checks (CI workflow)
- Require branches to be up to date
- Restrict pushes that create tags