using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ShieldHrm;

namespace FrontEnd.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class MembersController : Controller
    {
        public MembersController()
        {
        }

        // GET: api/Members
        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            EmployeeServiceClient client = new EmployeeServiceClient(
                EmployeeServiceClient.EndpointConfiguration.BasicHttpBinding_IEmployeeService,
                "http://shieldhrm.teamassembler:8080/EmployeeService.svc/EmployeeService");

            try
            {
                return Json(await client.GetEmployeeListAsync());
            }
            finally
            {
                await client.CloseAsync();
            }
        }
    }
}
