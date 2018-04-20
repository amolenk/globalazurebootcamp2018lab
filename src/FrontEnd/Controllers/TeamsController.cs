using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ShieldHrm;

namespace FrontEnd.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class TeamsController : Controller
    {
        private readonly HttpClient httpClient;
        private readonly FabricClient fabricClient;
        private readonly StatelessServiceContext serviceContext;

        public TeamsController(HttpClient httpClient, StatelessServiceContext serviceContext, FabricClient fabricClient)
        {
            this.fabricClient = fabricClient;
            this.httpClient = httpClient;
            this.serviceContext = serviceContext;
        }

        // GET: api/Teams
        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            Uri serviceName = GetBackEndServiceName(serviceContext);

            ServicePartitionList partitions = await fabricClient.QueryManager.GetPartitionListAsync(serviceName);

            List<Team> result = new List<Team>();

            foreach (Partition partition in partitions)
            {
                long partitionKey = ((Int64RangePartitionInformation)partition.PartitionInformation).LowKey;
                string proxyUrl = GetProxyUrl(serviceContext, partitionKey);

                using (HttpResponseMessage response = await httpClient.GetAsync(proxyUrl))
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        continue;
                    }

                    result.AddRange(JsonConvert.DeserializeObject<List<Team>>(await response.Content.ReadAsStringAsync()));
                }
            }

            return Json(result);
        }

        // PUT: api/Teams/name
        [HttpPut("{name}")]
        public async Task<IActionResult> Put(string name, [FromBody]string[] members)
        {
            string proxyUrl = GetProxyUrl(serviceContext, name);

            PowerGrid powerGrid = await CalculatePowerGridAsync(members);

            StringContent putContent = new StringContent(
                JsonConvert.SerializeObject(new Team
                {
                    Name = name,
                    Members = members,
                    PowerGrid = powerGrid,
                    Score = powerGrid.CalculateAverageScore()
                }),
                Encoding.UTF8,
                "application/json");

            putContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (HttpResponseMessage response = await httpClient.PutAsync(proxyUrl, putContent))
            {
                return new ContentResult()
                {
                    StatusCode = (int)response.StatusCode,
                    Content = await response.Content.ReadAsStringAsync()
                };
            }
        }

        // DELETE: api/Teams/name
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            string proxyUrl = GetProxyUrl(serviceContext, name);

            using (HttpResponseMessage response = await httpClient.DeleteAsync(proxyUrl))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return StatusCode((int)response.StatusCode);
                }
            }

            return Ok();
        }

        private static async Task<PowerGrid> CalculatePowerGridAsync(string[] members)
        {
            int intelligence = 0;
            int strength = 0;
            int speed = 0;
            int durability = 0;
            int energyProjection = 0;
            int fightingSkills = 0;

            EmployeeServiceClient client = new EmployeeServiceClient(
                EmployeeServiceClient.EndpointConfiguration.BasicHttpBinding_IEmployeeService,
                "http://shieldhrm.teamassembler:8080/EmployeeService.svc/EmployeeService");

            try
            {
                foreach (string member in members)
                {
                    Employee employeeDetails = await client.GetEmployeeDetailsAsync(member);

                    intelligence += employeeDetails.Intelligence;
                    strength += employeeDetails.Strength;
                    speed += employeeDetails.Speed;
                    durability += employeeDetails.Durability;
                    energyProjection += employeeDetails.EnergyProjection;
                    fightingSkills += employeeDetails.FightingSkills;
                }
            }
            finally
            {
                await client.CloseAsync();
            }

            int teamSize = members.Length;

            return new PowerGrid
            {
                Intelligence = intelligence / teamSize,
                Strength = strength / teamSize,
                Speed = speed / teamSize,
                Durability = durability / teamSize,
                EnergyProjection = energyProjection / teamSize,
                FightingSkills = fightingSkills / teamSize
            };
        }

        private static Uri GetBackEndServiceName(ServiceContext context)
        {
            return new Uri($"{context.CodePackageActivationContext.ApplicationName}/BackEnd");
        }

        private static string GetProxyUrl(ServiceContext context, string teamName)
        {
            // Create a partition key from the given name.
            // Use the zero-based numeric position in the alphabet of the first letter of the name (0-25).
            long partitionKey = Char.ToUpper(teamName.First()) - 'A';

            Uri serviceName = GetBackEndServiceName(context);

            return $"http://localhost:19081{serviceName.AbsolutePath}/api/Teams/{teamName}?PartitionKey={partitionKey}&PartitionKind=Int64Range";
        }

        private static string GetProxyUrl(ServiceContext context, long partitionKey)
        {
            Uri serviceName = GetBackEndServiceName(context);

            return $"http://localhost:19081{serviceName.AbsolutePath}/api/Teams?PartitionKey={partitionKey}&PartitionKind=Int64Range";
        }
    }

    public class Team
    {
        public string Name { get; set; }

        public string[] Members { get; set; }

        public int Score { get; set; }

        public PowerGrid PowerGrid { get; set; }
    }

    public class PowerGrid
    {
        public int Intelligence { get; set; }

        public int Strength { get; set; }

        public int Speed { get; set; }

        public int Durability { get; set; }

        public int EnergyProjection { get; set; }

        public int FightingSkills { get; set; }

        public int CalculateAverageScore()
        {
            return (Intelligence + Strength + Speed + Durability + EnergyProjection + FightingSkills) / 6;
        }
    }
}
