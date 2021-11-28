public interface ISaveLoad
{
    string GetGameObjectName();
    string GetDataTypeName();
    ISaveLoadData Save();
    void Load(ISaveLoadData loadData);
}
