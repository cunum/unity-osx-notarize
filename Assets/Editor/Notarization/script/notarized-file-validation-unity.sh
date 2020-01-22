PATH=$1
NAME=$2

echo "PATH: $PATH"
echo "NAME: $NAME"

echo "file validation"

/usr/sbin/spctl -a -v "$PATH/${NAME}.app"
/usr/bin/codesign --test-requirement="=notarized" --verify --verbose "$PATH/${NAME}.app"
