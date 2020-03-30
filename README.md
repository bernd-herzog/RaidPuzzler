# RaidPuzzler

Reconstruction of software- and hardware raids can be tricky when you dont know raid level, discs order, stipe size, left/right-symetric, etc. RaidPuzzer tries a completly different approch. It searches for images and displays them like a puzzle:

![alt text](/doc/cow.png)

While rearranging parameters the image gives feedback how far off they are. When done the resulting image can be created.

## Getting Started

* To try RaidPuzzler do:
  * Create and start a linux software raid:
    * `for i in {0..5}; do truncate -s 30M raid$i.img; done`
    * `for i in {0..5}; do losetup /dev/loop$i raid$i.img; done`
    * `mdadm --create /dev/md87 --level=5 --raid-devices=6 /dev/loop0 /dev/loop1 /dev/loop2 /dev/loop3 /dev/loop4 /dev/loop5`
    * `mkfs.ext4 /dev/md87`
  * Mount
    * `mount /dev/md87 /mnt`
  * Copy some jpeg images onto raid
  * Dismount
    * `umount /mnt`
  * Unloop
    * `for i in {0..5}; do losetup -d /dev/loop$i; done`
  * Open in RaidPuzzler
  * Puzzle away
  * Save disk image
  * Recover all data from disk image.

## Limitations

* Not Tested with large files (>100MB).
* No or limited error handling
* Only raid 5 is supported
* No clean code
* No unit or integration tests
* No documentation

## Author

* **Bernd Herzog** - [bernd-herzog](https://github.com/bernd-herzog/)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details
