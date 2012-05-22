using SDB.DataServices.Auth;

namespace SDB.DataServices.Tcp
{
    public class TcpBasicClientAuthenticator : IAuthenticator
    {
        private readonly DataServiceBase _dataService;
        private readonly TcpClient _client;

        public bool IsAuthenticated { get; private set; }
        public int? UserItemId { get; private set; }
        public int? UserWorkspaceContainerId { get; private set; }

        public TcpBasicClientAuthenticator(DataServiceBase dataService, TcpClient client)
        {
            _dataService = dataService;
            _client = client;
        }

        public void Login(string username, string password)
        {
            var request = new ParamTcpMessage("loginbasic");
            request.SetParam("username", username);
            request.SetParam("password", password);
            var response = _client.SendAndReceive(request);

            if (response.HasType(TcpRequestType.Ok))
            {
                var objResponse = new ObjectTcpMessage<DbItem>(response);
                if (objResponse.Item != null)
                {
                    IsAuthenticated = true;

                    UserItemId = objResponse.Item.Id;

                    UserWorkspaceContainerId = _dataService.GetOrCreateItem(UserItemId, "workspace").Id;
                }
                else
                {
                    throw new AuthException("Login failed. No user-item returned.");
                }
            }
            else
            {
                throw new AuthException("Login failed. Response: " + response.RequestType + " - " + response.Content);
            }
        }
    }
}
