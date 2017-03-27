using Accord;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShapeDetection
{
    class BlobDetector
    {
        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            return points.Select(p => new System.Drawing.Point(p.X, p.Y)).ToArray();
        }

        public void GetBlobs(string input, string output)
        {
            // process binary image
            Bitmap image = new Bitmap(input);

            // lock image
            BitmapData bitmapData = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadWrite, image.PixelFormat);

            // step 1 - turn background to black
            ColorFiltering colorFilter = new ColorFiltering();

            colorFilter.Red = new IntRange(0, 64);
            colorFilter.Green = new IntRange(0, 64);
            colorFilter.Blue = new IntRange(0, 64);
            colorFilter.FillOutsideRange = false;
            colorFilter.ApplyInPlace(bitmapData);

            // step 2 - locating objects
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 5;
            blobCounter.MinWidth = 5;

            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            image.UnlockBits(bitmapData);

            // step 3 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();
            Graphics g = Graphics.FromImage(image);
            Pen redPen = new Pen(Color.Red, 2);
            Pen yellowPen = new Pen(Color.Yellow, 2);
            Pen greenPen = new Pen(Color.Green, 2);
            Pen bluePen = new Pen(Color.Blue, 2);

            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                List<IntPoint> edgePoints =
                    blobCounter.GetBlobsEdgePoints(blobs[i]);

                Accord.Point center;
                float radius;

                if (shapeChecker.IsCircle(edgePoints, out center, out radius))
                {
                    g.DrawEllipse(yellowPen,
                        (float)(center.X - radius), (float)(center.Y - radius),
                        (float)(radius * 2), (float)(radius * 2));
                }
                else
                {
                    List<IntPoint> corners;

                    if (shapeChecker.IsQuadrilateral(edgePoints, out corners))
                    {
                        if (shapeChecker.CheckPolygonSubType(corners) ==
                            PolygonSubType.Rectangle)
                        {
                            g.DrawPolygon(greenPen, ToPointsArray(corners));
                        }
                        else
                        {
                            g.DrawPolygon(bluePen, ToPointsArray(corners));
                        }
                    }
                    else
                    {
                        corners = PointsCloud.FindQuadrilateralCorners(edgePoints);
                        g.DrawPolygon(redPen, ToPointsArray(corners));
                    }
                }
            }

            redPen.Dispose();
            greenPen.Dispose();
            bluePen.Dispose();
            yellowPen.Dispose();
            g.Dispose();

            // step 4 - save the image
            image.Save(output);
        }

        public void GetBlobs2(string inputFile, string outputPath)
        {
            UnmanagedImage skewedImg = null;
            UnmanagedImage bwInvImg = null;

            {
                var bmp = new Bitmap(inputFile);
                var img = UnmanagedImage.FromManagedImage(bmp);
                var grayImg = Accord.Imaging.Filters.Grayscale.CommonAlgorithms.BT709.Apply(img);
                var bwImg = new Accord.Imaging.Filters.OtsuThreshold().Apply(grayImg);
                var skewChecker = new DocumentSkewChecker();
                double angle = skewChecker.GetSkewAngle(bwImg);
                var rotateFilter = new Accord.Imaging.Filters.RotateBilinear(-angle);
                skewedImg = rotateFilter.Apply(img);
                bwImg.Dispose();
                grayImg.Dispose();
                img.Dispose();
                bmp.Dispose();
            }

            {
                var grayImg = Accord.Imaging.Filters.Grayscale.CommonAlgorithms.BT709.Apply(skewedImg);
                var bwImg = new Accord.Imaging.Filters.OtsuThreshold().Apply(grayImg);
                var openingFilter = new Accord.Imaging.Filters.Opening();
                openingFilter.ApplyInPlace(bwImg);
                bwInvImg = new Accord.Imaging.Filters.Invert().Apply(bwImg);
                bwImg.Dispose();
                grayImg.Dispose();
            }


            var blobProc = new Accord.Imaging.BlobCounter();
            blobProc.ProcessImage(bwInvImg);
            var blobs = blobProc.GetObjectsInformation().ToList();

            foreach (Accord.Imaging.Blob blob in blobs.OrderBy(b => b.Rectangle.Left).ThenBy(b => b.Rectangle.Top))
            {
                Console.WriteLine("{0} {1}", blob.Rectangle.ToString(), blob.Area.ToString());
            }

            //Layout parameters
            var expectedLineMarkerSize = new System.Drawing.Size(25, 10);//new System.Drawing.Size(35, 15); 
            var expectedCellSize = new System.Drawing.Size(15, 10); //new System.Drawing.Size(20, 13); 
            int expectedNumlineMarkers = 19;// 23;
            int tolerance = 3;
            //Limits to determine in a cell is marked
            double fullnessOk = .75;
            double fullnessUnsure = .65;
            var questions = new List<Tuple<int, Accord.Imaging.Blob, List<Accord.Imaging.Blob>>>();

            {
                var lineMarkers = blobs.Where(b =>
                {
                    if (b.Rectangle.Width < expectedLineMarkerSize.Width - tolerance)
                        return false;
                    if (b.Rectangle.Width > expectedLineMarkerSize.Width + tolerance)
                        return false;
                    if (b.Rectangle.Height < expectedLineMarkerSize.Height - tolerance)
                        return false;
                    if (b.Rectangle.Height > expectedLineMarkerSize.Height + tolerance)
                        return false;
                    return true;
                })
                                       .OrderBy(b => b.Rectangle.Left)
                                       .ThenBy(b => b.Rectangle.Top)
                                       .ToList();
                if (lineMarkers.Count() != expectedNumlineMarkers)
                    throw new Exception(string.Format("Can't locate all line markers. Expected {0}, found {1}", expectedNumlineMarkers, lineMarkers.Count));
                var cells = blobs.Where(b =>
                {
                    if (b.Rectangle.Width < expectedCellSize.Width - tolerance)
                        return false;
                    if (b.Rectangle.Width > expectedCellSize.Width + tolerance)
                        return false;
                    if (b.Rectangle.Height < expectedCellSize.Height - tolerance)
                        return false;
                    if (b.Rectangle.Height > expectedCellSize.Height + tolerance)
                        return false;
                    return true;
                }).ToList();

                int idxLine = 1;
                foreach (var lineMarker in lineMarkers.OrderBy(b => b.CenterOfGravity.Y))
                {
                    var cellsOfLine = cells.Where(b => Math.Abs(b.CenterOfGravity.Y - lineMarker.CenterOfGravity.Y) <= tolerance)
                                           .Take(5)
                                           .ToList()
                                           .OrderBy(b => b.CenterOfGravity.X)
                                           .ToList();
                    questions.Add(new Tuple<int, Accord.Imaging.Blob, List<Accord.Imaging.Blob>>(idxLine, lineMarker, cellsOfLine));
                    idxLine++;
                }
            }

            {
                var bmp = skewedImg.ToManagedImage();
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    foreach (var question in questions)
                    {
                        g.FillRectangle(new SolidBrush(Color.Blue), question.Item2.Rectangle);
                        g.DrawString(question.Item1.ToString(), new System.Drawing.Font("Arial", 8), new SolidBrush(Color.White), question.Item2.Rectangle);
                        int column = 1;
                        foreach (Accord.Imaging.Blob blob in question.Item3.OrderBy(b => b.Rectangle.Left).ThenBy(b => b.Rectangle.Top))
                        {
                            if (System.Diagnostics.Debugger.IsAttached)
                                Console.WriteLine("Line {0}, Column {1}, Fullness {2}", question.Item1, column, Math.Round(blob.Fullness, 2));
                            if (blob.Fullness >= fullnessOk)
                                g.DrawRectangle(new Pen(Color.Green, 2), blob.Rectangle);
                            else if (blob.Fullness >= fullnessUnsure)
                                g.DrawRectangle(new Pen(Color.Yellow, 2), blob.Rectangle);
                            else
                                g.DrawRectangle(new Pen(Color.Red, 2), blob.Rectangle);
                            column++;
                        }
                    }
                }
               // bmp.Save(outp);
            }
        }

        public void GetBlobs3(string inputFile, string outputPath)
        {
            UnmanagedImage skewedImg = null;

            {
                var bmp = new Bitmap(inputFile);
                var img = UnmanagedImage.FromManagedImage(bmp);
                var grayImg = Accord.Imaging.Filters.Grayscale.CommonAlgorithms.BT709.Apply(img);
                var bwImg = new Accord.Imaging.Filters.OtsuThreshold().Apply(grayImg);
                var skewChecker = new DocumentSkewChecker();
                double angle = skewChecker.GetSkewAngle(bwImg);
                var rotateFilter = new Accord.Imaging.Filters.RotateBilinear(-angle);
                skewedImg = rotateFilter.Apply(img);
                bwImg.Dispose();
                grayImg.Dispose();
                img.Dispose();
                bmp.Dispose();
            }

            // create filter
            BlobsFiltering filter = new BlobsFiltering();
            // configure filter
            filter.CoupledSizeFiltering = true;
            filter.MinHeight = 200;   // 1" @ 200 DPI
            filter.MinWidth = 200;   // 1" @ 200 DPI
            // apply the filter
            filter.ApplyInPlace(skewedImg);

            // save output file
            Bitmap finalImg = skewedImg.ToManagedImage();
            skewedImg.Dispose();
            System.IO.File.Delete(outputPath);
            finalImg.Save(outputPath);
            finalImg.Dispose();
        }
    }
}
