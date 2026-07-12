define(["emby-input", "emby-button", "emby-checkbox", "emby-select"], function () {
    "use strict";

    var pluginId = "6f2a9e2b-3c1d-4b7e-9a6a-1f0d8c5e2b71";

    // Release notes are loaded from the server (/ComingSoon/Changelog, backed by the embedded
    // changelog.json) rather than hardcoded here, so they stay current even when the browser
    // still has an older cached copy of this file. Edit changelog.json to update them.
    var changelogCache = null;

    var text = {
        English: {
            pageDescription: "Pulls upcoming releases from Sonarr/Radarr and shows them as one or more \"Coming Soon\" rows.",
            infoButton: "ℹ️ Info",
            logsButton: "📋 Logs",
            readmeButton: "📖 Readme",
            readmeHeading: "📖 How it works",
            readmeHowHeading: "What the plugin does",
            readmeHowText: "The scheduled task reads upcoming movies from Radarr and episodes from Sonarr. It creates one normal Emby library for every configured list and refreshes it automatically.",
            readmeStorageHeading: "Where files are stored",
            readmeStorageText: "Each entry is stored inside the plugin data folder with its poster, backdrop, NFO file, short placeholder video and, when enabled, a local trailer. Removing an expired entry removes its folder on the next successful refresh.",
            readmeVersionsHeading: "Supported Emby versions",
            readmeVersionsText: "Emby Server 4.9.5 Stable and Emby Server 4.10 Beta. Built against 4.9.5 and tested on 4.10 Beta.",
            daysAheadLabel: "Days ahead to show",
            daysAheadDescription: "How far into the future to pull upcoming releases from the Sonarr/Radarr calendars (up to 365 days).",
            sonarrSummary: "Sonarr",
            radarrSummary: "Radarr",
            enabledLabel: "Enabled",
            sonarrEnabledDescription: "Turn on to pull upcoming TV episodes from Sonarr.",
            radarrEnabledDescription: "Turn on to pull upcoming movies from Radarr.",
            serverUrlLabel: "Server URL",
            sonarrUrlDescription: "Base URL of your Sonarr server, e.g. http://localhost:8989.",
            radarrUrlDescription: "Base URL of your Radarr server, e.g. http://localhost:7878.",
            apiKeyLabel: "API Key",
            sonarrApiKeyDescription: "Found in Sonarr under Settings > General > Security > API Key.",
            radarrApiKeyDescription: "Found in Radarr under Settings > General > Security > API Key.",
            testConnection: "Test Connection",
            listsSummary: "Lists",
            listsDescription: "Each list becomes its own Emby library, shown as its own row on the home screen. A title can appear in more than one list.",
            addListButton: "+ Add List",
            listNameLabel: "List name",
            includeMoviesLabel: "Include movies",
            includeTvLabel: "Include TV shows",
            movieDateLabel: "Movie date",
            movieDateOptions: { Digital: "Digital Release", Physical: "Physical Release", InCinemas: "In Cinemas", Earliest: "Earliest of all three" },
            removeButton: "Remove",
            tmdbSummary: "TMDB & Trailers",
            tmdbApiKeyLabel: "API Key (optional - only needed for local trailer download)",
            tmdbApiKeyDescription: "Overview and poster still work without this, sourced from Emby's own provider / Sonarr / Radarr. Free key at themoviedb.org/settings/api.",
            downloadTrailersLabel: "Download trailers locally (recommended for Apple TV / native clients)",
            downloadTrailersDescription: "Downloads the trailer as a real file so it plays inside Emby instead of opening YouTube. Needs a TMDB API key.",
            ytDlpPathLabel: "yt-dlp path",
            ytDlpDescription: "Default \"yt-dlp\" tries PATH first, then auto-downloads a standalone copy into the plugin's own data folder if not found - works regardless of which Docker image is used.",
            ffmpegPathLabel: "ffmpeg path (optional)",
            ffmpegDescription: "Leave empty to download a ready-to-play trailer (up to 720p) with no ffmpeg needed. Set a path to a modern ffmpeg only if you want higher-resolution merged trailers.",
            languageSummary: "Appearance",
            languageSelectLabel: "Badge text & description language",
            languageDescription: "Affects poster badge text, the release-date caption, and fetched overview text.",
            badgePositionLabel: "Badge position",
            badgePositionDescription: "Which corner the status badge sits in. The release date always stays bottom-right; if the badge shares that corner it sits above the date.",
            badgePosOptions: { TopLeft: "Top left", TopRight: "Top right", BottomLeft: "Bottom left", BottomRight: "Bottom right" },
            saveButton: "Save",
            testingText: "Testing…",
            unknownResult: "Unknown result",
            saveFailedPrefix: "Save failed: ",
            infoHeading: "ℹ️ Coming Soon",
            releaseNotesHeading: "Release Notes",
            tmdbHeading: "TMDB API Key",
            tmdbInfo: "Optional. Only used to download trailers locally via yt-dlp (so playback stays " +
                "inside Emby instead of opening YouTube), and as a fallback for the overview/poster if " +
                "Emby's own metadata provider isn't configured. Cast/crew is normally fetched by Emby " +
                "itself once an item is scanned.",
            close: "Close",
            logHeading: "📋 Coming Soon Logs",
            logDescription: "Recent log lines for this plugin, newest at the bottom.",
            refresh: "Refresh",
            openInNewTab: "🗗 Open in new tab",
            loadingLogs: "Loading…",
            noLogs: "No log lines yet - run a refresh first.",
            logsFailedPrefix: "Failed to load logs: "
        },
        German: {
            pageDescription: "Holt kommende Veröffentlichungen von Sonarr/Radarr und zeigt sie als eine oder mehrere \"Coming Soon\"-Zeilen an.",
            infoButton: "ℹ️ Info",
            logsButton: "📋 Logs",
            readmeButton: "📖 Readme",
            readmeHeading: "📖 So funktioniert es",
            readmeHowHeading: "Was das Plugin macht",
            readmeHowText: "Die geplante Aufgabe liest kommende Filme aus Radarr und Episoden aus Sonarr. Für jede eingerichtete Liste wird eine normale Emby-Bibliothek erstellt und automatisch aktualisiert.",
            readmeStorageHeading: "Wo die Dateien gespeichert werden",
            readmeStorageText: "Jeder Eintrag liegt im Datenordner des Plugins – mit Poster, Hintergrundbild, NFO-Datei, kurzem Platzhaltervideo und, falls aktiviert, einem lokalen Trailer. Abgelaufene Einträge werden nach der nächsten erfolgreichen Aktualisierung entfernt.",
            readmeVersionsHeading: "Unterstützte Emby-Versionen",
            readmeVersionsText: "Emby Server 4.9.5 Stable und Emby Server 4.10 Beta. Gebaut gegen 4.9.5 und getestet auf 4.10 Beta.",
            daysAheadLabel: "Tage im Voraus anzeigen",
            daysAheadDescription: "Wie weit in die Zukunft kommende Veröffentlichungen aus den Sonarr/Radarr-Kalendern geholt werden (bis zu 365 Tage).",
            sonarrSummary: "Sonarr",
            radarrSummary: "Radarr",
            enabledLabel: "Aktiviert",
            sonarrEnabledDescription: "Aktivieren, um kommende Serien-Episoden aus Sonarr zu holen.",
            radarrEnabledDescription: "Aktivieren, um kommende Filme aus Radarr zu holen.",
            serverUrlLabel: "Server-URL",
            sonarrUrlDescription: "Basis-URL deines Sonarr-Servers, z.B. http://localhost:8989.",
            radarrUrlDescription: "Basis-URL deines Radarr-Servers, z.B. http://localhost:7878.",
            apiKeyLabel: "API-Schlüssel",
            sonarrApiKeyDescription: "Zu finden in Sonarr unter Einstellungen > Allgemein > Sicherheit > API-Schlüssel.",
            radarrApiKeyDescription: "Zu finden in Radarr unter Einstellungen > Allgemein > Sicherheit > API-Schlüssel.",
            testConnection: "Verbindung testen",
            listsSummary: "Listen",
            listsDescription: "Jede Liste wird zu einer eigenen Emby-Bibliothek und erscheint als eigene Zeile auf der Startseite. Ein Titel kann in mehreren Listen vorkommen.",
            addListButton: "+ Liste hinzufügen",
            listNameLabel: "Listenname",
            includeMoviesLabel: "Filme einschließen",
            includeTvLabel: "Serien einschließen",
            movieDateLabel: "Filmdatum",
            movieDateOptions: { Digital: "Digitale Veröffentlichung", Physical: "Physische Veröffentlichung", InCinemas: "Kinostart", Earliest: "Frühestes aller drei" },
            removeButton: "Entfernen",
            tmdbSummary: "TMDB & Trailer",
            tmdbApiKeyLabel: "API-Schlüssel (optional - nur für lokalen Trailer-Download nötig)",
            tmdbApiKeyDescription: "Beschreibung und Poster funktionieren auch ohne diesen Schlüssel, über Emby's eigenen Provider / Sonarr / Radarr. Kostenloser Schlüssel unter themoviedb.org/settings/api.",
            downloadTrailersLabel: "Trailer lokal herunterladen (empfohlen für Apple TV / native Clients)",
            downloadTrailersDescription: "Lädt den Trailer als echte Datei herunter, damit er in Emby abgespielt wird statt YouTube zu öffnen. Benötigt einen TMDB-API-Schlüssel.",
            ytDlpPathLabel: "yt-dlp-Pfad",
            ytDlpDescription: "Standard \"yt-dlp\" versucht zuerst PATH, lädt bei Bedarf automatisch eine eigene Kopie in den Plugin-Datenordner herunter - funktioniert unabhängig vom verwendeten Docker-Image.",
            ffmpegPathLabel: "ffmpeg-Pfad (optional)",
            ffmpegDescription: "Leer lassen, um einen fertig abspielbaren Trailer (bis 720p) ohne ffmpeg herunterzuladen. Pfad zu einem modernen ffmpeg nur setzen, wenn höher aufgelöste, zusammengeführte Trailer gewünscht sind.",
            languageSummary: "Darstellung",
            languageSelectLabel: "Sprache für Badge-Text & Beschreibung",
            languageDescription: "Betrifft den Poster-Badge-Text, die Datumsanzeige und die abgerufene Beschreibung.",
            badgePositionLabel: "Badge-Position",
            badgePositionDescription: "In welcher Ecke der Status-Badge sitzt. Das Datum bleibt immer unten rechts; teilt sich der Badge diese Ecke, sitzt er über dem Datum.",
            badgePosOptions: { TopLeft: "Oben links", TopRight: "Oben rechts", BottomLeft: "Unten links", BottomRight: "Unten rechts" },
            saveButton: "Speichern",
            testingText: "Teste…",
            unknownResult: "Unbekanntes Ergebnis",
            saveFailedPrefix: "Speichern fehlgeschlagen: ",
            infoHeading: "ℹ️ Coming Soon",
            releaseNotesHeading: "Versionshinweise",
            tmdbHeading: "TMDB API-Schlüssel",
            tmdbInfo: "Optional. Wird nur für den lokalen Trailer-Download über yt-dlp benötigt (damit " +
                "die Wiedergabe in Emby bleibt statt YouTube zu öffnen), sowie als Fallback für " +
                "Beschreibung/Poster, falls Emby's eigener Metadaten-Provider nicht konfiguriert ist. " +
                "Cast/Crew werden normalerweise von Emby selbst beim Scannen geladen.",
            close: "Schließen",
            logHeading: "📋 Coming Soon Logs",
            logDescription: "Letzte Log-Zeilen dieses Plugins, neueste unten.",
            refresh: "Aktualisieren",
            openInNewTab: "🗗 In neuem Tab öffnen",
            loadingLogs: "Lädt…",
            noLogs: "Noch keine Log-Einträge - erst einen Refresh ausführen.",
            logsFailedPrefix: "Logs konnten nicht geladen werden: "
        }
    };

    return function (view) {
        var currentLanguage = "English";
        var currentLists = [];

        function t() {
            return text[currentLanguage] || text.English;
        }

        // --- Static text (everything outside the dynamic list rows) ---

        function applyStaticText() {
            var s = t();

            view.querySelector('#CsPageDescription').textContent = s.pageDescription;
            view.querySelector('#BtnInfo').textContent = s.infoButton;
            view.querySelector('#BtnLogs').textContent = s.logsButton;
            view.querySelector('#BtnReadme').textContent = s.readmeButton;

            view.querySelector('#DaysAhead').setAttribute('label', s.daysAheadLabel);
            view.querySelector('#CsDaysAheadDescription').textContent = s.daysAheadDescription;

            view.querySelector('#CsSonarrSummary').textContent = s.sonarrSummary;
            view.querySelector('#CsRadarrSummary').textContent = s.radarrSummary;
            view.querySelector('#CsSonarrEnabledLabel').textContent = s.enabledLabel;
            view.querySelector('#CsRadarrEnabledLabel').textContent = s.enabledLabel;
            view.querySelector('#CsSonarrEnabledDescription').textContent = s.sonarrEnabledDescription;
            view.querySelector('#CsRadarrEnabledDescription').textContent = s.radarrEnabledDescription;
            view.querySelector('#SonarrUrl').setAttribute('label', s.serverUrlLabel);
            view.querySelector('#CsSonarrUrlDescription').textContent = s.sonarrUrlDescription;
            view.querySelector('#RadarrUrl').setAttribute('label', s.serverUrlLabel);
            view.querySelector('#CsRadarrUrlDescription').textContent = s.radarrUrlDescription;
            view.querySelector('#SonarrApiKey').setAttribute('label', s.apiKeyLabel);
            view.querySelector('#CsSonarrApiKeyDescription').textContent = s.sonarrApiKeyDescription;
            view.querySelector('#RadarrApiKey').setAttribute('label', s.apiKeyLabel);
            view.querySelector('#CsRadarrApiKeyDescription').textContent = s.radarrApiKeyDescription;
            view.querySelector('#TestSonarr').textContent = s.testConnection;
            view.querySelector('#TestRadarr').textContent = s.testConnection;

            view.querySelector('#CsListsSummary').textContent = s.listsSummary;
            view.querySelector('#CsListsDescription').textContent = s.listsDescription;
            view.querySelector('#BtnAddList').textContent = s.addListButton;

            view.querySelector('#CsTmdbSummary').textContent = s.tmdbSummary;
            view.querySelector('#TmdbApiKey').setAttribute('label', s.tmdbApiKeyLabel);
            view.querySelector('#CsTmdbApiKeyDescription').textContent = s.tmdbApiKeyDescription;
            view.querySelector('#CsDownloadTrailersLabel').textContent = s.downloadTrailersLabel;
            view.querySelector('#CsDownloadTrailersDescription').textContent = s.downloadTrailersDescription;
            view.querySelector('#YtDlpPath').setAttribute('label', s.ytDlpPathLabel);
            view.querySelector('#CsYtDlpDescription').textContent = s.ytDlpDescription;
            view.querySelector('#FfmpegPath').setAttribute('label', s.ffmpegPathLabel);
            view.querySelector('#CsFfmpegDescription').textContent = s.ffmpegDescription;

            view.querySelector('#CsLanguageSummary').textContent = s.languageSummary;
            view.querySelector('#Language').setAttribute('label', s.languageSelectLabel);
            view.querySelector('#CsLanguageDescription').textContent = s.languageDescription;

            var badgeSel = view.querySelector('#BadgePosition');
            badgeSel.setAttribute('label', s.badgePositionLabel);
            Array.prototype.forEach.call(badgeSel.options, function (opt) {
                opt.textContent = s.badgePosOptions[opt.value] || opt.value;
            });
            view.querySelector('#CsBadgePositionDescription').textContent = s.badgePositionDescription;

            view.querySelector('#CsSaveButtonLabel').textContent = s.saveButton;

            view.querySelector('#CsInfoHeading').textContent = s.infoHeading;
            view.querySelector('#BtnCloseInfo').textContent = s.close;

            view.querySelector('#CsReadmeHeading').textContent = s.readmeHeading;
            view.querySelector('#CsReadmeHowHeading').textContent = s.readmeHowHeading;
            view.querySelector('#CsReadmeHowText').textContent = s.readmeHowText;
            view.querySelector('#CsReadmeStorageHeading').textContent = s.readmeStorageHeading;
            view.querySelector('#CsReadmeStorageText').textContent = s.readmeStorageText;
            view.querySelector('#CsReadmeVersionsHeading').textContent = s.readmeVersionsHeading;
            view.querySelector('#CsReadmeVersionsText').textContent = s.readmeVersionsText;
            view.querySelector('#BtnCloseReadme').textContent = s.close;

            view.querySelector('#CsLogHeading').textContent = s.logHeading;
            view.querySelector('#CsLogDescription').textContent = s.logDescription;
            view.querySelector('#BtnRefreshLogs').textContent = s.refresh;
            view.querySelector('#BtnOpenLogsNewTab').textContent = s.openInNewTab;
            view.querySelector('#BtnCloseLogs').textContent = s.close;
        }

        // --- Info modal ---

        function openReadmeModal() {
            view.querySelector('#CsReadmeModalBackdrop').classList.add('open');
        }

        function closeReadmeModal() {
            view.querySelector('#CsReadmeModalBackdrop').classList.remove('open');
        }

        function renderChangelog(entries) {
            var container = view.querySelector('#CsReleaseNotes');
            container.innerHTML = '';
            // Only the newest version's notes are shown.
            entries.slice(0, 1).forEach(function (entry) {
                var block = document.createElement('div');
                block.className = 'csVersionBlock';

                var versionLine = document.createElement('div');
                versionLine.className = 'csVersionNumber';
                versionLine.textContent = entry.Version || entry.version;
                block.appendChild(versionLine);

                var list = document.createElement('ul');
                (entry.Notes || entry.notes || []).forEach(function (note) {
                    var li = document.createElement('li');
                    li.textContent = note;
                    list.appendChild(li);
                });
                block.appendChild(list);

                container.appendChild(block);
            });
        }

        function renderInfoModal() {
            var s = t();
            view.querySelector('#CsReleaseNotesHeading').textContent = s.releaseNotesHeading;
            view.querySelector('#CsTmdbInfoHeading').textContent = s.tmdbHeading;
            view.querySelector('#CsTmdbInfoText').textContent = s.tmdbInfo;

            if (changelogCache) {
                renderChangelog(changelogCache);
                return;
            }

            ApiClient.ajax({
                type: 'GET',
                url: ApiClient.getUrl('ComingSoon/Changelog'),
                dataType: 'json'
            }).then(function (entries) {
                changelogCache = entries || [];
                renderChangelog(changelogCache);
            }).catch(function () {
                view.querySelector('#CsReleaseNotes').textContent = '';
            });
        }

        function openInfoModal() {
            renderInfoModal();
            view.querySelector('#CsInfoModalBackdrop').classList.add('open');
        }

        function closeInfoModal() {
            view.querySelector('#CsInfoModalBackdrop').classList.remove('open');
        }

        // --- Log modal ---

        function loadLogs() {
            var s = t();
            var box = view.querySelector('#CsLogBox');
            box.textContent = s.loadingLogs;

            ApiClient.ajax({
                type: 'GET',
                url: ApiClient.getUrl('ComingSoon/Logs', { MaxLines: 300 }),
                dataType: 'json'
            }).then(function (result) {
                var s2 = t();
                box.textContent = (result && result.Text) ? result.Text : s2.noLogs;
                box.scrollTop = box.scrollHeight;
            }).catch(function (err) {
                describeError(err).then(function (msg) {
                    box.textContent = t().logsFailedPrefix + msg;
                });
            });
        }

        function openLogModal() {
            var s = t();
            view.querySelector('#CsLogHeading').textContent = s.logHeading;
            view.querySelector('#CsLogDescription').textContent = s.logDescription;
            view.querySelector('#BtnRefreshLogs').textContent = s.refresh;
            view.querySelector('#BtnOpenLogsNewTab').textContent = s.openInNewTab;
            view.querySelector('#BtnCloseLogs').textContent = s.close;
            view.querySelector('#CsLogModalBackdrop').classList.add('open');
            loadLogs();
        }

        function closeLogModal() {
            view.querySelector('#CsLogModalBackdrop').classList.remove('open');
        }

        function escapeHtml(str) {
            return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
        }

        // Renders the current log text into a standalone page in a new tab (via a Blob URL, no
        // server round-trip) so it's easier to read/search full-screen than the modal's fixed-
        // height box.
        function openLogsInNewTab() {
            var text = view.querySelector('#CsLogBox').textContent;
            var html = '<pre style="white-space:pre-wrap;word-break:break-word;' +
                'font-family:monospace;font-size:14px;line-height:1.4;background:#000;color:#ddd;' +
                'padding:1em;margin:0;">' + escapeHtml(text) + '</pre>';
            var blob = new Blob([html], { type: 'text/html' });
            var url = URL.createObjectURL(blob);
            window.open(url, '_blank');
        }

        // --- Dynamic list editor ---
        // Mirrors the createElement(tag, {is: ...}) pattern used elsewhere for Emby's custom
        // elements (emby-checkbox/emby-select) rather than innerHTML strings, so dynamically
        // inserted rows get upgraded the same way as elements present at initial page load.

        function defaultList() {
            return { Name: "Coming Soon", IncludeMovies: true, MovieDateType: "Digital", IncludeTvShows: true };
        }

        function buildDateTypeSelect(value) {
            var s = t();
            var select = document.createElement("select", { is: "emby-select" });
            select.setAttribute("label", s.movieDateLabel);
            ["Digital", "Physical", "InCinemas", "Earliest"].forEach(function (key) {
                var opt = document.createElement("option");
                opt.value = key;
                opt.textContent = s.movieDateOptions[key];
                select.appendChild(opt);
            });
            select.value = value;
            return select;
        }

        function renderLists() {
            var s = t();
            var container = view.querySelector('#ListsContainer');
            container.innerHTML = '';

            currentLists.forEach(function (list, index) {
                var row = document.createElement('div');
                row.className = 'csListRow';

                var nameWrap = document.createElement('div');
                nameWrap.className = 'inputContainer';
                var nameInput = document.createElement('input', { is: 'emby-input' });
                nameInput.type = 'text';
                nameInput.setAttribute('label', s.listNameLabel);
                nameInput.value = list.Name;
                nameInput.addEventListener('input', function () { list.Name = nameInput.value; });
                nameWrap.appendChild(nameInput);
                row.appendChild(nameWrap);

                var moviesLabel = document.createElement('label');
                moviesLabel.className = 'emby-checkbox-label';
                var moviesCheckbox = document.createElement('input', { is: 'emby-checkbox' });
                moviesCheckbox.type = 'checkbox';
                moviesCheckbox.checked = list.IncludeMovies;
                var moviesSpan = document.createElement('span');
                moviesSpan.textContent = s.includeMoviesLabel;
                moviesLabel.appendChild(moviesCheckbox);
                moviesLabel.appendChild(moviesSpan);
                row.appendChild(moviesLabel);

                var dateWrap = document.createElement('div');
                dateWrap.className = 'selectContainer';
                dateWrap.style.display = list.IncludeMovies ? '' : 'none';
                var dateSelect = buildDateTypeSelect(list.MovieDateType);
                dateSelect.addEventListener('change', function () { list.MovieDateType = dateSelect.value; });
                dateWrap.appendChild(dateSelect);
                row.appendChild(dateWrap);

                moviesCheckbox.addEventListener('change', function () {
                    list.IncludeMovies = moviesCheckbox.checked;
                    dateWrap.style.display = list.IncludeMovies ? '' : 'none';
                });

                var tvLabel = document.createElement('label');
                tvLabel.className = 'emby-checkbox-label';
                var tvCheckbox = document.createElement('input', { is: 'emby-checkbox' });
                tvCheckbox.type = 'checkbox';
                tvCheckbox.checked = list.IncludeTvShows;
                tvCheckbox.addEventListener('change', function () { list.IncludeTvShows = tvCheckbox.checked; });
                var tvSpan = document.createElement('span');
                tvSpan.textContent = s.includeTvLabel;
                tvLabel.appendChild(tvCheckbox);
                tvLabel.appendChild(tvSpan);
                row.appendChild(tvLabel);

                var removeBtn = document.createElement('button', { is: 'emby-button' });
                removeBtn.type = 'button';
                removeBtn.className = 'raised';
                removeBtn.style.marginTop = '0.5em';
                removeBtn.textContent = s.removeButton;
                removeBtn.disabled = currentLists.length <= 1;
                removeBtn.addEventListener('click', function () {
                    currentLists.splice(index, 1);
                    renderLists();
                });
                row.appendChild(removeBtn);

                container.appendChild(row);
            });
        }

        function addList() {
            currentLists.push(defaultList());
            renderLists();
        }

        // --- Load/save ---

        function loadConfig() {
            ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                currentLanguage = config.Language || 'English';
                view.querySelector('#Language').value = currentLanguage;
                view.querySelector('#BadgePosition').value = config.BadgePosition || 'TopLeft';
                applyStaticText();

                view.querySelector('#SonarrEnabled').checked = config.SonarrEnabled;
                view.querySelector('#SonarrUrl').value = config.SonarrUrl || '';
                view.querySelector('#SonarrApiKey').value = config.SonarrApiKey || '';

                view.querySelector('#RadarrEnabled').checked = config.RadarrEnabled;
                view.querySelector('#RadarrUrl').value = config.RadarrUrl || '';
                view.querySelector('#RadarrApiKey').value = config.RadarrApiKey || '';

                view.querySelector('#TmdbApiKey').value = config.TmdbApiKey || '';

                view.querySelector('#DaysAhead').value = config.DaysAhead;
                view.querySelector('#DownloadTrailersLocally').checked = config.DownloadTrailersLocally;
                view.querySelector('#YtDlpPath').value = config.YtDlpPath || 'yt-dlp';
                view.querySelector('#FfmpegPath').value = config.FfmpegPath || '';

                // Deduplicate by name so any duplicates left by the old config-growth bug are
                // cleaned up here and saved away on the next Save.
                var seenNames = {};
                currentLists = (config.Lists && config.Lists.length ? config.Lists : [defaultList()]).map(function (l) {
                    return {
                        Name: l.Name || 'Coming Soon',
                        IncludeMovies: l.IncludeMovies !== false,
                        MovieDateType: l.MovieDateType || 'Digital',
                        IncludeTvShows: l.IncludeTvShows !== false
                    };
                }).filter(function (l) {
                    var key = l.Name.trim().toLowerCase();
                    if (seenNames[key]) return false;
                    seenNames[key] = true;
                    return true;
                });
                renderLists();
            });
        }

        function buildConfigFromForm(config) {
            config.SonarrEnabled = view.querySelector('#SonarrEnabled').checked;
            config.SonarrUrl = view.querySelector('#SonarrUrl').value;
            config.SonarrApiKey = view.querySelector('#SonarrApiKey').value;

            config.RadarrEnabled = view.querySelector('#RadarrEnabled').checked;
            config.RadarrUrl = view.querySelector('#RadarrUrl').value;
            config.RadarrApiKey = view.querySelector('#RadarrApiKey').value;

            config.TmdbApiKey = view.querySelector('#TmdbApiKey').value;
            config.Language = view.querySelector('#Language').value;
            config.BadgePosition = view.querySelector('#BadgePosition').value;

            config.DaysAhead = parseInt(view.querySelector('#DaysAhead').value, 10);
            config.DownloadTrailersLocally = view.querySelector('#DownloadTrailersLocally').checked;
            config.YtDlpPath = view.querySelector('#YtDlpPath').value;
            config.FfmpegPath = view.querySelector('#FfmpegPath').value;

            config.Lists = currentLists.map(function (l) {
                return {
                    Name: (l.Name || 'Coming Soon').trim() || 'Coming Soon',
                    IncludeMovies: l.IncludeMovies,
                    MovieDateType: l.MovieDateType,
                    IncludeTvShows: l.IncludeTvShows
                };
            });
            return config;
        }

        // Emby's ApiClient rejects with a fetch Response object on HTTP errors; that
        // stringifies to "[object Response]", so extract a readable message instead.
        function describeError(err) {
            if (err && typeof err.text === "function" && typeof err.status !== "undefined") {
                var fallback = err.status + " " + (err.statusText || "Request failed");
                return err.text().then(function (body) {
                    var msg = body;
                    try {
                        var json = JSON.parse(body);
                        msg = (json.ResponseStatus && json.ResponseStatus.Message) || json.Message || body;
                    } catch (e) { /* body is not JSON */ }
                    return (msg && msg.trim()) || fallback;
                }).catch(function () {
                    return fallback;
                });
            }
            return Promise.resolve(err && err.message ? err.message : String(err));
        }

        function saveConfig(e) {
            e.preventDefault();
            ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                return ApiClient.updatePluginConfiguration(pluginId, buildConfigFromForm(config));
            }).then(function (result) {
                Dashboard.processPluginConfigurationUpdateResult(result);
            }).catch(function (err) {
                describeError(err).then(function (msg) {
                    Dashboard.alert(t().saveFailedPrefix + msg);
                });
            });
        }

        function testConnection(kind) {
            var s = t();
            var urlEl = view.querySelector('#' + kind + 'Url');
            var keyEl = view.querySelector('#' + kind + 'ApiKey');
            var resultEl = view.querySelector('#' + kind + 'TestResult');

            resultEl.style.display = 'block';
            resultEl.textContent = s.testingText;

            ApiClient.ajax({
                type: 'POST',
                url: ApiClient.getUrl('ComingSoon/Test' + kind),
                data: JSON.stringify({ Url: urlEl.value, ApiKey: keyEl.value }),
                contentType: 'application/json',
                dataType: 'json'
            }).then(function (result) {
                resultEl.textContent = (result && result.Success ? '✅ ' : '❌ ') + (result && result.Message ? result.Message : t().unknownResult);
            }).catch(function (err) {
                describeError(err).then(function (msg) {
                    resultEl.textContent = '❌ ' + msg;
                });
            });
        }

        // Wire up the handlers immediately. The controller runs once the view's DOM is
        // ready, so we must NOT defer this to a "viewshow" event - Emby does not reliably
        // dispatch viewshow to plugin config views, and when it doesn't fire the Save
        // button would otherwise never get a click handler.
        view.querySelector('#ComingSoonConfigForm').addEventListener('submit', saveConfig);
        view.querySelector('#TestSonarr').addEventListener('click', function () { testConnection('Sonarr'); });
        view.querySelector('#TestRadarr').addEventListener('click', function () { testConnection('Radarr'); });
        view.querySelector('#BtnAddList').addEventListener('click', addList);
        view.querySelector('#BtnInfo').addEventListener('click', openInfoModal);
        view.querySelector('#BtnReadme').addEventListener('click', openReadmeModal);
        view.querySelector('#BtnCloseReadme').addEventListener('click', closeReadmeModal);
        view.querySelector('#CsReadmeModalBackdrop').addEventListener('click', function (e) {
            if (e.target === e.currentTarget) closeReadmeModal();
        });
        view.querySelector('#BtnCloseInfo').addEventListener('click', closeInfoModal);
        view.querySelector('#CsInfoModalBackdrop').addEventListener('click', function (e) {
            if (e.target === e.currentTarget) closeInfoModal();
        });
        view.querySelector('#BtnLogs').addEventListener('click', openLogModal);
        view.querySelector('#BtnRefreshLogs').addEventListener('click', loadLogs);
        view.querySelector('#BtnOpenLogsNewTab').addEventListener('click', openLogsInNewTab);
        view.querySelector('#BtnCloseLogs').addEventListener('click', closeLogModal);
        view.querySelector('#CsLogModalBackdrop').addEventListener('click', function (e) {
            if (e.target === e.currentTarget) closeLogModal();
        });
        view.querySelector('#Language').addEventListener('change', function () {
            currentLanguage = view.querySelector('#Language').value;
            applyStaticText();
            renderLists();
        });

        loadConfig();
        view.addEventListener('viewshow', loadConfig);
    };
});
