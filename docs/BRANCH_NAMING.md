# Branch Naming Convention

This project enforces a consistent branch naming convention to improve clarity and organization.

## Branch Name Format

All branch names must follow this pattern:
```
{type}/{description}
```

Where:
- `{type}` is one of the allowed branch types (see below)
- `{description}` is lowercase with hyphens (`a-z`, `0-9`, `-`)

## Allowed Branch Types

- **feature/** - New features or enhancements
- **fix/** - Bug fixes  
- **hotfix/** - Critical fixes that need immediate attention
- **release/** - Release preparation branches
- **chore/** - Maintenance tasks, refactoring, tooling updates

## Examples

✅ **Valid branch names:**
- `feature/add-user-authentication`
- `feature/implement-search-api`
- `fix/resolve-memory-leak`
- `fix/correct-date-formatting`
- `hotfix/security-vulnerability-patch`
- `release/v2.0.0`
- `chore/update-dependencies`

❌ **Invalid branch names:**
- `feat/add-login` (use `feature` not `feat`)
- `feature/Add-Login` (use lowercase)
- `feature/add_login` (use hyphens not underscores)
- `add-login` (missing type prefix)
- `feature/add login` (no spaces allowed)

## Enforcement

Branch naming conventions are enforced through:

1. **Local Git Hooks** - Validates branch names before push
   - Run `./scripts/setup-git-hooks.sh` (Linux/macOS)
   - Run `./scripts/setup-git-hooks.ps1` (Windows)

2. **GitHub Actions** - Validates branch names on pull requests
   - Automatically runs on all PRs
   - Blocks merge if branch name is invalid

## Setting Up Git Hooks

To enable local branch name validation:

### Linux/macOS:
```bash
./scripts/setup-git-hooks.sh
```

### Windows:
```powershell
.\scripts\setup-git-hooks.ps1
```

## Exceptions

The following branches are exempt from naming conventions:
- `main` - The primary branch
- `develop` - The development branch (if used)