﻿using System;
using System.Collections.Generic;

namespace RogueElements
{
    [Serializable]
    public class ConnectRoomStep<T> : ConnectStep<T> where T : class, IFloorPlanGenContext
    {
        public RandRange ConnectFactor;

        public ConnectRoomStep() : base() { }

        public ConnectRoomStep(RandRange connectFactor)
            : this()
        {
            ConnectFactor = connectFactor;
        }

        public override void ApplyToPath(IRandom rand, FloorPlan floorPlan)
        {
            List<RoomHallIndex> candBranchPoints = new List<RoomHallIndex>();
            for (int ii = 0; ii < floorPlan.RoomCount; ii++)
                candBranchPoints.Add(new RoomHallIndex(ii, false));

            //compute a goal amount of terminals to connect
            //this computation ignores the fact that some terminals may be impossible
            int connectionsLeft = ConnectFactor.Pick(rand) * candBranchPoints.Count / 2 / 100;
            
            while (candBranchPoints.Count > 0 && connectionsLeft > 0)
            {
                //choose random point to connect from
                int randIndex = rand.Next(candBranchPoints.Count);
                ListPathTraversalNode chosenDest = chooseConnection(rand, floorPlan, candBranchPoints);
                
                if (chosenDest != null)
                {
                    //connect
                    PermissiveRoomGen<T> hall = GenericHalls.Pick(rand).Copy();
                    hall.PrepareSize(rand, chosenDest.Connector.Size);
                    hall.SetLoc(chosenDest.Connector.Start);
                    floorPlan.AddHall(hall, chosenDest.From, chosenDest.To);
                    candBranchPoints.RemoveAt(randIndex);
                    connectionsLeft--;

                    //check to see if connection destination was also a candidate,
                    //counting this as a double if so
                    for (int jj = 0; jj < candBranchPoints.Count; jj++)
                    {
                        if (candBranchPoints[jj] == chosenDest.To)
                        {
                            candBranchPoints.RemoveAt(jj);
                            connectionsLeft--;
                            break;
                        }
                    }
                }
                else //remove the list anyway, but don't call it a success
                    candBranchPoints.RemoveAt(randIndex);
            }

        }

    }


}
