<p align="center">
  <img src="docs/images/banner.svg" alt="Final Legacy — a text-based RPG" width="100%">
</p>

<p align="center">
  A <b>text-based RPG</b> for the terminal: pick a class and a weapon, wake up in a dark cave and fight your way out —
  in <b>Portuguese or English</b>, on Windows, Linux and macOS.
</p>

<p align="center">
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white">
  <img alt="C# console" src="https://img.shields.io/badge/C%23-console-239120?logo=csharp&logoColor=white">
  <img alt="No dependencies" src="https://img.shields.io/badge/dependencies-none-2ea44f">
  <img alt="Docker" src="https://img.shields.io/badge/Docker-docker%20run%20--it-2496ED?logo=docker&logoColor=white">
  <img alt="CI" src="https://github.com/rodrigocoelho2/Final-Legacy-V2/actions/workflows/ci.yml/badge.svg?branch=main">
</p>

<p align="center">
  <a href="#then-and-now">Then and now</a> ·
  <a href="#playing">Playing</a> ·
  <a href="#classes-and-weapons">Classes</a> ·
  <a href="#how-combat-works">Combat</a> ·
  <a href="#writing-more-story">More story</a> ·
  <a href="#development">Development</a>
</p>

---

Final Legacy started in 2023 as my secondary-school computer science project: 650 lines of C in one file
([the original](https://github.com/rodrigocoelho2/Final-Legacy), kept as it was in [`Original/`](Original)).
I loved making it, but I was still learning. **V2 keeps the game exactly as it was** — the story, the three classes, the
six weapons and their names, the Atacar / Defender / Fugir battles, the letter-by-letter text and the osteoporosis
joke — and rebuilds everything underneath.

## Then and now

| | 2023 (C) | V2 (.NET 10) |
| --- | --- | --- |
| Runs on | Windows only (`windows.h`, `conio.h`, `system("cls")`) | Windows, Linux, macOS, Docker |
| Language | Portuguese | Portuguese or English, chosen at the start |
| Structure | one 650-line `main` with `goto` | an engine (Library) and a console (Game), with the story and stats in data files |
| Menus | type a number | arrow keys, or a number |
| Stats used in combat | ATK and DEF | all four: AGI and INT finally count |
| Tests | — | 45 unit tests and a full playthrough in CI |

**Bugs fixed on the way** (each one has a test):

- The menu loops were `while (arma != 1 || arma != 2)`, which is always true: the only way out was a `goto`.
- `if (arma = 1)` assigned instead of comparing.
- Choosing *Fugir* always said "Não podes fugir desta batalha", even in a battle you could flee (missing braces).
- Agility and intelligence were shown but never used.
- The Mysterious Shadow and the Stone Golem had exactly the same stats.
- Typing a letter at a menu sent the game into an endless loop (`scanf("%d")` never consumed it).
- The name was read with `scanf("%s")` into 30 characters: a longer name overflowed the buffer, and a space cut it.
- The accents had turned into `�` (the file was saved in the wrong encoding), and a stray `p` on line 53 stopped it
  compiling at all.

## Playing

**Download** — every push to `main` builds a single executable for Windows, Linux and macOS (Apple silicon): open the
latest [CI run](https://github.com/rodrigocoelho2/Final-Legacy-V2/actions/workflows/ci.yml), download
`final-legacy-win-x64` (or yours) and run it. Nothing else to install.

**From the code** (.NET 10 SDK):

```bash
dotnet run --project FinalLegacy.Game
```

**Docker:**

```bash
docker build -f FinalLegacy.Game/Dockerfile -t final-legacy .
docker run -it --rm final-legacy
```

| Keys | |
| --- | --- |
| ↑ ↓ (or W S) | move through a menu |
| Enter / Space | confirm |
| 1, 2, 3… | pick an option at once |
| any key while text is being written | show the rest at once |

| Options | |
| --- | --- |
| `--lang pt` / `--lang en` | skip the language menu |
| `--fast` | text at once instead of letter by letter |
| `--no-color` | no colours (also when `NO_COLOR` is set) |
| `--seed 42` | the same dice every time, to replay a game |

It also plays with piped input — one answer per line, no colours, no animation — which is how CI plays the whole
chapter on every push.

```
  Fighting: Mysterious Shadow · Round 3

  Rodas · Assassin · Blade of Shojin
  HP ████████████████████░░░░  83/100

  Mysterious Shadow
  HP ██░░░░░░░░░░░░░░░░░░░░░░  3/60

  ────────────────────────────────────────────────────────
  The Mysterious Shadow defended (2 blocked).
  You attacked: 14 damage (2 from magic).
  ────────────────────────────────────────────────────────

 › [1] Attack       [2] Defend       [3] Flee (not in this fight)
```

## Classes and weapons

The stats are the original's, to the point.

| Class | ATK | DEF | AGI | INT | Weapons |
| --- | --- | --- | --- | --- | --- |
| **Guerreiro** / Warrior | 4 | 5 | 2 | 2 | Escudo de Dorans (+2 DEF −1 ATK) · Espada de Artúrias (+2 ATK −1 DEF) |
| **Mago** / Mage | 3 | 3 | 1 | 6 | Anel de Dorans (+2 INT −1 ATK) · Bastão Desnecessariamente Grande (+2 INT −1 AGI) |
| **Assassino** / Assassin | 5 | 1 | 5 | 2 | Faca de Dorans (+2 ATK −1 INT) · Lâmina de Shojin (+2 ATK −1 DEF) |

## How combat works

As in the original, every round you **attack**, **defend** or **flee**, and the enemy randomly attacks or defends.
HP starts at 100 in every battle. What's new is that every stat now matters:

- **Attacking** deals 2 × ATK, minus half the enemy's DEF (its armour) — or all of it if the enemy defends.
- **Intelligence** adds INT of magic damage that no armour stops: the Mage's strength.
- **Agility** gives 4 % per point of a **critical hit** (double the physical damage) and 3 % per point of **dodging** a
  hit.
- **Your armour** takes half your DEF off every hit. **Defending** takes all of it off — and a blocked hit sets up a
  **counter-attack**: your next hit is a critical that goes straight through the enemy's guard. The Shield Warrior
  lives on it.
- **Fleeing** works 40 % + 10 % per AGI of the time, when a battle allows it (none in chapter I do, like the original).
- You strike first: an enemy you knock out doesn't hit back.

Every number is in [`Content/rules.json`](FinalLegacy.Library/Content/rules.json).

| Enemy | HP | ATK | DEF | AGI | Attacks |
| --- | --- | --- | --- | --- | --- |
| Esqueleto / Skeleton | 50 | 12 | 2 | 1 | half the time |
| Sombra Misteriosa / Mysterious Shadow | 60 | 17 | 2 | 5 | 60 % — fast, hard to hit |
| Golem de Pedra / Stone Golem | 65 | 15 | 6 | 0 | half the time — slow, armoured |

## Writing more story

The story is data: chapter II needs no new code.

- [`story.json`](FinalLegacy.Library/Content/story.json) — the scenes and how they connect. A scene is a **narration**
  (`text` + `goto`), a **choice** (`text` + `choices`, each with a `goto`, or a `reply` that answers and asks again —
  like "Não" in the cave), a **battle** (`battle: { enemy, canFlee }` + `win`, and `flee` when it can be fled) or an
  **end** (`text` + `end: true`).
- [`pt.json`](FinalLegacy.Library/Content/pt.json) and [`en.json`](FinalLegacy.Library/Content/en.json) — every text,
  by key. `{name}` is the hero's name; `[gold]…[/]`, `[amber]`, `[red]`, `[crimson]`, `[cyan]`, `[violet]`,
  `[green]` and `[dim]` colour it; `[pause]` is the original's slow "...".
- [`rules.json`](FinalLegacy.Library/Content/rules.json) — classes, weapons, enemies and the combat numbers.

`ContentTests` checks that every scene leads somewhere real, every enemy exists, every scene can be reached, both
languages have every text and no tag is misspelled — so a broken chapter fails the build instead of a game.

## Development

```
FinalLegacy.slnx
├── FinalLegacy.Library/        the game without the console (no dependencies)
│   ├── Content/                rules.json, story.json, pt.json, en.json (embedded) and their loader and validator
│   ├── Models/                 Stats, classes, weapons, enemies, the hero
│   ├── Combat/                 Battle (the rules), BattleNarrator (what happened, in words), the dice
│   ├── Story/                  the scene graph
│   ├── Adventures/             Adventure (a playthrough) and IGameUi (what it needs from a screen)
│   ├── Localization/           the texts of a language and their {placeholders}
│   └── Text/                   the [colour] markup
├── FinalLegacy.Game/           the console: ANSI colours, menus, the typewriter text, the battle screen
│   ├── Terminal/               GameTerminal: keys or piped lines, colours, Windows console setup
│   └── Ui/                     TerminalUi (every screen), MenuPrompt, Draw (boxes, bars, wrapping)
├── FinalLegacy.UnitTest/       xUnit: combat with loaded dice, the content, whole playthroughs with a scripted player
└── Original/                   the 2023 C source, untouched
```

The Library knows nothing about the console: `Adventure` asks an `IGameUi` for every choice, so the tests play the
whole story with a scripted player, and a web version later would only need another `IGameUi`.

```bash
dotnet test FinalLegacy.UnitTest
```

GitHub Actions (`.github/workflows/ci.yml`) builds and tests everything, plays the whole chapter in both languages with
piped answers (the transcripts are kept as an artifact), publishes the single-file executables and builds and runs the
Docker image.

## Credits

The weapon names are a nod to League of Legends' items (Doran's, Shojin, the Needlessly Large Rod); Final Legacy is not
affiliated with Riot Games.
