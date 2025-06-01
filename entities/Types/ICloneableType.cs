namespace Entities.Types
{
    public interface ICloneableType<T>
    {
        T Clone();
    }
}