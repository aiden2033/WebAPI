﻿using System;
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
using WebAPI.Filters;
using Microsoft.AspNetCore.Http;

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
            long? l = Request?.ContentLength;
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
                         Include(x => x.SubDivision); //связываем с другими таблицам 
            await db.SaveChangesAsync();
            return Ok(employee);
        }
        [Authorize]
        [HttpPut("position")]
        public async Task<ActionResult<Position>> Create(Position position) //создать запись о должности
        {
            #region Проверка входных данных
            if (position == null)
                return BadRequest("Невозможно добавить должность с пустыми значениями");
            if (!CheckEntireData(position.Name))
                return BadRequest("Получены некоректные данные");
            bool result = await db.Positions.AnyAsync(x => x.Name == position.Name); //проверка
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
            bool result = await db.SubDivisions.AnyAsync(x => x.Name == subDivision.Name && x.ParentId == subDivision.ParentId); //проверяем, нет ли такого пользователя
            if (result)
                return BadRequest("Запись о данном подразделении уже внесена");
            #endregion
            await db.SubDivisions.AddAsync(subDivision);
            await db.SaveChangesAsync();
            return Ok(subDivision);
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

        #endregion


        #region READ
        [Authorize]
        [HttpGet("employee")]
        [EmployeeFilter]
        public async Task<ActionResult<Employee>> Read(Employee employee) //прочитать о сотруднике
        {
            if (Response.StatusCode == 400)
                return BadRequest("Не удалось обработать данный запрос.");
            Employee e = await db.Employees.FirstOrDefaultAsync(x =>
                                                            (x.Surname == employee.Surname &&
                                                            x.Name == employee.Name) ||
                                                            (x.Id == employee.Id)); //ищем сотрудника
            if (e == null)
                return BadRequest("Сотрудник не найден. Попробуйте изменить запрос.");
            return Ok(e);
        }

        [Authorize]
        [HttpGet("position")]
        public async Task<ActionResult<Position>> Read(Position position) //прочитать о должности
        {
            #region Проверка входных данных
            if (position == null)
                return BadRequest("Ошибка чтения запроса");
            Position pos = await db.Positions.FirstOrDefaultAsync(x => x.Id == position.Id ||
                                                                 (position.Name != null &&
                                                                 x.Name.ToLower() == position.Name.ToLower()));//ищем должность
            if (pos == null)
                return BadRequest("Информация о должности не найдена. Попробуйте изменить запрос"); // !!!!!!!
            #endregion
            return Ok(pos);
        }

        [Authorize]
        [HttpGet("subdivision")]
        [SubDivisionFilter]
        public async Task<ActionResult<SubDivision>> Read(SubDivision subdivision) //прочитать о подразделении
        {
            if (Response.StatusCode == 400)
                return BadRequest("Не удалось обработать данный запрос.");
            SubDivision sub = await db.SubDivisions.FirstOrDefaultAsync(x =>
                                                                            x.Id == subdivision.Id ||
                                                                           (subdivision.Name != null && x.Name.ToLower() == subdivision.Name.ToLower())); //ищем подразделение
            if (sub == null)
                return BadRequest("Информация о подразделении не найдена. Попробуйте изменить запрос");
            return Ok(sub);
        }

        [Authorize]
        [HttpGet("user")]
        public async Task<ActionResult<User>> Read(User user) //прочитать о пользователе
        {
            #region Проверка входных данных
            if (user == null)
                return BadRequest("Ошибка чтения запроса");
            User u = await db.Users.FirstOrDefaultAsync(x =>
                                                                x.Id == user.Id ||
                                                                (user.Login != null && x.Login == user.Login)); //ищем пользователя
            if (u == null)
                return BadRequest("Пользователь не найден. Попробуйте изменить запрос");
            #endregion
            return Ok(u);
        }
        #endregion


        #region UPDATE

        [Authorize]
        [HttpPost("employee")]
        public async Task<ActionResult<Employee>> Update(Employee employee) //изменить сотрудника
        {
            #region Проверка входных данных
            if (employee == null)
                return BadRequest("Ошибка чтения запроса");
            Employee e = await db.Employees.FirstOrDefaultAsync(x => x.Id == employee.Id); //ищем сотрудника            
            if (e == null)
                return BadRequest("Сотрудник не найден. Попробуйте изменить запрос");
            e.Surname = employee.Surname;
            e.Name = employee.Name;
            if (employee.MiddleName != null && e.MiddleName != employee.MiddleName) e.MiddleName = employee.MiddleName;
            if (employee.Position != null && e.Position != employee.Position) e.Position = employee.Position;
            if (employee.SubDivision != null && e.SubDivision != employee.SubDivision) e.SubDivision = employee.SubDivision;
            if (employee.Birthday != null && e.Birthday != employee.Birthday) e.Birthday = employee.Birthday;
            db.Employees.Update(e);
            db.Employees.Include(s => s.SubDivision).Include(p => p.Position);
            await db.SaveChangesAsync();
            #endregion
            return Ok(e);
        }

        [Authorize]
        [HttpPost("position")]
        public async Task<ActionResult<Position>> Update(Position position) // изменить должность
        {
            #region Проверка входных данных
            if (position == null)
                return BadRequest("Ошибка чтения запроса");
            Position p = await db.Positions.FirstOrDefaultAsync(x => x.Id == position.Id);
            if (p == null)
                return BadRequest("Должность не найдена. Попробуйте изменить запрос");
            if (position.Name != null && position.Name != "") p.Name = position.Name;
            if (position.Employees != null) p.Employees.AddRange(position.Employees);
            db.Positions.Update(p);
            db.Positions.Include(e => e.Employees);
            await db.SaveChangesAsync();
            #endregion
            return Ok(p);
        }

        [Authorize]
        [HttpPost("subdivision")]
        public async Task<ActionResult<SubDivision>> Update(SubDivision subdivision) //изменить подразделение
        {
            #region Проверка входных данных
            if (subdivision == null)
                return BadRequest("Ошибка чтения запроса");
            SubDivision s = await db.SubDivisions.FirstOrDefaultAsync(x => x.Id == subdivision.Id);
            if (s == null)
                return BadRequest("Подразделение не найдено. Попробуйте изменить запрос");
            if (subdivision.Name != null && subdivision.Name != "") s.Name = subdivision.Name;
            if (subdivision.ParentId != s.ParentId) s.ParentId = subdivision.ParentId;
            if (subdivision.Employees != null) s.Employees.AddRange(subdivision.Employees);
            db.SubDivisions.Update(s);
            db.SubDivisions.Include(e => e.Employees);
            await db.SaveChangesAsync();
            #endregion
            return Ok(s);
        }

        #endregion


        #region DELETE

        [Authorize]
        [HttpDelete("employee/{id}")]
        public async Task<ActionResult<Employee>> Delete(uint? id) //удалить сотрудника
        {
            #region Проверка входных данных
            if (id == null)
                return BadRequest("Ошибка чтения запроса");
            Employee e = await db.Employees.FirstOrDefaultAsync(x => x.Id == id); //ищем сотрудника
            #endregion            
            if (e != null)
            {
                db.Employees.Remove(e);
                await db.SaveChangesAsync();
                return Ok(@"Данные о сотруднике: { " + e.Surname + " " + e.Name + " } успешно удалены");
            }
            return BadRequest("Не удалось выполнить удаление. Сотрудник с данным идентификатором не найден.");
        }

        [Authorize]
        [HttpDelete("position/{id}")]
        public async Task<ActionResult<Position>> DeletePosition(uint? id) //удалить должность
        {
            #region Проверка входных данных
            if (id == null)
                return BadRequest("Ошибка чтения запроса");
            Position p = await db.Positions.FirstOrDefaultAsync(x => x.Id == id);
            #endregion
            if (p != null)
            {
                db.Positions.Remove(p);
                await db.SaveChangesAsync();
                return Ok("Данные о должности { " + p.Name + " } успешно удалены");
            }
            return BadRequest("Неудалось выполнить удаление. Должность не найдена");
        }

        [Authorize]
        [HttpDelete("subdivision/{id}")]
        public async Task<ActionResult<SubDivision>> DeleteSubDiv(uint? id) //удалить данные о подразделении
        {
            #region Проверка входных данных
            if (id == null)
                return BadRequest("Ошибка чтения запроса");
            SubDivision s = await db.SubDivisions.FirstOrDefaultAsync(x => x.Id == id);
            #endregion
            if (s != null)
            {
                db.SubDivisions.Remove(s);
                await db.SaveChangesAsync();
                return Ok("Данные о подразделении { " + s.Name + " } успешно удалены");
            }
            return BadRequest("Не удалось выполнить удаление. Подразделение не найдено.");
        }
        #endregion

    }
}
