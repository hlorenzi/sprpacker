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
            var paramUseCropInfo = parser.Add("use-crop-info", "on", "Whether to use crop information in sprite sheets to save space.");
            var paramUseSrcExt = parser.Add("use-src-ext", "on", "Whether to include the file extension in the \"src\" field.");
            var paramDebug = parser.Add("debug", "off", "Whether to print debug information.");

            Console.Out.WriteLine("SpritePacker v0.5");
            Console.Out.WriteLine("Copyright 2016 Henrique Lorenzi");
            Console.Out.WriteLine("Build date: 25 apr 2016");
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
            useFolders = paramUseFolders.GetBool();
            useCropInfo = paramUseCropInfo.GetBool();
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
        private static bool useFolders;
        private static bool useCropInfo;
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

                var count = 0.0f;
                Console.Out.WriteLine("Reading sprites...");

                var spritesToExport = new List<Sprite>();
                var imageFiles = new Dictionary<string, string>();
                foreach (var sheet in masterList)
                {
                    GetSheetSprites(imageFiles, spritesToExport, PathUtil.MakeAbsolutePath(srcDir, sheet));
                    count += 1.0f;
                }

                spritesToExport.Sort((a, b) =>
                    ((b.width - (useCropInfo ? b.cropLeft + b.cropRight : 0)) *
                     (b.height - (useCropInfo ? b.cropTop + b.cropBottom : 0))) -
                     ((a.width - (useCropInfo ? a.cropLeft + a.cropRight : 0)) *
                     (a.height - (useCropInfo ? a.cropTop + a.cropBottom : 0))));

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
                        packing, imageFiles,
                        outName + "_" + exportCount + ".png");

                    for (int r = 0; r < packing.rectangles.Count; r++)
                    {
                        var rect = packing.rectangles[r];

                        var spr = (Sprite)rect.tag;
                        s.WriteLine("{");
                        s.Indent();

                        s.WriteLine("\"name\": \"" + spr.spriteFullName + "\",");
                        s.WriteLine("\"src\": \"" + Path.GetFileName(outName) + "_" + exportCount + (useSrcExt ? ".png" : "") + "\",");
                        s.WriteLine("\"x\": " + rect.x + ",");
                        s.WriteLine("\"y\": " + rect.y + ",");
                        s.WriteLine("\"width\": " + rect.width + ",");
                        s.WriteLine("\"height\": " + rect.height + ",");
                        if (useCropInfo)
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

        private static void GetSheetSprites(Dictionary<string, string> imgs, List<Sprite> list, string filename)
        {
            var pathName =
                (Path.GetDirectoryName(PathUtil.MakeRelativePath(srcDir + Path.DirectorySeparatorChar, filename)) +
                Path.DirectorySeparatorChar).
                Replace(Path.DirectorySeparatorChar, '/');

            if (pathName == "/")
                pathName = "";

            using (var f = new FileStream(filename, FileMode.Open))
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(f);

                var nodeSpriteSheet = xml.GetElementsByTagName("sprite-sheet")[0];
                imgs.Add(filename, PathUtil.MakeAbsolutePath(Path.GetDirectoryName(filename), nodeSpriteSheet.Attributes["src"].Value));

                for (int i = 0; i < nodeSpriteSheet.ChildNodes.Count; i++)
                {
                    var node = nodeSpriteSheet.ChildNodes[i];
                    if (node.LocalName == "sprite")
                    {
                        var spr = new Sprite();
                        spr.sheetFilename = filename;
                        spr.spriteLocalName = node.Attributes["name"].Value.ToLower();
                        spr.spriteFullName = (sprNamePrefix + (useFolders ? pathName : "") + spr.spriteLocalName).ToLower();
                        spr.x = Convert.ToInt32(node.Attributes["x"].Value);
                        spr.y = Convert.ToInt32(node.Attributes["y"].Value);
                        spr.width = Convert.ToInt32(node.Attributes["width"].Value);
                        spr.height = Convert.ToInt32(node.Attributes["height"].Value);
                        if (node.Attributes["crop-left"] != null)
                        {
                            spr.cropLeft = Convert.ToInt32(node.Attributes["crop-left"].Value);
                            spr.cropRight = Convert.ToInt32(node.Attributes["crop-right"].Value);
                            spr.cropTop = Convert.ToInt32(node.Attributes["crop-top"].Value);
                            spr.cropBottom = Convert.ToInt32(node.Attributes["crop-bottom"].Value);
                        }

                        foreach (XmlNode data in node.ChildNodes)
                        {
                            var dataObj = new DataObject();
                            dataObj.name = data.LocalName;
                            foreach (XmlAttribute attrb in data.Attributes)
                            {
                                dataObj.attributeNames.Add(attrb.Name);
                                dataObj.attributeValues.Add(attrb.Value);
                            }
                            spr.dataObjects.Add(dataObj);
                        }

                        list.Add(spr);
                    }
                }
            }
        }


        private static RectanglePacker.Packing TryPacking(List<Sprite> sprites, int maxSize)
        {
            var rectList = new List<RectanglePacker.Rectangle>();

            foreach (var spr in sprites)
            {
                var rect = new RectanglePacker.Rectangle();
                rect.width = spr.width - (useCropInfo ? (spr.cropLeft + spr.cropRight) : 0);
                rect.height = spr.height - (useCropInfo ? (spr.cropTop + spr.cropBottom) : 0);
                rect.tag = spr;
                rect.debugName = spr.spriteFullName;
                rectList.Add(rect);
            }

            var packing = RectanglePacker.PackAsManyAsPossible(margin, maxSize, maxSize, rectList, debug);
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


        private static void ExportPackFile(RectanglePacker.Packing packing, Dictionary<string, string> imageFiles, string filename)
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
                            if (!imageBitmaps.TryGetValue(imageFiles[tag.sheetFilename], out srcBmp))
                            {
                                try
                                {
                                    srcBmp = new Bitmap(imageFiles[tag.sheetFilename]);
                                }
                                catch
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("Unable to load image <" + imageFiles[tag.sheetFilename] + ">.");
                                    goto next;
                                }
                                imageBitmaps.Add(imageFiles[tag.sheetFilename], srcBmp);
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

                            var srcPixelPtr = (byte*)srcLockBits.Scan0.ToPointer();

                            for (int j = 0; j < packing.rectangles[k].height; j++)
                            {
                                var readY = tag.y + (useCropInfo ? tag.cropTop : 0) + j;
                                byte* readPtr = srcPixelPtr + readY * srcLockBits.Stride;

                                var writeY = packing.rectangles[k].y + j;
                                byte* writePtr = destPixelPtr + writeY * destLockBits.Stride;

                                for (int i = 0; i < packing.rectangles[k].width; i++)
                                {
                                    var readX = tag.x + (useCropInfo ? tag.cropLeft : 0) + i;
                                    var writeX = packing.rectangles[k].x + i;

                                    if (writeX >= 0 &&
                                        writeY >= 0 &&
                                        writeX < w &&
                                        writeY < h)
                                    {
                                        for (var p = 0; p < 4; p++)
                                        {
                                            writePtr[writeX * 4 + p] = readPtr[readX * 4 + p];
                                        }
                                    }
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
    }
}
