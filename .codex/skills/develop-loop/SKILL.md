---
name: develop-loop
description: Checklist-driven development workflow for completing tasks from a provided Markdown todolist where each task links to its own task document. Use when Codex must coordinate isolated developer and reviewer sub-agents, avoid main-agent code reading or implementation, enforce prerequisite checks, loop on review feedback until approval, update the todolist with completion status and execution notes, and create one Git commit per completed task when the repository is a Git worktree.
---

# Develop Loop

## Core Contract

Use this skill to drive development from a Markdown todolist. The main agent coordinates only:

- Read the todolist and task documents.
- Check prerequisites before starting each task.
- Spawn and coordinate sub-agents.
- Update the todolist after each task.
- Create one Git commit after each completed task when the repository is inside a Git worktree.
- Do not read source code, inspect diffs, write implementation code, or perform review yourself.

Each runnable task uses exactly two fresh sub-agents:

- Developer agent: reads the task document and code, implements the task, runs relevant checks, and reports changed files plus validation results.
- Reviewer agent: reads the task document and resulting code changes, performs a code-review pass, runs or requests checks as needed, and returns `PASS` or `FAIL` with findings.

For every new todolist task, spawn new agents with `fork_context: false`. Do not reuse agents, messages, summaries, or hidden assumptions from a previous task.

## Preconditions

Before starting, verify all required conditions from documents only:

- The user provided a Markdown todolist path.
- The todolist exists and is readable.
- The todolist contains task entries with unchecked checkboxes.
- Each task entry has a path or Markdown link to a dedicated task document.
- Each task document exists and is readable.
- Each task document includes enough detail for an agent to develop without asking the main agent to inspect code: goal, scope, acceptance criteria, expected validation, and any required constraints or references.
- Multi-agent tools are available for spawning, waiting on, messaging, and closing sub-agents.

If any task lacks required prerequisites, do not execute that task. Add an execution note to the todolist explaining why it was skipped or blocked, and leave the checkbox unchecked.

If global prerequisites are missing, stop before spawning any agents and tell the user what is missing.

## Todolist Expectations

Prefer this shape:

```markdown
- [ ] Task title - [task doc](docs/task-title.md)
```

Also accept plain paths on the task line when unambiguous:

```markdown
- [ ] Task title: docs/task-title.md
```

After each task, update the todolist near the task line:

```markdown
- [x] Task title - [task doc](docs/task-title.md)
  - 执行情况：完成；开发 Agent：<id/name>；Review Agent：<id/name>；验证：<checks>；变更：<summary>；提交：<commit-hash or 非 Git 工作区>
```

For blocked tasks:

```markdown
- [ ] Task title - [task doc](docs/task-title.md)
  - 执行情况：未执行；原因：<missing prerequisite>
```

Preserve the user's existing Markdown style where practical.

## Workflow

1. Read the todolist.
2. Parse unchecked tasks in order.
3. Determine once whether the repository is inside a Git worktree.
4. For each unchecked task, read only its task document and check prerequisites.
5. If the task is blocked, update the todolist with `未执行` and continue to the next task.
6. If inside a Git worktree, record a pre-task `git status --short` baseline without reading source code.
7. Spawn a fresh developer agent with `agent_type: "worker"` and `fork_context: false`.
8. Wait for the developer agent to finish.
9. Spawn a fresh reviewer agent with `fork_context: false`; use `agent_type: "worker"` if it needs to run checks, otherwise `agent_type: "default"` is acceptable.
10. Wait for the reviewer agent to return `PASS` or `FAIL`.
11. If review fails, send the findings back to the same developer agent and ask it to fix them. Then send the revised developer result back to the same reviewer agent. Repeat until the reviewer returns `PASS`.
12. When review passes, update the todolist checkbox to complete and add the execution note.
13. If inside a Git worktree, create exactly one commit for the completed task.
14. Close both agents before moving to the next task.
15. Continue until all runnable tasks are complete or blocked.

Do not start the next task until the current task has passed review, been marked blocked, or the user interrupts.

## Developer Agent Prompt

Give the developer agent only the context needed for the current task:

- Repository root.
- Todolist path.
- Current task title.
- Current task document path.
- A reminder to read repository instructions such as `AGENTS.md`.
- A reminder that it is not alone in the codebase and must not revert unrelated user changes.

Use a prompt shaped like:

```text
You are the developer agent for one isolated develop-loop task.

Repository root: <repo>
Todolist: <todo.md>
Task: <title>
Task document: <task-doc.md>

Read the task document and any repository instructions needed to implement this task. You may read and edit code. The main agent will not read code or implement anything.

Implement only this task. Do not revert unrelated changes. If repository instructions require documentation updates, include them. Run the most relevant validation available in this project.

When finished, report:
- Summary of changes
- Changed file paths
- Commit candidate paths
- Validation commands and results
- Any risks or follow-up notes
```

If review fails, send the reviewer findings to this same developer agent:

```text
Review failed for the current task. Address these findings without broadening scope:

<review findings>

After fixing, report the updated changed files and validation results.
```

## Reviewer Agent Prompt

Give the reviewer agent only the current task context plus the developer's completion report. The reviewer may read source code and run checks, but must not edit files.

Use a prompt shaped like:

```text
You are the reviewer agent for one isolated develop-loop task.

Repository root: <repo>
Todolist: <todo.md>
Task: <title>
Task document: <task-doc.md>
Developer result:
<developer report>

Review the implementation against the task document and repository instructions. Do not modify files.

Return exactly one status:
- PASS if the task is complete and the implementation is acceptable.
- FAIL if changes are needed.

For FAIL, list concrete findings with file paths, line references when possible, severity, and required fixes.
For PASS, summarize the accepted changes, validation evidence, and whether the developer's commit candidate paths are safe to commit for this task.
```

When the developer submits fixes, send the reviewer the updated developer report and previous findings, asking for another `PASS` or `FAIL`.

## Git Commit Per Task

When the repository is inside a Git worktree, commit once after each task passes review and after updating the todolist.

Rules:

- Use `git rev-parse --is-inside-work-tree` to detect Git. If it is not a Git worktree, skip committing and record `提交：非 Git 工作区` in the todolist note.
- Do not inspect source diffs. The main agent may run metadata commands such as `git status --short`, `git add`, `git commit`, and `git rev-parse --short HEAD`.
- Stage only the reviewer-approved commit candidate paths plus the todolist file. Do not use broad staging commands such as `git add -A` or `git add .`.
- If the commit candidate paths are missing or unclear, ask the developer agent to provide them before committing.
- If the reviewer does not explicitly confirm the commit candidate paths are safe for this task, ask the reviewer agent to confirm before committing.
- If pre-existing or unrelated changes would be included and cannot be safely separated without the main agent reading diffs, do not commit. Leave the task unchecked or mark it blocked according to the user's todolist style, and record the reason.
- Use a concise task-scoped commit message, for example `develop-loop: <task title>`.
- After committing, record the short commit hash in the todolist execution note. If the todolist update itself changes after the commit hash is known, amend the same commit to include the final note without changing the message.

## Main Agent Boundaries

The main agent must not:

- Open source files for implementation or review.
- Inspect diffs for correctness.
- Run code edits.
- Decide that review findings are invalid by reading code.
- Reuse a previous task's agents for a new task.
- Spawn new task agents with inherited context.
- Commit unrelated changes or use broad Git staging commands.

The main agent may:

- Read and update the todolist.
- Read task documents for prerequisite checks.
- Read high-level repository instruction files when needed to prepare agent prompts.
- Relay reports and findings between the two agents.
- Run Git metadata, staging, and commit commands needed for the per-task commit.
- Stop and ask the user only when documents are missing, contradictory, or insufficient for safe execution.

If the execution environment requires the main agent to manually apply or merge child-agent code changes, stop and tell the user that the requested boundary cannot be preserved in this environment.

## Completion Criteria

A task is complete only when all are true:

- The developer agent reports implementation and validation.
- The reviewer agent returns `PASS`.
- The todolist checkbox is marked complete.
- The todolist includes execution notes with agents, validation, and a concise change summary.
- If inside a Git worktree, exactly one task-scoped commit was created and recorded in the todolist note.

A task is blocked when prerequisites are missing or the agents cannot complete it without violating the main-agent boundary. Blocked tasks remain unchecked and receive a `未执行` note.
