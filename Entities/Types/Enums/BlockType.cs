namespace Entities.Types.Enums;

public enum BlockType
{
    Unknown = -1,
    Teammate = 0,
    Opponent = 1,
}
public static class BlockTypeParser
{
    public static BlockType ParseFromString(string? blockType)
    {
        switch (blockType)
        {
            case "teammate-blocked":
                return BlockType.Teammate;
            case "blocked":
                return BlockType.Opponent;
            case null:
                return BlockType.Unknown;
            default:
                throw new ArgumentException($"Invalid BlockType value: {blockType}", nameof(blockType));
        }
    }
}