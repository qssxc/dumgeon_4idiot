using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Room
{
    public Vector2Int Position;
    public int Width;
    public int Height;
    public List<Room> Neighbors;
}
