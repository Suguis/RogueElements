﻿using System;
using System.Collections.Generic;
using System.Text;
using RogueElements;
using System.Diagnostics;

namespace RogueElements.Examples
{
    public static class ExampleDebug
    {
        const ConsoleKey STEP_IN_KEY = ConsoleKey.F5;
        const ConsoleKey STEP_OUT_KEY = ConsoleKey.F6;
        public static int Printing;
        public static bool SteppingIn;
        static List<string> stepStack;
        static List<DebugState> gridDebugString;
        static List<DebugState> listDebugString;
        static List<DebugState> tileDebugString;
        static int currentDepth;
        static IGenContext curMap;

        public static void Init(IGenContext newMap)
        {
            curMap = newMap;
            currentDepth = 0;
            stepStack = new List<string>();
            gridDebugString = new List<DebugState>();
            listDebugString = new List<DebugState>();
            tileDebugString = new List<DebugState>();

            stepStack.Add("");
            gridDebugString.Add(new DebugState());
            listDebugString.Add(new DebugState());
            tileDebugString.Add(new DebugState());
        }

        public static void StepIn(string msg)
        {
            currentDepth++;
            stepStack.Add(msg);
            gridDebugString.Add(new DebugState(gridDebugString[gridDebugString.Count - 1].MapString));
            listDebugString.Add(new DebugState(listDebugString[listDebugString.Count - 1].MapString));
            tileDebugString.Add(new DebugState(tileDebugString[tileDebugString.Count - 1].MapString));

            if (SteppingIn)
                Printing = Math.Max(Printing, currentDepth + 1);

        }

        public static void StepOut()
        {
            currentDepth--;
            string stepOutName = stepStack[stepStack.Count - 1];
            stepStack.RemoveAt(stepStack.Count - 1);
            DebugState gridState = gridDebugString[gridDebugString.Count - 1];
            gridDebugString.RemoveAt(gridDebugString.Count - 1);
            DebugState listState = listDebugString[listDebugString.Count - 1];
            listDebugString.RemoveAt(listDebugString.Count - 1);
            DebugState tileState = tileDebugString[tileDebugString.Count - 1];
            tileDebugString.RemoveAt(tileDebugString.Count - 1);

            Printing = Math.Min(Printing, currentDepth + 1);

            //print within printing
            printStep(createStackString() + "<" + stepOutName + "<");
        }

        public static void OnStep(string msg)
        {
            printStep(createStackString() + ">" + msg);
        }



        private static void clearLine(int lineNum)
        {
            Console.SetCursorPosition(0, lineNum);
            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));
        }

        private static void rewriteLine(int lineNum, string msg)
        {
            Console.SetCursorPosition(0, lineNum);
            Console.Write(msg);
            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));
        }


        private static string createStackString()
        {
            StringBuilder str = new StringBuilder();
            for (int ii = 0; ii < stepStack.Count; ii++)
            {
                if (ii > 0)
                    str.Append(">");
                str.Append(stepStack[ii]);
            }
            return str.ToString();
        }


        //Code below is specific to the map gen context; it can be tweaked to vary by game implementation

        public static void printStep(string msg)
        {
            bool printDebug = false;
            bool printViewer = false;
            if (Printing > -1)
            {
                if (currentDepth < Printing)
                {
                    printDebug = true;
                    printViewer = true;
                }
                if (currentDepth == 0)
                    printDebug = true;
            }


            ConsoleKey key = ConsoleKey.Enter;
            {
                ConsoleKey newKey = PrintGridRoomHalls(curMap, msg, printDebug, printViewer);
                if (key == ConsoleKey.Enter)
                    key = newKey;
            }

            {
                ConsoleKey newKey = PrintListRoomHalls(curMap, msg, printDebug, printViewer);
                if (key == ConsoleKey.Enter)
                    key = newKey;
            }

            {
                ConsoleKey newKey = PrintTiles(curMap, msg, printDebug, printViewer);
                if (key == ConsoleKey.Enter)
                    key = newKey;
            }
            if (key == STEP_IN_KEY)
                SteppingIn = true;
            else if (key == STEP_OUT_KEY)
                Printing--;
            else if (key == ConsoleKey.Escape)
                Printing = 0;

        }



        public static ConsoleKey PrintTiles(IGenContext map, string msg, bool printDebug, bool printViewer)
        {
            ITiledGenContext context = map as ITiledGenContext;
            if (context == null)
                return ConsoleKey.Enter;
            if (!context.TilesInitialized)
                return ConsoleKey.Enter;

            StringBuilder str = new StringBuilder();

            for (int yy = 0; yy < context.Height; yy++)
            {
                if (yy > 0)
                    str.Append('\n');
                for (int xx = 0; xx < context.Width; xx++)
                {
                    if (context.GetTile(new Loc(xx, yy)).TileEquivalent(context.RoomTerrain))
                        str.Append('.');
                    else if (context.GetTile(new Loc(xx, yy)).TileEquivalent(context.WallTerrain))
                        str.Append('#');
                    else
                    {
                        if (context.TileBlocked(new Loc(xx, yy)))
                            str.Append('+');
                        else
                            str.Append('_');
                    }
                }
            }


            string newStr = str.ToString();
            if (tileDebugString[currentDepth].MapString == newStr)
                return ConsoleKey.Enter;

            tileDebugString[currentDepth].MapString = newStr;

            if (printDebug)
            {
                Debug.WriteLine(msg);
                Debug.Print(newStr);
            }

            if (printViewer)
            {
                //TODO: print with highlighting (use the bounds variable)
                //TODO: print with color
                SteppingIn = false;
                Console.Clear();
                Console.WriteLine(msg);
                Loc start = new Loc(Console.CursorLeft, Console.CursorTop);
                Console.Write(newStr);
                Loc end = new Loc(Console.CursorLeft, Console.CursorTop + 1);
                Console.SetCursorPosition(start.X, start.Y);
                int prevFarthestPrint = end.Y;

                while (true)
                {
                    int farthestPrint = end.Y;
                    Loc mapLoc = new Loc(Console.CursorLeft, Console.CursorTop) - start;
                    rewriteLine(farthestPrint, String.Format("X:{0}  Y:{1}", mapLoc.X.ToString("D3"), mapLoc.Y.ToString("D3")));
                    farthestPrint++;
                    ITile tile = context.GetTile(mapLoc);
                    rewriteLine(farthestPrint, String.Format("Tile: {0}", tile.ToString()));
                    farthestPrint++;

                    for (int ii = farthestPrint; ii < prevFarthestPrint; ii++)
                        clearLine(ii);
                    prevFarthestPrint = farthestPrint;
                    Console.SetCursorPosition(start.X + mapLoc.X, start.Y + mapLoc.Y);


                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.UpArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Max(start.Y, Console.CursorTop - 1));
                    else if (key.Key == ConsoleKey.DownArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Min(Console.CursorTop + 1, end.Y - 1));
                    else if (key.Key == ConsoleKey.LeftArrow)
                        Console.SetCursorPosition(Math.Max(start.X, Console.CursorLeft - 1), Console.CursorTop);
                    else if (key.Key == ConsoleKey.RightArrow)
                        Console.SetCursorPosition(Math.Min(Console.CursorLeft + 1, end.X - 1), Console.CursorTop);
                    else
                        return key.Key;
                }
            }
            else
                return ConsoleKey.Enter;
        }

        public static ConsoleKey PrintListRoomHalls(IGenContext map, string msg, bool printDebug, bool printViewer)
        {
            IFloorPlanGenContext context = map as IFloorPlanGenContext;
            if (context == null)
                return ConsoleKey.Enter;

            StringBuilder str = new StringBuilder();
            FloorPlan plan = context.RoomPlan;
            if (plan == null)
                return ConsoleKey.Enter;

            for (int yy = 0; yy < plan.DrawRect.Bottom; yy++)
            {
                for (int xx = 0; xx < plan.DrawRect.Right; xx++)
                {
                    str.Append(' ');
                }
            }

            for (int ii = 0; ii < plan.RoomCount; ii++)
            {
                char chosenChar = '@';
                //if (ii < 26)
                chosenChar = (char)('A' + ii % 26);
                IRoomGen gen = plan.GetRoom(ii);
                for (int xx = gen.Draw.Left; xx < gen.Draw.Right; xx++)
                {
                    for (int yy = gen.Draw.Top; yy < gen.Draw.Bottom; yy++)
                    {
                        int index = yy * plan.DrawRect.Right + xx;

                        if (str[index] == ' ')
                            str[index] = chosenChar;
                        else
                            str[index] = '!';
                    }
                }
            }
            for (int ii = 0; ii < plan.HallCount; ii++)
            {
                char chosenChar = '#';
                //if (ii < 26)
                chosenChar = (char)('a' + ii % 26);

                IRoomGen gen = plan.GetHall(ii);

                for (int xx = gen.Draw.Left; xx < gen.Draw.Right; xx++)
                {
                    for (int yy = gen.Draw.Top; yy < gen.Draw.Bottom; yy++)
                    {
                        int index = yy * plan.DrawRect.Right + xx;

                        if (str[index] == ' ')
                            str[index] = chosenChar;
                        else if (str[index] >= 'a' && str[index] <= 'z' || str[index] == '#')
                            str[index] = '+';
                        else
                            str[index] = '!';
                    }
                }
            }

            for (int yy = plan.DrawRect.Bottom - 1; yy > 0; yy--)
                str.Insert(plan.DrawRect.Right * yy, '\n');


            string newStr = str.ToString();
            if (listDebugString[currentDepth].MapString == newStr)
                return ConsoleKey.Enter;

            listDebugString[currentDepth].MapString = newStr;


            if (printDebug)
            {
                Debug.WriteLine(msg);
                Debug.Print(newStr);
            }

            if (printViewer)
            {
                SteppingIn = false;
                Console.Clear();
                Console.WriteLine(msg);
                Loc start = new Loc(Console.CursorLeft, Console.CursorTop);
                Console.Write(newStr);
                Loc end = new Loc(Console.CursorLeft, Console.CursorTop + 1);
                Console.SetCursorPosition(start.X, start.Y);
                int prevFarthestPrint = end.Y;

                while (true)
                {
                    int farthestPrint = end.Y;
                    Loc mapLoc = new Loc(Console.CursorLeft, Console.CursorTop) - start;
                    rewriteLine(farthestPrint, String.Format("X:{0}  Y:{1}", mapLoc.X.ToString("D3"), mapLoc.Y.ToString("D3")));
                    farthestPrint++;

                    for (int ii = 0; ii < plan.RoomCount; ii++)
                    {
                        FloorRoomPlan roomPlan = plan.GetRoomPlan(ii);
                        if (roomPlan.Gen.Draw.Contains(mapLoc))
                        {
                            //stats
                            string roomString = String.Format("Room #{0}: {1}x{2} {3}", ii, roomPlan.Gen.Draw.X, roomPlan.Gen.Draw.Y, roomPlan.RoomGen.ToString());
                            if (roomPlan.Immutable)
                                roomString += " [Immutable]";
                            rewriteLine(farthestPrint, roomString);
                            farthestPrint++;
                            //borders
                            StringBuilder lineString = new StringBuilder(" ");
                            for (int xx = 0; xx < roomPlan.Gen.Draw.Width; xx++)
                                lineString.Append(roomPlan.Gen.GetFulfillableBorder(Dir4.Up, xx) ? "^" : " ");
                            rewriteLine(farthestPrint, lineString.ToString());
                            farthestPrint++;
                            for (int yy = 0; yy < roomPlan.Gen.Draw.Height; yy++)
                            {
                                lineString = new StringBuilder(roomPlan.Gen.GetFulfillableBorder(Dir4.Left, yy) ? "<" : " ");
                                for (int xx = 0; xx < roomPlan.Gen.Draw.Width; xx++)
                                    lineString.Append("#");
                                lineString.Append(roomPlan.Gen.GetFulfillableBorder(Dir4.Right, yy) ? ">" : " ");
                                rewriteLine(farthestPrint, lineString.ToString());
                                farthestPrint++;
                            }
                            lineString = new StringBuilder(" ");
                            for (int xx = 0; xx < roomPlan.Gen.Draw.Width; xx++)
                                lineString.Append(roomPlan.Gen.GetFulfillableBorder(Dir4.Down, xx) ? "V" : " ");
                            rewriteLine(farthestPrint, lineString.ToString());
                            farthestPrint++;
                        }
                    }
                    for (int ii = 0; ii < plan.HallCount; ii++)
                    {
                        IPermissiveRoomGen gen = plan.GetHall(ii);
                        if (gen.Draw.Contains(mapLoc))
                        {
                            string roomString = String.Format("Hall #{0}: {1}x{2} {3}", ii, gen.Draw.X, gen.Draw.Y, gen.ToString());
                            rewriteLine(farthestPrint, roomString);
                            farthestPrint++;
                        }
                    }


                    for (int ii = farthestPrint; ii < prevFarthestPrint; ii++)
                        clearLine(ii);
                    prevFarthestPrint = farthestPrint;
                    Console.SetCursorPosition(start.X + mapLoc.X, start.Y + mapLoc.Y);


                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.UpArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Max(start.Y, Console.CursorTop - 1));
                    else if (key.Key == ConsoleKey.DownArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Min(Console.CursorTop + 1, end.Y - 1));
                    else if (key.Key == ConsoleKey.LeftArrow)
                        Console.SetCursorPosition(Math.Max(start.X, Console.CursorLeft - 1), Console.CursorTop);
                    else if (key.Key == ConsoleKey.RightArrow)
                        Console.SetCursorPosition(Math.Min(Console.CursorLeft + 1, end.X - 1), Console.CursorTop);
                    else
                        return key.Key;
                }
            }
            else
                return ConsoleKey.Enter;
        }

        public static ConsoleKey PrintGridRoomHalls(IGenContext map, string msg, bool printDebug, bool printViewer)
        {
            IRoomGridGenContext context = map as IRoomGridGenContext;
            if (context == null)
                return ConsoleKey.Enter;

            StringBuilder str = new StringBuilder();
            GridPlan plan = context.GridPlan;
            if (plan == null)
                return ConsoleKey.Enter;

            for (int yy = 0; yy < plan.GridHeight; yy++)
            {
                if (yy > 0)
                    str.Append('\n');

                for (int xx = 0; xx < plan.GridWidth; xx++)
                {
                    int roomIndex = plan.GetRoomIndex(new Loc(xx, yy));
                    if (roomIndex == -1)
                        str.Append('0');
                    else// if (roomIndex < 26)
                        str.Append((char)('A' + roomIndex % 26));
                    //else
                    //    str.Append('@');

                    if (xx < plan.GridWidth - 1)
                    {
                        if (plan.GetHall(new LocRay4(xx, yy, Dir4.Right)) != null)
                            str.Append('#');
                        else
                            str.Append('.');
                    }
                }

                if (yy < plan.GridHeight - 1)
                {
                    str.Append('\n');
                    for (int xx = 0; xx < plan.GridWidth; xx++)
                    {
                        if (plan.GetHall(new LocRay4(xx, yy, Dir4.Down)) != null)
                            str.Append('#');
                        else
                            str.Append('.');

                        if (xx < plan.GridWidth - 1)
                        {
                            str.Append(' ');
                        }
                    }
                }
            }


            string newStr = str.ToString();
            if (gridDebugString[currentDepth].MapString == newStr)
                return ConsoleKey.Enter;

            gridDebugString[currentDepth].MapString = newStr;


            if (printDebug)
            {
                Debug.WriteLine(msg);
                Debug.Print(newStr);
            }

            if (printViewer)
            {
                SteppingIn = false;
                Console.Clear();
                Console.WriteLine(msg);
                Loc start = new Loc(Console.CursorLeft, Console.CursorTop);
                Console.Write(newStr);
                Loc end = new Loc(Console.CursorLeft, Console.CursorTop + 1);
                Console.SetCursorPosition(start.X, start.Y);
                int prevFarthestPrint = end.Y;

                while (true)
                {
                    int farthestPrint = end.Y;
                    Loc gridLoc = new Loc(Console.CursorLeft, Console.CursorTop) - start;
                    Loc mapLoc = gridLoc / 2;
                    rewriteLine(farthestPrint, String.Format("X:{0:0.0}  Y:{1:0.0}", ((float)gridLoc.X / 2), ((float)gridLoc.Y / 2)));
                    farthestPrint++;

                    bool alignX = gridLoc.X % 2 == 0;
                    bool alignY = gridLoc.Y % 2 == 0;

                    if (alignX && alignY)
                    {
                        int index = plan.GetRoomIndex(mapLoc);
                        GridRoomPlan roomPlan = plan.GetRoomPlan(mapLoc);
                        if (roomPlan != null)
                        {
                            string roomString = String.Format("Room #{0}: {1}", index, roomPlan.RoomGen.ToString());
                            if (roomPlan.Immutable)
                                roomString += " [Immutable]";
                            if (roomPlan.PreferHall)
                                roomString += " [Hall]";
                            rewriteLine(farthestPrint, roomString);
                            farthestPrint++;
                        }
                    }
                    else if (alignX)
                    {
                        IPermissiveRoomGen hall = plan.GetHall(new LocRay4(mapLoc, Dir4.Down));
                        if (hall != null)
                        {
                            rewriteLine(farthestPrint, "Hall: " + hall.ToString());
                            farthestPrint++;
                        }
                    }
                    else if (alignY)
                    {
                        IPermissiveRoomGen hall = plan.GetHall(new LocRay4(mapLoc, Dir4.Right));
                        if (hall != null)
                        {
                            rewriteLine(farthestPrint, "Hall: " + hall.ToString());
                            farthestPrint++;
                        }
                    }

                    for (int ii = farthestPrint; ii < prevFarthestPrint; ii++)
                        clearLine(ii);
                    prevFarthestPrint = farthestPrint;
                    Console.SetCursorPosition(start.X + gridLoc.X, start.Y + gridLoc.Y);


                    ConsoleKeyInfo key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.UpArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Max(start.Y, Console.CursorTop - 1));
                    else if (key.Key == ConsoleKey.DownArrow)
                        Console.SetCursorPosition(Console.CursorLeft, Math.Min(Console.CursorTop + 1, end.Y - 1));
                    else if (key.Key == ConsoleKey.LeftArrow)
                        Console.SetCursorPosition(Math.Max(start.X, Console.CursorLeft - 1), Console.CursorTop);
                    else if (key.Key == ConsoleKey.RightArrow)
                        Console.SetCursorPosition(Math.Min(Console.CursorLeft + 1, end.X - 1), Console.CursorTop);
                    else
                        return key.Key;
                }
            }
            else
                return ConsoleKey.Enter;
        }
    }
}
