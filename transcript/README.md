# Claude Code Session Transcripts

Raw and readable transcripts of the Claude Code (Anthropic, Opus 4.8) sessions that
produced this project. See [`../cloud-session.md`](../cloud-session.md) for the curated
narrative log.

| Session | Raw `.jsonl` (authoritative) | Readable `.md` |
|---|---|---|
| **1 — initial build** (requirements → backend → frontend → tests → first Railway deploy → GitHub) | `session-1-build.jsonl` | [`session-1-build.md`](session-1-build.md) |
| **2 — code-clean, live form validation, SQLitePCLRaw security bump, prod DB reset, v1.0.0 release** | `session-2-clean-and-release.jsonl` | [`session-2-clean-and-release.md`](session-2-clean-and-release.md) |

## Formats

- **`.jsonl`** — the raw, complete Claude Code session export, one JSON event per line
  (every user/assistant message, tool call, and tool result), copied verbatim from
  `~/.claude/projects/<project-hash>/`. This is the authoritative record.
- **`.md`** — a human-readable rendering of the same session: user prompts, Claude's
  replies, and each tool call (name + brief input) with a truncated result. Internal
  chain-of-thought (`thinking`) blocks and long outputs are trimmed for readability;
  refer to the `.jsonl` for the full, untruncated content.

## Caveat

Each transcript reflects what had been flushed to disk when it was copied. The very
last turns of session 2 — the ones that generated and committed these transcript files —
are therefore not present in `session-2-clean-and-release.jsonl`.
