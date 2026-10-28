# AGENTS.md

Project rules for every coding agent are in `CLAUDE.md` (read it first; it applies to all agents, not only Claude).

**Multi-agent coordination:** tasks from the user arrive in `AgentComms/<agent>/inbox/` (this agent: `codex`). Follow `AgentComms/README.md`: report in your outbox, keep your section of `AgentComms/STATUS.md` up to date, and never edit files the other agent holds. **Only monitor or wait for inbox updates when the user explicitly requests it. Do not automatically wait for 1 hour after finishing work. Stop monitoring immediately when asked.** This replaces the previous default (user instruction, 2026-10-25).

**Questions to the user are pick-or-type prompts** (user, 2026-10-29): never ask in plain prose. When run from the message page (`codex exec`), end the reply with a fenced block of language `hoi` holding valid JSON in the AskUserQuestion shape (`{"questions":[{"header":..,"question":..,"multiSelect":false,"options":[{"label":..,"description":..}]}]}`), 2 to 4 options, recommended first. Details: `docs/QuyTac/QuyTacLamViec.md` §1.
