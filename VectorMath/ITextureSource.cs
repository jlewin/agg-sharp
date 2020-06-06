namespace MatterHackers.VectorMath
{
    public interface ITextureSource
    {
        byte[] GetBuffer();

        int ChangedCount { get;  }

        bool HasTransparency { get; }
    }
}
