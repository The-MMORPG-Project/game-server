namespace Valk.Networking
{
    public abstract class Commands
    {
        public virtual void Run(string[] args)
        {
            Console.Log(LogType.Warning, "Unimplemented command.");
        }
    }
}