namespace SDB.DataServices.Auth
{
    public interface IAuthenticator
    {
        bool IsAuthenticated { get; }
        int? UserItemId { get; }
        int? UserWorkspaceContainerId { get;}
    }
}
