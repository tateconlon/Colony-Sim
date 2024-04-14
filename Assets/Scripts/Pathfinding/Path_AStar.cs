using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Priority_Queue;

public class Path_AStar
{
    Stack<Tile> path;

    
    //g_score is the current REAL distance from start to node
    //f_score is the GUESSED distance. f_score = current_g_score + guessed_distance(here, goal)
    public Path_AStar(World world, Tile start, Tile goal)
    {
        if (world.tileGraph == null)
        {
            world.tileGraph = new Path_TileGraph(world);
        }
        Dictionary<Tile, Path_Node<Tile>> map = world.tileGraph.nodes;

        if (!map.ContainsKey(start))
        {
            Debug.LogError($"world tileGraph does not contain start node {start}");
            return;
        }
        if (!map.ContainsKey(goal))
        {
            Debug.LogError($"world tileGraph does not contain goal node {goal}");
            return;
        }
        
        List<Path_Node<Tile>> closed = new();
        SimplePriorityQueue<Path_Node<Tile>> open_f = new();
        Dictionary<Path_Node<Tile>, Path_Node<Tile>> came_from = new();
        
        Dictionary<Path_Node<Tile>, float> g_scores = new();
        foreach (Path_Node<Tile> node in map.Values)
        {
            g_scores.Add(node, Mathf.Infinity);
        }
        
        open_f.Enqueue(map[start], CalculateFScore(map[start], map[goal]));

        g_scores[map[start]] = 0;

        while (open_f.Count > 0)
        {
            Path_Node<Tile> curr = open_f.Dequeue();
            if (curr.data == goal)
            {
                path = ReconstructPath(came_from, map[goal]);  //RECONSTRUCT PATH
            }

            //open_f.Remove(curr);  //This happens because of the Dequeue
            closed.Add(curr);

            foreach (Path_Edge<Tile> currNeighbour in curr.edges)
            {
                Path_Node<Tile> nNode = currNeighbour.node;
                if (closed.Contains(nNode))
                {
                    continue;
                }

                float tentative_g_score = g_scores[curr] + currNeighbour.cost;    // Vector2.Distance(curr.data.Pos, nNode.data.Pos);

                //We've navigated from start to here before
                //and we got here quicker from a different route (curr g_score is lower)
                //g_score represents travelled distance from start.
                if(open_f.Contains(nNode) && tentative_g_score >= g_scores[nNode])  
                {
                    continue;
                }
                
                //Update the lowest g_score
                g_scores[nNode] = tentative_g_score;

                //the neighbour came from the current. this has been the neighbors lowest g_score
                came_from[nNode] = curr;    
                
                //f_score (guessed score) is now re-guessed using the new g_score
                float f_score= tentative_g_score + CalculateFScore(nNode, map[goal]);
                
                //If we've naved here before (thus it's in the OpenSet), update the f_score
                if (open_f.Contains(nNode))
                {
                    open_f.UpdatePriority(nNode, f_score);
                }
                else //We haven't naved here before, put it in the open set
                {
                    open_f.Enqueue(nNode, f_score);
                }
            }   //end neighbour loop
            
            
        } //end of open set loop
        
        // There is no path from start to goal. We've looped through every node and didn't find the goal. 
    }

    Stack<Tile> ReconstructPath(Dictionary<Path_Node<Tile>, Path_Node<Tile>> cameFrom, Path_Node<Tile> curr)
    {
        Stack<Tile> totalPath = new();
        totalPath.Push(curr.data);
        while (cameFrom.ContainsKey(curr))
        {
            curr = cameFrom[curr];
            totalPath.Push(curr.data);
        }

        return totalPath;
    }

    public Tile DequeueNextTile()
    {
        if (path == null)   //no path
        {
            return null;
        }
        bool success = path.TryPop(out Tile t);
        if (path.Count == 0)
        {
            path = null;
        }
        return success ? t : null;
    }

    float CalculateFScore(Path_Node<Tile> curr, Path_Node<Tile> dest)
    {
        return Vector2.Distance(curr.data.Pos, dest.data.Pos);
    }

    public int Length()
    {
        if (path == null) return 0;

        return path.Count;
    }
}