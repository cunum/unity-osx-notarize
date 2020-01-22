USER=$1
PASS=$2

/usr/bin/xcrun altool --notarization-history 0 -u $USER -p $PASS
