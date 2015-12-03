using System.Collections.Generic;


namespace SpritePacker
{
    class RectanglePacker
    {
        public class Packing
        {
            public int totalWidth, totalHeight;
            public List<Rectangle> rectangles;
        }

        public class Rectangle
        {
            public int x, y, width, height;
            public object tag;
        }

        public class CompareRectangleArea : Comparer<Rectangle>
        {
            public override int Compare(Rectangle x, Rectangle y)
            {
                return (y.width * y.height) - (x.width * x.height);
            }
        }

        int border = 0;

        public void SetBorder(int b)
        {
            border = b;
        }

        public Packing Pack(int maxWidth, int maxHeight, List<Rectangle> list)
        {
            List<Rectangle> rects = new List<Rectangle>();
            List<Rectangle> fittedRects = new List<Rectangle>();

            foreach (Rectangle r in list)
                rects.Add(r);

            rects.Sort(new CompareRectangleArea());

            List<List<bool>> cellsOccupation = new List<List<bool>>();
            List<int> cellsWidth = new List<int>();
            List<int> cellsHeight = new List<int>();

            cellsOccupation.Add(new List<bool>());
            cellsOccupation[0].Add(false);
            cellsWidth.Add(maxWidth);
            cellsHeight.Add(maxHeight);

            foreach (Rectangle rect in rects)
            {
                int rectWidth = rect.width + border;
                int rectHeight = rect.height + border;

                if (rectWidth > maxWidth || rectHeight > maxHeight)
                    continue;

                bool fit = false;
                int fitX = -1;
                int fitY = -1;
                int spanX = 0;
                int spanY = 0;
                int remainX = 0;
                int remainY = 0;

                for (int i = 0; i < cellsWidth.Count; i++)
                {
                    for (int j = 0; j < cellsHeight.Count; j++)
                    {
                        if (cellsWidth[i] == 0 || cellsHeight[j] == 0) continue;

                        int accumWidth = 0;
                        int accumHeight = 0;
                        spanX = 0;
                        spanY = 0;

                        while (accumHeight < rectHeight)
                        {
                            if (j + spanY >= cellsHeight.Count) goto didNotFit;
                            if (cellsOccupation[(j + spanY)][i]) goto didNotFit;

                            remainY = cellsHeight[j + spanY] - (rectHeight - accumHeight);
                            accumHeight += cellsHeight[j + spanY];
                            spanY++;
                        }

                        while (accumWidth < rectWidth)
                        {
                            if (i + spanX >= cellsWidth.Count) goto didNotFit;

                            for (int jj = j; jj < j + spanY; jj++)
                            {
                                if (cellsOccupation[jj][i + spanX]) goto didNotFit;
                            }

                            remainX = cellsWidth[i + spanX] - (rectWidth - accumWidth);
                            accumWidth += cellsWidth[i + spanX];
                            spanX++;
                        }

                        fit = true;
                        fitX = i;
                        fitY = j;
                        goto endFitLoop;

                    didNotFit:
                        continue;
                    }
                }

            endFitLoop:
                if (!fit)
                    continue;
                else
                {
                    fittedRects.Add(rect);

                    int placeX = 0;
                    int placeY = 0;
                    for (int k = 0; k < fitX; k++) placeX += cellsWidth[k];
                    for (int k = 0; k < fitY; k++) placeY += cellsHeight[k];
                    rect.x = placeX;
                    rect.y = placeY;

                    if (remainX > 0)
                    {
                        cellsWidth[fitX + spanX - 1] -= remainX;
                        cellsWidth.Insert(fitX + spanX, remainX);

                        for (int k = 0; k < cellsHeight.Count; k++)
                        {
                            cellsOccupation[k].Insert(fitX + spanX, cellsOccupation[k][fitX + spanX - 1]);
                        }
                    }

                    if (remainY > 0)
                    {
                        cellsHeight[fitY + spanY - 1] -= remainY;
                        cellsHeight.Insert(fitY + spanY, remainY);

                        cellsOccupation.Insert(fitY + spanY, new List<bool>());
                        for (int k = 0; k < cellsWidth.Count; k++)
                            cellsOccupation[fitY + spanY].Add(cellsOccupation[fitY + spanY - 1][k]);
                    }

                    for (int j = fitY; j < fitY + spanY; j++)
                    {
                        for (int i = fitX; i < fitX + spanX; i++)
                        {
                            cellsOccupation[j][i] = true;
                        }
                    }
                }
            }

            var packing = new Packing();
            packing.rectangles = fittedRects;
            packing.totalWidth = maxWidth;
            packing.totalHeight = maxHeight;
            return packing;
        }



        public Packing PackAll(int maxWidth, int maxHeight, List<Rectangle> list)
        {
            List<Rectangle> rects = new List<Rectangle>();

            foreach (Rectangle r in list)
                rects.Add(r);

            rects.Sort(new CompareRectangleArea());

            List<List<bool>> cellsOccupation = new List<List<bool>>();
            List<int> cellsWidth = new List<int>();
            List<int> cellsHeight = new List<int>();

            cellsOccupation.Add(new List<bool>());
            cellsOccupation[0].Add(false);
            cellsWidth.Add(maxWidth);
            cellsHeight.Add(maxHeight);

            foreach (Rectangle rect in rects)
            {
                int rectWidth = rect.width + border;
                int rectHeight = rect.height + border;

                if (rectWidth > maxWidth || rectHeight > maxHeight)
                    return null;

                bool fit = false;
                int fitX = -1;
                int fitY = -1;
                int spanX = 0;
                int spanY = 0;
                int remainX = 0;
                int remainY = 0;

                for (int i = 0; i < cellsWidth.Count; i++)
                {
                    for (int j = 0; j < cellsHeight.Count; j++)
                    {
                        if (cellsWidth[i] == 0 || cellsHeight[j] == 0) continue;

                        int accumWidth = 0;
                        int accumHeight = 0;
                        spanX = 0;
                        spanY = 0;

                        while (accumHeight < rectHeight)
                        {
                            if (j + spanY >= cellsHeight.Count) goto didNotFit;
                            if (cellsOccupation[(j + spanY)][i]) goto didNotFit;

                            remainY = cellsHeight[j + spanY] - (rectHeight - accumHeight);
                            accumHeight += cellsHeight[j + spanY];
                            spanY++;
                        }

                        while (accumWidth < rectWidth)
                        {
                            if (i + spanX >= cellsWidth.Count) goto didNotFit;

                            for (int jj = j; jj < j + spanY; jj++)
                            {
                                if (cellsOccupation[jj][i + spanX]) goto didNotFit;
                            }

                            remainX = cellsWidth[i + spanX] - (rectWidth - accumWidth);
                            accumWidth += cellsWidth[i + spanX];
                            spanX++;
                        }

                        fit = true;
                        fitX = i;
                        fitY = j;
                        goto endFitLoop;

                    didNotFit:
                        continue;
                    }
                }

            endFitLoop:
                if (!fit)
                    return null;
                else
                {
                    int placeX = 0;
                    int placeY = 0;
                    for (int k = 0; k < fitX; k++) placeX += cellsWidth[k];
                    for (int k = 0; k < fitY; k++) placeY += cellsHeight[k];
                    rect.x = placeX;
                    rect.y = placeY;

                    if (remainX > 0)
                    {
                        cellsWidth[fitX + spanX - 1] -= remainX;
                        cellsWidth.Insert(fitX + spanX, remainX);

                        for (int k = 0; k < cellsHeight.Count; k++)
                        {
                            cellsOccupation[k].Insert(fitX + spanX, cellsOccupation[k][fitX + spanX - 1]);
                        }
                    }

                    if (remainY > 0)
                    {
                        cellsHeight[fitY + spanY - 1] -= remainY;
                        cellsHeight.Insert(fitY + spanY, remainY);

                        cellsOccupation.Insert(fitY + spanY, new List<bool>());
                        for (int k = 0; k < cellsWidth.Count; k++)
                            cellsOccupation[fitY + spanY].Add(cellsOccupation[fitY + spanY - 1][k]);
                    }

                    for (int j = fitY; j < fitY + spanY; j++)
                    {
                        for (int i = fitX; i < fitX + spanX; i++)
                        {
                            cellsOccupation[j][i] = true;
                        }
                    }
                }
            }

            var packing = new Packing();
            packing.rectangles = rects;
            packing.totalWidth = maxWidth;
            packing.totalHeight = maxHeight;
            return packing;
        }
    }
}