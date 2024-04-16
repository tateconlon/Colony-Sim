// Things that are lying on the floor/stockpiled, like metal bars
//ore, or a non-installed copy of furniture (eg: a cabinet still in the box
//from IKEA.

using System;

public class Inventory
{
    public string objectType;
    public int maxStackSize = 0;
    public int stackSize = 0;

    public Tile tile;
    public Character character;

    public event Action<Inventory> OnChanged;

    protected Inventory(Inventory other)
    {
        objectType = other.objectType;
        maxStackSize = other.maxStackSize;
        stackSize = other.stackSize;
    }
    
    public Inventory(string objectType, int stackSize, int maxStackSize)
    {
        this.objectType = objectType;
        this.maxStackSize = maxStackSize;
        this.stackSize = stackSize;
    }

    public virtual Inventory Clone()
    {
        return new Inventory(this);
    }

    public void CallOnChanged()
    {
        OnChanged?.Invoke(this);
    }

    public int GetEmptyStackSpace()
    {
        return maxStackSize - stackSize;
    }
}