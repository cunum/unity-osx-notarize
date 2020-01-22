USER=$1
PASS=$2
ID=$3

xcrun altool --notarization-info $ID --username $USER --password $PASS
