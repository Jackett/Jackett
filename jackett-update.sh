#!/bin/bash

if [ ! -f cur-jackett.txt ]; then
echo 'v0.0.0' > cur-jackett.txt
fi
CVERSION=$(cat cur-jackett.txt)
#WVERSION=$(curl -s https://jackett.net/Download| grep -m 1 -o 'v[0-9.].[0-9.][0-9.]*[\ \t]*'|head -n 1)
WVERSION=$(curl https://github.com/zone117x/Jackett/releases/latest -s -L -I -o /dev/null -w '%{url_effective}'| grep -o 'v[0-9.].[0-9.].[0-9.]')
echo "CVERSION:$CVERSION"
echo "WVERSION:$WVERSION"
if [[ $WVERSION != $CVERSION ]]; then
	wget https://github.com/zone117x/Jackett/releases/download/${WVERSION}/Jackett.Binaries.Mono.${WVERSION}.tar.bz2
	#wget https://jackett.net/Download/${WVERSION}/Jackett.Mono.${WVERSION}.tar.bz2
	if [ -f Jackett.Binaries.Mono.${WVERSION}.tar.bz2 ]; then
		tar -xf Jackett.Binaries.Mono*.bz2
		sudo rm -rf /opt/Jackett/
		sudo mkdir /opt/Jackett
		sudo mv Jackett/* /opt/Jackett
		sudo chown -R osmc:osmc /opt/Jackett
		sudo rm -rf Jackett.Binaries.Mono*.bz2
		sudo rm -rf Jackett
		echo $WVERSION > cur-jackett.txt
		echo "New version $WVERSION!"
	else
		echo "File not found!"
	fi
else
	echo "$CVERSION is up to date !"
fi
