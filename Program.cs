using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Drawing;


namespace SpritePacker
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Util.CommandParser();
            parser.AddCommand("in", "The directory from where sprite sheets will be recursively fetched.", null);
            parser.AddCommand("out", "The filename to be used for the exported files, without extension.", null);
            parser.AddCommand("prefix", "A prefix to be inserted before sprite names.", "");
            parser.AddCommand("use-folders", "Whether to use folder structure as a prefix for sprite names.", "on");
            parser.AddCommand("max-size", "The maximum size in pixels in any direction of the exported images.", "2048");
            parser.AddCommand("margin", "The minimum space in pixels between any sprites.", "1");
            parser.AddCommand("use-crop-info", "Whether to use crop information in sprite sheets to save space.", "on");
            parser.AddCommand("use-src-ext", "Whether to include the file extension in the \"src\" field.", "on");

            Console.Out.WriteLine("## SpritePacker v0.1");
            Console.Out.WriteLine("## Written by Henrique Lorenzi, 21 nov 2015");
            Console.Out.WriteLine();

            var commands = new Dictionary<string, string>();
            if (!parser.ParseCommands(args, commands))
            {
                Console.Out.WriteLine("Commands:");
                parser.PrintHelp();
                return;
            }

            srcDir = Path.GetFullPath(commands["in"]);
            outName = Path.GetFullPath(commands["out"]);
            sprNamePrefix = commands["prefix"];
            maxSize = int.Parse(commands["max-size"]);
            margin = int.Parse(commands["margin"]);
            useFolders = (commands["use-folders"] == "on");
            useCropInfo = (commands["use-crop-info"] == "on");
            useSrcExt = (commands["use-src-ext"] == "on");

            Export();
        }




        private static string srcDir;
        private static string outName;
        private static string sprNamePrefix;
        private static int maxSize;
        private static int margin;
        private static bool useFolders;
        private static bool useCropInfo;
        private static bool useSrcExt;


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
            var delCount = 0;
            while (File.Exists(outName + "_" + delCount + ".png"))
            {
                File.Delete(outName + "_" + delCount + ".png");
                delCount++;
            }

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
                Console.Out.WriteLine("Reading sprites from sheet files...");

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
                    Console.Out.WriteLine("" + spritesToExport.Count + " sprites remaining to pack.");

                    var lastSpritesToExportNum = spritesToExport.Count;

                    var tentativeSprites = new List<Sprite>();
                    var tentativeArea = 0;

                    for (int i = 0; i < spritesToExport.Count; i++)
                    {
                        var spr = spritesToExport[i];
                        var area =
                            (spr.width - (useCropInfo ? spr.cropLeft + spr.cropRight : 0)) *
                            (spr.height - (useCropInfo ? spr.cropTop + spr.cropBottom : 0));
                        if (tentativeArea + area < maxArea)
                        {
                            tentativeSprites.Add(spr);
                            tentativeArea += area;
                            if (tentativeArea >= maxArea)
                                break;
                        }
                    }

                    if (tentativeSprites.Count == 0)
                        return false;

                    for (int i = 0; i < tentativeSprites.Count; i++)
                    {
                        spritesToExport.Remove(tentativeSprites[i]);
                    }

                    RectanglePacker.Packing packing = null;

                    Console.Out.WriteLine("Binary search for Pack " + exportCount + " with at most " + tentativeSprites.Count + " sprites:");
                    var searchMin = 0;
                    var searchMax = tentativeSprites.Count * 2;
                    while (true)
                    {
                        var searchMid = (searchMin + searchMax + 1) / 2;

                        Console.Out.Write("-- Trying to fit " + searchMid + " sprites...");

                        var curList = new List<Sprite>(tentativeSprites.Count);
                        for (var i = 0; i < searchMid; i++)
                            curList.Add(tentativeSprites[i]);

                        var curPacking = TryPacking(curList, maxSize);
                        if (curPacking == null)
                        {
                            Console.Out.WriteLine("Failed.");
                            searchMax = searchMid - 1;
                        }
                        else
                        {
                            Console.Out.WriteLine("OK.");
                            searchMin = searchMid;
                            packing = curPacking;

                            if (searchMid >= tentativeSprites.Count)
                                break;
                        }

                        if (searchMin >= searchMax)
                            break;
                    }

                    if (packing == null)
                        return false;

                    for (var rem = (searchMin + searchMax + 1) / 2; rem < tentativeSprites.Count; rem++)
                    {
                        spritesToExport.Add(tentativeSprites[rem]);
                    }

                    Console.Out.Write("Exporting " + packing.rectangles.Count + " sprites for Pack " + exportCount + "...");

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
            var totalArea = 0;

            foreach (var spr in sprites)
            {
                var rect = new RectanglePacker.Rectangle();
                rect.width = spr.width - (useCropInfo ? (spr.cropLeft + spr.cropRight) : 0);
                rect.height = spr.height - (useCropInfo ? (spr.cropTop + spr.cropBottom) : 0);
                rect.tag = spr;
                rectList.Add(rect);
                totalArea += rect.width * rect.height;
            }

            var packer = new RectanglePacker();
            packer.SetBorder(margin);

            RectanglePacker.Packing packing = null;

            for (var curSize = maxSize; curSize >= 8 && curSize * curSize >= totalArea; curSize /= 2)
            {
                var newPacking = packer.Pack(curSize, curSize, rectList);
                if (newPacking == null)
                    break;

                packing = newPacking;
            }

            return packing;
        }


        private static void ExportPackFile(RectanglePacker.Packing packing, Dictionary<string, string> imageFiles, string filename)
        {
            var w = packing.totalWidth;
            var h = packing.totalHeight;

            var progress = 0;

            Bitmap pngpack = new Bitmap(w, h);
            for (int j = 0; j < h; j++)
            {
                for (int i = 0; i < w; i++)
                {
                    pngpack.SetPixel(i, j, Color.FromArgb(0, 0, 0, 0));
                }

                if (j > progress + h / 10)
                {
                    progress += h / 10;
                    Console.Out.Write(".");
                }
            }

            var imageBitmaps = new Dictionary<string, Bitmap>();
            progress = 0;

            for (int k = 0; k < packing.rectangles.Count; k++)
            {
                if (k > progress + packing.rectangles.Count / 20)
                {
                    progress += packing.rectangles.Count / 20;
                    Console.Out.Write(".");
                }

                var tag = (Sprite)packing.rectangles[k].tag;

                Bitmap img = null;
                if (!imageBitmaps.TryGetValue(imageFiles[tag.sheetFilename], out img))
                {
                    try
                    {
                        img = new Bitmap(imageFiles[tag.sheetFilename]);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Unable to load image <" + imageFiles[tag.sheetFilename] + ">.");
                    }
                    imageBitmaps.Add(imageFiles[tag.sheetFilename], img);
                }

                for (int j = 0; j < packing.rectangles[k].height; j++)
                {
                    for (int i = 0; i < packing.rectangles[k].width; i++)
                    {
                        pngpack.SetPixel(
                            packing.rectangles[k].x + i,
                            packing.rectangles[k].y + j,
                            img.GetPixel(
                                tag.x + (useCropInfo ? tag.cropLeft : 0) + i,
                                tag.y + (useCropInfo ? tag.cropTop : 0) + j));
                    }
                }
            }

            pngpack.Save(filename);
        }
    }
}
