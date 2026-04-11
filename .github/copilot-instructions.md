 – AI Assistant Context



This file is the primary context entry point for AI assistants (GitHub Copilot, Claude) working on the Dresser codebase.

## Project Overview

Dresser is a tool part of an imGui project built in C#.
It consumes various IPCs and offer an intuitive GUI for the user to enjoy design outfits by mixing & matching clothes parts.


## Agent Behavior Rules

- NEVER use terminal/shell commands to create or modify file contents.
  Forbidden: sed, awk, echo >, cat >, tee, mv, cp used for edits, etc.
- ALWAYS use the built-in VS Code file edit tool for any file changes,
  so edits appear in the diff view and can be tracked/undone.
- Terminal is only allowed for: running builds, tests, package installs,
  or read-only commands (ls, cat to read, grep, etc.)
- If you cannot make an edit with the file tool, STOP and explain why —
  do not attempt a shell workaround.
- Never retry a failed command more than once. If it fails, stop and report.
