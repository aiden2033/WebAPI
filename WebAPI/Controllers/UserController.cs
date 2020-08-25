using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        //GET PUT POST DELETE
        [HttpGet]
        public Task<ActionResult<User>> Get(User user)
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
}
