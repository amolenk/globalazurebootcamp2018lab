# Add a Stateful Service for the Team Assembler Back-End

Now that we've moved the S.H.I.E.L.D. HRM system to the cloud, it's time to add some functionality.
We want to let users create different team configurations of characters and shows some stats for the assembled teams.
The first service you'll build for this scenario is a back-end service that will store the assembled teams along with the stats retrieved from the HRM system.
To minimize complexity and latency, we'll use a Service Fabric Stateful Reliable Service instead of an external data store.

## Basic Concepts

To get started with Reliable Services, you only need to understand a few basic concepts:

- **Service type**: This is your service implementation. It is defined by the class you write that extends either `StatelessService` or `StatefulService` and any other code or dependencies used therein, along with a name and a version number.
- **Named service instance**: To run your service, you create named instances of your service type, much like you create object instances of a class type. A service instance has a name in the form of a URI using the "fabric:/" scheme, such as "fabric:/MyApp/MyService".
- **Service host**: The named service instances you create need to run inside a host process. The service host is just a process where instances of your service can run.
- **Service registration**: Registration brings everything together. The service type must be registered with the Service Fabric runtime in a service host to allow Service Fabric to create instances of it to run.

## Create a Stateful Service

Service Fabric introduces a new kind of service that is stateful. A stateful service can maintain state reliably within the service itself, co-located with the code that's using it. State is made highly available by Service Fabric without the need to persist state to an external store.

To make team configurations highly available and persistent, even when the service moves or restarts, you need a stateful service.

In the same "TeamAssembler" application, you can add a new service by right-clicking on the Services references in the application project and selecting **Add -> New Service Fabric Service**.

Select **.NET Core 2.0 -> Stateful ASP.NET Core** and name it "BackEnd". Click **OK**.

In the **New ASP.NET Core Web Application** dialog, select **API**. Click **OK**.

Open the *BackEnd.cs* file in the service project. In Service Fabric, a service can run any business logic. The service API provides two entry points for your code:

1. An open-ended entry point method, called `RunAsync`, where you can begin executing any workloads, including long-running compute workloads. This method is not included in the ASP.NET Core template, but you can add it to the `BackEnd` class yourself. We won't need it in this lab though.

```csharp
protected override async Task RunAsync(CancellationToken cancellationToken)
{
    ...
}
```

2. A communication entry point where you can plug in your communication stack of choice, such as ASP.NET Core in this case. This is where you can start receiving requests from users and other services.

```csharp
protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
{
    return new ServiceReplicaListener[]
    {
        new ServiceReplicaListener(serviceContext =>
            new KestrelCommunicationListener(serviceContext, (url, listener) =>
            {
                ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                return new WebHostBuilder()
                            .UseKestrel()
                            .ConfigureServices(
                                services => services
                                    .AddSingleton<StatefulServiceContext>(serviceContext)
                                    .AddSingleton<IReliableStateManager>(this.StateManager))
                            .UseContentRoot(Directory.GetCurrentDirectory())
                            .UseStartup<Startup>()
                            .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                            .UseUrls(url)
                            .Build();
            }))
    };
}
```

The project template includes a sample implementation of `CreateServiceInstanceListeners()` that adds a `KestelCommunicationListener` that in turn uses a `WebHostBuilder` to host our ASP.NET Core service.

Note that the *state provider* instance `this.StateManager` is registered with the ASP.NET Dependency Injection mechanism. The `IReliableStateManager` uses [Reliable Collections](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reliable-services-reliable-collections) to let you create replicated data structures. By registering it with the DI mechanism, this Service Fabric class will be available to use in the ASP.NET Core API controller. 

## Add Models and Controller

Add a *Models.cs* file to the project and insert the following class definitions:

```csharp
using System.Runtime.Serialization;

namespace BackEnd
{
    [DataContract]
    public class Team
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string[] Members { get; set; }

        [DataMember]
        public int Score { get; set; }

        [DataMember]
        public PowerGrid PowerGrid { get; set; }
    }

    [DataContract]
    public class PowerGrid
    {
        [DataMember]
        public int Intelligence { get; set; }

        [DataMember]
        public int Strength { get; set; }

        [DataMember]
        public int Speed { get; set; }

        [DataMember]
        public int Durability { get; set; }

        [DataMember]
        public int EnergyProjection { get; set; }

        [DataMember]
        public int FightingSkills { get; set; }
    }
}
```

These classes form the domain model for the *BackEnd* service.

Rename *ValuesController.cs* to *TeamsController.cs* and replace the contents with the following content:

```csharp
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    public class TeamsController : Controller
    {
        private const string DICTIONARY_TEAMS = "Teams";

        private readonly IReliableStateManager stateManager;

        public TeamsController(IReliableStateManager stateManager)
        {
            this.stateManager = stateManager;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var teamsDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, Team>>(DICTIONARY_TEAMS);
            var teams = new List<Team>();

            using (var tx = stateManager.CreateTransaction())
            {
                var enumerable = await teamsDictionary.CreateEnumerableAsync(tx);
                var enumerator = enumerable.GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync(CancellationToken.None))
                {
                    teams.Add(enumerator.Current.Value);
                }
            }

            return Json(teams);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            var teamsDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, Team>>(DICTIONARY_TEAMS);

            using (var tx = stateManager.CreateTransaction())
            {
                var team = await teamsDictionary.TryGetValueAsync(tx, name);
                if (team.HasValue)
                {
                    return Ok(team);
                }
            }

            return NotFound();
        }

        [HttpPut("{name}")]
        public async Task Put(string name, [FromBody]Team team)
        {
            var teamsDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, Team>>(DICTIONARY_TEAMS);

            using (var tx = stateManager.CreateTransaction())
            {
                await teamsDictionary.SetAsync(tx, name, team);
                await tx.CommitAsync();
            }
        }

        [HttpDelete("{name}")]
        public async Task Delete(string name)
        {
            var teamsDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, Team>>(DICTIONARY_TEAMS);

            using (var tx = stateManager.CreateTransaction())
            {
                await teamsDictionary.TryRemoveAsync(tx, name);
                await tx.CommitAsync();
            }
        }
    }
}
```

## Reliable Collections and the Reliable State Manager

```csharp
var teamsDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, Team>>(DICTIONARY_TEAMS);
```

[IReliableDictionary](https://msdn.microsoft.com/library/dn971511.aspx) is a dictionary implementation that you can use to reliably store state in the service. With Service Fabric and Reliable Collections, you can store data directly in your service without the need for an external persistent store.

Reliable Collections make your data highly available by *replicating* state across nodes, and Reliable Collections store your data to local disk on each replica. This means that everything that is stored in Reliable Collections must be serializable. By default, Reliable Collections use DataContract for serialization, so it's important to make sure that your types are supported by the Data Contract Serializer.

The Reliable State Manager manages Reliable Collections for you. You can simply ask the Reliable State Manager for a reliable collection by name at any time and at any place in your service. The Reliable State Manager ensures that you get a reference back. It is not recommended to save references to reliable collection instances in class member variables or properties. Special care must be taken to ensure that the reference is set to an instance at all times in the service lifecycle. The Reliable State Manager handles this work for you, and it's optimized for repeat visits.

## Configure partitioning

Service Fabric makes it easy to develop scalable stateful services by offering a first-class way to partition state (data) (see [here](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-concepts-partitioning) for more info on partitioning).

In this application you'll partition the teams data based on the first letter of the team name (so that will give you at most 26 partitions).

Open the *ApplicationManifest.xml* file in the Service Fabric hosting project.

Navigate to the *DefaultServices* element. Set the values of the **LowKey** and **HighKey** properties to respectively 0 and 25 (we'll use zero-based indexing here, A => 0, B => 1 etc).

```xml
<Service Name="BackEnd" ServicePackageActivationMode="ExclusiveProcess">
  <StatefulService ServiceTypeName="BackEndType" TargetReplicaSetSize="[BackEnd_TargetReplicaSetSize]" MinReplicaSetSize="[BackEnd_MinReplicaSetSize]">
    <UniformInt64Partition PartitionCount="[BackEnd_PartitionCount]" LowKey="0" HighKey="25" />
  </StatefulService>
</Service>
```

## Transactional and asynchronous operations

Consider the `Put` method in the `TeamsController`:

```csharp
[HttpPut("{name}")]
public async Task Put(string name, [FromBody]Team team)
{
    var teamsDictionary = await stateManager.GetOrAddAsync<IReliableDictionary<string, Team>>(DICTIONARY_TEAMS);

    using (var tx = stateManager.CreateTransaction())
    {
        await teamsDictionary.SetAsync(tx, name, team);
        await tx.CommitAsync();
    }
}
```

Reliable Collections have many of the same operations that their `System.Collections.Generic` and `System.Collections.Concurrent` counterparts do, except LINQ. Operations on Reliable Collections are asynchronous. This is because write operations with Reliable Collections perform I/O operations to replicate and persist data to disk.

Reliable Collection operations are `transactional`, so that you can keep state consistent across multiple Reliable Collections and operations. For example, you may dequeue a work item from a Reliable Queue, perform an operation on it, and save the result in a Reliable Dictionary, all within a single transaction. This is treated as an atomic operation, and it guarantees that either the entire operation will succeed or the entire operation will roll back. If an error occurs after you dequeue the item but before you save the result, the entire transaction is rolled back and the item remains in the queue for processing.
