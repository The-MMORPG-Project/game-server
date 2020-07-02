namespace Valk.Networking
{
    public abstract class Commands
    {
        public virtual void Run(string[] args)
        {
            Console.LogWarning("Unimplemented command.");
        }
    }
}