using NetArchTest.Rules;
using NetSdrClientApp;
using System.Reflection;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        private static readonly Assembly AppAssembly = typeof(NetSdrClient).Assembly;

        [Test]
        public void Messages_ShouldNot_DependOn_Networking()
        {
            var result = Types.InAssembly(AppAssembly)
                .That().ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot().HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Messages layer must not reference the Networking layer directly.\n" +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }

        [Test]
        public void Networking_ShouldNot_DependOn_Messages()
        {
            var result = Types.InAssembly(AppAssembly)
                .That().ResideInNamespace("NetSdrClientApp.Networking")
                .ShouldNot().HaveDependencyOn("NetSdrClientApp.Messages")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Networking layer must not reference the Messages layer directly.\n" +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }

        [Test]
        public void NetSdrClient_ShouldNot_DependOn_ConcreteWrappers()
        {
            var result = Types.InAssembly(AppAssembly)
                .That().HaveNameMatching("^NetSdrClient$")
                .ShouldNot().HaveDependencyOnAny(
                    "NetSdrClientApp.Networking.TcpClientWrapper",
                    "NetSdrClientApp.Networking.UdpClientWrapper")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "NetSdrClient must depend on interfaces, not concrete wrapper implementations.\n" +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }

        [Test]
        public void Interfaces_ShouldReside_InNetworkingNamespace()
        {
            var result = Types.InAssembly(AppAssembly)
                .That().AreInterfaces()
                .Should().ResideInNamespace("NetSdrClientApp.Networking")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "All interfaces must reside in the NetSdrClientApp.Networking namespace.\n" +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }

        [Test]
        public void OnlyWrappers_ShouldReference_SystemNetSockets()
        {
            var result = Types.InAssembly(AppAssembly)
                .That().DoNotHaveNameEndingWith("Wrapper")
                .And().AreNotInterfaces()
                .ShouldNot().HaveDependencyOn("System.Net.Sockets")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True,
                "Only *Wrapper classes may reference System.Net.Sockets directly.\n" +
                "Failing types: " + string.Join(", ", result.FailingTypeNames ?? []));
        }
    }
}
