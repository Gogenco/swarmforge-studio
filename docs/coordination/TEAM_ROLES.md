# SWARMFORGE — nine-role coordination contract

This repository is the shared source of truth for **GAME-001 / VS-01 only**.
ChatGPT specialist conversations do **not** automatically receive one another's
messages. Each role must actively read current GitHub issues, approved specs,
commits and PRs before acting. GitHub accounts/assignees are real human/service
identities; an AI role name does not create a GitHub account.

| Role | Owner | GitHub issue | Scope |
| --- | --- | --- | --- |
| 01 | Game Director / Studio Owner | [#1](https://github.com/Gogenco/swarmforge-studio/issues/1) | Integration decisions and release gates |
| 02 | Game Architect | [#2](https://github.com/Gogenco/swarmforge-studio/issues/2) | ARCH-003 deterministic event-order spec |
| 03 | Game Designer | [#3](https://github.com/Gogenco/swarmforge-studio/issues/3) | VS-01 gameplay, level geometry and balance review |
| 04 | Game Developer | [#4](https://github.com/Gogenco/swarmforge-studio/issues/4) | DEV-003 bug fix after ARCH-003 sign-off |
| 05 | Art Director | [#5](https://github.com/Gogenco/swarmforge-studio/issues/5) | Visual language and contrast review |
| 06 | UI/UX Designer | [#6](https://github.com/Gogenco/swarmforge-studio/issues/6) | QA2-02 mobile HUD specification |
| 07 | Sound Designer | [#7](https://github.com/Gogenco/swarmforge-studio/issues/7) | VS-01 audio planning only |
| 08 | Performance Engineer | [#8](https://github.com/Gogenco/swarmforge-studio/issues/8) | Static/performance budgets and device test plan |
| 09 | QA Engineer | [#9](https://github.com/Gogenco/swarmforge-studio/issues/9) | QA-003 independent regression |

## Mandatory workflow

1. Read [#1](https://github.com/Gogenco/swarmforge-studio/issues/1) and your
   own issue plus linked active PRs at the start of **every** work session.
2. Comment `TODO`, `IN_PROGRESS`, `BLOCKED`, `READY_FOR_REVIEW`, `VERIFIED`
   or `DONE` with evidence, exact files/commits, checks run/not run, dependencies.
3. For code, use a scoped branch and draft PR. No direct `main` push,
   force-push, rebase, squash, self-merge, or overwrite of another role's WIP.
4. Sign off on architecture before dependent code. Do not call static checks
   \"Unity PASS\". Runtime Unity tests require the actual Editor.
5. If GitHub access is missing in a specialist's chat, the specialist reports
   `BLOCKED_GITHUB_ACCESS` to Owner; do not invent repo status or claim updates.
6. No Forge/Mech/Titan, VS-02, VS-03 or VS-04 implementation without a new
   Owner directive.
7. This repository was PUBLIC when setup began by explicit Owner decision.
   Never commit secrets, tokens, personal data, Unity `Library/`, `Temp/`,
   `Build/` outputs or generated binaries. Owner can make it Private later.

## Release dependencies

ARCH-003 [#2](https://github.com/Gogenco/swarmforge-studio/issues/2)
→ DEV-003 [#4](https://github.com/Gogenco/swarmforge-studio/issues/4)
→ QA-003 [#9](https://github.com/Gogenco/swarmforge-studio/issues/9).
UI-002 and QA2-03 URP also require implementation/verification before VS-01
Owner release sign-off.
