#!/bin/bash
# Bakes the web prototype's square terrain textures into the isometric diamonds the province tilemap draws:
# rotated 45° on a canvas that fits the whole square, then squashed to 2:1 (256 × 128, one cell).
#   Tools/Art/bake-terrain.sh [web assets folder]
set -euo pipefail
SRC=${1:-$HOME/Proyectos/Codigames/kingdom/src/render/assets}
OUT=$(dirname "$0")/../../Assets/Art/Terrain
mkdir -p "$OUT"
for terrain in grassland plains desert snow tundra water; do
  for variant in "" _2 _3 _4; do
    file="$SRC/terrain_${terrain}${variant}.png"
    [ -f "$file" ] || continue
    magick "$file" -background none -virtual-pixel transparent +distort SRT 45 -trim +repage -resize '256x128!' \
      "$OUT/terrain_${terrain}${variant}.png"
  done
done
echo "baked into $OUT"
