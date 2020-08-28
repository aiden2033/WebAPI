using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebAPI.Models;

namespace WebAPI.Filters
{
    public class SubDivisionFilter : ActionFilterAttribute
    {
        SubDivision s;
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            s = (SubDivision)context.ActionArguments["subdivision"];
            if (s == null || s.Id < 0)
                context.HttpContext.Response.StatusCode = 400;

        }
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            OkObjectResult res = (OkObjectResult)context.Result;
            s = (SubDivision)res.Value;
            if (s.ParentId < 0 || s.ParentId == s.Id)
            {
                s.ParentId = 0;
                res.Value = s;
                context.Result = res;
            }
        }
    }

}

