PATH=$1
NAME=$2

echo "PATH: $PATH"
echo "NAME: $NAME"

echo "stapling ticket"

/usr/bin/xcrun stapler staple -v "$PATH/${NAME}.app"
/usr/sbin/spctl -a -v "$PATH/${NAME}.app"
