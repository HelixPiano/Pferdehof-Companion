# PferdehofGUI

A desktop companion tool for **Pferdehof** — view, edit, and manage your horses, players, and breeding decisions directly from your save files.

Built with [Avalonia](https://avaloniaui.net/) and .NET, running on Windows.

> ⚠️ **This tool edits your save files directly.** Back up your `dat` folder before using it. See [Backing up your saves](#backing-up-your-saves) below.

---

## Which game is this for?

This tool is built for the **PC version** of the game known under several regional names:

| Region | Title |
|---|---|
| German (original) | *Pferd & Pony – Mein Pferdehof* |
| English | *My Pony Stable* (also sold as *My Horse Farm*) |
| Dutch | *Paard & Pony – Mijn manege* |
| French | *Cheval & Poney – Mon centre équestre Manager* |
| Game Boy Advance | *Let's Ride! Sunshine Stables* (different platform/version — not what this tool targets) |

If your save files decrypt using the XOR key and Lingo property-list format described below, you're almost certainly running a compatible PC edition, regardless of which of the names above is on your box.

---

## Features

1. **Edit Horse** — browse every horse in your save (filterable by *Mine*, *Breeder*, or *Offline*), and edit individual stats, genetics, equipment, and more, with built-in validation so you can't save invalid values.
2. **Breeder Support** — calculates the best stallion/mare pairing based on whichever stats you choose to weigh (health, power, condition, flexibility, adrenalin, weight), across three modes: any eligible horse, your mare + a breeder stallion, or only your own horses. Includes a live overview of all your horses and their scores.
3. **Foal Checker** — lists foals you own and shows how their stats rolled compared to their parents' values.
4. **My Horses — Overview** — a read-only summary table of every horse you own: name, sex, life stage, pregnancy status, value, and all six core stats.
5. **Repair Horse List** — diagnoses and fixes three specific consistency problems between your player file and your horse files (see [Why a repair tool?](#why-a-repair-tool) below).

---

## Getting started

### 1. Find your `dat` folder

This is the folder literally named `dat` inside Pferdehof's `data` folder, containing `players` and `horses` subfolders.

**Best long-term fix — install the game somewhere you have write permissions**, like your Desktop or a folder directly under your user account (e.g. `C:\Users\<YourUsername>\Games\Pferdehof`). Avoid `C:\Program Files\` and `C:\Program Files (x86)\` entirely. Modern Windows restricts normal write access to those folders, and older games like this one that don't expect that restriction can end up with their save data silently duplicated and redirected (see below) — installing outside them avoids the problem altogether, with exactly one copy of your save data, always where you'd expect it.

**If it's already installed under `Program Files (x86)`**, Windows almost certainly redirects its actual save data elsewhere via **UAC File Virtualization** — a compatibility feature for older programs that predate modern Windows permission rules. Check here first:

```
C:\Users\<YourUsername>\AppData\Local\VirtualStore\Program Files (x86)\Pferdehof\data\dat\
```

If that folder exists and has recently-modified files in `players`/`horses`, that's your real save data — not the copy sitting directly under `Program Files (x86)`, which is likely a frozen, stale copy from install time. Point the app at whichever `dat` folder has the most recently modified files.

### 2. Select your player

The app decrypts every file in `dat/players` and lists the player names it finds. Pick yours (or switch between players anytime via the main menu).

### 3. Pick a feature from the main menu

Your folder and player selection are remembered for next time.

---

## Building from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) (targeting `net10.0`).

```bash
dotnet restore
dotnet build
dotnet run
```

To build a standalone, self-contained executable (no .NET install required on the target machine):

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## Why a repair tool?

Pferdehof tracks a horse's ownership in **three separate places** that all have to agree:

1. The horse's own file (`#owner` field)
2. The player file's `#horses` array (the master ownership list)
3. The player file's `#boxes` array (which stall, if any, the horse is physically placed in)

If any one of these three drifts out of sync with the other two — which can happen through normal play, or through editing — a horse can become invisible in-game (or in this app) even though nothing about the horse file itself is "broken." The Repair tool checks all three combinations and can fix each independently:

- **Missing from the ownership list** — correctly owned and boxed, but absent from `#horses`.
- **Not placed in a stall box** — correctly owned and listed, but not sitting in any of the 5 stable boxes.
- **Boxed but not owned by you** — physically in one of your stalls, but the horse's own file says it belongs to someone else (or no one).

---

## Backing up your saves

Before making any edits, copy your entire `dat` folder somewhere safe. Since this tool works directly with the game's real save format, a mistake (or an edge case in a save file this tool hasn't seen before) could leave a horse or player file in a state the game can't read. Keeping a backup means you can always recover.

**Also close Pferdehof completely before editing anything.** If the game is running while you edit a save file, it may overwrite your changes when it next saves, or you may be looking at data that's already out of date.

---

## Known limitations

- The `#auto` (horse trailer) stall slot isn't handled by the Repair tool — only the 5 numbered stable boxes are. `#auto` also appears elsewhere in player files as a building ID, and a text-based fix can't safely tell the two apart.
- A few numeric fields (`genetics.eyes`, `genetics.ears`, `genetics.PatternFactor`, horse `weight`) are validated as non-negative numbers only — there wasn't enough variation across real save data to confidently infer their true upper bounds, so no artificial cap is enforced.
- Equipment item lists (saddle, halter, blanket, pad variants) are based on what could be found in the game's own data tables and may not be fully exhaustive.

---

## Disclaimer

This is an unofficial, third-party tool. It is not affiliated with or endorsed by the developers or publishers of Pferdehof. Use at your own risk, and always keep a backup of your save data.