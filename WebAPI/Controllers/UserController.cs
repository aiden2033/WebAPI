using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        DataContext db;
        public UserController(DataContext context)
        {
            db = context;
        }

        [HttpPut("register")]
        public async Task<ActionResult<User>> Register(User user) //регистрация юзера
        {
            if (user == null)
                return BadRequest();
            bool res = await db.Users.AnyAsync(x => x.Login == user.Login); //нет ли пользователя с таким логином
            if (res)
                return BadRequest();
            user.Role = "admin"; //только одна роль возможна
            db.Users.Add(user);
            await db.SaveChangesAsync();
            await Token(user);
            return Ok(user);
        }

        [HttpPost("token")]
        public async Task<ActionResult<User>> Token(User user) //запросить токен
        {
            ClaimsIdentity Identity = GetIdentity(user.Login, user.Password);
            if (Identity == null) return BadRequest("Ошибка выдачи токена. Пользователь с такими данными не зарегестрирован.");
            var encodedJwt = CreateJWT(Identity); // создаем сам токен
            user.Token = encodedJwt;
            user.Login = Identity.Name;
            db.Users.Update(user);
            await db.SaveChangesAsync();
            return Ok(user);
        }

        private ClaimsIdentity GetIdentity(string username, string password)
        {
            var user = db.Users.FirstOrDefault(x => x.Login == username && x.Password == password);
            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimsIdentity.DefaultNameClaimType, user.Login),
                    new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role)
                };
                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Token",
                    ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
                return claimsIdentity;
            }
            return null; // если пользователя не нашли
        }
        private string CreateJWT(ClaimsIdentity identity)
        {
            var Jwt = new JwtSecurityToken(
                issuer: AuthOptions.Issuer,
                audience: AuthOptions.Audience,
                claims: identity.Claims, //чтобы передать логин и пароль пользователя
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(Jwt); //получаем в виде строки токен
        }

        #region CREATE
        [HttpPut("employee")]
        public Task<ActionResult<Employee>> Create(Employee employee) //создать запись о сотруднике
        {

            return null;
        }

        //[Required]
        //public string Surname { get; set; }
        //[Required]
        //public string Name { get; set; }
        //public string MiddleName { get; set; }
        //public DateTime Birthday { get; set; }
        //[Required]
        //public SubDivision SubDivision { get; set; } //подразделение 
        //[Required]
        //public Position Position { get; set; } //должность

        #endregion
        #region READ
        #endregion
        #region UPDATE
        #endregion
        #region DELETE
        #endregion

        //GET PUT POST DELETE     
        [HttpGet]
        public Task<ActionResult<User>> Get()
        {
            return null;
        }
        [HttpPut]
        public Task<ActionResult<User>> Put(User user)
        {
            return null;
        }
        [HttpPost]
        public Task<ActionResult<User>> Post(User user)
        {
            return null;
        }
        [HttpDelete]
        public Task<ActionResult<User>> Delete(User user)
        {
            return null;
        }
    }


    #region TEST CONTROLLER
    [ApiController]
    [Route("api/[controller]")]
    public class ValuesController : ControllerBase
    {
        [Authorize]
        [Route("getlogin")]
        public IActionResult GetLogin()
        {
            return Ok($"Ваш логин: {User.Identity.Name}");
        }

        [Authorize(Roles = "admin")]
        [Route("getrole")]
        public IActionResult GetRole()
        {
            return Ok("Ваша роль: admin");
        }
    }
    #endregion
}
