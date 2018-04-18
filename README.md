# Global Azure Bootcamp 2018 Lab

Welcome to the Global Azure Bootcamp 2018: Avenger Edition! In this track, we'll work with Service Fabric to lift & shift S.H.I.E.L.D.'s aging HRM system to the cloud and extend its functionality by introducing some new services.

Consider:
- You only have 3 hours!
- Make it a team work excercise by teaming up with a buddy (or more)
- Use all the [good stuff out there, samples, tutorials](http://docs.microsoft.com/azure/service-fabric)

## Introductory Goals

1. [Set up your developer environment](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-get-started) for Service Fabric
2. [Deploy the containerized S.H.I.E.L.D. HRM application](labs/WorkshopPart1.md) on Azure
3. [Add a Stateful Service for the Team Assembler Back-End](labs/WorkshopPart2.md)
4. [Add a Stateless Service for the Team Assembler Front-End](labs/WorkshopPart3.md)

## Stretch Goals

- [Deploy a Docker Compose file on Service Fabric](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-docker-compose)
- Configure and set up a [CI/CD pipeline](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-tutorial-deploy-app-with-cicd-vsts)
- Configure and [set up a monitoring solution](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-tutorial-monitoring-aspnet) with Service Fabric
- Service to service communication
    - Use [API Management](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-api-management-overview) with Service Fabric 
    -  Use [Traefik](https://github.com/jjcollinge/traefik-on-service-fabric) on Service Fabric
- Run [chaos tests](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-controlled-chaos) on the .NET Reliable Services sample
- Configure your cluster for [dynamic scale](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-cluster-scale-up-down)
