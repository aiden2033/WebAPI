using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
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
        public async Task<ActionResult<User>> Register(User user) //регистрация пользователя
        {
            if (user == null)
                return BadRequest("Невозможно зарегестрировать пустого пользователя");
            bool result = await db.Users.AnyAsync(x => x.Login == user.Login); //нет ли пользователя с таким логином
            if (result)
                return BadRequest();
            user.Role = "admin"; //только одна роль возможна
            await db.Users.AddAsync(user);
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
            user.Role = "admin"; //только одна роль возможна
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

        private bool CheckEntireData(params string[] s) //проверка на некорректные символы в значениях
        {
            Regex r = new Regex(@"[\d!#h№;%:?*()-_=+@#$%^&*|.,><']");
            Match m;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == null || s[i] == "") return false;
                m = r.Match(s[i]);
                if (m.Success)
                    return false;
            }
            return true;
        }

        #region CREATE
        [Authorize]
        [HttpPut("employee")]
        public async Task<ActionResult<Employee>> Create(Employee employee) //создать запись о сотруднике
        {
            #region Проверка входных данных
            if (employee == null)
                return BadRequest("Ошибка добавления пустого значения");
            if (!CheckEntireData(employee.Name, employee.Surname))
                return BadRequest("Получены некоректные данные");
            bool result = await db.Employees.AnyAsync(x => x.Surname == employee.Surname &&
                                                           x.Name == employee.Name &&
                                                           x.Birthday == employee.Birthday); //проверяем, нет ли такого пользователя
            if (result)
                return BadRequest("Запись о данном сотруднике уже присутствует");
            #endregion
            await db.Employees.AddAsync(employee);
            db.Employees.Include(x => x.Position).
                         Include(x => x.SubDivision);
            await db.SaveChangesAsync();
            return Ok(employee);
        }
        [Authorize]
        [HttpPut("position")]
        public async Task<ActionResult<Position>> Create(Position position) //создать запись о новой должности
        {
            #region Проверка входных данных
            if (position == null)
                return BadRequest("Невозможно добавить должность с пустыми значениями");
            if (!CheckEntireData(position.Name))
                return BadRequest("Получены некоректные данные");
            bool result = await db.Positions.ContainsAsync(position); //проверка
            if (result)
                return BadRequest("Запись о данной должности уже внесена");
            #endregion

            await db.Positions.AddAsync(position);
            await db.SaveChangesAsync();
            return Ok(position);
        }
        [Authorize]
        [HttpPut("subdivision")]
        public async Task<ActionResult<SubDivision>> Create(SubDivision subDivision) //создать запись о подразделении
        {
            #region Проверка входных данных
            if (subDivision == null)
                return BadRequest("Невозможно добавить подразделение с пустыми значениями");
            if (!CheckEntireData(subDivision.Name))
                return BadRequest("Получены некоректные данные");
            bool result = await db.SubDivisions.ContainsAsync(subDivision); //проверяем, нет ли такого пользователя
            if (result)
                return BadRequest("Запись о данном подразделении уже внесена");
            #endregion
            await db.SubDivisions.AddAsync(subDivision);
            await db.SaveChangesAsync();
            return Ok(subDivision);
        }


        #endregion
        #region READ
        #endregion
        #region UPDATE
        #endregion
        #region DELETE
        #endregion

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
