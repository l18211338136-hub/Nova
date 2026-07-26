namespace Nova.Framework.Web.CQRS;

public class CommandTypeMetadata
{
    public Type CommandType { get; }

    public CommandTypeMetadata(Type commandType)
    {
        CommandType = commandType;
    }
}
