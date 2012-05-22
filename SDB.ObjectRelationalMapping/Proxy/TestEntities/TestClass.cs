namespace SDB.ObjectRelationalMapping.Proxy.TestEntities
{
    class TestClass
    {
        public override bool Equals(object obj)
        {
            System.Diagnostics.Debug.WriteLine("Test");

            return base.Equals(obj);
        }
    }
}
