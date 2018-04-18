# Implement the Team Assembler front end

The front-end will provide S.H.I.E.L.D. personnel the UI to configure teams.
The system will provide team insights by displaying the average team member scores for intelligence, strength, speed, durability, energy projection and fighting skills.

Clone or download the https://github.com/amolenk/globalazurebootcamp2018lab repository to get the front-end source code.

Copy the entire *FrontEnd* folder from the *src* folder to your solution folder (the same folder that contains the *BackEnd* project folder).

In the Visual Studio Solution Explorer, right-click on the **Solution** and select **Add -> Existing Project...**.

In the **Add Existing Project** dialog, select the *FrontEnd.csproj* file in the previously copied *FrontEnd* folder. Click **OK**.

The front-end project is now added to your solution, but it's not part of the Service Fabric application yet. For that you must add the service to the Service Fabric host project.

Expand the Service Fabric host project (e.g. "TeamAssemblerJohn") and right-click on **Services**. Select **Add -> Existing Service Fabric Service in Solution...**.

In the opened dialog, check the **FrontEnd** project. Click **OK**.

A dialog will pop-up warning that the *ApplicationManifest.xml* file may be updated. Click **OK**.

Open the *ApplicationManifest.xml* file and add the following settings to tell Service Fabric to create instances of the FrontEnd service:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ApplicationManifest xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" ApplicationTypeName="TestWhateverType" ApplicationTypeVersion="1.0.0" xmlns="http://schemas.microsoft.com/2011/01/fabric">
  <Parameters>
    ...
    <Parameter Name="FrontEnd_InstanceCount" DefaultValue="-1" />
  </Parameters>
  ...
  <ServiceManifestImport>
    <ServiceManifestRef ServiceManifestName="FrontEndPkg" ServiceManifestVersion="1.0.0" />
    <ConfigOverrides />
  </ServiceManifestImport>
  <DefaultServices>
    ...
    <Service Name="FrontEnd" ServicePackageActivationMode="ExclusiveProcess">
      <StatelessService ServiceTypeName="FrontEndType" InstanceCount="[FrontEnd_InstanceCount]">
        <SingletonPartition />
      </StatelessService>
    </Service>
  </DefaultServices>
</ApplicationManifest>
```

You can now build and redeploy your application to either the party cluster or your own Azure Service Fabric cluster. The front-end is available on port 80.
