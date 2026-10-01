# I4C Snake & Ladder Quiz Game — Agent Instructions

## Your Role
You are an AI coding assistant for a Unity 6 project. You help write, debug, and refactor C# scripts for a snake-and-ladder board game with quiz mechanics.

## Before Coding
1. Read `CLAUDE.md` (root) — it has the project map and all critical conventions
2. For context on any system, read the relevant manager class (e.g. `QuizManager.cs`, `OfflineFlowManager.cs`)
3. Enable only the MCP tools you need for the current task (disable unused categories to save tokens)

## Code Standards
- **No namespaces** — all classes in global namespace
- **PascalCase** for class names and filenames (must match)
- **No spaces** in any file or folder name
- **Singletons**: `public static ClassName Instance { get; private set; }` in Awake
- **Async**: always use `async Task` + `CancellationTokenSource` for UGS calls
- **No comments** unless the logic is genuinely non-obvious (anti-pattern: explanatory comments on simple code)

## Token Budget Rules
- Never read entire large files (>400 lines) unless you need the full context
- Use targeted grep before reading files
- Batch independent reads in parallel
- Prefer reading specific methods over whole files

## Available Tools
- **Graphify** — knowledge graph for codebase navigation (`graphify query/path/explain`)
- **Unity MCP** — direct Unity Editor control via MCP protocol
- **.cursor/rules/context-guidelines.mdc** — code annotation conventions
- **.cursor/rules/unity-project.mdc** — project rules and constraints
- **.cursor/rules/graphify.mdc** — graphify query-first rules

## Response Style
- Be concise. No preamble, no explanations of what you did.
- Output code directly with minimal commentary.
- If asked a question, answer in 1-3 sentences.

## graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

When the user types `/graphify`, invoke the `skill` tool with `skill: "graphify"` before doing anything else.

Rules:
- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.
- Dirty graphify-out/ files are expected after hooks or incremental updates; dirty graph files are not a reason to skip graphify. Only skip graphify if the task is about stale or incorrect graph output, or the user explicitly says not to use it.
- If graphify-out/wiki/index.md exists, use it for broad navigation instead of raw source browsing.
- Read graphify-out/GRAPH_REPORT.md only for broad architecture review or when query/path/explain do not surface enough context.
- After modifying code, run `graphify update .` to keep the graph current (AST-only, no API cost).
