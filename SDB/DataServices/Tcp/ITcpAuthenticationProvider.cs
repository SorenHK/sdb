namespace SDB.DataServices.Tcp
{
    public interface ITcpAuthenticationProvider
    {
        bool IsAuthenticated(TcpConnectedHost host);
        DbItem GetUserItem(TcpConnectedHost host);
    }
}
