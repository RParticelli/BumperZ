#!/bin/bash
set -euo pipefail

SOURCE_IPA="$CM_BUILD_DIR/ci-upload/BumperZ.ipa"
OUTPUT_DIR="$CM_BUILD_DIR/build"
WORK_DIR="$CM_BUILD_DIR/.ipa-icon-fix"
ICON_SOURCE="$CM_BUILD_DIR/Assets/Resources/UI/AppIcon.png"

rm -rf "$WORK_DIR"
mkdir -p "$WORK_DIR/payload" "$WORK_DIR/catalog/AppIcon.appiconset" "$WORK_DIR/compiled" "$OUTPUT_DIR"
unzip -q "$SOURCE_IPA" -d "$WORK_DIR/payload"

APP_PATH="$(find "$WORK_DIR/payload/Payload" -maxdepth 1 -type d -name '*.app' -print -quit)"
test -n "$APP_PATH"
test -f "$ICON_SOURCE"

cp "$ICON_SOURCE" "$WORK_DIR/catalog/AppIcon.appiconset/AppIcon-1024.png"
cat > "$WORK_DIR/catalog/AppIcon.appiconset/Contents.json" <<'JSON'
{
  "images": [
    {
      "filename": "AppIcon-1024.png",
      "idiom": "universal",
      "platform": "ios",
      "size": "1024x1024"
    }
  ],
  "info": {
    "author": "BumperZ",
    "version": 1
  }
}
JSON

xcrun actool "$WORK_DIR/catalog" \
  --compile "$WORK_DIR/compiled" \
  --app-icon AppIcon \
  --platform iphoneos \
  --minimum-deployment-target 15.0 \
  --target-device iphone \
  --target-device ipad \
  --output-partial-info-plist "$WORK_DIR/icon-info.plist"

test -f "$WORK_DIR/compiled/Assets.car"
cp "$WORK_DIR/compiled/Assets.car" "$APP_PATH/Assets.car"

set_plist_string() {
  local key="$1"
  local value="$2"
  /usr/libexec/PlistBuddy -c "Set $key $value" "$APP_PATH/Info.plist" 2>/dev/null || \
    /usr/libexec/PlistBuddy -c "Add $key string $value" "$APP_PATH/Info.plist"
}

set_plist_string ':CFBundleIconName' 'AppIcon'
set_plist_string ':CFBundleIcons:CFBundlePrimaryIcon:CFBundleIconName' 'AppIcon'
set_plist_string ':CFBundleIcons~ipad:CFBundlePrimaryIcon:CFBundleIconName' 'AppIcon'

PROFILE_PATH="$(find "$HOME/Library/MobileDevice/Provisioning Profiles" -type f -name '*.mobileprovision' -print -quit)"
test -n "$PROFILE_PATH"
cp "$PROFILE_PATH" "$APP_PATH/embedded.mobileprovision"

security cms -D -i "$PROFILE_PATH" > "$WORK_DIR/profile.plist"
/usr/libexec/PlistBuddy -x -c 'Print :Entitlements' "$WORK_DIR/profile.plist" > "$WORK_DIR/entitlements.plist"

SIGNING_IDENTITY="$(security find-identity -v -p codesigning | sed -n 's/.*"\(Apple Distribution:.*\)"/\1/p' | head -1)"
test -n "$SIGNING_IDENTITY"

while IFS= read -r nested; do
  codesign --force --sign "$SIGNING_IDENTITY" --timestamp=none "$nested"
done < <(find "$APP_PATH" \( -type d -name '*.framework' -o -type d -name '*.appex' -o -type f -name '*.dylib' \) -print | awk '{ print length, $0 }' | sort -rn | cut -d' ' -f2-)

codesign --force --sign "$SIGNING_IDENTITY" \
  --entitlements "$WORK_DIR/entitlements.plist" \
  --timestamp=none "$APP_PATH"
codesign --verify --deep --strict --verbose=2 "$APP_PATH"

(cd "$WORK_DIR/payload" && zip -qry "$OUTPUT_DIR/BumperZ-fixed.ipa" Payload)
test -f "$OUTPUT_DIR/BumperZ-fixed.ipa"
echo "Patched and re-signed IPA: $OUTPUT_DIR/BumperZ-fixed.ipa"
