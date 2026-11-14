# GAME-001 — VS-01 release and evidence gate

Status: **BLOCKED**, pending QA2-01 HIGH and independent verification.

| Gate | Evidence | Status |
| --- | --- | --- |
| Unity Editor 6000.3.28f1 imports DEV-002.2 | Owner observed normal launch | PASS (Owner) |
| EditMode test suite | Owner screenshot: 24/24 green, 0 failed | PASS (Owner) |
| PlayMode test suite | Owner reported 2/2 green; separate screenshot not supplied | PASS (Owner report) |
| Battle scene, shooting, Victory | Owner screenshots | PASS (limited manual) |
| Restart resets game | Owner screenshot shows restored fortress and restarted timer | PASS (limited manual) |
| BUG-01 magenta stripe | No stripe visible on later screenshot | PASS visually; cross-device pending |
| QA2-01 collision before gate | QA-002 source audit found chronology defect | BLOCKED / HIGH |
| QA2-02 portrait UI legibility | Tiny labels observed; device validation absent | OPEN / MEDIUM |
| QA2-03 persisted URP resources | Unity setup generates assets, not shipped in DEV-002.2 | OPEN / MEDIUM |
| QA-003 independent Unity runtime | QA environment has no Unity Editor | NOT TESTED |
| Android/iOS build and performance | No verified device run | NOT TESTED |

**Do not downgrade QA2-01 simply because existing 26 tests passed**: their
coverage did not include the edge case.

Owner acceptance requires: ARCH-003 decision; DEV-003 PR and regression tests;
QA-003 review; QA2-02/QA2-03 verification; owner runtime tests and decision.
No VS-02 implementation before explicit separate authorization.

References: [master #1](https://github.com/Gogenco/swarmforge-studio/issues/1),
[architect #2](https://github.com/Gogenco/swarmforge-studio/issues/2),
[developer #4](https://github.com/Gogenco/swarmforge-studio/issues/4),
[QA #9](https://github.com/Gogenco/swarmforge-studio/issues/9).
