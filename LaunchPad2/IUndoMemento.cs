namespace LaunchPad2
{
    public interface IUndoMemento
    {
        void Do();

        void Undo();
    }
}
