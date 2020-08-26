using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI
{
    public class AuthOptions
    {
        public const string Issuer = "MyJWTServer"; // издатель
        public const string Audience = "MyJWTClient"; // потребитель
        const string Key = "unbreakablekey912*"; //для создания токена
        public const int LifeTime = 60;
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Key));
        }
    }
}
