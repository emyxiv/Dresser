 – AI Assistant Context



This file is the primary context entry point for AI assistants (GitHub Copilot, Claude) working on the Dresser codebase.

## Project Overview

Dresser is a tool part of an imGui project built in C#.
It consumes various IPCs and offer an intuitive GUI for the user to enjoy design outfits by mixing & matching clothes parts.


## Agent Behavior Rules

- **Never use terminal commands to edit files** (no `sed`, `awk`, `echo >`, `cat >`, `tee`, `mv`, `cp` for file content changes, etc.)
- Always use the built-in file editing tool to modify source files so changes appear in the VS Code diff view.
- If you need to create or modify a file, use the edit tool — not the shell.
- Terminal use is only allowed for: running builds, tests, installs, or read-only inspection commands.
