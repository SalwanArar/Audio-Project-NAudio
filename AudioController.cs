using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mm_audio_2020_2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        WaveStream sound;

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "MP3 file|*.mp3";
            dialog.Title = "Choose you audio";
            var res = dialog.ShowDialog();
            if (res == System.Windows.Forms.DialogResult.Cancel)
                return;
            sound = new Mp3FileReader(dialog.FileName);
            int sampleRate = sound.WaveFormat.SampleRate;
            int bitsPersample = sound.WaveFormat.BitsPerSample;
            int channels = sound.WaveFormat.Channels;
            String encoding = (sound as Mp3FileReader)
                .Mp3WaveFormat.Encoding.ToString();
            int abbs = (sound as Mp3FileReader)
                .Mp3WaveFormat.AverageBytesPerSecond;
            listView1.Items.Add(new ListViewItem(
                new String[] {"Sample rate", sampleRate.ToString() }
                ));
            listView1.Items.Add(new ListViewItem(
                new String[] { "Bits per sample", bitsPersample.ToString() }
                ));
            listView1.Items.Add(new ListViewItem(
                new String[] { "Channels", channels.ToString() }
                ));
            listView1.Items.Add(new ListViewItem(
                new String[] { "Average bytes", abbs.ToString() }
                ));
            listView1.Items.Add(new ListViewItem(
                new String[] { "Encoding", encoding }
                ));
            var data = getSamplesOf16bitsSterioSound(sound);
            var ch1 = new short[data.Length / 2];
            for (int i = 0; i < data.Length; i += 2)
                ch1[i / 2] = data[i];
            var b = GenerateImageOfSound(ch1, 1000, 400);
            pictureBox1.Image = b;
        }

        short[] getSamplesOf16bitsSterioSound(WaveStream sound)
        {
            sound.Seek(0, System.IO.SeekOrigin.Begin);
            byte[] buffer = new byte[sound.Length];
            sound.Read(buffer, 0, buffer.Length);
            short[] result = new short[buffer.Length / 2];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }

        Bitmap GenerateImageOfSound(short[] data, int width, int height)
        {
            Bitmap b = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.Clear(Color.Black);
                int spp = data.Length / width;
                int px = 0, py = 0;
                for (int i = 0; i < data.Length; i += spp)
                {
                    int max = data[i], min = data[i];
                    for (int j = i + 1; j < Math.Min(i + spp, data.Length); j++)
                    {
                        if (data[j] > max)
                            max = data[j];
                        if (data[j] < min)
                            min = data[j];
                    }
                    int x = i / spp;
                    int y1 = (height / 2) - max * (height / 2) / short.MaxValue;
                    int y2 = (height / 2) - min * (height / 2) / short.MaxValue;
                    g.DrawLine(Pens.White, px, py, x, y1);
                    g.DrawLine(Pens.White, x, y1, x, y2);
                    px = x;
                    py = y2;
                }
            }
            return b;
        }

        WaveOut wout;

        private void playToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sound == null)
                return;
            sound.Seek(0, System.IO.SeekOrigin.Begin);
            wout = new WaveOut();
            wout.Init(sound);
            wout.Play();
            timer1.Enabled = true;
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (wout == null)
                return;
            if (wout.PlaybackState == PlaybackState.Stopped)
                return;
            if (wout.PlaybackState == PlaybackState.Playing)
            {
                wout.Pause();
                pauseToolStripMenuItem.Text = "Resume";
            }
            else
            {
                wout.Resume();
                pauseToolStripMenuItem.Text = "Pause";
            }
        }

        private void upToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sound == null)
                return;
            var samples = getSamplesOf16bitsSterioSound(sound);
            for(int i=0;i<samples.Length;i++)
            {
                int r = (int)(samples[i] * 1.5);
                samples[i] = r > short.MaxValue ? short.MaxValue : (short)r;
            }
            var bytes = new byte[samples.Length * 2];
            Buffer.BlockCopy(samples, 0, bytes, 0, samples.Length);
            MemoryStream ms = new MemoryStream(bytes);
            var source = new RawSourceWaveStream(ms, sound.WaveFormat);
            sound = source;
            var ch1 = new short[samples.Length / 2];
            for (int i = 0; i < samples.Length; i += 2)
                ch1[i / 2] = samples[i];
            var b = GenerateImageOfSound(ch1, 1000, 400);
            pictureBox1.Image = b;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (sound == null)
                return;
            if (wout == null || wout.PlaybackState != PlaybackState.Playing)
            {
                timer1.Enabled = false;
                return;
            }
            int x = (int)(sound.Position * pictureBox1.Width / sound.Length);
            using(Graphics g = pictureBox1.CreateGraphics())
            using(Pen p = new Pen(Color.Red))
            {
                p.Width = 4;
                g.DrawImage(pictureBox1.Image, 0, 0, pictureBox1.Width, pictureBox1.Height);
                g.DrawLine(p, x, 0, x, pictureBox1.Height);
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (sound == null || wout == null || wout.PlaybackState != PlaybackState.Playing)
                return;
            sound.Position = e.X * sound.Length / pictureBox1.Width;
        }

        private void fasterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sound == null)
                return;
            var samples = getSamplesOf16bitsSterioSound(sound);
            var result = new short[samples.Length / 2];
            for (int i = 0; i < samples.Length; i+=2)
                result[i / 2] = samples[i];
            var bytes = new byte[result.Length * 2];
            Buffer.BlockCopy(result, 0, bytes, 0, samples.Length);
            MemoryStream ms = new MemoryStream(bytes);
            var source = new RawSourceWaveStream(ms, sound.WaveFormat);
            sound = source;
            var ch1 = new short[result.Length / 2];
            for (int i = 0; i < result.Length; i += 2)
                ch1[i / 2] = result[i];
            var b = GenerateImageOfSound(ch1, 1000, 400);
            pictureBox1.Image = b;
        }
    }
}
