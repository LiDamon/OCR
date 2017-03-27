using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Accord.Imaging;
using System.Drawing;

using Common;

namespace ShapeDetection
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Logger.Log("Usage: ShapeDetector input output");
                return;
            }

            try
            {
                BlobDetector worker = new ShapeDetection.BlobDetector();
                worker.GetBlobs3(args[0], args[1]);
            }
            catch (Exception e)
            {
                Logger.LogError($"Exception: {e.Message} at {e.StackTrace}");
            }
        }
    }
}
