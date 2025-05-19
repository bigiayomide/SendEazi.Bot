// using System.Threading.Tasks;
// using Bot.Core.StateMachine.Helpers;
// using Bot.Shared.DTOs;
// using FluentAssertions;
// using MassTransit;
// using MassTransit.TestFramework;
// using MassTransit.Testing;
// using Microsoft.Extensions.DependencyInjection;
// using Xunit;
//
// namespace Bot.Tests.Helpers;
//
// public class ErrorPublishTests
// {
//     [Fact]
//     public async Task PublishFail_Should_Publish_Message_With_Reason_And_CorrelationId()
//     {
//         await using var provider = new ServiceCollection()
//             .AddMassTransitTestHarness()
//             .BuildServiceProvider(true);
//
//         var harness = provider.GetRequiredService<ITestHarness>();
//         await harness.Start();
//
//         var ctx = new TestConsumeContext(provider, harness.Bus);
//         var id = NewId.NextGuid();
//         ctx.CorrelationId = id;
//
//         const string reason = "boom";
//         await ctx.PublishFail<SignupFailed>(reason);
//
//         var published = await harness.Published.Any<SignupFailed>(x =>
//             x.Context.Message.CorrelationId == id && x.Context.Message.Reason == reason);
//         published.Should().BeTrue();
//     }
// }

