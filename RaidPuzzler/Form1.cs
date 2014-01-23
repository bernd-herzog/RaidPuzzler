using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RaidPuzzler
{
    public partial class Form1 : Form
    {
        RaidSimulator rs = new RaidSimulator();

        public Form1()
        {
            rs.ChunkSize = 64 * 1024;
            InitializeComponent();


        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    //listBox1.Items.AddRange(ofd.FileNames);
                    foreach (var file in ofd.FileNames)
                    {
                        listBox1.Items.Add(Path.GetFileName(file));
                    }
                    backgroundWorker1.RunWorkerAsync(ofd.FileNames);
                    this.Enabled = false;
                }
                else
                {

                }
            }




        }

        RaidSimulator.Picture lastPic = null;
        private void FillPictures()
        {
            if (comboBox2.SelectedItem != null)
            {
                lastPic = comboBox2.SelectedItem as RaidSimulator.Picture;
            }

            List<RaidSimulator.Picture> pics = rs.GetPictures();
            comboBox2.DataSource = pics;

            if (lastPic != null)
            {
                var q = pics.Where(p => p.OnDiskStart == lastPic.OnDiskStart && p.DiskId == lastPic.DiskId);
                if (q.Any())
                {
                    comboBox2.SelectedItem = q.First();
                }
                else
                {
                    comboBox2.SelectedItem = null;
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedItem != null)
            {
                pictureBox1.Image = (comboBox2.SelectedItem as RaidSimulator.Picture).GetImage();

                if (pictureBox1.Image != null)
                {
                    //pictureBox1.Image.
                    toolStripStatusLabel1.Text = string.Format("Size: {0} x {1}", pictureBox1.Image.Width, pictureBox1.Image.Height);
                }

            }
            else
            {
                pictureBox1.Image = null;
            }
        }

        private void dataGridView1_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < 255 && e.ColumnIndex >= 0 && e.ColumnIndex < 255)
                DoDragDrop(e, DragDropEffects.Move);
        }

        private void dataGridView1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            DataGridViewCellMouseEventArgs other = e.Data.GetData(typeof(DataGridViewCellMouseEventArgs)) as DataGridViewCellMouseEventArgs;
            if (other != null)
            {
                DataGridViewCell otherCell = dataGridView1.Rows[other.RowIndex].Cells[other.ColumnIndex];


                Point clientPoint = dataGridView1.PointToClient(new Point(e.X, e.Y));
                var thisLocation = dataGridView1.HitTest(clientPoint.X, clientPoint.Y);

                if (thisLocation.RowIndex >= 0 && thisLocation.RowIndex < 255 &&
                    thisLocation.ColumnIndex >= 0 && thisLocation.ColumnIndex < 255 &&
                    (thisLocation.RowIndex != otherCell.RowIndex || thisLocation.ColumnIndex != otherCell.ColumnIndex))
                {
                    DataGridViewCell thisCell = dataGridView1.Rows[thisLocation.RowIndex].Cells[thisLocation.ColumnIndex];

                    if (!thisCell.Value.Equals("P"))
                    {
                        rs.Arrangement.Remove(int.Parse((string)thisCell.Value));
                    }

                    if (!otherCell.Value.Equals("P"))
                    {
                        rs.Arrangement.Remove(int.Parse((string)otherCell.Value));
                    }

                    object tmp = thisCell.Value;

                    thisCell.Value = otherCell.Value;
                    otherCell.Value = tmp;

                    if (!thisCell.Value.Equals("P"))
                    {
                        rs.Arrangement.Add(int.Parse((string)thisCell.Value),
                            rs.NumDiscs * thisCell.RowIndex + thisCell.ColumnIndex);
                    }

                    if (!otherCell.Value.Equals("P"))
                    {
                        rs.Arrangement.Add(int.Parse((string)otherCell.Value),
                            rs.NumDiscs * otherCell.RowIndex + otherCell.ColumnIndex);
                    }


                    this.FillPictures();
                }
            }


        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            int o;
            if (int.TryParse(textBox1.Text, out o) && o > 0)
            {
                rs.ChunkSize = o * 1024;
                this.FillPictures();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {

                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    rs.WriteAllData(sfd.FileName);
                }
            }

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] files = e.Argument as string[];
            int i = 1;
            int j = 0;
            backgroundWorker1.ReportProgress(100 * i / files.Length);
            rs.DiskPositions = new Dictionary<int, int>();
            foreach (var file in files)
            {
                backgroundWorker1.ReportProgress(100 * i++ / files.Length);

                rs.AddFile(file);
                rs.DiskPositions.Add(j, j);
                j++;
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.ProgressBar.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripProgressBar1.Visible = false;
            this.Enabled = true;

            rs.SetArrangement();

            DataTable dt = new DataTable();
            for (int i = 0; i < rs.NumDiscs; i++)
            {
                dt.Columns.Add(i.ToString());
            }

            for (int i = 0; i < rs.NumDiscs; i++)
            {
                //rs.
                var row = dt.NewRow();

                for (int j = 0; j < rs.NumDiscs; j++)
                {
                    var q = rs.Arrangement.Where(o => o.Value == rs.NumDiscs * i + j);

                    //dt.Columns.Add(i.ToString());
                    if (q.Any())
                    {
                        row.SetField(j, q.First().Key);
                    }
                    else
                    {
                        row.SetField(j, "P");
                    }
                }

                dt.Rows.Add(row);

            }

            dataGridView1.DataSource = dt;
            dataGridView1.RowHeadersVisible = false;
            //dataGridView1.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.Width = 25;
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            FillPictures();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // diskorder up
            if (listBox1.SelectedIndex > 0)
            {
                int up = listBox1.SelectedIndex - 1;
                var tmp = listBox1.Items[listBox1.SelectedIndex];
                listBox1.Items[listBox1.SelectedIndex] = listBox1.Items[up];
                listBox1.Items[up] = tmp;

                var iTmp = rs.DiskPositions.First(p => p.Value == listBox1.SelectedIndex);
                var iTmp2 = rs.DiskPositions.First(p => p.Value == up);
                rs.DiskPositions[iTmp.Key] = up;
                rs.DiskPositions[iTmp2.Key] = listBox1.SelectedIndex;

                foreach (DataRow row in (dataGridView1.DataSource as DataTable).Rows)
                {
                    int rowIndex = (dataGridView1.DataSource as DataTable).Rows.IndexOf(row);
                    if (!row[listBox1.SelectedIndex].Equals("P"))
                    {
                        rs.Arrangement.Remove(int.Parse((string)row[listBox1.SelectedIndex]));
                    }

                    if (!row[up].Equals("P"))
                    {
                        rs.Arrangement.Remove(int.Parse((string)row[up]));
                    }

                    var oTmp = row[listBox1.SelectedIndex];
                    row[listBox1.SelectedIndex] = row[up];
                    row[up] = oTmp;

                    if (!row[listBox1.SelectedIndex].Equals("P"))
                    {
                        rs.Arrangement.Add(int.Parse((string)row[listBox1.SelectedIndex]),
                            rs.NumDiscs * rowIndex + listBox1.SelectedIndex);
                    }

                    if (!row[up].Equals("P"))
                    {
                        rs.Arrangement.Add(int.Parse((string)row[up]),
                            rs.NumDiscs * rowIndex + up);
                    }
                }


                listBox1.SelectedIndex = up;


                FillPictures();
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0 && listBox1.SelectedIndex < listBox1.Items.Count - 1)
            {
                int down = listBox1.SelectedIndex + 1;
                var tmp = listBox1.Items[listBox1.SelectedIndex];
                listBox1.Items[listBox1.SelectedIndex] = listBox1.Items[down];
                listBox1.Items[down] = tmp;

                var iTmp = rs.DiskPositions.First(p => p.Value == listBox1.SelectedIndex);
                var iTmp2 = rs.DiskPositions.First(p => p.Value == down);
                rs.DiskPositions[iTmp.Key] = down;
                rs.DiskPositions[iTmp2.Key] = listBox1.SelectedIndex;

                foreach (DataRow row in (dataGridView1.DataSource as DataTable).Rows)
                {
                    int rowIndex = (dataGridView1.DataSource as DataTable).Rows.IndexOf(row);
                    if (!row[listBox1.SelectedIndex].Equals("P"))
                    {
                        rs.Arrangement.Remove(int.Parse((string)row[listBox1.SelectedIndex]));
                    }

                    if (!row[down].Equals("P"))
                    {
                        rs.Arrangement.Remove(int.Parse((string)row[down]));
                    }

                    var oTmp = row[listBox1.SelectedIndex];
                    row[listBox1.SelectedIndex] = row[down];
                    row[down] = oTmp;

                    if (!row[listBox1.SelectedIndex].Equals("P"))
                    {
                        rs.Arrangement.Add(int.Parse((string)row[listBox1.SelectedIndex]),
                            rs.NumDiscs * rowIndex + listBox1.SelectedIndex);
                    }

                    if (!row[down].Equals("P"))
                    {
                        rs.Arrangement.Add(int.Parse((string)row[down]),
                            rs.NumDiscs * rowIndex + down);
                    }
                }


                listBox1.SelectedIndex = down;


                FillPictures();

            }
        }
    }
}
