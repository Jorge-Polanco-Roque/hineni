#!/bin/zsh
# Publishes Builds/WebGL to the gh-pages branch (GitHub Pages). Run from the project root after BuildDemo.BuildWeb.
# Each deploy gets a stamp in every asset URL so browsers never combine a cached wasm with new data.
set -e
STAMP=$(date +%Y%m%d%H%M%S)
SITE=$(mktemp -d)
cp -R Builds/WebGL/Build "$SITE/"
sed "s/__BUILD__/$STAMP/g" Tools/web/index.html > "$SITE/index.html"
touch "$SITE/.nojekyll"
cd "$SITE"
git init -q -b gh-pages && git add -A && git commit -q -m "Web build $STAMP"
git push -q -f https://github.com/Jorge-Polanco-Roque/hineni.git gh-pages
echo "Publicado $STAMP → https://jorge-polanco-roque.github.io/hineni/"
