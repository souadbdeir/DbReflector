namespace DbReflector.Common;

public interface ITriggerInfo : ISchemaObjectInfo
{
    string TriggerName { get; }
    string SchemaName { get; }
    string TableName { get; }
    string Definition { get; }
    string TriggerType { get; }
    string TriggerTiming { get; }
    bool IsEnabled { get; }
}
