// Accord.NET Sample Applications
// http://accord.googlecode.com
//
// Copyright © César Souza, 2009-2012
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

using System;
using System.Drawing;
using System.Windows.Forms;
using Accord.Imaging.Filters;
using Accord.Vision.Detection;
using Accord.Vision.Detection.Cascades;
using System.Diagnostics;
using Accord.Imaging.Filters;

namespace FaceDetection
{
    public partial class MainForm : Form
    {
        Bitmap picture = FaceDetection.Properties.Resources.judybats;

        HaarObjectDetector detector;

        public MainForm()
        {
            InitializeComponent();

            pictureBox1.Image = picture;

            cbMode.DataSource = Enum.GetValues(typeof(ObjectDetectorSearchMode));
            cbScaling.DataSource = Enum.GetValues(typeof(ObjectDetectorScalingMode));

            cbMode.SelectedItem = ObjectDetectorSearchMode.NoOverlap;
            cbScaling.SelectedItem = ObjectDetectorScalingMode.SmallerToGreater;

            toolStripStatusLabel1.Text = "Please select the detector options and click Detect to begin.";

            HaarCascade cascade = new FaceHaarCascade();
            detector = new HaarObjectDetector(cascade, 30);
        }


        private void button1_Click(object sender, EventArgs e)
        {
            detector.SearchMode = (ObjectDetectorSearchMode)cbMode.SelectedValue;
            detector.ScalingMode = (ObjectDetectorScalingMode)cbScaling.SelectedValue;
            detector.ScalingFactor = 1.5f;
            detector.MinSize = new Size(32, 32);
            detector.UseParallelProcessing = cbParallel.Checked;

            Stopwatch sw = Stopwatch.StartNew();

            // prepare grayscale image
            picture = Accord.Imaging.Image.Clone(new Bitmap(pictureBox1.Image), System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            // Process frame to detect objects
            Rectangle[] objects = detector.ProcessFrame(picture);

            sw.Stop();


            if (objects.Length > 0)
            {
                RectanglesMarker marker = new RectanglesMarker(objects, Color.Red);
                pictureBox1.Image = marker.Apply(picture);
            }

            toolStripStatusLabel1.Text = string.Format("Completed detection of {0} objects in {1}.",
                objects.Length, sw.Elapsed);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "Choose a picture to detect face";
            fdlg.InitialDirectory = @"D:\Temp\Downloads";
            fdlg.Filter = "All images files (*.jpg)|*.jpg|All files (*.*)|*.*";
            fdlg.FilterIndex = 1;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.ImageLocation = fdlg.FileName;
            }

            if (string.IsNullOrEmpty(pictureBox1.ImageLocation))
                return;
            pictureBox1.Image = new Bitmap(pictureBox1.ImageLocation);
        }
    }
}
