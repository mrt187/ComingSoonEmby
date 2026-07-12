Place a .ttf/.otf font file here (e.g. Roboto-Bold.ttf) and point
PluginConfiguration/ImageOverlayService's bundledFontPath at it.

Not included in this scaffold because a real font binary can't be generated as source text -
grab any open-license bold font (Roboto, Inter, Noto Sans Bold, etc.) and drop the file here.

Why this is needed: a minimal/headless Linux server (typical for an Unraid Docker container)
usually has zero system fonts installed, so SixLabors.Fonts' SystemFonts lookup will fail.
Bundling a font file with the plugin avoids that dependency entirely.
