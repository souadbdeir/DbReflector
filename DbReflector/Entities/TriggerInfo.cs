using DbReflector.Common;

namespace DbReflector;

public sealed class TriggerInfo : ITriggerInfo
{
    public TriggerInfo(
        string triggerName,
        string schemaName,
        string tableName,
        string definition,
        string triggerType,
        string triggerTiming,
        bool isEnabled)
    {
        TriggerName = triggerName;
        SchemaName = schemaName;
        TableName = tableName;
        Definition = definition;
        TriggerType = triggerType;
        TriggerTiming = triggerTiming;
        IsEnabled = isEnabled;
    }

    public string TriggerName { get; }
    public string SchemaName { get; }
    public string TableName { get; }
    public string Definition { get; }
    public string TriggerType { get; }
    public string TriggerTiming { get; }
    public bool IsEnabled { get; }
    public string DictionaryKey => $"{SchemaName}.{TableName}.{TriggerName}";
}
