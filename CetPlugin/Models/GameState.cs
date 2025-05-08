using System.Collections.Generic;

namespace CetPlugin.Models;

public class HouseSnapshot
{
    public int x;
    public int y;
    public int color;
}

public class DestinationSnapshot
{
    public int x;
    public int y;
    public int color;
    public int demand;
    public int type; // 0 = NotShown, 1 = Square, 2 = Circle
}

public class ResourceSnapshot
{
    public int roads;
    public int bridges;
    public int highways;
    public int tunnels;
    public int trafficLights;
}

public class GameState
{
    public List<HouseSnapshot> houses = new();
    public List<DestinationSnapshot> destinations = new();
    public ResourceSnapshot resources = new();
    public int time_tick;
}
