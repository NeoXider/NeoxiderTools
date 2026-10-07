using Neo.Network;
using NUnit.Framework;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     The whole startup decision table of <see cref="NeoNetworkBootstrap"/> lives in a pure parser: no Unity, no scene,
    ///     no Mirror session.
    /// </summary>
    [TestFixture]
    public sealed class NeoStartupCommandLineTests
    {
        private static NeoStartupIntent Parse(params string[] args)
        {
            return NeoStartupCommandLine.Parse(args);
        }

        [Test]
        public void NoArguments_IsSolo_AndNothingIsNetworked()
        {
            NeoStartupIntent intent = Parse();
            Assert.That(intent.Mode, Is.EqualTo(NeoStartupMode.Solo));
            Assert.That(intent.IsNetworked, Is.False);
            Assert.That(intent.SimulatesLocally, Is.True);
            Assert.That(intent.SuppressesLocalPresentation, Is.False);
            Assert.That(NeoStartupCommandLine.Parse(null).Mode, Is.EqualTo(NeoStartupMode.Solo));
        }

        [Test]
        public void UnityOwnSwitches_AreIgnored_ButMarkTheProcessHeadless()
        {
            NeoStartupIntent intent = Parse("game.exe", "-screen-width", "800", "-logFile", "x.log", "-batchmode");
            Assert.That(intent.Mode, Is.EqualTo(NeoStartupMode.Solo));
            Assert.That(intent.Headless, Is.True);
            Assert.That(Parse("-nographics").Headless, Is.True);
            Assert.That(Parse("-host").Headless, Is.False);
        }

        [TestCase("-host", NeoStartupMode.Host)]
        [TestCase("-server", NeoStartupMode.DedicatedServer)]
        [TestCase("-client", NeoStartupMode.Client)]
        [TestCase("--server", NeoStartupMode.DedicatedServer)]
        [TestCase("-SERVER", NeoStartupMode.DedicatedServer)]
        [TestCase("-Host", NeoStartupMode.Host)]
        public void RoleSwitches_ResolveTheMode(string argument, NeoStartupMode expected)
        {
            Assert.That(Parse(argument).Mode, Is.EqualTo(expected));
        }

        [Test]
        public void RolePrecedence_IsHostThenServerThenClient()
        {
            Assert.That(Parse("-client", "-server", "-host").Mode, Is.EqualTo(NeoStartupMode.Host));
            Assert.That(Parse("-client", "-server").Mode, Is.EqualTo(NeoStartupMode.DedicatedServer));
            Assert.That(NeoStartupCommandLine.ResolveMode(false, false, true), Is.EqualTo(NeoStartupMode.Client));
            Assert.That(NeoStartupCommandLine.ResolveMode(false, false, false), Is.EqualTo(NeoStartupMode.Solo));
        }

        [Test]
        public void ModeFlags_DescribeWhoMayAuthorGameplayAndWhoRenders()
        {
            NeoStartupIntent host = Parse("-host");
            NeoStartupIntent dedicated = Parse("-server");
            NeoStartupIntent client = Parse("-client");
            Assert.That(host.IsAuthorityPeer, Is.True);
            Assert.That(host.SimulatesLocally, Is.True);
            Assert.That(host.SuppressesLocalPresentation, Is.False);
            Assert.That(dedicated.IsAuthorityPeer, Is.True);
            Assert.That(dedicated.SimulatesLocally, Is.False);
            Assert.That(dedicated.SuppressesLocalPresentation, Is.True);
            Assert.That(client.IsAuthorityPeer, Is.False);
            Assert.That(client.SimulatesLocally, Is.False);
        }

        [Test]
        public void ValueSwitches_AcceptSpaceEqualsAndDoubleDashForms()
        {
            NeoStartupIntent spaced = Parse("-client", "-address", "10.0.0.5", "-port", "7778", "-maxplayers", "12", "-name", "Neo");
            Assert.That(spaced.Address, Is.EqualTo("10.0.0.5"));
            Assert.That(spaced.Port, Is.EqualTo(7778));
            Assert.That(spaced.MaxPlayers, Is.EqualTo(12));
            Assert.That(spaced.DisplayName, Is.EqualTo("Neo"));

            NeoStartupIntent inline = Parse("-client", "-address=play.example.com", "--port=9000", "-name=Bob Smith");
            Assert.That(inline.Address, Is.EqualTo("play.example.com"));
            Assert.That(inline.Port, Is.EqualTo(9000));
            Assert.That(inline.DisplayName, Is.EqualTo("Bob Smith"));
        }

        [Test]
        public void DefaultsApply_WhenSwitchesAreMissing()
        {
            NeoStartupIntent intent = Parse("-client");
            Assert.That(intent.Address, Is.EqualTo(NeoStartupIntent.DefaultAddress));
            Assert.That(intent.HasPort, Is.False, "0 means: leave the transport's own port alone");
            Assert.That(intent.HasMaxPlayers, Is.False);
            Assert.That(intent.DisplayName, Is.Empty);

            NeoStartupDefaults defaults = new NeoStartupDefaults("game.example.com", 7777, 10, "Anon");
            NeoStartupIntent withDefaults = NeoStartupCommandLine.Parse(new[] { "-host" }, defaults);
            Assert.That(withDefaults.Address, Is.EqualTo("game.example.com"));
            Assert.That(withDefaults.Port, Is.EqualTo(7777));
            Assert.That(withDefaults.MaxPlayers, Is.EqualTo(10));
            Assert.That(withDefaults.DisplayName, Is.EqualTo("Anon"));
            Assert.That(withDefaults.HasPort, Is.True);

            NeoStartupIntent overridden = NeoStartupCommandLine.Parse(new[] { "-host", "-port", "8000" }, defaults);
            Assert.That(overridden.Port, Is.EqualTo(8000));
        }

        [Test]
        public void Port_IsClampedToAValidRange_AndGarbageIsIgnored()
        {
            Assert.That(Parse("-host", "-port", "70000").Port, Is.EqualTo(65535));
            Assert.That(Parse("-host", "-port", "0").HasPort, Is.False, "0 is not a port; the default stays");
            Assert.That(Parse("-host", "-port", "-5").Port, Is.EqualTo(1));
            Assert.That(Parse("-host", "-port", "abc").HasPort, Is.False);
            Assert.That(Parse("-host", "-port", "7778x").HasPort, Is.False);
            NeoStartupDefaults defaults = new NeoStartupDefaults(port: 7777);
            Assert.That(NeoStartupCommandLine.Parse(new[] { "-host", "-port", "abc" }, defaults).Port, Is.EqualTo(7777),
                "an unparsable value keeps the default");
        }

        [Test]
        public void MaxPlayers_NegativeBecomesOne()
        {
            Assert.That(Parse("-server", "-maxplayers", "-3").MaxPlayers, Is.EqualTo(1));
            Assert.That(Parse("-server", "-maxplayers", "16").MaxPlayers, Is.EqualTo(16));
        }

        [Test]
        public void AValueSwitchWithoutAValue_IsIgnored_AndDoesNotSwallowTheNextSwitch()
        {
            NeoStartupIntent trailing = Parse("-client", "-address");
            Assert.That(trailing.Mode, Is.EqualTo(NeoStartupMode.Client));
            Assert.That(trailing.Address, Is.EqualTo(NeoStartupIntent.DefaultAddress));

            NeoStartupIntent swallowed = Parse("-address", "-host", "-port", "7778");
            Assert.That(swallowed.Mode, Is.EqualTo(NeoStartupMode.Host), "-host must not be eaten as the address");
            Assert.That(swallowed.Address, Is.EqualTo(NeoStartupIntent.DefaultAddress));
            Assert.That(swallowed.Port, Is.EqualTo(7778));

            Assert.That(Parse("-client", "-address=").Address, Is.EqualTo(NeoStartupIntent.DefaultAddress));
        }

        [Test]
        public void UnknownSwitches_AreIgnored()
        {
            NeoStartupIntent intent = Parse("-host", "-fancy", "yes", "-quality=3", "positional", "");
            Assert.That(intent.Mode, Is.EqualTo(NeoStartupMode.Host));
            Assert.That(intent.CustomCount, Is.EqualTo(0));
        }

        [Test]
        public void SwitchNames_AreMatchedExactly_NotByPrefix()
        {
            Assert.That(Parse("-hostname").Mode, Is.EqualTo(NeoStartupMode.Solo));
            Assert.That(Parse("-servers").Mode, Is.EqualTo(NeoStartupMode.Solo));
            Assert.That(Parse("-clients").Mode, Is.EqualTo(NeoStartupMode.Solo));
            Assert.That(Parse("host").Mode, Is.EqualTo(NeoStartupMode.Solo), "a bare word without a dash is not a switch");
        }

        [Test]
        public void CustomSwitches_AreCollectedForTheGame()
        {
            string[] values = { "-botfill", "-suicide-after" };
            string[] flags = { "-autoplay" };
            NeoStartupIntent intent = NeoStartupCommandLine.Parse(
                new[] { "-server", "-botfill", "6", "-autoplay", "-suicide-after=2.5", "-other", "x" },
                default, values, flags);

            Assert.That(intent.Mode, Is.EqualTo(NeoStartupMode.DedicatedServer));
            Assert.That(intent.CustomCount, Is.EqualTo(3));
            Assert.That(intent.GetCustomInt("botfill", 4), Is.EqualTo(6));
            Assert.That(intent.GetCustomInt("-BotFill", 4), Is.EqualTo(6), "names are matched without dashes and case");
            Assert.That(intent.HasCustom("autoplay"), Is.True);
            Assert.That(intent.HasCustom("other"), Is.False);
            Assert.That(intent.GetCustomFloat("suicide-after", 0f), Is.EqualTo(2.5f));
            Assert.That(intent.GetCustomInt("missing", 42), Is.EqualTo(42));
            Assert.That(intent.TryGetCustom("autoplay", out string flagValue), Is.True);
            Assert.That(flagValue, Is.Empty);
        }

        [Test]
        public void ACustomValueThatIsNotANumber_FallsBack()
        {
            NeoStartupIntent intent = NeoStartupCommandLine.Parse(
                new[] { "-botfill", "many" }, default, new[] { "-botfill" }, null);
            Assert.That(intent.GetCustomInt("botfill", 4), Is.EqualTo(4));
            Assert.That(intent.GetCustomFloat("botfill", 1.5f), Is.EqualTo(1.5f));
        }

        [Test]
        public void FromUrlQuery_BuildsTokensForWebGl()
        {
            string[] tokens = NeoStartupCommandLine.FromUrlQuery(
                "https://play.example.com/game/?client&address=srv.example.com&port=7778&name=Bob%20Smith#top");
            Assert.That(tokens, Is.EqualTo(new[] { "-client", "-address=srv.example.com", "-port=7778", "-name=Bob Smith" }));

            NeoStartupIntent intent = NeoStartupCommandLine.Parse(tokens);
            Assert.That(intent.Mode, Is.EqualTo(NeoStartupMode.Client));
            Assert.That(intent.Address, Is.EqualTo("srv.example.com"));
            Assert.That(intent.Port, Is.EqualTo(7778));
            Assert.That(intent.DisplayName, Is.EqualTo("Bob Smith"));
        }

        [Test]
        public void FromUrlQuery_HandlesBareQueriesAndEmptyInput()
        {
            Assert.That(NeoStartupCommandLine.FromUrlQuery("client&port=1"), Is.EqualTo(new[] { "-client", "-port=1" }));
            Assert.That(NeoStartupCommandLine.FromUrlQuery("?client"), Is.EqualTo(new[] { "-client" }));
            Assert.That(NeoStartupCommandLine.FromUrlQuery("https://example.com/game"), Is.Empty, "no query, no tokens");
            Assert.That(NeoStartupCommandLine.FromUrlQuery(null), Is.Empty);
            Assert.That(NeoStartupCommandLine.FromUrlQuery(""), Is.Empty);
            Assert.That(NeoStartupCommandLine.FromUrlQuery("?&&=x&"), Is.Empty);
            Assert.That(NeoStartupCommandLine.FromUrlQuery("?name=a+b%zz"), Is.EqualTo(new[] { "-name=a b%zz" }),
                "plus is a space, a broken escape is kept literally");
        }

        [Test]
        public void WithMode_And_WithAddress_KeepEverythingElse()
        {
            NeoStartupIntent original = NeoStartupCommandLine.Parse(
                new[] { "-client", "-address", "a.example", "-port", "7000", "-maxplayers", "8", "-name", "N", "-flag" },
                default, null, new[] { "-flag" });

            NeoStartupIntent host = original.WithMode(NeoStartupMode.Host);
            Assert.That(host.Mode, Is.EqualTo(NeoStartupMode.Host));
            Assert.That(host.Address, Is.EqualTo("a.example"));
            Assert.That(host.Port, Is.EqualTo(7000));
            Assert.That(host.MaxPlayers, Is.EqualTo(8));
            Assert.That(host.DisplayName, Is.EqualTo("N"));
            Assert.That(host.HasCustom("flag"), Is.True);

            NeoStartupIntent moved = original.WithAddress("b.example");
            Assert.That(moved.Address, Is.EqualTo("b.example"));
            Assert.That(moved.Mode, Is.EqualTo(NeoStartupMode.Client));
        }

        [Test]
        public void Solo_FactoryUsesTheDefaults()
        {
            NeoStartupIntent solo = NeoStartupIntent.Solo(new NeoStartupDefaults("h", 5, 6, "me"));
            Assert.That(solo.Mode, Is.EqualTo(NeoStartupMode.Solo));
            Assert.That(solo.Address, Is.EqualTo("h"));
            Assert.That(solo.Port, Is.EqualTo(5));
            Assert.That(solo.DisplayName, Is.EqualTo("me"));
        }

        [Test]
        public void ToString_NamesTheDecision()
        {
            string text = Parse("-server", "-port", "7778").ToString();
            StringAssert.Contains("DedicatedServer", text);
            StringAssert.Contains("7778", text);
        }

        [Test]
        public void BootstrapResolve_IsTheSameParser()
        {
            Assert.That(NeoNetworkBootstrap.Resolve(new[] { "-host", "-port", "7778" }).Port, Is.EqualTo(7778));
            NeoStartupIntent intent = NeoNetworkBootstrap.Resolve(
                new[] { "-server", "-botfill", "3" }, new NeoStartupDefaults(), new[] { "-botfill" });
            Assert.That(intent.GetCustomInt("botfill", 0), Is.EqualTo(3));
        }
    }
}
