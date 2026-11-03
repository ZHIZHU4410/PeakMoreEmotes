"""Build a self-contained Thunderstore ZIP using the user's installed PEAK references."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import zipfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-dir', default=os.environ.get('PEAK_GAME_DIR'),
                        help='PEAK directory containing PEAK_Data and BepInEx (or set PEAK_GAME_DIR)')
    args = parser.parse_args()
    if not args.game_dir:
        parser.error('--game-dir or PEAK_GAME_DIR is required')
    game = Path(args.game_dir).resolve()
    for reference in ['PEAK_Data/Managed/Assembly-CSharp.dll',
                      'BepInEx/core/BepInEx.dll', 'BepInEx/core/0Harmony.dll']:
        if not (game / reference).is_file():
            parser.error(f'Missing build reference: {game / reference}')
    root = Path(__file__).resolve().parents[1]
    project = root / 'src/PeakMoreEmotes/PeakMoreEmotes.csproj'
    manifest = json.loads((root / 'thunderstore/manifest.json').read_text(encoding='utf-8'))
    version = manifest['version_number']
    # An absolute config path avoids relying on the caller's working directory.
    subprocess.run(['dotnet', 'restore', str(project), '--configfile', str(root / 'NuGet.Config')],
                   cwd=root, check=True)
    subprocess.run(['dotnet', 'build', str(project), '-c', 'Release', '--no-restore',
                    f'-p:GameDir={game.as_posix()}'], cwd=root, check=True)
    plugin = root / 'src/PeakMoreEmotes/bin/Release/netstandard2.1/PeakMoreEmotes.dll'
    files = {
        'manifest.json': root / 'thunderstore/manifest.json',
        'README.md': root / 'thunderstore/README.md',
        'icon.png': root / 'thunderstore/icon.png',
        'CHANGELOG.md': root / 'CHANGELOG.md',
        'LICENSE': root / 'LICENSE',
        'NOTICE': root / 'NOTICE',
        'plugins/PeakMoreEmotes/PeakMoreEmotes.dll': plugin,
        'plugins/PeakMoreEmotes/NOTICE': root / 'NOTICE',
    }
    for name in ['default_dance.bundle', 'twerk.bundle', 'middle_finger.bundle', 'repo_emotes.bundle']:
        files[f'plugins/PeakMoreEmotes/{name}'] = root / 'assets' / name
    for license_path in (root / 'licenses').glob('*.txt'):
        files[f'plugins/PeakMoreEmotes/licenses/{license_path.name}'] = license_path
    assert len(list((root / 'licenses').glob('*.txt'))) == 5, 'Missing third-party licenses'
    for name, source in files.items():
        if not source.is_file():
            raise FileNotFoundError(f'{name}: {source}')
    output = root / 'dist' / f'PeakMoreEmotes-{version}-Thunderstore.zip'
    output.parent.mkdir(parents=True, exist_ok=True)
    # All paths come from the explicit manifest above; no game files are copied.
    with zipfile.ZipFile(output, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name, source in sorted(files.items()):
            archive.write(source, name)
    with zipfile.ZipFile(output) as archive:
        assert archive.testzip() is None, 'ZIP integrity check failed'
    print(f'Created {output} ({output.stat().st_size:,} bytes, {len(files)} files)')


if __name__ == '__main__':
    main()
