# Deploy a Service Fabric Windows container application on Azure

Running an existing application in a Windows container on a Service Fabric cluster doesn't require any changes to the application. This tutorial shows you how to deploy the pre-built S.H.I.E.L.D. HRM Docker container image in a Service Fabric application.

## Package a Docker image container with Visual Studio

The Service Fabric SDK and tools provide a service template to help you deploy a container to a Service Fabric cluster.

Start Visual Studio as "Administrator". Select **File > New > Project**.

Select **Service Fabric application** and give it a name. We will use "TeamAssembler" in these instructions, but it's better to choose something unique because we'll deploy the solution to a party cluster. Party clusters are a public, shared environment and therefor each application in the cluster must have a unique name. Enter the chosen name (e.g. "TeamAssemblerJohn") and click **OK**.

Select **Container** from the **Hosted Containers and Applications** templates.

In **Image Name**, enter "amolenk/shieldhrm", which contains the containerized S.H.I.E.L.D. HRM system based on the [microsoft WCF base image](https://hub.docker.com/r/microsoft/wcf/) and is hosted in Docker Hub.

Name your service "ShieldHRM", and click **OK**.

## Configure communication and container port-to-host port mapping

The service needs an endpoint for communication. For this lab, the containerized service listens on port 8080. In Solution Explorer, open *TeamAssembler/ApplicationPackageRoot/ShieldHRMPkg/ServiceManifest.xml*. Update the existing Endpoint in the ServiceManifest.xml file and add the protocol, port, and uri scheme:

```xml
<Resources>
    <Endpoints>
        <Endpoint Name="ShieldHRMTypeEndpoint" UriScheme="http" Port="8080" Protocol="http"/>
   </Endpoints>
</Resources>
```

Providing the `UriScheme` automatically registers the container endpoint with the Service Fabric Naming service for discoverability.

Configure the container port-to-host port mapping so that incoming requests to the service on port 8080 are mapped to port 83 on the container. This allows the solution to work on [Service Fabric Party Clusters](https://try.servicefabric.azure.com) which has a limited set of available ports. In Solution Explorer, open *TeamAssembler/ApplicationPackageRoot/ApplicationManifest.xml* and specify a `PortBinding` policy in `ContainerHostPolicies`. For this lab, `ContainerPort` is 83 and `EndpointRef` is "ShieldHRMTypeEndpoint" (the endpoint defined in the service manifest). 

```xml
<ServiceManifestImport>
...
  <ConfigOverrides />
  <Policies>
    <ContainerHostPolicies CodePackageRef="Code">
      <PortBinding ContainerPort="83" EndpointRef="ShieldHRMTypeEndpoint"/>
    </ContainerHostPolicies>
  </Policies>
</ServiceManifestImport>
```

## Register a DNS name for the service

We must [register a DNS name](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-dnsservice) for the service so that the front-end service will know where to find it.

Open the *ApplicationManifest.xml* file and locate the *DefaultServices* element. It should contain an inner *Service* element for the "ShieldHRM" service:

```xml
<Service Name="ShieldHRM" ServiceDnsName="shieldhrm.teamassembler" ServicePackageActivationMode="ExclusiveProcess">
  <StatelessService ServiceTypeName="ShieldHRMType" InstanceCount="[ShieldHRM_InstanceCount]">
    <SingletonPartition />
  </StatelessService>
</Service>
```

Add a *ServiceDnsName* property to the *Service* element and assign it the value of "shieldhrm.teamassembler":

```xml
<Service Name="ShieldHRM" ServicePackageActivationMode="ExclusiveProcess" ServiceDnsName="shieldhrm.teamassembler">
    ...
</Service>
```

## Create a cluster

To deploy the application to a cluster in Azure, you can join a party cluster. Party clusters are free, limited-time Service Fabric clusters hosted on Azure and run by the Service Fabric team where anyone can deploy applications and learn about the platform. The cluster uses a single self-signed certificate for-node-to node as well as client-to-node security. Party clusters support containers. If you decide to set up and use your own cluster, the cluster must be running on a SKU that supports containers (such as Windows Server 2016 Datacenter with Containers).

Sign in and [join a Windows cluster](https://try.servicefabric.azure.com). Download the PFX certificate to your computer by clicking the **PFX** link. Click the **How to connect to a secure Party cluster?** link and copy the certificate password. The certificate, certificate password, and the **Connection endpoint** value are used in following steps.

> There are a limited number of Party clusters available per hour. If you get an error when you try to sign up for a Party cluster, you can wait for a period and try again, or you can follow these steps in the [Deploy a .NET app](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-tutorial-deploy-app-to-party-cluster#deploy-the-sample-application) tutorial to create a Service Fabric cluster in your Azure subscription and deploy the application to it. The cluster created through Visual Studio supports containers.

On a Windows computer, install the PFX in *CurrentUser\My* certificate store.

```PowerShell
PS C:\mycertificates> Import-PfxCertificate -FilePath .\party-cluster-753829355-client-cert.pfx -CertStoreLocation Cert:\CurrentUser\My -Password (ConvertTo-SecureString 753829355 -AsPlainText -Force)


  PSParentPath: Microsoft.PowerShell.Security\Certificate::CurrentUser\My

Thumbprint                                Subject
----------                                -------
AED8C90EC2BEF38AB5A50E35E4524DC102C7045E  CN=win2433lfstwh9.westus.cloudapp.azure.com
```

Remember the thumbprint for the following step.

## Deploy the application to Azure using Visual Studio

Now that the application is ready, you can deploy it to a cluster directly from Visual Studio.

Right-click the **TeamAssembler** project in the Solution Explorer and choose **Publish**. The Publish dialog appears.

Copy the **Connection Endpoint** from the Party cluster page into the **Connection Endpoint** field. For example, `win2433lfstwh9.westus.cloudapp.azure.com:19000`.

Click **Advanced Connection Parameters** and verify the connection parameter information. *FindValue* and *ServerCertThumbprint* values must match the thumbprint of the certificate installed in the previous step.

Click **Publish**.

Open a browser and navigate to the **Service Fabric Explorer** URL specified in the Party cluster page. This will take you to the Service Fabric Explorer UI where you can monitor the progress of the deployment.

Once the deployment has completed and the containers are running, you can navigate to the WCF service.
Use the **Connection endpoint** specified in the Party cluster and prepend the scheme identifier,  `http://`, change the port to `8080`, and append the path `EmployeeService.svc`, to the URL. For example, http://win2433lfstwh9.westus.cloudapp.azure.com:8080/EmployeeService.svc. You should see the EmployeeService WCF page where you can browse the WSDL.

