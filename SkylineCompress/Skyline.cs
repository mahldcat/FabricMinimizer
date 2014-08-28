using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkylineCompress
{
    public class Skyline
    {
        public float Occupancy
        {
            get
            {
                return (float)usedSurfaceArea / (binWidth * binHeight);
            }
        }
        private int binWidth = 0;
        private int binHeight = 0;
        private List<SkylineNode> skyLine = new List<SkylineNode>();
        ulong usedSurfaceArea;
        private bool useWasteMap;
        private GuillotineBinPack wasteMap = new GuillotineBinPack();

        public Skyline() { }

        public Skyline(int binWidth, int binHeight, bool useWasteMap) 
        {
            Init(binWidth, binHeight, useWasteMap);
        }

        public void Init(int binWidth, int binHeight, bool useWasteMap) 
        {
            this.binWidth = binWidth;
            this.binHeight = binHeight;
            this.useWasteMap = useWasteMap;
            usedSurfaceArea = 0;
            skyLine.Clear();
            SkylineNode node = new SkylineNode
            {
                x = 0,
                y = 0,
                width = binWidth
            };
            skyLine.Add(node);
            if(useWasteMap)
            {
                wasteMap.Init(binWidth, binHeight);
                wasteMap.FreeRectangles.Clear();
            }
        }

        public void Insert(IList<Rect> rects, IList<Rect> dest, LevelChoiceHeuristic method)
        {
            dest.Clear();
            while (rects.Count > 0)
            {
                Rect bestNode = new Rect();

                int bestScore1 = int.MaxValue;
                int bestScore2 = int.MaxValue;
                int bestSkylineIndex = -1;
                int bestRectIndex = -1;
                for (int i = 0; i < rects.Count; ++i)
                {
                    Rect newNode;
                    int score1 = 0;
                    int score2 = 0;
                    int index = 0;
                    switch (method)
                    {
                        case LevelChoiceHeuristic.LevelBottomLeft:
                            newNode = FindPositionForNewNodeBottomLeft(rects[i].width, rects[i].height, ref score1, ref score2, ref index);
                            break;
                        case LevelChoiceHeuristic.LevelMinWasteFit:
                            newNode = FindPositionForNewNodeMinWaste(rects[i].width, rects[i].height, ref score2, ref score1, ref index);
                            break;
                        default:
                            throw new Exception("something failed");

                    }
                    if (newNode.height != 0)
                    {
                        if (score1 < bestScore1 || (score1 == bestScore1 && score2 < bestScore2))
                        {
                            bestNode = newNode;
                            bestScore1 = score1;
                            bestScore2 = score2;
                            bestSkylineIndex = index;
                            bestRectIndex = i;
                        }
                    }
                }

                if (bestRectIndex == -1)
                {
                    return;
                }

                AddSkylineLevel(bestSkylineIndex, ref bestNode);
                usedSurfaceArea += (uint)rects[bestRectIndex].width * (uint)rects[bestRectIndex].height;

                rects.RemoveAt(bestRectIndex);
                dest.Add(bestNode);
            }
        }

        public Rect Insert(int width, int height, LevelChoiceHeuristic method)
        {
            // First try to pack this rectangle into the waste map, if it fits.
            Rect node = wasteMap.Insert(width, height, true,
                FreeRectChoiceHeuristic.RectBestShortSideFit,
                GuillotineSplitHeuristic.SplitMaximizeArea);

            if (node.height != 0)
            {
                Rect newNode;
                newNode.x = node.x;
                newNode.y = node.y;
                newNode.width = node.width;
                newNode.height = node.height;
                usedSurfaceArea += (uint)width * (uint)height;
                return newNode;
            }

            switch (method)
            {
                case LevelChoiceHeuristic.LevelBottomLeft:
                    return InsertBottomLeft(width, height);
                case LevelChoiceHeuristic.LevelMinWasteFit: 
                    return InsertMinWaste(width, height);
                default:
                    throw new Exception("default");
            }
        }

        private Rect InsertBottomLeft(int width, int height)
        {
            int bestHeight=0;
            int bestWidth=0;
            int bestIndex=0;
            Rect newNode = FindPositionForNewNodeBottomLeft(width, height, ref bestHeight, ref bestWidth, ref bestIndex);

            if (bestIndex != -1)
            {
                AddSkylineLevel(bestIndex, ref newNode);
                usedSurfaceArea += (uint)width * (uint)height;
            }
            else
            {
                newNode = new Rect
                {
                    x = 0,
                    y = 0,
                    height = 0,
                    width = 0
                };
            }
            return newNode;
        }

        private Rect InsertMinWaste(int width, int height)
        {
            int bestHeight = 0;
            int bestWastedArea = 0;
            int bestIndex = 0;
            Rect newNode = FindPositionForNewNodeMinWaste(width, height, ref bestHeight, ref bestWastedArea, ref bestIndex);

            if (bestIndex != -1)
            {
                // Perform the actual packing.
                AddSkylineLevel(bestIndex, ref newNode);

                usedSurfaceArea += (uint)width * (uint)height;
            }
            else
            {
                newNode = new Rect();
            }
            return newNode;
        }

        private Rect FindPositionForNewNodeMinWaste(int width, int height, ref int bestHeight, ref int bestWastedArea, ref int bestIndex)
        {
            bestHeight = int.MaxValue;
            bestWastedArea = int.MaxValue;
            bestIndex = -1;
            Rect newNode = new Rect();
            for (int i = 0; i < skyLine.Count; ++i)
            {
                int y=0;
                int wastedArea=0;

                if (RectangleFits(i, width, height, ref y, ref  wastedArea))
                {
                    if (wastedArea < bestWastedArea || (wastedArea == bestWastedArea && y + height < bestHeight))
                    {
                        bestHeight = y + height;
                        bestWastedArea = wastedArea;
                        bestIndex = i;
                        newNode.x = skyLine[i].x;
                        newNode.y = y;
                        newNode.width = width;
                        newNode.height = height;
                    }
                }
                if (RectangleFits(i, height, width, ref y, ref wastedArea))
                {
                    if (wastedArea < bestWastedArea || (wastedArea == bestWastedArea && y + width < bestHeight))
                    {
                        bestHeight = y + width;
                        bestWastedArea = wastedArea;
                        bestIndex = i;
                        newNode.x = skyLine[i].x;
                        newNode.y = y;
                        newNode.width = height;
                        newNode.height = width;
                    }
                }
            }

            return newNode;
        }
        private Rect FindPositionForNewNodeBottomLeft(int width, int height, ref int bestHeight, ref int bestWidth, ref int bestIndex)
        {
            bestHeight = int.MaxValue;
            bestIndex = -1;
            // Used to break ties if there are nodes at the same level. Then pick the narrowest one.
            bestWidth = int.MaxValue;
            Rect newNode = new Rect();

            for (int i = 0; i < skyLine.Count; ++i)
            {
                int y=0;
                if (RectangleFits(i, width, height, ref y))
                {
                    if (y + height < bestHeight || (y + height == bestHeight && skyLine[i].width < bestWidth))
                    {
                        bestHeight = y + height;
                        bestIndex = i;
                        bestWidth = skyLine[i].width;
                        newNode.x = skyLine[i].x;
                        newNode.y = y;
                        newNode.width = width;
                        newNode.height = height;
                    }
                }
                if (RectangleFits(i, height, width, ref y))
                {
                    if (y + width < bestHeight || (y + width == bestHeight && skyLine[i].width < bestWidth))
                    {
                        bestHeight = y + width;
                        bestIndex = i;
                        bestWidth = skyLine[i].width;
                        newNode.x = skyLine[i].x;
                        newNode.y = y;
                        newNode.width = height;
                        newNode.height = width;
                    }
                }
            }

            return newNode;
        }
        private bool RectangleFits(int skylineNodeIndex, int width, int height, ref int y)
        {
            int x = skyLine[skylineNodeIndex].x;
            if (x + width > binWidth)
            {
                return false;
            }
            int widthLeft = width;
            int i = skylineNodeIndex;
            y = skyLine[skylineNodeIndex].y;
            while (widthLeft > 0)
            {
                y = Math.Max(y, skyLine[i].y);
                if (y + height > binHeight)
                    return false;
                widthLeft -= skyLine[i].width;
                ++i;
            }
            return true;
        }
        private bool RectangleFits(int skylineNodeIndex, int width, int height, ref int y, ref int wastedArea)
        {
            bool fits = RectangleFits(skylineNodeIndex, width, height, ref y);
            if (fits)
            {
                wastedArea = ComputeWastedArea(skylineNodeIndex, width, height, y);
            }
            return fits;
        }
        private int ComputeWastedArea(int skylineNodeIndex, int width, int height, int y)
        {
            int wastedArea = 0;
            int rectLeft = skyLine[skylineNodeIndex].x;
            int rectRight = rectLeft + width;
            for (; skylineNodeIndex < (int)skyLine.Count && skyLine[skylineNodeIndex].x < rectRight; ++skylineNodeIndex)
            {
                if (skyLine[skylineNodeIndex].x >= rectRight || skyLine[skylineNodeIndex].x + skyLine[skylineNodeIndex].width <= rectLeft)
                    break;

                int leftSide = skyLine[skylineNodeIndex].x;
                int rightSide = Math.Min(rectRight, leftSide + skyLine[skylineNodeIndex].width);
                wastedArea += (rightSide - leftSide) * (y - skyLine[skylineNodeIndex].y);
            }
            return wastedArea;
        }

        private void AddWasteMapArea(int skylineNodeIndex, int width, int height, int y)
        {
            int wastedArea = 0;
            int rectLeft = skyLine[skylineNodeIndex].x;
            int rectRight = rectLeft + width;
            for (; skylineNodeIndex < (int)skyLine.Count && skyLine[skylineNodeIndex].x < rectRight; ++skylineNodeIndex)
            {
                if (skyLine[skylineNodeIndex].x >= rectRight || skyLine[skylineNodeIndex].x + skyLine[skylineNodeIndex].width <= rectLeft)
                    break;

                int leftSide = skyLine[skylineNodeIndex].x;
                int rightSide = Math.Min(rectRight, leftSide + skyLine[skylineNodeIndex].width);

                Rect waste;
                waste.x = leftSide;
                waste.y = skyLine[skylineNodeIndex].y;
                waste.width = rightSide - leftSide;
                waste.height = y - skyLine[skylineNodeIndex].y;

                wasteMap.FreeRectangles.Add(waste);
            }
        }
        private void AddSkylineLevel(int skylineNodeIndex, ref Rect rect)
        {
            // First track all wasted areas and mark them into the waste map if we're using one.
            if (useWasteMap)
            {
                AddWasteMapArea(skylineNodeIndex, rect.width, rect.height, rect.y);
            }

            SkylineNode newNode;
            newNode.x = rect.x;
            newNode.y = rect.y + rect.height;
            newNode.width = rect.width;
            skyLine.Insert(skylineNodeIndex, newNode);

            for (int i = skylineNodeIndex + 1; i < skyLine.Count; ++i)
            {
                if (skyLine[i].x < skyLine[i - 1].x + skyLine[i - 1].width)
                {
                    int shrink = skyLine[i - 1].x + skyLine[i - 1].width - skyLine[i].x;
                    SkylineNode skyline_i = skyLine[i];

                    skyline_i.x = skyLine[i].x + shrink;
                    skyline_i.width -= shrink;

                    if (skyLine[i].width <= 0)
                    {
                        skyLine.RemoveAt(i);
                        --i;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            MergeSkylines();
        }
        /// Merges all skyline nodes that are at the same level.
        private void MergeSkylines()
        {
            for (int i = 0; i < skyLine.Count - 1; ++i)
            {
                if (skyLine[i].y == skyLine[i + 1].y)
                {
                    SkylineNode slNode = skyLine[i];
                    slNode.width += skyLine[i + 1].width;
                    skyLine.RemoveAt(i + 1);
                    --i;
                }
            }
        }
    }

    public struct SkylineNode
    {
        public int x;
        public int y;
        public int width;
    }

    public enum LevelChoiceHeuristic
    {
        LevelBottomLeft,
        LevelMinWasteFit
    }
}
