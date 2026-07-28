---
title: Apply the house GitHub repo standard to value-type
summary: Catalog of the repo settings, workflows, and Dependabot config that plumber and pool share; value-type has the main ruleset already and needs the rest.
tags: [todo, github, repo-standard, ci, house-canon]
created: 2026-07-28
priority: medium
effort: medium
status: open
---

Bring `marklauter/value-type` in line with `marklauter/plumber` and
`marklauter/pool`, the two reference repos for the house standard. The
branch ruleset is already applied; everything below is outstanding.

## Done

**Branch ruleset `main`** (created 2026-07-28, id `19931264`) — identical to
plumber's and pool's:

- Target `~DEFAULT_BRANCH`, enforcement `active`.
- Rules: `deletion`, `non_fast_forward`, `required_linear_history`,
  `pull_request`.
- Pull-request parameters: 0 required approvals, dismiss stale reviews on
  push, no code-owner review, no last-push approval, no review-thread
  resolution, merge methods `merge`/`squash`/`rebase`.
- `bypass_actors: []` and `current_user_can_bypass: never` — enforced for
  admins, so direct pushes to `main` are refused for the owner too.

## Repo settings

Both reference repos carry these; value-type differs on the first three.

| Setting | plumber / pool | value-type |
| --- | --- | --- |
| `delete_branch_on_merge` | `true` | `false` |
| `allow_auto_merge` | `true` | `false` |
| fork-PR CI approval policy | `all_external_contributors` | `first_time_contributors` |
| `allow_squash_merge` | `true` | `true` |
| `allow_rebase_merge` | `true` | `true` |
| `allow_merge_commit` | `true` | `true` |
| `squash_merge_commit_title` | `COMMIT_OR_PR_TITLE` | same |
| `squash_merge_commit_message` | `COMMIT_MESSAGES` | same |
| `default_workflow_permissions` | `read` | same |
| `can_approve_pull_request_reviews` | `false` | same |
| Dependabot security updates | enabled | enabled |
| Secret scanning + push protection | enabled | enabled |

Apply with:

```sh
gh api repos/marklauter/value-type --method PATCH \
  -F delete_branch_on_merge=true -F allow_auto_merge=true
gh api repos/marklauter/value-type/actions/permissions/fork-pr-contributor-approval \
  --method PUT -f approval_policy=all_external_contributors
```

## Workflows

Both reference repos ship the same four files under `.github/workflows/`:

- `dotnet.tests.yml` — build and test on PR and push.
- `dotnet.publish.yml` — packs and pushes to NuGet on release, using the
  `NUGET_API_KEY` repository secret. value-type ships as a package, so this
  one is required and the secret has to be added.
- `codeql.yml` — committed advanced setup, not GitHub's API default setup.
  C# uses `build-mode: none`.
- `dependabot-auto-merge.yml` — auto-merges patch and minor Dependabot PRs
  once checks pass; majors stay manual.

plumber also has a `.github/actions/` directory of composite actions and a
`github-pages` environment; pool has neither. Decide whether value-type
needs either.

## Dependabot

`.github/dependabot.yml`, weekly, open-PR limit 10, NuGet ecosystem with
`directory: "/"` (NuGet recurses subdirectories).

## Deferred

The `code_scanning` merge gate (block PRs on high CodeQL findings) is
deliberately left off until CodeQL has run at least once on this repo —
adding it first deadlocks merges under an admin-enforced ruleset.

## Secret to add

`NUGET_API_KEY` — repository Actions secret, consumed by
`dotnet.publish.yml`. Not yet present on value-type.
