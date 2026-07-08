# EternalAudioExtractor

Extracts audio from DOOM Eternal's `.snd` sound containers with human-readable file names, which it recovers by cross-referencing the game's `soundmetadata.bin` and the Wwise soundbanks inside the game's `.pck` packages.

Original tool by proteh. This fork ports it from .NET Framework to .NET 8 so it builds and runs natively on Linux, and adds Linux-friendly conversion tooling.

## Usage

```
EternalAudioExtractor <path to .snd file> <dir containing soundmetadata.bin and .pck files> <output folder> [options]

OPTIONS:
    -c - Converts .wem files to .ogg automatically (will increase extraction time)
    -u - Extract unused sounds (the name of these sound files will be their sound id)
```

Extracted files come in two formats: `.opus` files are standard Ogg Opus and play everywhere as-is; `.wem` files are Wwise Vorbis and need the `-c` flag to become playable `.ogg` files. Most music is `.wem`, so use `-c` when extracting music.

## Extracting music on Linux (Steam install)

### 1. Install the .NET 8 SDK

Use your distro's package if available, or the official install script (no root needed):

```bash
curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0
export PATH="$HOME/.dotnet:$PATH" DOTNET_ROOT="$HOME/.dotnet"
```

### 2. Build the extractor

From the repository root:

```bash
dotnet publish EternalAudioExtractor/EternalAudioExtractor.csproj -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

This produces a standalone binary at `EternalAudioExtractor/bin/Release/net8.0/linux-x64/publish/EternalAudioExtractor` that runs without any .NET environment setup.

### 3. Build the conversion utilities (needed for `-c`)

The `-c` flag shells out to two helpers looked up in `./utils/` **relative to the directory you run the extractor from**, so build them into `utils/` at the repository root:

```bash
# ww2ogg (Wwise .wem -> Ogg Vorbis) — self-contained C++, no dependencies
git clone https://github.com/hcs64/ww2ogg
make -C ww2ogg
mkdir -p utils
cp ww2ogg/ww2ogg ww2ogg/packed_codebooks_aoTuV_603.bin utils/

# revorb (fixes granule positions so the .ogg files get correct duration/seeking)
sudo apt install libogg-dev libvorbis-dev        # Debian/Ubuntu
gcc -O2 -Wall -o utils/revorb utils-src/revorb.c -lvorbis -logg
```

`utils-src/` also contains `oggcheck.c`, a small validator that fully decodes an `.ogg` file and prints its sample count — handy for sanity checks (`gcc -O2 -Wall -o oggcheck utils-src/oggcheck.c -lvorbisfile`).

### 4. Extract the music

The game's sound data lives inside your Steam library under
`steamapps/common/DOOMEternal/base/sound/soundbanks/pc/` — that one directory holds the `.snd` containers, the `.pck` packages, and `soundmetadata.bin`. For a default Steam install:

```bash
SND=~/.local/share/Steam/steamapps/common/DOOMEternal/base/sound/soundbanks/pc

for f in music.snd music_patch_1.snd music_patch_2.snd; do
    ./EternalAudioExtractor/bin/Release/net8.0/linux-x64/publish/EternalAudioExtractor "$SND/$f" "$SND" output -c
done
```

Each container is extracted to `output/<container name>/`, e.g.:

```
output/music/bloodharvester_music_main_ambient_0_id#848038585.ogg
output/music/dlc1_maykr_city_music_main_light_11_id#908923261.ogg
...
```

Notes:

- `music.snd` is ~1.8 GB; extracting and converting it takes a few minutes and yields roughly 1,000 tracks (~1.3 GB).
- Run the command from the repository root so `./utils/` is found.
- Sound effects and voice lines work the same way — point it at `sfx*.snd` or `vo_English(US).snd` (quote the VO paths, they contain parentheses). VO is mostly `.opus`, so `-c` is not needed there.
- If a `.wem` fails to convert, it is kept as `.wem` instead of being lost.
- File names end in `_id#<sound id>`; sounds the game never references by name are skipped unless you pass `-u`.

## Building on Windows

The project is a normal .NET 8 console app: `dotnet build -c Release` (or open the solution in Visual Studio 2022+). The `-c` flag expects `utils\ww2ogg.exe`, `utils\revorb.exe` and `utils\packed_codebooks_aoTuV_603.bin` next to your working directory, as in the original tool.
