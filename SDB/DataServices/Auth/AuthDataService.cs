using System.Collections.Generic;
using System.Linq;

namespace SDB.DataServices.Auth
{
    public class AuthDataService : DataServiceBase
    {
        private readonly DataServiceBase _dataService;
        private readonly LinkedList<IAuthenticator> _authenticators;

        public bool IsAuthenticated
        {
            get
            {
                var auth = GetActiveAuthenticator();
                return auth != null && auth.IsAuthenticated;
            }
        }

        public int? UserItemId
        {
            get
            {
                var auth = GetActiveAuthenticator();
                return auth != null ? auth.UserItemId : null;
            }
        }

        public int? UserWorkspaceContainerId
        {
            get
            {
                var auth = GetActiveAuthenticator();
                return auth != null ? auth.UserWorkspaceContainerId : null;
            }
        }

        public AuthDataService(DataServiceBase dataService)
        {
            _dataService = dataService;
            _authenticators = new LinkedList<IAuthenticator>();

            _dataService.ItemChanged += OnItemChanged;
            _dataService.RelationAdded += OnRelationAdded;
            _dataService.RelationRemoved += OnRelationRemoved;
        }

        public AuthDataService(DataServiceBase dataService, IAuthenticator authenticator)
            : this(dataService)
        {
            Add(authenticator);
        }

        public void Add(IAuthenticator authenticator)
        {
            _authenticators.AddLast(authenticator);
        }

        private IAuthenticator GetActiveAuthenticator()
        {
            return _authenticators.FirstOrDefault(a => a.IsAuthenticated);
        }

        #region DataServiceBase members

        public override ICollection<DbRelation> GetRelations(int? fromId)
        {
            return _dataService.GetRelations(fromId);
        }

        public override DbRelation GetRelation(int? fromId, string identifier)
        {
            return _dataService.GetRelation(fromId, identifier);
        }

        public override DbItem GetItem(int id)
        {
            if (!IsAuthenticated)
                throw AuthException.NotLoggedIn();

            return _dataService.GetItem(id);
        }

        public override void Insert(DbItem item)
        {
            if (!IsAuthenticated)
                throw AuthException.NotLoggedIn();

            _dataService.Insert(item);
        }

        public override void Update(DbItem item)
        {
            if (!IsAuthenticated)
                throw AuthException.NotLoggedIn();

            _dataService.Update(item);
        }

        public override void Delete(DbItem item)
        {
            if (!IsAuthenticated)
                throw AuthException.NotLoggedIn();

            _dataService.Delete(item);
        }

        public override void Insert(DbRelation relation)
        {
            if (!IsAuthenticated)
                throw AuthException.NotLoggedIn();

            _dataService.Insert(relation);
        }

        public override void Delete(DbRelation relation)
        {
            if (!IsAuthenticated)
                throw AuthException.NotLoggedIn();

            _dataService.Delete(relation);
        }

        #endregion DataServiceBase members

        public override void Dispose()
        {
            if (_dataService != null)
                _dataService.Dispose();

            base.Dispose();
        }
    }
}
