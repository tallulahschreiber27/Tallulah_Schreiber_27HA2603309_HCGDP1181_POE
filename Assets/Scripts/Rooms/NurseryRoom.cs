using UnityEngine;

public class NurseryRoom : Room
{
    public override void Start()
    {
        base.Start();
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "Nursery Room";
        }
    }

    public override string ToString()
    {
        return roomName + " (" + GetRoomCount() + "/" + creatureCap + " creatures)";
    }
}
