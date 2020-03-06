using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GroupInfo
{
    public GroupInfo(int id, Vector3 initialPosition)
    {
        this.id = id;
        positions = new List<Vector3> { initialPosition };
    }

    public int id;
    public List<Vector3> positions;
}

public struct GroupableInfo
{
    public GroupableInfo(Vector3 position, int groupID)
    {
        this.position = position;
        this.groupID = groupID;
    }

    public Vector3 position;
    public int groupID;
}

public static class DistanceBasedGrouping
{
    private static List<GroupInfo> groupWorkingList;
    public static bool GroupByDistance(List<GroupableInfo> groupables, float distanceLimit)
    {
        if (groupables == null)
        {
            return false;
        }

        if (groupWorkingList == null) { groupWorkingList = new List<GroupInfo>(); }
        else { groupWorkingList.Clear(); }

        float distanceSquaredLimit = distanceLimit * distanceLimit;

        for (int index = 0; index < groupables.Count; ++index)
        {
            Vector3 currentPosition = groupables[index].position;
            bool foundGroup = false;
            int foundGroupID = -1;
            for (int groupIndex = 0; !foundGroup && groupIndex < groupWorkingList.Count; ++groupIndex)
            {
                if (TryJoinGroup(currentPosition, groupWorkingList[groupIndex], distanceSquaredLimit))
                {
                    foundGroupID = groupIndex;
                    foundGroup = true;
                }
            }

            if (!foundGroup)
            {
                groupWorkingList.Add(new GroupInfo(groupWorkingList.Count, currentPosition));
                foundGroupID = groupWorkingList.Count - 1;
            }

            groupables[index] = new GroupableInfo(currentPosition, foundGroupID);
        }

        // Then, phase 2: merge groups if possible

        return true;
    }

    public static bool TryJoinGroup(Vector3 pos, GroupInfo group, float distanceSquaredLimit)
    {
        for (int i = 0; i < group.positions.Count; ++i)
        {
            if (distanceSquaredLimit > Vector3.SqrMagnitude(pos - group.positions[i]))
            {
                return true;
            }
        }

        return false;
    }
}
