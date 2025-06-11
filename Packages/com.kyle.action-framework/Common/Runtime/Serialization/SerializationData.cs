[System.Serializable]
public struct SerializationData
{
    public string Type;
    public string TypeGUID;
    public string JsonDatas;
    public override string ToString()
    {
        return "type: " + Type + " | JSON: " + JsonDatas;
    }
}