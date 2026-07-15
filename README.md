# Coming Soon for Emby

Coming Soon creates one or more Emby home-screen rows containing upcoming movies and TV episodes
from Radarr and Sonarr.

## Supported Emby versions

- Emby Server 4.9.5 Stable
- Emby Server 4.10 Beta

Version 1.1.3 is built against Emby 4.9.5 and tested on Emby 4.10 Beta.

## How it works

1. The scheduled task reads upcoming movies from Radarr and episodes from Sonarr.
2. For movies, TMDB supplies cinema, digital or physical release dates for the country selected
   in each list. Germany is the default. Movies without the selected regional date are omitted;
   Radarr is only used when regional lookup is unavailable.
3. Poster, description and release information are collected and a release badge is added.
4. Every configured list becomes a normal Emby library and can be placed on the home screen.
5. Expired entries are removed after the next successful refresh. If Sonarr or Radarr is briefly
   unavailable, existing entries from that source are kept.

A TMDB API key enables regional movie dates and optional local trailer downloads. Without a TMDB
key, the basic Coming Soon libraries still work and movie dates come from Radarr.

## Installation

1. Download `ComingSoonPlugin.dll` from the latest GitHub release.
2. Copy it into the Emby Server `plugins` folder.
3. Restart Emby Server.
4. Open **Dashboard > Plugins > Coming Soon** and enter the Sonarr/Radarr connection details.
5. Run **Refresh Coming Soon** once under **Scheduled Tasks**.
6. Add the created library or libraries to the desired users' home screens.

## Where files are stored

The generated libraries are stored below:

```text
{Emby plugins folder}/ComingSoon/data/library-<List Name>/
```

Each entry contains its poster, backdrop, NFO metadata and a short placeholder video. When local
trailers are enabled, the trailer is stored in the entry's `trailers` folder.

<sub>Developed with the assistance of AI.</sub>
