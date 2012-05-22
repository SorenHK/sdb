using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDB.DataServices.Auth
{
    public class AuthException : Exception
    {
        public AuthException(string message)
            : base(message)
        {
        }

        public static AuthException NotLoggedIn()
        {
            return new AuthException("You are not logged in");
        }

        public static AuthException AccessDenied()
        {
            return new AuthException("You do not have the required access to the resource");
        }
    }
}
