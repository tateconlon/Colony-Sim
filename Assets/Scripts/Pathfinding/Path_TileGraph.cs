using System.Collections.Generic;

public class Path_TileGraph
{
    //This class constructs a simple path-finding compatible graph
    //of our world. Each tile is a node. Each WALKABLE neighbour from 
    // a tile is linked via an edge connection.

    public Dictionary<Tile, Path_Node<Tile>> nodes = new();

    public Path_TileGraph(World world, bool allowCornerClipping = false)
    {
        //We won't create nodes for impassible tiles
        //No nodes for Walls or non-floor (ie: Empty) tiles
        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                Tile tile_data = world.GetTileAt(x, y);

                //We still add a NODE for an impassible tile in case you are standing on it
                //after a job. We just won't create an edge INTO an impassible tile.
                //We will create an edge OUT of an impassible tile though.
                //if (tile_data.movementCost <= 0) { continue; }
                
                Path_Node<Tile> nodeTile = new Path_Node<Tile>();
                nodeTile.data = tile_data;
                nodes.Add(tile_data, nodeTile);
            }
        }

        foreach (KeyValuePair<Tile,Path_Node<Tile>> tile_node in nodes)
        {
            // Get a list of neighbours for the tile
            // If neighboour is walkable, create an edge to the relevant node

            Tile[] neighbours = tile_node.Key.GetNeighbours(true);
            List<Path_Edge<Tile>> tempEdgeList = new();

            for (int i = 0; i < 8; i++)
            {
                Tile t = neighbours[i];

                //Clipping is if you can path diagonally through
                //an impassible object like a  wall (movementCost == 0).
                //If clipping is allowed, you can go diagonally from x to g,
                //Even though you'll walk through the corner of the wall.
                //no clipping is default so you have to go around a wall properly
                //         g      ||| g 
                //      x |||      x 
                if (allowCornerClipping == false)
                {
                    Tile t_N = neighbours[0];
                    Tile t_E = neighbours[1];
                    Tile t_S = neighbours[2];
                    Tile t_W = neighbours[3];
                    
                    switch (i)
                    {
                        case 4: //NE tile
                            if ((t_N == null || t_N.movementCost == 0)
                                || (t_E == null || t_E.movementCost == 0))
                                continue;
                            break;
                        case 5: //SE tile
                            if ((t_S == null || t_S.movementCost == 0)
                                || (t_E == null || t_E.movementCost == 0))
                                continue;
                            break;
                        case 6: //SW tile
                            if ((t_S == null || t_S.movementCost == 0)
                                || (t_W == null || t_W.movementCost == 0))
                                continue;
                            break;
                        case 7: //NW tile
                            if ((t_N == null || t_N.movementCost == 0)
                                || (t_W == null || t_W.movementCost == 0))
                                continue;
                            break;
                        default:
                            break;
                    }
                }


                if (t == null || t.movementCost <= 0) continue;

                Path_Edge<Tile> edge = new Path_Edge<Tile>();
                edge.node = nodes[t];
                edge.cost = i < 4 ? t.movementCost : t.movementCost * 1.41421356237f;  //sqrt(2) = 1.414...  //handle corner neighbours being farther away

                tempEdgeList.Add(edge);
            }

            tile_node.Value.edges = tempEdgeList.ToArray();
            tempEdgeList.Clear();
        }
    }
    
    
}