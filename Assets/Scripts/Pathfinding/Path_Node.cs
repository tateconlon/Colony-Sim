using System.Collections.Generic;

public class Path_Node<T>
{
    public T data;
    
    public Path_Edge<T>[] edges; //Nodes leading OUT. We won't know what leads in, and we don't care!

}