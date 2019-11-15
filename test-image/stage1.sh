#!/bin/bash
sudo umount -l `pwd`/mount/dev/vda
sudo umount -l `pwd`/mount
sudo rm image.bin
sudo mkdir mount
set -e
set -x
dd if=/dev/zero of=image.bin bs=16M count=64
mkfs.ext4 image.bin
sudo mount -o loop image.bin `pwd`/mount
sudo debootstrap --include=ca-certificates,libc6,libgcc1,libgssapi-krb5-2,libicu57,liblttng-ust0,libssl1.0.2,libstdc++6,zlib1g,linux-image-amd64,grub-pc,net-tools stretch `pwd`/mount
sudo mkdir `pwd`/mount/parent

sudo umount -l `pwd`/mount

#sudo mount --bind `pwd` `pwd`/mount/parent

echo "Stage 1 finished"
