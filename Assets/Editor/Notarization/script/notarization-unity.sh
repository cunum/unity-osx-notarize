USER=$1
PASS=$2
PATH=$3
NAME=$4
DEVELOPER_CERT_ID=$5
BUNDLE=$6

echo "User: $USER"
echo "PATH: $PATH"
echo "NAME: $NAME"
echo "CERT: $DEVELOPER_CERT_ID"
echo "Bundle: $BUNDLE"

echo "code signing"

/usr/bin/codesign --deep --force --verify --verbose --timestamp --options runtime --entitlements $(/usr/bin/dirname "$0")/entitlements.xml --sign "$DEVELOPER_CERT_ID" "$PATH/${NAME}.app"

echo "zipping"

/usr/bin/zip -r "$PATH/archive.zip" "$PATH/${NAME}.app"

echo "uploading to notarization service"

/usr/bin/xcrun altool --notarize-app --primary-bundle-id $BUNDLE --username $USER --password $PASS --file "$PATH/archive.zip"

echo "removing archive"

if [ -f "$PATH/archive.zip" ]
then
    /bin/rm "$PATH/archive.zip"
fi