---
name: develop-design
description: Collaborative software design and PRD workflow for repository-aware feature planning, module architecture design, technical proposal drafting, and pre-implementation design reviews. Use when the user asks to design a feature, write or refine a PRD, plan module architecture, split implementation tasks, evaluate a technical approach, or produce design documentation before coding, especially when the design must align with an existing codebase, domain terminology, architecture decisions, and user clarification.
---

# Develop Design

## Core Rule

Do design work before implementation. Do not write product code, large code samples, or final documentation until the problem, terminology, constraints, and direction are clarified with the user.

For Chinese user requests or repositories whose documentation language is Chinese, conduct clarification, architecture summaries, PRDs, technical design documents, review prompts, and final design updates in Chinese. Preserve existing project-specific terms and code identifiers exactly when translating surrounding prose.

## Workflow

### 1. Explore the codebase first

Build a repository-grounded understanding before proposing a solution.

- Read project guidance files such as `AGENTS.md`, module docs, architecture notes, and existing PRDs when present.
- Use structural code tools first when available, such as CodeGraph for symbol relationships, entry points, callers, and architecture context.
- Inspect only the relevant modules deeply enough to learn current architecture, naming, extension points, data flow, constraints, and domain terms.
- Capture the vocabulary the project already uses. Reuse it in all PRD and design text instead of inventing parallel names.
- Respect existing architecture decisions, assembly/module boundaries, public APIs, editor/runtime separation, persistence models, and extension mechanisms.
- If the repository has rules for language, documentation location, testing, or generated files, carry those rules into the design.
- When the project requires Chinese documentation, make the context summary Chinese as well.

End this step with a short context summary for the user: current architecture, relevant terms, likely extension points, and unresolved unknowns.

### 2. Clarify through progressive choices

Discuss before writing the final design. Ask when unclear, and move from large decisions to details.

- Start with high-level choices: product goal, target user/workflow, module boundary, data ownership, UI/API surface, migration strategy, and success criteria.
- Present two to four concrete options at each decision point. Include tradeoffs, risks, and the option that best fits the existing project.
- Avoid asking many unrelated questions at once. Ask the next most important question, then refine.
- Treat missing requirements as design risk, not as permission to assume silently.
- When making a reasonable assumption, state it plainly and invite correction.
- Continue until the direction is stable enough that the design can be written without hidden guesses.

Use this pattern, translating it to the user's language when appropriate:

```text
I see a few possible directions:
1. Direction A: best for..., with the cost of...
2. Direction B: best for..., with the cost of...
3. Direction C: best for..., with the cost of...

I lean toward B because it better matches the current... Which direction should we continue with?
```

### 3. Draft the design at the right level

Produce design documentation only after alignment.

- Use the repository's established terminology throughout.
- For Chinese projects, write the document body, headings, acceptance criteria, task breakdown, risks, and validation strategy in Chinese.
- Prefer architecture diagrams in prose, tables, responsibilities, state/data flow, lifecycle descriptions, and task boundaries.
- Do not include large code blocks. Use short pseudocode or API sketches only when they clarify a contract.
- For module architecture, split work into incremental, independently reviewable steps that can be completed progressively.
- Include how the design interacts with existing modules, what remains unchanged, and what must be explicitly avoided.
- Include risks, alternatives considered, validation strategy, and open questions.

Recommended structure, translated to the project's required documentation language:

```markdown
# Title

## Background and Goals
## Current Architecture Understanding
## Terms and Boundaries
## Solution Overview
## Key Design
## Data Flow / Lifecycle
## Task Breakdown
## Compatibility and Migration
## Risks and Tradeoffs
## Validation
## Open Questions
```

For PRDs, emphasize user goals, scenarios, requirements, non-goals, acceptance criteria, and project terminology. For technical designs, emphasize module boundaries, APIs, ownership, lifecycle, performance, compatibility, and phased implementation.

### 4. Review with a subagent

After the first complete design draft, request an independent review from a subagent when multi-agent tooling is available. If the tool is not currently exposed, discover it with the tool search mechanism if available. If no subagent can be launched, perform a clearly labeled self-review and tell the user the limitation.

Give the reviewer only the design draft and the essential repository context. Do not give it your preferred answer or hidden rationale.

For Chinese design drafts, ask the reviewer to respond in Chinese.

Use a reviewer prompt like this, translating it to the user's language when appropriate:

```text
Please review the following design. Focus on usability, structural clarity, understandability, consistency with the existing architecture, performance, maintainability, task breakdown granularity, missing risks, and validation strategy. Sort findings by severity, and identify which suggestions are required changes versus optional improvements.

[design draft]
```

Revise the design after the review. If the review raises substantial issues, either ask the user to choose between competing directions or update the draft and run another review pass. Iterate until the design is coherent and the user is satisfied.

### 5. Finalize

End with a concise summary of:

- Chosen direction.
- Important tradeoffs.
- Final document path or draft content.
- Remaining decisions, if any.
- Suggested next implementation step, only after design approval.

## Quality Bar

The final design must be easy for a future implementer to follow without rediscovering the codebase. It must explain what to build, where it fits, what not to change, how to split the work, and how to verify the result.
