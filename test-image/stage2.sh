#!/bin/bash
set -x

sudo umount -l `pwd`/mount/dev/vda
sudo umount -l `pwd`/mount
sudo losetup -d /dev/loop8
set -e
sudo losetup loop8 `pwd`/image.bin
sudo mount /dev/loop8 `pwd`/mount
sudo touch mount/dev/vda
sudo mount -o bind /dev/loop8 `pwd`/mount/dev/vda 
sudo mkdir -p mount/boot/grub

cat > device.map <<EOF
(hd0)   /dev/vda
EOF

sudo mv device.map mount/boot/grub/device.map
sudo mkdir -p  mount/proc/self
sudo cp mountinfo mount/proc/self
sudo cp devices mount/proc
sudo chroot `pwd`/mount grub-install --no-floppy --force --grub-mkdevicemap=/boot/grub/device.map /dev/vda
sudo rm -rf mount/proc/*
sudo umount `pwd`/mount/dev/vda


cat > grub.cfg <<EOF
insmod ext2
set root='hd0'
menuentry 'Linux' {
	linux /boot/`basename  mount/boot/vmlinuz-*` root=/dev/vda rw
	initrd /boot/`basename  mount/boot/initrd*`
}
set default=0
set timeout=2
EOF
sudo mv grub.cfg mount/boot/grub
echo -e 'root\nroot' | sudo chroot `pwd`/mount passwd root
echo 'test-image'|sudo tee `pwd`/mount/etc/hostname
echo 'nameserver 8.8.8.8'|sudo tee `pwd`/mount/etc/resolv.conf

cat > interfaces <<EOF
auto lo
iface lo inet loopback

auto ens3
iface ens3 inet dhcp
EOF
sudo mv interfaces mount/etc/network




rm -rf agent-pub
cd ../RapiAgent/
dotnet publish -r linux-x64 -o ../test-image/agent-pub
cd ../test-image

sudo rm -rf mount/rapi
sudo mv agent-pub mount/rapi

cat > rapi.service <<EOF
[Unit]
Description=RAPI

[Service]
WorkingDirectory=/rapi
ExecStart=/rapi/RapiAgent http://0.0.0.0:5000/
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=rapi
User=root

[Install]
WantedBy=multi-user.target

EOF
sudo mv rapi.service mount/lib/systemd/system
sudo rm -f mount/etc/systemd/system/multi-user.target.wants/rapi.service
sudo chroot `pwd`/mount ln -s /lib/systemd/system/rapi.service /etc/systemd/system/multi-user.target.wants/rapi.service

sudo umount -l `pwd`/mount
sudo losetup -d /dev/loop8
