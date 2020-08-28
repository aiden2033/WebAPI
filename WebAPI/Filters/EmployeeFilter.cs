using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebAPI.Models;

namespace WebAPI.Filters
{
    public class EmployeeFilter : ActionFilterAttribute
    {
        Employee e;
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            e = (Employee)context.ActionArguments["employee"];
            if (e == null || e.Name == null || e.Surname == null || e.Name == "" || e.Surname == null)
                context.HttpContext.Response.StatusCode = 400;
        }
        public override void OnActionExecuted(ActionExecutedContext context) //если у сотрудника не указан отдел, говорим, что он из руководства
        {
            OkObjectResult res = (OkObjectResult)context.Result;
            e = (Employee)res.Value;
            if (e.SubDivision == null || e.SubDivision.Name == null || e.SubDivision.Name == "")
            {
                e.SubDivision = new SubDivision
                {
                    Name = "Руководящий отдел",
                    ParentId = 0
                };
                res.Value = e;
                context.Result = res;
            }
        }
    }
}
