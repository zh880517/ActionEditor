using DataVisit;

public class TestDataCatalogAttribute : VisitCatalogAttribute
{
    public TestDataCatalogAttribute() : base(0, "TestData", "TestNamespace", "Assets/Generated/TestData/")
    {
    }
}
