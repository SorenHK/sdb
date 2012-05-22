namespace SDB
{
    public class DbItem
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public int? RefId { get; set; }

        public DbItem()
        {
        }

        public DbItem(string value)
        {
            Value = value;
        }

        public DbItem(string value, int? refId)
        {
            Value = value;
            RefId = refId;
        }

        public DbItem(int? refId)
        {
            RefId = refId;
        }

        public DbItem Copy(bool includeId = false)
        {
            var item = new DbItem(Value, RefId);
            if (includeId)
                item.Id = Id;
            return item;
        }
    }
}
