using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.Services.Core.Environments.Editor")]

// Test assemblies
#if UNITY_INCLUDE_TESTS
[assembly: InternalsVisibleTo("Unity.Services.Core.EditorTests")]
[assembly: InternalsVisibleTo("Unity.Services.Core.EditorTests.Environments")]
[assembly: InternalsVisibleTo("Unity.Services.Core.TestUtils.EditorTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
#endif
