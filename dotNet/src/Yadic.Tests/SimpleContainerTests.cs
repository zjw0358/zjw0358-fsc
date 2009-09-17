using Yadic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using System.Threading;
using System.Collections.Generic;

namespace Container.Tests
{
    [TestFixture]
    public class ContainerTests
    {
        [Test]
        public void ShouldOnlyCallCreationLambdaOnceEvenFromDifferentThreads()
        {
            int count = 0;
            IContainer container = new SimpleContainer();
            
            container.Add<IThing>(() => {
                count++;
                Thread.Sleep(10);
                return new ThingWithNoDependencies();
            });

            IThing[] results = new IThing[2];
            ThreadPool.QueueUserWorkItem( (ignore) => results[0] = container.Resolve<IThing>() );
            ThreadPool.QueueUserWorkItem( (ignore) => results[1] = container.Resolve<IThing>() );

            Thread.Sleep(50);

            Assert.That(count, Is.EqualTo(1));
            Assert.AreSame(results[0],results[1]);
        }

	[Test]
  	public void ShouldResolveUsingConstructorWithMostDependenciesThatIsSatisfiable()
	{
    	    IContainer container = new SimpleContainer();
    	    container.Add<MyThingWithReverseConstructor>();

	    var myThing = container.Resolve<MyThingWithReverseConstructor>();

	    Assert.That(myThing.Dependency, Is.Null);
	}

        [Test]
        public void ShouldChainContainersThroughMissingAction()
        {
            IContainer parent = new SimpleContainer();
            parent.Add<IThing, ThingWithNoDependencies>();

            IContainer child = new SimpleContainer(parent.Resolve);

            IThing thing = child.Resolve<IThing>();

            Assert.That(thing, Is.InstanceOfType(typeof(ThingWithNoDependencies)));
        }

        [Test]
        public void ShouldResolveByType()
        {
            IContainer container = new SimpleContainer();
            container.Add<IThing, ThingWithNoDependencies>();

            object thing = container.Resolve(typeof (IThing));

            Assert.That(thing, Is.InstanceOfType(typeof(ThingWithNoDependencies)));
        }

        [Test]
        public void ShouldCallMissingMethodWhenItemNotFound()
        {
            bool wasCalled = false;
            IContainer container = new SimpleContainer(t =>
                                                           {
                                                               wasCalled = true;
                                                               return null;
                                                           });
            container.Resolve<IThing>();

            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public void ShouldOnlyCallCreationLambdaOnce()
        {
            int count = 0;
            IContainer container = new SimpleContainer();
            
            container.Add<IThing>(() => {
                  count++;
                  return new ThingWithNoDependencies();
              });

            container.Resolve<IThing>();
            container.Resolve<IThing>();

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void ShouldDecorateAnExistingComponent()
        {
            IContainer container = new SimpleContainer();
            container.Add<IThing, ThingWithNoDependencies>();
            container.Decorate<IThing, DecoratedThing>();

            var thing = container.Resolve<IThing>();

            Assert.That(thing, Is.InstanceOfType(typeof(DecoratedThing)));
            Assert.That(thing.Dependency, Is.InstanceOfType(typeof(ThingWithNoDependencies)));
        }
        [Test]
        public void ShouldAddAndReolveByConcrete()
        {
            IContainer container = new SimpleContainer();
            container.Add<IThing>(() => new ThingWithNoDependencies());

            var thing = container.Resolve<IThing>();

            Assert.That(thing, Is.InstanceOfType(typeof(ThingWithNoDependencies)));
        }

        [Test]
        public void ShouldAddAndResolveByInterface()
        {
            IContainer container = new SimpleContainer();
            container.Add<IThing, ThingWithNoDependencies>();

            var thing = container.Resolve<IThing>();

            Assert.That(thing, Is.InstanceOfType(typeof(ThingWithNoDependencies)));
        }

        [Test]
        [ExpectedException(typeof(ContainerException))]
        public void ShouldThrowExceptionIfAddSameTypeTwice()
        {
            IContainer container = new SimpleContainer();
            container.Add<MyThing>();
            container.Add<MyThing>();
            Assert.Fail("Should have thrown exception");
        }

        [Test]
        [ExpectedException(typeof(ContainerException))]
        public void ResolveShouldThrowExceptionIfTypeNotInContainer()
        {
            IContainer container = new SimpleContainer();
            container.Resolve<MyThing>();
            Assert.Fail("Should have thrown exception");
        }

        [Test]
        public void ShouldAddAndResolveByClass()
        {
            IContainer container = new SimpleContainer();
            container.Add<ThingWithNoDependencies>();
            
            var result = container.Resolve<ThingWithNoDependencies>();
            
            Assert.That(result, Is.InstanceOfType(typeof(ThingWithNoDependencies)));
        }

        [Test]
        public void ResolveShouldReturnSameInstanceWhenCalledTwice()
        {
            IContainer container = new SimpleContainer();
            container.Add<ThingWithNoDependencies>();

            var result1 = container.Resolve<ThingWithNoDependencies>();
            var result2 = container.Resolve<ThingWithNoDependencies>();
            
            Assert.AreSame(result1,result2);
        }

        [Test]
        public void ShouldResolveDependencies()
        {
            IContainer container = new SimpleContainer();
            container.Add<MyDependency>();
            container.Add<ThingWithNoDependencies>();

            var myThing = container.Resolve<MyDependency>();

            Assert.That(myThing.Dependency, Is.InstanceOfType(typeof(ThingWithNoDependencies)));
        } 

        [Test]
        public void ShouldRecursivelyResolveDependencies()
        {
            IContainer container = new SimpleContainer();
            container.Add<MyThing>();
            container.Add<MyDependency>();
            container.Add<ThingWithNoDependencies>();

            var myThing = container.Resolve<MyThing>();

            Assert.That(myThing.Dependency, Is.InstanceOfType(typeof(MyDependency)));
            Assert.That(myThing.Dependency.Dependency, Is.InstanceOfType(typeof(ThingWithNoDependencies)));
        }

        [Test]
        public void ShouldResolveWithDependenciesInAnyOrder()
        {
            IContainer container = new SimpleContainer();
            container.Add<MyDependency>();
            container.Add<MyThing>();
            container.Add<ThingWithNoDependencies>();

            var myThing = container.Resolve<MyThing>();

            Assert.That(myThing.Dependency, Is.InstanceOfType(typeof(MyDependency)), "1st level Dependency was not fulfilled");
            Assert.That(myThing.Dependency.Dependency, Is.InstanceOfType(typeof(ThingWithNoDependencies)), "2nd level Dependency was not fulfiled");
        }

        [Test]
        public void ShouldResolveUsingConstructorWithMostDependencies()
        {
            IContainer container = new SimpleContainer();
            container.Add<MyThingWithReverseConstructor>();
            container.Add<ThingWithNoDependencies>();

            var myThing = container.Resolve<MyThingWithReverseConstructor>();

            Assert.That(myThing.Dependency, Is.Not.Null, "Wrong constructor was used");
            Assert.That(myThing.Dependency, Is.InstanceOfType(typeof(ThingWithNoDependencies)));
        }
    }

    internal class MyThingWithReverseConstructor
    {
        private readonly ThingWithNoDependencies _dependency;

        public MyThingWithReverseConstructor()
        {
        }

        public MyThingWithReverseConstructor(ThingWithNoDependencies dependency)
        {
            _dependency = dependency;
        }

        public ThingWithNoDependencies Dependency
        {
            get { return _dependency; }
        }
    }

    internal class MyThing
    {
        private readonly MyDependency _dependency;

        public MyThing(MyDependency dependency)
        {
            _dependency = dependency;
        }

        public MyDependency Dependency
        {
            get { return _dependency; }
        }
    }

    internal class MyDependency
    {
        private readonly ThingWithNoDependencies _dependency;

        public MyDependency(ThingWithNoDependencies dependency)
        {
            _dependency = dependency;
        }

        public ThingWithNoDependencies Dependency
        {
            get { return _dependency; }
        }
    }

    internal class ThingWithNoDependencies : IThing
    {
        public IThing Dependency
        {
            get { return null; }
        }
    }

    internal class DecoratedThing : IThing
    {
        private readonly IThing _wrapped;

        public DecoratedThing(IThing wrapped)
        {
            _wrapped = wrapped;
        }

        public IThing Dependency
        {
            get { return _wrapped; }
        }
    }

    internal interface IThing
    {
        IThing Dependency { get; }
    }
}
