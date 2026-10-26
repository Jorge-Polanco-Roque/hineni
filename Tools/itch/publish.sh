#!/bin/zsh
# Publishes HINENI to itch.io with butler: the web build (played in the browser) and the macOS, Windows and Linux builds
# (BuildDemo.Build, BuildWindows, BuildLinux; the Windows/Linux modules are installed through Unity Hub).
# Usage: Tools/itch/publish.sh <itch-user> [game-slug]      (run from the project root, after both builds)
# First time: `butler login`, and create the project page on itch.io (Kind of project: HTML) with that slug.
# Without butler, Builds/itch/*.zip can be uploaded by hand on the project's edit page.
set -e
USER_NAME=${1:?"usage: Tools/itch/publish.sh <itch-user> [game-slug]"}
GAME=${2:-hineni}
VERSION=$(grep bundleVersion ProjectSettings/ProjectSettings.asset | awk '{print $2}')
BUTLER=$(command -v butler || echo ~/.local/bin/butler)

OUT=Builds/itch; rm -rf $OUT; mkdir -p $OUT/web
cp -R Builds/WebGL/Build $OUT/web/
sed "s/__BUILD__/$(date +%Y%m%d%H%M%S)/g" Tools/web/index.html > $OUT/web/index.html
(cd $OUT/web && zip -qr ../hineni-web.zip .)
ditto -c -k --keepParent Builds/macOS/Hineni.app $OUT/Hineni-macOS.zip

$BUTLER push $OUT/web "$USER_NAME/$GAME:html5" --userversion "$VERSION"
$BUTLER push Builds/macOS "$USER_NAME/$GAME:osx" --userversion "$VERSION"
$BUTLER push Builds/Windows "$USER_NAME/$GAME:windows" --userversion "$VERSION"
$BUTLER push Builds/Linux "$USER_NAME/$GAME:linux" --userversion "$VERSION"
echo "Publicado $VERSION → https://$USER_NAME.itch.io/$GAME"
