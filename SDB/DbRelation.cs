namespace SDB
{
    public class DbRelation
    {
        public int Id { get; set; }
        public int? FromId { get; set; }
        public string Identifier { get; set; }
        public int? ToId { get; set; }
        public DbRelationType RelationType { get; set; }
        public int? SortNum { get; set; }

        public DbRelation()
        {
        }

        public DbRelation(int? fromId, string identifier, int? toId)
        {
            FromId = fromId;
            Identifier = identifier;
            ToId = toId;
        }

        public DbRelation(int? fromId, string identifier, int? toId, DbRelationType relationType)
            : this(fromId, identifier, toId)
        {
            RelationType = relationType;
        }

        public DbRelation(int? fromId, string identifier, int? toId, int? sortNum)
            : this(fromId, identifier, toId)
        {
            SortNum = sortNum;
        }

        public DbRelation(int? fromId, string identifier, int? toId, DbRelationType relationType, int? sortNum)
            : this(fromId, identifier, toId)
        {
            SortNum = sortNum;
            RelationType = relationType;
        }

        public DbRelation Copy(bool includeId = false)
        {
            var rel = new DbRelation(FromId, Identifier, ToId, RelationType, SortNum);
            if (includeId)
                rel.Id = Id;
            return rel;
        }
    }
}
