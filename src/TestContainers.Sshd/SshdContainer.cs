using DotNet.Testcontainers.Containers;
using JetBrains.Annotations;

namespace Testcontainers.Sshd;

/// <inheritdoc cref="DockerContainer" />
[PublicAPI]
public sealed class SshdContainer : DockerContainer
{
    public SshdContainer(SshdConfiguration configuration) : base(configuration)
    {
        
    }
}
