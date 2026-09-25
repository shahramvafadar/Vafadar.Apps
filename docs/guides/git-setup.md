# Git setup and identity

The development machine also hosts **work repositories with a different Git identity and a different GitHub
account**. This repository must never be committed or pushed with that identity, and work code must never be pushed
here. Separation is enforced in layers, so a single mistake is not enough to mix them up.

| Layer | What it does | Scope |
|---|---|---|
| 1. Repository identity | `user.name` / `user.email` set in this repository's own config | This clone |
| 2. Account in the remote URL | `https://shahramvafadar@github.com/...` makes Git Credential Manager use the `shahramvafadar` login, never another stored GitHub account | This clone |
| 3. Guard hooks (`eng/git-hooks`) | `pre-commit` rejects commits with any other e-mail; `pre-push` rejects pushes to any other server or account and any commit with a foreign identity | This clone (versioned in the repo) |
| 4. Conditional global config (optional) | `includeIf` applies the personal identity automatically to every clone of `github.com/shahramvafadar/*` | The machine |

Layers 1–3 are set per clone; layer 4 makes new clones correct automatically.

## Setting up a clone

```powershell
git clone https://shahramvafadar@github.com/shahramvafadar/Vafadar.Apps.git
cd Vafadar.Apps
git config user.name "Shahram Vafadar"
git config user.email "shahramvafadar@gmail.com"
git config core.hooksPath eng/git-hooks
```

The first push opens the Git Credential Manager sign-in; sign in as **shahramvafadar**. The login is stored
separately from any other GitHub account on the machine.

## Optional: automatic identity for all personal clones

Add to the global Git config (`git config --global --edit`):

```ini
[includeIf "hasconfig:remote.*.url:https://github.com/shahramvafadar/**"]
    path = .gitconfig-personal
[includeIf "hasconfig:remote.*.url:https://*@github.com/shahramvafadar/**"]
    path = .gitconfig-personal
```

and create `.gitconfig-personal` next to the global config file:

```ini
[user]
    name = Shahram Vafadar
    email = shahramvafadar@gmail.com
[credential "https://github.com"]
    username = shahramvafadar
[core]
    hooksPath = eng/git-hooks
```

Work repositories are unaffected: the include only applies to repositories whose remote points to
`github.com/shahramvafadar`.

## Checking a clone

```powershell
git config user.email        # shahramvafadar@gmail.com
git remote -v                # https://shahramvafadar@github.com/shahramvafadar/Vafadar.Apps.git
git config core.hooksPath    # eng/git-hooks
```

## Visual Studio

Visual Studio's Git tools use the same repository configuration and Git Credential Manager, so layers 1–3 apply.
Add the personal GitHub account under *File → Account Settings* (Visual Studio supports several GitHub accounts) and
select it when Visual Studio asks which account to use for this repository.

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `Permission to shahramvafadar/Vafadar.Apps.git denied to <other account>` | The remote URL has no user name, so a stored login of another account was used | `git remote set-url origin https://shahramvafadar@github.com/shahramvafadar/Vafadar.Apps.git` |
| `pre-commit: this repository must be committed as <shahramvafadar@gmail.com>` | Wrong identity in this clone | `git config user.email shahramvafadar@gmail.com` |
| `pre-push: commit ... was not made with <shahramvafadar@gmail.com>` | A commit was created with another identity | `git commit --amend --reset-author` (last commit) or rewrite the affected commits before pushing |
