#!/bin/bash

set -e

DEFAULT_PLATFORM="ubuntu"
PLATFORM="${1:-$DEFAULT_PLATFORM}"

DEFAULT_WABT_VERSION="1.0.33"
WABT_VERSION="${2:-$DEFAULT_WABT_VERSION}"

WABT_TAR="wabt-$WABT_VERSION-$PLATFORM.tar.gz"
WABT_URL="https://github.com/WebAssembly/wabt/releases/download/$WABT_VERSION/$WABT_TAR"

echo "WABT URL: $WABT_URL"

mkdir -p wabt
curl -o wabt/$WABT_TAR -L $WABT_URL
tar -zxf wabt/$WABT_TAR -C ./wabt --strip-components 1

export PATH="${PATH:+${PATH}:}$PWD/wabt/bin"
echo "$PWD/wabt/bin" >> $GITHUB_PATH
wat2wasm --version

