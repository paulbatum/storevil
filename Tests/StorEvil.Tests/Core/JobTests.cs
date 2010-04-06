using System.Collections.Generic;
using NUnit.Framework;
using Rhino.Mocks;
using StorEvil.Context;
using StorEvil.InPlace;
using StorEvil.Utility;

namespace StorEvil.Core.StorEvilJob_Specs
{
    [TestFixture]
    public class JobTests : TestBase
    {
        [Test]
        public void Properties_Should_Match_Constructor_Parameters()
        {
            var storyProvider = Fake<IStoryProvider>();
            var storyToContextMapper = Fake<IStoryContextFactory>();
            var testStoryHandler = Fake<IStoryHandler>();

            var job = new StorEvilJob(storyProvider, storyToContextMapper, testStoryHandler);

            job.StoryProvider
                .ShouldEqual(storyProvider);

            job.StoryContextFactory
                .ShouldEqual(storyToContextMapper);

            job.Handler
                .ShouldEqual(testStoryHandler);
        }

        [Test]
        public void Invokes_Handler_For_Single_Story_And_Context()
        {
            var story = new Story("test context", "summary", new List<IScenario>());
            var contextType = new StoryContext(typeof (object));

            var job = GetJobWithMockDependencies();

            job.StoryProvider.Stub(x => x.GetStories()).Return(new[] {story});

            job.StoryContextFactory.Stub(x => x.GetContextForStory(story))
                .Return(contextType);

            job.Run();
            job.Handler.AssertWasCalled(x => x.HandleStory(story));
        }

        [Test]
        public void Notifies_Handler_When_Finished()
        {
            var job = GetJobWithMockDependencies();

            job.StoryProvider.Stub(x => x.GetStories())
                .Return(new[] {new Story("test context", "summary", new List<IScenario>())});

            job.Run();

            job.Handler.AssertWasCalled(x => x.Finished());
        }

        private StorEvilJob GetJobWithMockDependencies()
        {
            return new StorEvilJob(
                Fake<IStoryProvider>(),
                Fake<IStoryContextFactory>(),
                Fake<IStoryHandler>());
        }
    }
}