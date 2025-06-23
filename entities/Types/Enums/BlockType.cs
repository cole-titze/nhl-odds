namespace Entities.Types.Enums
{
    public enum BlockType
    {
        Teammate,
        Opponent,
    }
    public static class BlockTypeParser
    {
        public static BlockType ParseFromString(string blockType)
        {
            switch (blockType)
            {
            case "teammate-blocked":
                return BlockType.Teammate;
            case "blocked":
                return BlockType.Opponent;
            default:
                throw new ArgumentException($"Invalid BlockType value: {blockType}", nameof(blockType));
            }
        }
    }
}