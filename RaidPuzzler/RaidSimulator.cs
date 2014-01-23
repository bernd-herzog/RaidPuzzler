using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidPuzzler
{
    public class RaidSimulator : IDisposable
    {
        private const int size = 512;
        private byte[] jpegMagic = new byte[] { 0xFF, 0xD8, 0xFF };
        private byte[] jpegEOI = new byte[] { 0xFF, 0xD9, 0x00, 0x00 };

        public int ChunkSize { get; set; }

        public Dictionary<int, int> Arrangement { get; set; } // key is virtual, value is pyhsikal

        public int NumDiscs { get { return files.Count; } }
        public long Size { get; set; }

        public Dictionary<int, string> DiskNames { get; set; }
        public Dictionary<int, int> DiskPositions { get; set; } // [diskId] = position


        private Dictionary<string, FileStream> files = new Dictionary<string, FileStream>();
        private Dictionary<string, List<int>> _imageStarts = new Dictionary<string, List<int>>();
        private Dictionary<string, List<int>> _imageEnds = new Dictionary<string, List<int>>();

        public RaidSimulator()
        {
            DiskNames = new Dictionary<int, string>();
        }

        public void AddFile(string file)
        {
            DiskNames.Add(NumDiscs, Path.GetFileName(file));

            _imageStarts.Add(file, new List<int>());
            _imageEnds.Add(file, new List<int>());

            FileStream fs = new FileStream(file, FileMode.Open);
            files.Add(file, fs);
            Size = fs.Length;

            byte[] buffer = new byte[size];
            for (int i = 0; i < fs.Length; i += size)
            {
                fs.Read(buffer, 0, size);

                //start of jpeg only at start of sector (512byte)
                if (buffer[0] == jpegMagic[0] &&
                    buffer[1] == jpegMagic[1] &&
                    buffer[2] == jpegMagic[2])
                {
                    _imageStarts[file].Add(i);
                }

                for (int j = 0; j < size - 3; j++)
                {
                    if (buffer[j] == jpegEOI[0] &&
                        buffer[j + 1] == jpegEOI[1] &&
                        buffer[j + 2] == jpegEOI[2] &&
                        buffer[j + 3] == jpegEOI[3])
                    {
                        _imageEnds[file].Add(i + j + 2);
                    }
                }
            }
            fs.Position = 0;

        }

        public void SetArrangement()
        {
            Arrangement = new Dictionary<int, int>();


            Arrangement.Add(0, 0);
            Arrangement.Add(1, 1);
            Arrangement.Add(2, 2);
            Arrangement.Add(3, 3);
            Arrangement.Add(4, 4);

            Arrangement.Add(5, 6);
            Arrangement.Add(6, 7);
            Arrangement.Add(7, 8);
            Arrangement.Add(8, 9);
            Arrangement.Add(9, 11);

            Arrangement.Add(10, 12);
            Arrangement.Add(11, 13);
            Arrangement.Add(12, 14);
            Arrangement.Add(13, 16);
            Arrangement.Add(14, 17);

            Arrangement.Add(15, 18);
            Arrangement.Add(16, 19);
            Arrangement.Add(17, 21);
            Arrangement.Add(18, 22);
            Arrangement.Add(19, 23);

            Arrangement.Add(20, 24);
            Arrangement.Add(21, 26);
            Arrangement.Add(22, 27);
            Arrangement.Add(23, 28);
            Arrangement.Add(24, 29);

            Arrangement.Add(25, 31);
            Arrangement.Add(26, 32);
            Arrangement.Add(27, 33);
            Arrangement.Add(28, 34);
            Arrangement.Add(29, 35);

            /*
            Arrangement.Add(0, 1);
            Arrangement.Add(1, 2);
            Arrangement.Add(2, 3);
            Arrangement.Add(3, 4);
            Arrangement.Add(4, 5);

            Arrangement.Add(5, 6);
            Arrangement.Add(6, 8);
            Arrangement.Add(7, 9);
            Arrangement.Add(8, 10);
            Arrangement.Add(9, 11);

            Arrangement.Add(10, 12);
            Arrangement.Add(11, 13);
            Arrangement.Add(12, 15);
            Arrangement.Add(13, 16);
            Arrangement.Add(14, 17);

            Arrangement.Add(15, 18);
            Arrangement.Add(16, 19);
            Arrangement.Add(17, 20);
            Arrangement.Add(18, 22);
            Arrangement.Add(19, 23);

            Arrangement.Add(20, 24);
            Arrangement.Add(21, 25);
            Arrangement.Add(22, 26);
            Arrangement.Add(23, 27);
            Arrangement.Add(24, 29);

            Arrangement.Add(25, 30);
            Arrangement.Add(26, 31);
            Arrangement.Add(27, 32);
            Arrangement.Add(28, 33);
            Arrangement.Add(29, 34);
            */
        }

        public IEnumerable<int> GetImageStarts()
        {
            return GetImagePoss(_imageStarts);
        }
        public IEnumerable<int> GetImageEnds()
        {
            return GetImagePoss(_imageEnds);
        }


        public IEnumerable<int> GetImagePoss(Dictionary<string, List<int>> list)
        {
            int diskId = 0;
            int repeatSizeOnDisk = NumDiscs * ChunkSize;
            int dataSizeOnDisk = (NumDiscs - 1) * ChunkSize;

            foreach (var file in this.files)
            {
                foreach (var start in list[file.Key])
                {
                    //from disk Position to virtual position
                    //start in byte on disk i
                    //in: start, disk(i)
                    //out: basePos + otherPos * ChunkSize + offsetInChunk 
                    int ret;
                    CalcPositionDiskToVirtual(start, diskId, out ret);

                    if (ret != -1)
                        yield return ret;
                }
                diskId++;
            }
        }

        private void CalcPositionDiskToVirtual(int start, int diskId, out int ret)
        {
            ret = -1;
            int repeatSizeOnDisk = NumDiscs * ChunkSize;
            int dataSizeOnDisk = (NumDiscs - 1) * ChunkSize;

            int offsetInChunk = start % ChunkSize;
            int chunkAdressOnDisk = start - offsetInChunk; // pos in %ChunkSize

            int chunkOffsetOnDisk = chunkAdressOnDisk % repeatSizeOnDisk;

            int basePosOnDisk = chunkAdressOnDisk - chunkOffsetOnDisk;

            int basePos = basePosOnDisk / repeatSizeOnDisk * dataSizeOnDisk * NumDiscs;

            int inTablePos = DiskPositions[diskId] + NumDiscs * (chunkOffsetOnDisk / ChunkSize);

            if (Arrangement.Where(o => o.Value == inTablePos).Select(o => o.Key).Any())
            {
                int otherPos = Arrangement.Where(o => o.Value == inTablePos).Select(o => o.Key).First();
                ret = basePos + otherPos * ChunkSize + offsetInChunk;
            }
        }

        private void CalcPositionVirtualToDisk(long absPos, out long startOnDisk, out int discId)
        {
            startOnDisk = discId = -1;

            long offsetInChunk = absPos % ChunkSize;
            long chunkPositionOnVirtual = absPos - offsetInChunk; // pos in %ChunkSize

            int repeatSize = NumDiscs * NumDiscs * ChunkSize;
            int dataSize = NumDiscs * (NumDiscs - 1) * ChunkSize;

            long basePos = chunkPositionOnVirtual / dataSize * repeatSize;
            long basePosInDisk = basePos / NumDiscs;

            long inTable = chunkPositionOnVirtual % dataSize;
            long inTablePos = inTable / ChunkSize; // key

            int onDiskTablePos = Arrangement[(int)inTablePos];
            int diskPos = onDiskTablePos % NumDiscs;
            discId = DiskPositions.First(p => p.Value == diskPos).Key;
             
            int line = onDiskTablePos / NumDiscs;

            startOnDisk = basePosInDisk + line * ChunkSize + offsetInChunk;
        }

        public void Dispose()
        {
            foreach (var file in files)
            {
                file.Value.Dispose();
            }
        }

        internal byte[] GetData(long start, long end)
        {
            byte[] ret = new byte[end - start];
            long arraypos = 0;

            long pos;
            int discId;
            CalcPositionVirtualToDisk(start, out pos, out discId);

            long nextChunk = ((start / ChunkSize) + 1) * ChunkSize;
            var filekeys = this.files.Select(o => o.Key).ToList();

            if (end < nextChunk) // ende im gleichen chunk
            {
                //passiert bei großen chunks > 1MB

                CalcPositionVirtualToDisk(start, out pos, out discId);
                this.files[filekeys[discId]].Position = pos;
                this.files[filekeys[discId]].Read(ret, (int)arraypos, (int)(end - start - arraypos));
                arraypos += end - start - arraypos;

                //System.Windows.Forms.MessageBox.Show("Der unwahrscheinliche fall ist eingetreten...");
            }
            else if (end == nextChunk)
            {
                //geht exakt bis zum nächsten chunk... als ob...
                System.Windows.Forms.MessageBox.Show("Der unwahrscheinliche fall ist eingetreten...");
            }
            else
            {
                // es geht über den chunk hinaus...

                // lesen bis zum ende

                long toEnd = nextChunk - start;

                this.files[filekeys[discId]].Position = pos;
                this.files[filekeys[discId]].Read(ret, (int)arraypos, (int)toEnd);
                arraypos += toEnd;

                long newStart = nextChunk;
                nextChunk = ((newStart / ChunkSize) + 1) * ChunkSize;

                //weitere chunks lesen
                while (end >= nextChunk) // ende im gleichen chunk
                {

                    CalcPositionVirtualToDisk(newStart, out pos, out discId);
                    this.files[filekeys[discId]].Position = pos;
                    this.files[filekeys[discId]].Read(ret, (int)arraypos, ChunkSize);
                    arraypos += ChunkSize;

                    newStart = nextChunk;
                    nextChunk = ((newStart / ChunkSize) + 1) * ChunkSize;
                }

                //letzten chunk lesen

                if (end > start + arraypos) // there is more
                {
                    CalcPositionVirtualToDisk(newStart, out pos, out discId);
                    this.files[filekeys[discId]].Position = pos;
                    this.files[filekeys[discId]].Read(ret, (int)arraypos, (int)(end - start - arraypos));
                    arraypos += end - start - arraypos;

                }
            }


            return ret;
        }

        public List<Picture> GetPictures()
        {
            List<Picture> ret = new List<Picture>();

            var starts = this.GetImageStarts().OrderBy(o => o).ToList();
            var ends = this.GetImageEnds().OrderBy(o => o).ToList();

            foreach (var start in starts)
            {
                int nextEnd = start + 3 * 1024 * 1024;
                if (ends.Where(o => o > start).Any())
                {
                    nextEnd = ends.Where(o => o > start).First();
                }

                if ((nextEnd - start) > 25 * 1024 * 2014)
                {
                    nextEnd = start + 25 * 1024 * 1024;
                }

                //if (nextEnd != -1)
                {
                    long onDiskStart;
                    int diskId;
                    CalcPositionVirtualToDisk(start, out onDiskStart, out diskId);

                    ret.Add(new Picture(this)
                    {
                        Start = start,
                        End = nextEnd,
                        OnDiskStart = onDiskStart,
                        DiskId = diskId
                    });
                }
            }

            return ret;
        }

        public class Picture
        {
            public RaidSimulator RaidSimulator { get; set; }
            public Picture(RaidSimulator rs)
            {
                RaidSimulator = rs;
            }

            public long Start { get; set; }
            public long End { get; set; }

            public long OnDiskStart { get; set; }
            public int DiskId { get; set; }


            public Image GetImage()
            {

                byte[] data = RaidSimulator.GetData(Start, End);
                //File.WriteAllBytes(r.Next(100000) + ".jpg", data);

                //if (data.Length < 3 * 1024 * 1024)
                {
                    MemoryStream ms = new MemoryStream(data);
                    try
                    {
                        var i = Image.FromStream(ms);
                        return i;
                    }
                    catch
                    { }
                }

                return null;
            }

            public override string ToString()
            {
                return string.Format("Image: Size {0} byte, starts on disk {1} at {2}", End - Start, DiskId, OnDiskStart);
            }
        }

        internal void WriteAllData(string p)
        {
            using (var fs = File.OpenWrite(p))
            {

                int repeatSizeOnDisk = NumDiscs * ChunkSize;
                int dataSizeOnDisk = (NumDiscs - 1) * ChunkSize;

                long virtualSize = (NumDiscs - 1) * Size;


                for (long i = 0; i < virtualSize; i += 1024 * 1024)
                {
                    byte[] data = GetData(i, i + 1024 * 1024);
                    fs.Write(data, 0, 1024 * 1024);
                }

            }
        }
    }
}
