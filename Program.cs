using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Drawing;
using System.Threading.Tasks;


namespace SpritePacker
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Util.ParameterParser();
            var paramIn = parser.Add("in", null, "The directory from where sprite sheets will be recursively fetched. May contain a path.");
            var paramOut = parser.Add("out", null, "The filename to be used for the exported files, without extension. May contain a path. The file index and extension will be automatically appended.");
            var paramPrefix = parser.Add("prefix", "", "A prefix to be inserted before sprite names.");
            var paramUseFolders = parser.Add("use-folders", "on", "Whether to use folder structure as a prefix for sprite names.");
            var paramMaxSize = parser.Add("max-size", "2048", "The maximum width and height of the exported images, in pixels.");
            var paramMargin = parser.Add("margin", "1", "The minimum space between any sprites, in pixels.");
            var paramBleedingMargin = parser.Add("bleeding-margin", "0", "The margin in which to extend the border colors of sprites, in pixels.");
            var paramCrop = parser.Add("crop", "on", "Crop transparent sprite areas to save space.");
            var paramUseSrcExt = parser.Add("use-src-ext", "on", "Whether to include the file extension in the \"src\" field.");
            var paramDebug = parser.Add("debug", "off", "Whether to print debug information.");

            Console.Out.WriteLine("SpritePacker v0.7");
            Console.Out.WriteLine("Copyright 2016 Henrique Lorenzi");
            Console.Out.WriteLine("Build date: 29 apr 2016");
            Console.Out.WriteLine();

            if (!parser.Parse(args) ||
                !paramIn.HasValue() ||
                !paramOut.HasValue())
            {
                Console.Out.WriteLine("Parameters:");
                parser.PrintHelp("  ");
                return;
            }

            srcDir = Path.GetFullPath(paramIn.GetString());
            outName = Path.GetFullPath(paramOut.GetString());
            sprNamePrefix = paramPrefix.GetString();
            maxSize = paramMaxSize.GetInt();
            margin = paramMargin.GetInt();
            bleedingMargin = paramBleedingMargin.GetInt();
            useFolders = paramUseFolders.GetBool();
            crop = paramCrop.GetBool();
            useSrcExt = paramUseSrcExt.GetBool();
            debug = paramDebug.GetBool();

            var result = Export();
            if (result)
                Console.Out.WriteLine("Export successful.");
            else
                Console.Out.WriteLine("Error: Could not fit every sprite.");
        }




        private static string srcDir;
        private static string outName;
        private static string sprNamePrefix;
        private static int maxSize;
        private static int margin;
        private static int bleedingMargin;
        private static bool useFolders;
        private static bool crop;
        private static bool useSrcExt;
        private static bool debug;


        private class DataObject
        {
            public string name;
            public List<string> attributeNames = new List<string>();
            public List<string> attributeValues = new List<string>();
        }


        private class Sprite
        {
            public string sheetFilename;
            public string sheetImageFilename;
            public string spriteLocalName;
            public string spriteFullName;
            public int x, y, width, height;
            public int cropLeft, cropRight, cropTop, cropBottom;
            public List<DataObject> dataObjects = new List<DataObject>();
        }


        public static bool Export()
        {
            var masterList = new List<string>();
            foreach (var file in Directory.GetFiles(srcDir, "*.sprsheet", SearchOption.AllDirectories))
            {
                masterList.Add(PathUtil.MakeRelativePath(srcDir + Path.DirectorySeparatorChar, file));
            }

            using (var f = new FileStream(outName + ".json", FileMode.Create))
            {
                var s = new IndentedStream(f, "\t");
                s.WriteLine("{");
                s.Indent();
                s.WriteLine("\"sprites\":");
                s.WriteLine("[");
                s.Indent();

                Console.Out.WriteLine("Reading sprite sheets...");

                var spritesToExport = new List<Sprite>();
                Parallel.For(0, masterList.Count, (i) =>
                {
                    var sheetSprites = LoadSheetFile(PathUtil.MakeAbsolutePath(srcDir, masterList[i]));
                    lock (spritesToExport)
                    {
                        spritesToExport.AddRange(sheetSprites);
                    }
                });
                Console.Out.WriteLine("Found " + spritesToExport.Count + " sprites.");
                Console.Out.WriteLine();

                spritesToExport.Sort((a, b) =>
                    ((b.width - (crop ? b.cropLeft + b.cropRight : 0)) *
                     (b.height - (crop ? b.cropTop + b.cropBottom : 0))) -
                     ((a.width - (crop ? a.cropLeft + a.cropRight : 0)) *
                     (a.height - (crop ? a.cropTop + a.cropBottom : 0))));

                var spritesToExportNum = spritesToExport.Count;
                var maxArea = maxSize * maxSize;
                var exportCount = 0;

                while (spritesToExport.Count > 0)
                {
                    Console.Out.WriteLine("" + spritesToExport.Count + " sprites remaining to pack...");

                    var packing = TryPacking(spritesToExport, maxSize);
                    if (packing == null || packing.rectangles.Count == 0)
                        return false;

                    foreach (var rect in packing.rectangles)
                        spritesToExport.Remove((Sprite)rect.tag);

                    Console.Out.WriteLine("" + packing.rectangles.Count + " sprites were fitted in pack " + exportCount + ".");
                    Console.Out.WriteLine("Writing image file...");

                    ExportPackFile(
                        packing,
                        outName + "_" + exportCount + ".png");

                    for (int r = 0; r < packing.rectangles.Count; r++)
                    {
                        var rect = packing.rectangles[r];

                        var spr = (Sprite)rect.tag;
                        s.WriteLine("{");
                        s.Indent();

                        s.WriteLine("\"name\": \"" + spr.spriteFullName + "\",");
                        s.WriteLine("\"src\": \"" + Path.GetFileName(outName) + "_" + exportCount + (useSrcExt ? ".png" : "") + "\",");
                        s.WriteLine("\"x\": " + (rect.x + bleedingMargin) + ",");
                        s.WriteLine("\"y\": " + (rect.y + bleedingMargin) + ",");
                        s.WriteLine("\"width\": " + rect.width + ",");
                        s.WriteLine("\"height\": " + rect.height + ",");
                        if (crop)
                        {
                            s.WriteLine("\"crop-left\": " + spr.cropLeft + ",");
                            s.WriteLine("\"crop-right\": " + spr.cropRight + ",");
                            s.WriteLine("\"crop-top\": " + spr.cropTop + ",");
                            s.WriteLine("\"crop-bottom\": " + spr.cropBottom + ",");
                        }
                        else
                        {
                            s.WriteLine("\"crop-left\": 0,");
                            s.WriteLine("\"crop-right\": 0,");
                            s.WriteLine("\"crop-top\": 0,");
                            s.WriteLine("\"crop-bottom\": 0,");
                        }

                        s.WriteLine("\"guides\":");
                        s.WriteLine("[");
                        s.Indent();
                        for (int j = 0; j < spr.dataObjects.Count; j++)
                        {
                            var dataObj = spr.dataObjects[j];

                            s.WriteLine("{");
                            s.Indent();
                            for (int i = 0; i < dataObj.attributeNames.Count; i++)
                            {
                                var attrbName = dataObj.attributeNames[i];
                                var attrbValue = dataObj.attributeValues[i];

                                var attrbStr = "\"" + attrbName + "\": ";
                                if (attrbName == "x" || attrbName == "y" ||
                                    attrbName == "x1" || attrbName == "y1" ||
                                    attrbName == "x2" || attrbName == "y2" ||
                                    attrbName == "x-min" || attrbName == "y-min" ||
                                    attrbName == "x-max" || attrbName == "y-max" ||
                                    attrbName == "value")
                                    attrbStr += attrbValue;
                                else
                                    attrbStr += "\"" + attrbValue + "\"";

                                if (i < dataObj.attributeNames.Count - 1)
                                    attrbStr += ",";

                                s.WriteLine(attrbStr);
                            }
                            s.Unindent();
                            s.WriteLine("}" + (j < spr.dataObjects.Count - 1 ? "," : ""));
                        }
                        s.Unindent();
                        s.WriteLine("]");

                        s.Unindent();

                        if (r == packing.rectangles.Count - 1 && spritesToExport.Count == 0)
                            s.WriteLine("}");
                        else
                            s.WriteLine("},");
                    }

                    Console.Out.WriteLine();
                    exportCount++;
                }

                s.Unindent();
                s.WriteLine("]");
                s.Unindent();
                s.WriteLine("}");
            }

            return true;
        }

        private static List<Sprite> LoadSheetFile(string filename)
        {
            var sprites = new List<Sprite>();

            var spriteFullNamePathPrefix =
                (Path.GetDirectoryName(PathUtil.MakeRelativePath(srcDir + Path.DirectorySeparatorChar, filename)) +
                Path.DirectorySeparatorChar).
                Replace(Path.DirectorySeparatorChar, '/');

            if (spriteFullNamePathPrefix == "/")
                spriteFullNamePathPrefix = "";

            if (!useFolders)
                spriteFullNamePathPrefix = "";

            using (var f = new FileStream(filename, FileMode.Open))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(f);

                var nodeSpriteSheet = xml.GetElementsByTagName("sprite-sheet")[0];

                var sheetSrcImageFile = PathUtil.MakeAbsolutePath(Path.GetDirectoryName(filename), nodeSpriteSheet.Attributes["src"].Value);
                var sheetBitmap = new Bitmap(sheetSrcImageFile);
                var sheetBitmapBits = sheetBitmap.LockBits(
                    new Rectangle(0, 0, sheetBitmap.Width, sheetBitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                
                for (int i = 0; i < nodeSpriteSheet.ChildNodes.Count; i++)
                {
                    var node = nodeSpriteSheet.ChildNodes[i];
                    if (node.LocalName == "sprite")
                    {
                        var spr = new Sprite();
                        spr.sheetFilename = filename;
                        spr.sheetImageFilename = sheetSrcImageFile;
                        spr.spriteLocalName = node.Attributes["name"].Value.ToLower();
                        spr.spriteFullName = (sprNamePrefix + spriteFullNamePathPrefix + spr.spriteLocalName).ToLower();
                        spr.x = Convert.ToInt32(node.Attributes["x"].Value);
                        spr.y = Convert.ToInt32(node.Attributes["y"].Value);
                        spr.width = Convert.ToInt32(node.Attributes["width"].Value);
                        spr.height = Convert.ToInt32(node.Attributes["height"].Value);

                        FindCroppableArea(sheetBitmapBits,
                            spr.x, spr.y, spr.width, spr.height,
                            out spr.cropLeft, out spr.cropRight, out spr.cropTop, out spr.cropBottom);

                        foreach (XmlNode data in node.ChildNodes)
                        {
                            var dataObj = new DataObject();
                            dataObj.name = data.LocalName;

                            foreach (XmlAttribute attrb in data.Attributes)
                            {
                                dataObj.attributeNames.Add(attrb.Name);
                                dataObj.attributeValues.Add(attrb.Value);

                                if (dataObj.name == "guide" && attrb.Name == "name" && attrb.Value == "only-guides")
                                {
                                    spr.width = 1;
                                    spr.height = 1;
                                    spr.cropLeft = 0;
                                    spr.cropRight = 0;
                                    spr.cropTop = 0;
                                    spr.cropBottom = 0;
                                }
                            }
                            spr.dataObjects.Add(dataObj);
                        }

                        sprites.Add(spr);
                    }
                }

                sheetBitmap.UnlockBits(sheetBitmapBits);
            }

            return sprites;
        }


        private static RectanglePacker.Packing TryPacking(List<Sprite> sprites, int maxSize)
        {
            var rectList = new List<RectanglePacker.Rectangle>();

            foreach (var spr in sprites)
            {
                var rect = new RectanglePacker.Rectangle();
                rect.width = spr.width - (crop ? (spr.cropLeft + spr.cropRight) : 0);
                rect.height = spr.height - (crop ? (spr.cropTop + spr.cropBottom) : 0);
                rect.tag = spr;
                rect.debugName = spr.spriteFullName;
                rectList.Add(rect);
            }

            var packing = RectanglePacker.PackAsManyAsPossible(margin + bleedingMargin * 2, maxSize, maxSize, rectList, debug);
            if (packing == null || packing.rectangles.Count == 0)
                return null;

            // FIXME: PackAll is overwriting rectangle positions.
            /*for (var curSize = maxSize; curSize >= 8; curSize /= 2)
            {
                var newPacking = RectanglePacker.PackAll(margin, maxSize, maxSize, packing.rectangles, debug);
                if (newPacking == null || newPacking.rectangles.Count == 0)
                    break;

                packing = newPacking;
            }*/

            return packing;
        }


        private static void ExportPackFile(RectanglePacker.Packing packing, string filename)
        {
            var w = packing.totalWidth;
            var h = packing.totalHeight;

            Bitmap destBmp = new Bitmap(w, h);

            if (w != 0 && h != 0)
            {
                unsafe
                {
                    var destLockBits = destBmp.LockBits(
                        new Rectangle(0, 0, w, h),
                        System.Drawing.Imaging.ImageLockMode.WriteOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    byte* destPixelPtr = (byte*)destLockBits.Scan0.ToPointer();

                    Parallel.For(0, h, (j) =>
                    {
                        byte* writePtr = destPixelPtr + j * destLockBits.Stride;

                        for (int i = 0; i < w; i++)
                        {
                            writePtr[i * 4 + 0] = 0;
                            writePtr[i * 4 + 1] = 0;
                            writePtr[i * 4 + 2] = 0;
                            writePtr[i * 4 + 3] = 0;
                        }
                    });

                    var imageBitmaps = new Dictionary<string, Bitmap>();

                    Parallel.For(0, packing.rectangles.Count, (k) =>
                    {
                        var tag = (Sprite)packing.rectangles[k].tag;

                        Bitmap srcBmp = null;

                        lock (imageBitmaps)
                        {
                            if (!imageBitmaps.TryGetValue(tag.sheetImageFilename, out srcBmp))
                            {
                                try
                                {
                                    srcBmp = new Bitmap(tag.sheetImageFilename);
                                }
                                catch
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("Unable to load image <" + tag.sheetImageFilename + ">.");
                                    goto next;
                                }
                                imageBitmaps.Add(tag.sheetImageFilename, srcBmp);
                            }
                        }

                        lock (srcBmp)
                        {
                            if (srcBmp.Width <= 0 || srcBmp.Height <= 0 ||
                                packing.rectangles[k].width <= 0 ||
                                packing.rectangles[k].height <= 0)
                                goto next;

                            var srcLockBits = srcBmp.LockBits(
                                new Rectangle(0, 0, packing.rectangles[k].width, packing.rectangles[k].height),
                                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                            var srcX = tag.x + (crop ? tag.cropLeft : 0);
                            var srcY = tag.y + (crop ? tag.cropTop : 0);
                            var destX = packing.rectangles[k].x + bleedingMargin;
                            var destY = packing.rectangles[k].y + bleedingMargin;

                            // Write sprite image.
                            for (int j = 0; j < packing.rectangles[k].height; j++)
                            {
                                for (int i = 0; i < packing.rectangles[k].width; i++)
                                {
                                    TransferPixel(
                                        srcLockBits, destLockBits,
                                        srcX + i, srcY + j,
                                        destX + i, destY + j);
                                }
                            }

                            // Write color bleeding.
                            for (int b = 0; b < bleedingMargin; b++)
                            {
                                for (int j = 0; j < packing.rectangles[k].height; j++)
                                {
                                    TransferPixel(
                                        srcLockBits, destLockBits,
                                        srcX, srcY + j,
                                        destX - 1 - b, destY + j);

                                    TransferPixel(
                                        srcLockBits, destLockBits,
                                        srcX + packing.rectangles[k].width - 1, srcY + j,
                                        destX + packing.rectangles[k].width + b, destY + j);
                                }

                                for (int i = 0; i < packing.rectangles[k].width; i++)
                                {
                                    TransferPixel(
                                        srcLockBits, destLockBits,
                                        srcX + i, srcY,
                                        destX + i, destY - 1 - b);

                                    TransferPixel(
                                        srcLockBits, destLockBits,
                                        srcX + i, srcY + packing.rectangles[k].height - 1,
                                        destX + i, destY + packing.rectangles[k].height + b);
                                }

                                for (int c = 0; c < bleedingMargin; c++)
                                {
                                    TransferPixel(
                                        srcLockBits, destLockBits,
                                        srcX, srcY,
                                        destX - 1 - b, destY - 1 - c);

                                    TransferPixel(
                                        srcLockBits, destLockBits,
                                        srcX + packing.rectangles[k].width - 1, srcY,
                                        destX + packing.rectangles[k].width + b, destY - 1 - c);

                                    TransferPixel(
                                        srcLockBits, destLockBits,
                                        srcX, srcY + packing.rectangles[k].height - 1,
                                        destX - 1 - b, destY + packing.rectangles[k].height + c);

                                    TransferPixel(
                                        srcLockBits, destLockBits,
                                        srcX + packing.rectangles[k].width - 1, srcY + packing.rectangles[k].height - 1,
                                        destX + packing.rectangles[k].width + b, destY + packing.rectangles[k].height + c);
                                }
                            }

                            srcBmp.UnlockBits(srcLockBits);
                        }

                    next:;
                    });

                    destBmp.UnlockBits(destLockBits);
                }
            }

            destBmp.Save(filename);
        }


        private static unsafe void TransferPixel(
            System.Drawing.Imaging.BitmapData src,
            System.Drawing.Imaging.BitmapData dest,
            int srcX, int srcY,
            int destX, int destY)
        {
            if (destX < 0 || destY < 0 || destX >= dest.Width || destY >= dest.Height)
                return;

            byte* srcPtr = (byte*)src.Scan0.ToPointer() + srcY * src.Stride + srcX * 4;
            byte* destPtr = (byte*)dest.Scan0.ToPointer() + destY * dest.Stride + destX * 4;

            destPtr[0] = srcPtr[0];
            destPtr[1] = srcPtr[1];
            destPtr[2] = srcPtr[2];
            destPtr[3] = srcPtr[3];
        }


        private static unsafe void FindCroppableArea(
            System.Drawing.Imaging.BitmapData src,
            int x, int y, int width, int height,
            out int left, out int right, out int top, out int bottom)
        {
            right = 0;
            while (right < width && IsColumnCroppable(src, x + width - right - 1, y, height))
                right++;

            left = 0;
            while (left + right < width && IsColumnCroppable(src, x + left + 1, y, height))
                left++;

            bottom = 0;
            while (bottom < height && IsRowCroppable(src, y + height - bottom - 1, x, width))
                bottom++;

            top = 0;
            while (top + bottom < height && IsRowCroppable(src, y + top + 1, x, width))
                top++;
        }


        private static unsafe bool IsColumnCroppable(
            System.Drawing.Imaging.BitmapData src,
            int x, int y, int height)
        {
            for (var j = 0; j < height; j++)
            {
                byte* srcPtr = (byte*)src.Scan0.ToPointer() + (y + j) * src.Stride + x * 4;
                if (srcPtr[3] != 0)
                    return false;
            }

            return true;
        }


        private static unsafe bool IsRowCroppable(
            System.Drawing.Imaging.BitmapData src,
            int y, int x, int width)
        {
            for (var i = 0; i < width; i++)
            {
                byte* srcPtr = (byte*)src.Scan0.ToPointer() + y * src.Stride + (x + i) * 4;
                if (srcPtr[3] != 0)
                    return false;
            }

            return true;
        }
    }
}
