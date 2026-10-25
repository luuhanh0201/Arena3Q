# AGENTS.md

Project rules for every coding agent are in `CLAUDE.md` (read it first; it applies to all agents, not only Claude).

**Multi-agent coordination:** tasks from the user arrive in `AgentComms/<agent>/inbox/` (this agent: `codex`). Follow `AgentComms/README.md`: report in your outbox, keep your section of `AgentComms/STATUS.md` up to date, and never edit files the other agent holds. **Only monitor or wait for inbox updates when the user explicitly requests it. Do not automatically wait for 1 hour after finishing work. Stop monitoring immediately when asked.** This replaces the previous default (user instruction, 2026-10-25).
