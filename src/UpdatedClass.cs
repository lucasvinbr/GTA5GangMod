namespace GTA.GangAndTurfMod
{
    /// <summary>
    /// basically a class that requires updates every once in a while
    /// not frequently enough to be a child of the script class, however
    /// </summary>
    abstract public class UpdatedClass
    {
        public int ticksBetweenUpdates = 600;
        public int ticksSinceLastUpdate;
        abstract public void Update();
    }
}
