using System.Threading.Tasks;
using Asionyx.Library.Testing;
using NUnit.Framework;

[SetUpFixture]
public class IntegrationTestSetup
{
    public static SharedContainerManager ContainerManager;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        ContainerManager = new SharedContainerManager();
        await ContainerManager.StartContainerAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (ContainerManager != null)
        {
            await ContainerManager.DisposeAsync();
        }
    }
}
