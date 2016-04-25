using System;
using System.Collections.Generic;


namespace SpritePacker
{
    public class OccupationMatrix
    {
        int totalWidth;
        int totalHeight;
        List<int> widths = new List<int>();
        List<int> heights = new List<int>();
        List<List<bool>> cells = new List<List<bool>>();


        public OccupationMatrix(int width, int height)
        {
            this.totalWidth = width;
            this.totalHeight = height;
            this.widths.Add(width);
            this.heights.Add(height);
            this.cells.Add(new List<bool> { false });
        }


        public List<int> GetWidths()
        {
            return widths;
        }


        public List<int> GetHeights()
        {
            return heights;
        }


        public List<List<bool>> GetCells()
        {
            return cells;
        }


        public bool TryFitting(int width, int height, out int x, out int y, bool printDebug = false, string debugName = null)
        {
            if (width <= 0 || height <= 0)
            {
                x = 0;
                y = 0;
                return true;
            }

            var accumX = 0;
            for (var i = 0; i < widths.Count; accumX += widths[i], i++)
            {
                var accumY = 0;
                for (var j = 0; j < heights.Count; accumY += heights[j], j++)
                {
                    var maxI = i;
                    var maxJ = j;

                    var accumHeight = 0;
                    while (accumHeight < height)
                    {
                        if (maxJ >= heights.Count)
                            goto next;

                        accumHeight += heights[maxJ];
                        maxJ++;
                    }

                    var accumWidth = 0;
                    while (accumWidth < width)
                    {
                        if (maxI >= widths.Count)
                            goto next;

                        accumWidth += widths[maxI];
                        maxI++;
                    }

                    for (var fillJ = j; fillJ < maxJ; fillJ++)
                    {
                        for (var fillI = i; fillI < maxI; fillI++)
                        {
                            if (cells[fillJ][fillI])
                                goto next;
                        }
                    }

                    x = accumX;
                    y = accumY;

                    if (x < 0 ||
                        y < 0 ||
                        x + width > this.totalWidth ||
                        y + height > this.totalHeight)
                        System.Diagnostics.Trace.Fail("size miscalculation");

                    AddOccupation(i, j, width, height, printDebug, debugName);
                    return true;

                next:
                    continue;
                }
            }

            x = -1;
            y = -1;
            return false;
        }


        public void AddOccupation(int i, int j, int width, int height, bool printDebug = false, string debugName = null)
        {
            if (width <= 0 || height <= 0)
                return;

            if (printDebug)
            {
                Console.WriteLine("Fitted \"" + debugName + "\", size (" + width + ", " + height + "), at cell (" + i + ", " + j + "), at position (" + GetXAtCell(i) + ", " + GetYAtCell(j) + ")");
                PrintDebug(i, j);
            }

            var maxI = i;
            var maxJ = j;

            var accumHeight = 0;
            while (accumHeight < height)
            {
                accumHeight += heights[maxJ];

                if (accumHeight > height)
                    SplitHeight(maxJ, accumHeight - height);

                maxJ++;
            }

            var accumWidth = 0;
            while (accumWidth < width)
            {
                accumWidth += widths[maxI];

                if (accumWidth > width)
                    SplitWidth(maxI, accumWidth - width);

                maxI++;
            }

            if (printDebug)
                PrintDebug(i, j);

            for (var fillJ = j; fillJ < maxJ; fillJ++)
            {
                for (var fillI = i; fillI < maxI; fillI++)
                {
                    System.Diagnostics.Trace.Assert(!cells[fillJ][fillI]);
                    cells[fillJ][fillI] = true;
                }
            }

            if (printDebug)
                PrintDebug(-1, -1);
        }


        private int GetXAtCell(int i)
        {
            var accum = 0;
            while (i >= 0)
            {
                accum += widths[i];
                i--;
            }
            return accum;
        }


        private int GetYAtCell(int j)
        {
            var accum = 0;
            while (j >= 0)
            {
                accum += heights[j];
                j--;
            }
            return accum;
        }


        private void SplitWidth(int i, int rest)
        {
            widths[i] -= rest;
            widths.Insert(i + 1, rest);

            foreach (var row in cells)
                row.Insert(i + 1, row[i]);
        }


        private void SplitHeight(int j, int rest)
        {
            heights[j] -= rest;
            heights.Insert(j + 1, rest);

            cells.Insert(j + 1, new List<bool>(cells[j]));
        }


        public void PrintDebug(int highlightX = -1, int highlightY = -1)
        {
            Console.Write("".PadLeft(5));
            foreach (var w in widths)
                Console.Write(w.ToString().PadLeft(5));

            for (var j = 0; j < heights.Count; j++)
            {
                Console.WriteLine();
                Console.Write(heights[j].ToString().PadLeft(5));
                for (var i = 0; i < widths.Count; i++)
                {
                    if (i == highlightX && j == highlightY)
                        Console.Write(("$$$$").PadLeft(5));
                    else
                        Console.Write((cells[j][i] ? "####" : "").PadLeft(5));
                }
            }

            Console.WriteLine();
            Console.WriteLine();
        }
    }
}
