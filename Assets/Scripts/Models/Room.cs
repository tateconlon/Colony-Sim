using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Room
{
    public float atmos_O2;
    public float atmos_N;
    public float atmos_CO2;

    HashSet<Tile> tiles;

    public Room()
    {
        tiles = new();
    }

    public void AssignTile(Tile t)
    {
        if (tiles.Contains(t) && t == null) { return; }

        //Unassign from old room
        if (t.room != null)
        {
            t.room.tiles.Remove(t);
        }
        
        t.room = this;
        tiles.Add(t);
    } 

    //Removed because it's confusing with UnassignAllTiles, which unassigns and reassigns to Outside
    public void UnassignTile(Tile t)
    {
        if (t.room.tiles.Remove(t))
        {
            t.room = null;
        }
    }

    public Tile[] UnAssignAllTiles()
    {
        Tile[] removedTiles = new Tile[tiles.Count];
        tiles.CopyTo(removedTiles);
        while (tiles.Count > 0)
        {
            UnassignTile(tiles.First());
        }
        tiles.Clear();
        return removedTiles;
    }
    
    public static void DoRoomFloodFill(Furniture sourceFurniture)
    {
        DoRoomFloodFill(sourceFurniture.tile);
    }
    
    /// <summary>
    /// Rooms can be divided diagonally (since you can't walk through them that way)
    /// x      r2
    ///     x
    /// r1      x 
    /// </summary>
    /// <param name="sourceFurniture">Piece of furniture that may be splitting two existing rooms, or the final
    /// enclosing piece to form a new room</param>
    /// <param name="world"></param>
    public static void DoRoomFloodFill(Tile tile)
    {
        //Check the NEWS neighbourds of the sourceFurniture's tile
        //and do flood fill from them.
        
        //Destroy the room that the sourceFurniture is in, then rebuild it,
        //We only room that can be altered is the room it's placed in.
        //Unless we are DESTROYING a piece of furniture, which we may be merging multiple rooms.
        //We asssume that the "outside" is one big room.

        World world = tile.world;

        Room oldRoom = tile.room;
        
        
        //Get the neighbouring NESW tiles and add them 
        foreach (Tile neighbour in tile.GetNeighbours(false))
        {
            Room newRoom = ActualFloodFill(neighbour, oldRoom);
            if (newRoom != null)
            {
                world.AddRoom(newRoom);
            }
        }

        //If we placed down a wall, make it not have a room
        if (oldRoom != null && tile.furniture != null && tile.furniture.roomEnclosure)
        {
            oldRoom.UnassignTile(tile);
        }

        if (oldRoom != null && oldRoom != world.GetOutsideRoom())
        {
            if (oldRoom.tiles.Count > 0)
            {
                Debug.LogError($"Room::DoRoomFloodFill - oldRoom still has tiles assigned to it, which is wrong.");
            }
            world.DeleteRoom(oldRoom);
        }
    }
    
    //oldRoom:
    //Since we do multiple flood fill passes that could operate on the same tile,
    //we check if tile.room == oldRoom, to see if this tile has not been visited.
    //It means that this tile has not been assigned a new room (ie: it still
    //belongs to its old (outdated, soon to be deleted) room
    protected static Room ActualFloodFill(Tile startTile, Room oldRoom)
    {
        Room potentialRoom = new();
        Queue<Tile> toVisit = new();
        HashSet<Tile> visited = new();

        toVisit.Enqueue(startTile);

        while (toVisit.Count > 0)
        {
            Tile t = toVisit.Dequeue(); //Visit this tile
            visited.Add(t);
            
            //If we hit a null, so we're "outside"
            if (t == null || t.TileType == TileType.Empty)
            {
                if (oldRoom != t.world.GetOutsideRoom())
                {
                    if (oldRoom != null)
                    {// incase oldRoom is null if we're destroying an object.
                        Tile[] oldRoomTiles = oldRoom.UnAssignAllTiles();
                        foreach (Tile oldRoomTile in oldRoomTiles)
                        {
                            oldRoomTile.world.GetOutsideRoom()
                                .AssignTile(oldRoomTile);                
                        }
                    }
                }
                
                //Put the tiles outside
                foreach (Tile visitedTile in potentialRoom.UnAssignAllTiles())
                {
                    visitedTile.world.GetOutsideRoom().AssignTile(visitedTile);
                }
                
                return null;
            }
            
            //Check if we should add this tile to the room and flood it's neighbours
            if (t.room != oldRoom) {continue;}  //This tile's room has already been changed (probably from a previous pass)
            if (t.furniture != null && t.furniture.roomEnclosure) {continue;} //Is edge of room

            potentialRoom.AssignTile(t);
            
            //Get the neighbouring NESW tiles and add them 
            foreach (Tile neighbour in t.GetNeighbours(false))
            {
                if(visited.Contains(neighbour) || toVisit.Contains(neighbour)) {continue;}  //SKIP: we've visited or will visit this tile already
                toVisit.Enqueue(neighbour);
            }
        }

        if (potentialRoom.tiles.Count == 0)
        {
            return null;
        }

        potentialRoom.atmos_N = oldRoom.atmos_N;
        potentialRoom.atmos_O2 = oldRoom.atmos_O2;
        potentialRoom.atmos_CO2 = oldRoom.atmos_CO2;
        

        return potentialRoom;
    }
}