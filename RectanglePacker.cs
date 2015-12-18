using System.Collections.Generic;


namespace SpritePacker
{
    public static class RectanglePacker
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
            public string debugName;
        }


        private class CompareRectangleArea : Comparer<Rectangle>
        {
            public override int Compare(Rectangle x, Rectangle y)
            {
                return (y.width * y.height) - (x.width * x.height);
            }
        }


        public static Packing PackAsManyAsPossible(int margin, int maxWidth, int maxHeight, List<Rectangle> list, bool debug = false)
        {
            List<Rectangle> rects = new List<Rectangle>();
            List<Rectangle> fittedRects = new List<Rectangle>();

            foreach (Rectangle r in list)
                rects.Add(r);

            rects.Sort(new CompareRectangleArea());

            var matrix = new OccupationMatrix(maxWidth, maxHeight);

            foreach (Rectangle rect in rects)
            {
                if (matrix.TryFitting(
                    rect.width + margin,
                    rect.height + margin,
                    out rect.x,
                    out rect.y,
                    debug,
                    rect.debugName))
                {
                    fittedRects.Add(rect);
                }
            }

            var packing = new Packing();
            packing.rectangles = fittedRects;
            packing.totalWidth = maxWidth;
            packing.totalHeight = maxHeight;
            return packing;
        }



        public static Packing PackAll(int margin, int maxWidth, int maxHeight, List<Rectangle> list, bool debug = false)
        {
            List<Rectangle> rects = new List<Rectangle>();

            foreach (Rectangle r in list)
                rects.Add(r);

            rects.Sort(new CompareRectangleArea());

            var matrix = new OccupationMatrix(maxWidth, maxHeight);

            foreach (Rectangle rect in rects)
            {
                if (!matrix.TryFitting(
                    rect.width + margin,
                    rect.height + margin,
                    out rect.x,
                    out rect.y,
                    debug,
                    rect.debugName))
                {
                    return null;
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