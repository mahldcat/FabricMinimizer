using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkylineCompress
{
    public class GuillotineBinPack
    {
        private IList<Rect> freeRectangles = new List<Rect>();
        private IList<Rect> usedRectangles = new List<Rect>();
        public void Init(int width, int height)
        {
            Rect r = new Rect
            {
                x = 0,
                y = 0,
                width = width,
                height = height
            };

            freeRectangles.Clear();
            freeRectangles.Add(r);
        }

        public IList<Rect> FreeRectangles
        {
            get { return freeRectangles; }
        }

        /*           
           Rect node = wasteMap.Insert(width, height, true,
             FreeRectChoiceHeuristic.RectBestShortSideFit,
             GuillotineSplitHeuristic.SplitMaximizeArea);
         */
        public Rect Insert(int width, int height, bool merge, FreeRectChoiceHeuristic rectChoice,
            GuillotineSplitHeuristic splitMethod)
        {
            // Find where to put the new rectangle.
            int freeNodeIndex = 0;
            Rect newRect = FindPositionForNewNode(width, height, rectChoice, ref freeNodeIndex);

            // Abort if we didn't have enough space in the bin.
            if (newRect.height == 0)
                return newRect;

            // Remove the space that was just consumed by the new rectangle.
            SplitFreeRectByHeuristic(freeRectangles[freeNodeIndex], newRect, splitMethod);
            freeRectangles.RemoveAt(freeNodeIndex);

            // Perform a Rectangle Merge step if desired.
            if (merge)
            {
                MergeFreeList();
            }
            // Remember the new used rectangle.
            usedRectangles.Insert(usedRectangles.Count, newRect);
            return newRect;
        }

        void MergeFreeList()
        {
            // Do a Theta(n^2) loop to see if any pair of free rectangles could me merged into one.
            // Note that we miss any opportunities to merge three rectangles into one. (should call this function again to detect that)
            for (int i = 0; i < freeRectangles.Count; ++i)
                for (int j = i + 1; j < freeRectangles.Count; ++j)
                {
                    if (freeRectangles[i].width == freeRectangles[j].width && freeRectangles[i].x == freeRectangles[j].x)
                    {
                        if (freeRectangles[i].y == freeRectangles[j].y + freeRectangles[j].height)
                        {
                            Rect r_i = freeRectangles[i];
                            Rect r_j = freeRectangles[j];
                            r_i.y -= r_j.height;
                            r_i.height += r_j.height;
                            freeRectangles.RemoveAt(j);
                            --j;
                        }
                        else if (freeRectangles[i].y + freeRectangles[i].height == freeRectangles[j].y)
                        {
                            Rect r_i = freeRectangles[i];
                            Rect r_j = freeRectangles[j];
                            r_i.height += r_j.height;
                            freeRectangles.RemoveAt(j);
                            --j;
                        }
                    }
                    else if (freeRectangles[i].height == freeRectangles[j].height && freeRectangles[i].y == freeRectangles[j].y)
                    {
                        if (freeRectangles[i].x == freeRectangles[j].x + freeRectangles[j].width)
                        {
                            Rect r_i = freeRectangles[i];
                            Rect r_j = freeRectangles[j];
                            r_i.x -= r_j.width;
                            r_i.width += r_j.width;
                            freeRectangles.RemoveAt(j);
                            --j;
                        }
                        else if (freeRectangles[i].x + freeRectangles[i].width == freeRectangles[j].x)
                        {
                            Rect r_i = freeRectangles[i];
                            Rect r_j = freeRectangles[j];
                            r_i.width += r_j.width;
                            freeRectangles.RemoveAt(j);
                            --j;
                        }
                    }
                }
        }


        Rect FindPositionForNewNode(int width, int height, FreeRectChoiceHeuristic rectChoice, ref int nodeIndex)
        {
            Rect bestNode = new Rect();

            int bestScore = int.MaxValue;

            /// Try each free rectangle to find the best one for placement.
            for (int i = 0; i < freeRectangles.Count; ++i)
            {
                // If this is a perfect fit upright, choose it immediately.
                if (width == freeRectangles[i].width && height == freeRectangles[i].height)
                {
                    bestNode.x = freeRectangles[i].x;
                    bestNode.y = freeRectangles[i].y;
                    bestNode.width = width;
                    bestNode.height = height;
                    bestScore = int.MinValue;
                    nodeIndex = i;
                    break;
                }
                // If this is a perfect fit sideways, choose it.
                else if (height == freeRectangles[i].width && width == freeRectangles[i].height)
                {
                    bestNode.x = freeRectangles[i].x;
                    bestNode.y = freeRectangles[i].y;
                    bestNode.width = height;
                    bestNode.height = width;
                    bestScore = int.MinValue;
                    nodeIndex = i;
                    break;
                }
                // Does the rectangle fit upright?
                else if (width <= freeRectangles[i].width && height <= freeRectangles[i].height)
                {
                    int score = ScoreByHeuristic(width, height, freeRectangles[i], rectChoice);

                    if (score < bestScore)
                    {
                        bestNode.x = freeRectangles[i].x;
                        bestNode.y = freeRectangles[i].y;
                        bestNode.width = width;
                        bestNode.height = height;
                        bestScore = score;
                        nodeIndex = i;
                    }
                }
                // Does the rectangle fit sideways?
                else if (height <= freeRectangles[i].width && width <= freeRectangles[i].height)
                {
                    int score = ScoreByHeuristic(height, width, freeRectangles[i], rectChoice);

                    if (score < bestScore)
                    {
                        bestNode.x = freeRectangles[i].x;
                        bestNode.y = freeRectangles[i].y;
                        bestNode.width = height;
                        bestNode.height = width;
                        bestScore = score;
                        nodeIndex = i;
                    }
                }
            }
            return bestNode;
        }

        int ScoreByHeuristic(int width, int height, Rect freeRect, FreeRectChoiceHeuristic rectChoice)
        {
            switch (rectChoice)
            {
                /*
        case FreeRectChoiceHeuristic.RectBestAreaFit: return ScoreBestAreaFit(width, height, freeRect);
        case FreeRectChoiceHeuristic.RectBestShortSideFit: return ScoreBestShortSideFit(width, height, freeRect);
        case FreeRectChoiceHeuristic.RectBestLongSideFit: return ScoreBestLongSideFit(width, height, freeRect);
        case FreeRectChoiceHeuristic.RectWorstAreaFit: return ScoreWorstAreaFit(width, height, freeRect);
        case FreeRectChoiceHeuristic.RectWorstShortSideFit: return ScoreWorstShortSideFit(width, height, freeRect);
        case FreeRectChoiceHeuristic.RectWorstLongSideFit: return ScoreWorstLongSideFit(width, height, freeRect);
                */
                default: throw new Exception("whoops");
            }
        }


        void SplitFreeRectByHeuristic(Rect freeRect, Rect placedRect, GuillotineSplitHeuristic method)
        {
            // Compute the lengths of the leftover area.
            int w = freeRect.width - placedRect.width;
            int h = freeRect.height - placedRect.height;

            // Placing placedRect into freeRect results in an L-shaped free area, which must be split into
            // two disjoint rectangles. This can be achieved with by splitting the L-shape using a single line.
            // We have two choices: horizontal or vertical.	

            // Use the given heuristic to decide which choice to make.

            bool splitHorizontal=true;
            switch (method)
            {
                //case GuillotineSplitHeuristic.SplitShorterLeftoverAxis:
                //    // Split along the shorter leftover axis.
                //    splitHorizontal = (w <= h);
                //    break;
                //case GuillotineSplitHeuristic.SplitLongerLeftoverAxis:
                //    // Split along the longer leftover axis.
                //    splitHorizontal = (w > h);
                //    break;
                //case GuillotineSplitHeuristic.SplitMinimizeArea:
                //    // Maximize the larger area == minimize the smaller area.
                //    // Tries to make the single bigger rectangle.
                //    splitHorizontal = (placedRect.width * h > w * placedRect.height);
                //    break;
                case GuillotineSplitHeuristic.SplitMaximizeArea:
                    // Maximize the smaller area == minimize the larger area.
                    // Tries to make the rectangles more even-sized.
                    splitHorizontal = (placedRect.width * h <= w * placedRect.height);
                    break;
                //case GuillotineSplitHeuristic.SplitShorterAxis:
                //    // Split along the shorter total axis.
                //    splitHorizontal = (freeRect.width <= freeRect.height);
                //    break;
                //case GuillotineSplitHeuristic.SplitLongerAxis:
                //    // Split along the longer total axis.
                //    splitHorizontal = (freeRect.width > freeRect.height);
                //    break;
                //default:
                //    splitHorizontal = true;
                //    break;
            }

            // Perform the actual split.
            SplitFreeRectAlongAxis(freeRect, placedRect, splitHorizontal);
        }

        void SplitFreeRectAlongAxis(Rect freeRect, Rect placedRect, bool splitHorizontal)
        {
            // Form the two new rectangles.
            Rect bottom = new Rect
            {
                x=freeRect.x,
                y=freeRect.y+placedRect.height,
                height=freeRect.height-placedRect.height
            };
            Rect right = new Rect
            {
                x=freeRect.x+placedRect.width,
                y=freeRect.y,
                width=freeRect.width-placedRect.width
            };

            if (splitHorizontal)
            {
                bottom.width = freeRect.width;
                right.height = placedRect.height;
            }
            else // Split vertically
            {
                bottom.width = placedRect.width;
                right.height = freeRect.height;
            }

            // Add the new rectangles into the free rectangle pool if they weren't degenerate.
            if (bottom.width > 0 && bottom.height > 0)
            {
                freeRectangles.Insert(freeRectangles.Count, bottom);
            }
            if (right.width > 0 && right.height > 0)
            {
                freeRectangles.Insert(freeRectangles.Count, right);
            }
        }
    }

//FreeRectChoiceHeuristic.RectBestShortSideFit,
//GuillotineSplitHeuristic.SplitMaximizeArea);
    public enum FreeRectChoiceHeuristic{
//        RectBestAreaFit, ///< -BAF
        RectBestShortSideFit, ///< -BSSF
//        RectBestLongSideFit, ///< -BLSF
//        RectWorstAreaFit, ///< -WAF
//        RectWorstShortSideFit, ///< -WSSF
//        RectWorstLongSideFit ///< -WLSF
    }
    public enum GuillotineSplitHeuristic
    {
//        SplitShorterLeftoverAxis, ///< -SLAS
//        SplitLongerLeftoverAxis, ///< -LLAS
//        SplitMinimizeArea, ///< -MINAS, Try to make a single big rectangle at the expense of making the other small.
        SplitMaximizeArea, ///< -MAXAS, Try to make both remaining rectangles as even-sized as possible.
//        SplitShorterAxis, ///< -SAS
//        SplitLongerAxis ///< -LAS
    }
}
