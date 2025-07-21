#!/bin/bash

# Setup git hooks for the project

HOOKS_DIR="scripts/hooks"
GIT_HOOKS_DIR=".git/hooks"

echo "Setting up git hooks..."

# Check if hooks directory exists
if [ ! -d "$HOOKS_DIR" ]; then
    echo "❌ Hooks directory not found at $HOOKS_DIR"
    exit 1
fi

# Install each hook
for hook in "$HOOKS_DIR"/*; do
    if [ -f "$hook" ]; then
        hook_name=$(basename "$hook")
        echo "Installing $hook_name..."
        cp "$hook" "$GIT_HOOKS_DIR/$hook_name"
        chmod +x "$GIT_HOOKS_DIR/$hook_name"
    fi
done

echo "✅ Git hooks installed successfully!"
echo ""
echo "Branch naming convention enforced:"
echo "  - feature/* : New features"
echo "  - fix/*     : Bug fixes"
echo "  - hotfix/*  : Critical fixes"
echo "  - release/* : Release branches"
echo "  - chore/*   : Maintenance tasks"