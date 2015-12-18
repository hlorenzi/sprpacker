using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpritePacker;


namespace SprPackerTest
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void TestOccupationMatrix()
        {
            var matrix = new OccupationMatrix(10, 10);
            Assert.IsTrue(matrix.GetWidths().Count == 1);
            Assert.IsTrue(matrix.GetHeights().Count == 1);
            Assert.IsTrue(matrix.GetWidths()[0] == 10);
            Assert.IsTrue(matrix.GetHeights()[0] == 10);
            Assert.IsTrue(matrix.GetCells().Count == 1);
            Assert.IsTrue(matrix.GetCells()[0].Count == 1);
            Assert.IsTrue(matrix.GetCells()[0][0] == false);

            int x, y;
            Assert.IsTrue(matrix.TryFitting(5, 11, out x, out y) == false);
            Assert.IsTrue(matrix.GetWidths().Count == 1);
            Assert.IsTrue(matrix.GetHeights().Count == 1);
            Assert.IsTrue(matrix.GetWidths()[0] == 10);
            Assert.IsTrue(matrix.GetHeights()[0] == 10);
            Assert.IsTrue(matrix.GetCells().Count == 1);
            Assert.IsTrue(matrix.GetCells()[0].Count == 1);
            Assert.IsTrue(matrix.GetCells()[0][0] == false);

            Assert.IsTrue(matrix.TryFitting(3, 4, out x, out y));
            Assert.IsTrue(x == 0);
            Assert.IsTrue(y == 0);
            Assert.IsTrue(matrix.GetWidths().Count == 2);
            Assert.IsTrue(matrix.GetHeights().Count == 2);
            Assert.IsTrue(matrix.GetWidths()[0] == 3);
            Assert.IsTrue(matrix.GetWidths()[1] == 7);
            Assert.IsTrue(matrix.GetHeights()[0] == 4);
            Assert.IsTrue(matrix.GetHeights()[1] == 6);
            Assert.IsTrue(matrix.GetCells().Count == 2);
            Assert.IsTrue(matrix.GetCells()[0].Count == 2);
            Assert.IsTrue(matrix.GetCells()[1].Count == 2);
            Assert.IsTrue(matrix.GetCells()[0][0] == true);
            Assert.IsTrue(matrix.GetCells()[0][1] == false);
            Assert.IsTrue(matrix.GetCells()[1][0] == false);
            Assert.IsTrue(matrix.GetCells()[1][1] == false);

            Assert.IsTrue(matrix.TryFitting(2, 1, out x, out y));
            Assert.IsTrue(x == 0);
            Assert.IsTrue(y == 4);
            Assert.IsTrue(matrix.GetWidths().Count == 3);
            Assert.IsTrue(matrix.GetHeights().Count == 3);
            Assert.IsTrue(matrix.GetWidths()[0] == 2);
            Assert.IsTrue(matrix.GetWidths()[1] == 1);
            Assert.IsTrue(matrix.GetWidths()[2] == 7);
            Assert.IsTrue(matrix.GetHeights()[0] == 4);
            Assert.IsTrue(matrix.GetHeights()[1] == 1);
            Assert.IsTrue(matrix.GetHeights()[2] == 5);
            Assert.IsTrue(matrix.GetCells().Count == 3);
            Assert.IsTrue(matrix.GetCells()[0].Count == 3);
            Assert.IsTrue(matrix.GetCells()[1].Count == 3);
            Assert.IsTrue(matrix.GetCells()[2].Count == 3);
            Assert.IsTrue(matrix.GetCells()[0][0] == true);
            Assert.IsTrue(matrix.GetCells()[0][1] == true);
            Assert.IsTrue(matrix.GetCells()[0][2] == false);
            Assert.IsTrue(matrix.GetCells()[1][0] == true);
            Assert.IsTrue(matrix.GetCells()[1][1] == false);
            Assert.IsTrue(matrix.GetCells()[1][2] == false);
            Assert.IsTrue(matrix.GetCells()[2][0] == false);
            Assert.IsTrue(matrix.GetCells()[2][1] == false);
            Assert.IsTrue(matrix.GetCells()[2][2] == false);

            Assert.IsTrue(matrix.TryFitting(8, 7, out x, out y) == false);

            Assert.IsTrue(matrix.TryFitting(1, 1, out x, out y));
            Assert.IsTrue(x == 0);
            Assert.IsTrue(y == 5);

            Assert.IsTrue(matrix.TryFitting(10, 4, out x, out y));
            Assert.IsTrue(x == 0);
            Assert.IsTrue(y == 6);


            matrix = new OccupationMatrix(10, 10);
            Assert.IsTrue(matrix.TryFitting(10, 10, out x, out y));
            Assert.IsTrue(x == 0);
            Assert.IsTrue(y == 0);


            for (var w = 1; w < 10; w++)
            {
                for (var h = 1; h < 10; h++)
                {
                    matrix = new OccupationMatrix(10, 10);
                    for (var i = 0; i < 10 / w; i++)
                    {
                        for (var j = 0; j < 10 / h; j++)
                        {
                            Assert.IsTrue(matrix.TryFitting(w, h, out x, out y));
                            Assert.IsTrue(x == i * w);
                            Assert.IsTrue(y == j * h);
                        }
                    }

                    Assert.IsTrue(matrix.TryFitting(w, h, out x, out y) == false);
                }
            }


            matrix = new OccupationMatrix(10, 10);
            Assert.IsTrue(matrix.TryFitting(5, 5, out x, out y));
            Assert.IsTrue(x == 0);
            Assert.IsTrue(y == 0);

            Assert.IsTrue(matrix.TryFitting(6, 6, out x, out y) == false);
        }


        [TestMethod]
        public void TestOccupationMatrixInternals()
        {
            var matrix = new OccupationMatrix(10, 10);
            matrix.AddOccupation(0, 0, 10, 10);


            matrix = new OccupationMatrix(10, 10);
            matrix.AddOccupation(0, 0, 9, 9);
            matrix.AddOccupation(1, 0, 1, 9);
            matrix.AddOccupation(0, 1, 9, 1);
            matrix.AddOccupation(1, 1, 1, 1);


            matrix = new OccupationMatrix(10, 10);
            matrix.AddOccupation(0, 0, 3, 3);
            matrix.AddOccupation(0, 1, 3, 3);
            matrix.AddOccupation(0, 2, 3, 3);
            matrix.AddOccupation(1, 0, 3, 2);
            matrix.AddOccupation(1, 1, 3, 2);
        }
    }
}
