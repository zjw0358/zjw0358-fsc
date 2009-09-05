package com.googlecode.yadic

import org.hamcrest.CoreMatchers._
import org.junit.Assert.{assertThat, assertTrue, fail, assertSame}
import org.junit.{Test}
import yadic.SimpleContainerTest._

class SimpleContainerTest {
  @Test
  def shouldChainContainersThroughMissingAction {
    val parent = new SimpleContainer
    parent.add(classOf[Thing], classOf[ThingWithNoDependencies])

    val child = new SimpleContainer(parent.resolve)

    val thing = child.resolve(classOf[Thing])

    assertThat(thing, is(instanceOf(classOf[ThingWithNoDependencies])))
  }

  @Test
  def shouldResolveByType {
    val container = new SimpleContainer
    container.add(classOf[Thing], classOf[ThingWithNoDependencies])

    val thing = container.resolve(classOf[Thing])

    assertThat(thing, is(instanceOf(classOf[ThingWithNoDependencies])))
  }

  @Test
  def shouldCallMissingMethodWhenItemNotFound {
    var wasCalled = false
    val container = new SimpleContainer((_) =>
            {
              wasCalled = true
              return null
            })
    container.resolve(classOf[Thing])

    assertTrue(wasCalled)
  }

  @Test
  def shouldOnlyCallCreationLambdaOnce {
    var count = 0
    val container = new SimpleContainer

    container.add(classOf[Thing], () => {
      count = count + 1
      return new ThingWithNoDependencies
    })

    container.resolve(classOf[Thing])
    val thing = container.resolve(classOf[Thing])

    assertThat(count, is(equalTo(1)))
  }

  @Test
  def shouldDecorateAnExistingComponent {
    val container = new SimpleContainer
    container.add(classOf[Thing], classOf[ThingWithNoDependencies])
    container.decorate(classOf[Thing], classOf[DecoratedThing])

    var thing = container.resolve(classOf[Thing]).asInstanceOf[Thing]

    assertThat(thing, is(instanceOf(classOf[DecoratedThing])))
    assertThat(thing.dependency, is(instanceOf(classOf[ThingWithNoDependencies])))
  }

  @Test
  def shouldAddAndReolveByConcrete {
    val container = new SimpleContainer
    container.add(classOf[Thing], () => new ThingWithNoDependencies)

    var thing = container.resolve(classOf[Thing])

    assertThat(thing, is(instanceOf(classOf[ThingWithNoDependencies])))
  }

  @Test
  def shouldAddAndResolveByInterface {
    val container = new SimpleContainer
    container.add(classOf[Thing], classOf[ThingWithNoDependencies])

    var thing = container.resolve(classOf[Thing])

    assertThat(thing, is(instanceOf(classOf[ThingWithNoDependencies])))
  }

  @Test {val expected = classOf[ContainerException]}
  def shouldThrowExceptionIfAddSameTypeTwice {
    val container = new SimpleContainer
    container.add(classOf[MyThing])
    container.add(classOf[MyThing])
    fail("should have thrown exception")
  }

  @Test {val expected = classOf[ContainerException]}
  def resolveShouldThrowExceptionIfTypeNotInContainer {
    val container = new SimpleContainer
    container.resolve(classOf[MyThing])
    fail("should have thrown exception")
  }

  @Test
  def shouldAddAndResolveByClass {
    val container = new SimpleContainer
    container.add(classOf[ThingWithNoDependencies])

    var result = container.resolve(classOf[ThingWithNoDependencies])

    assertThat(result, is(instanceOf(classOf[ThingWithNoDependencies])))
  }

  @Test
  def ResolveshouldReturnSameInstanceWhenCalledTwice {
    val container = new SimpleContainer
    container.add(classOf[ThingWithNoDependencies])

    var result1 = container.resolve(classOf[ThingWithNoDependencies])
    var result2 = container.resolve(classOf[ThingWithNoDependencies])

    assertSame(result1, result2)
  }

  @Test
  def shouldResolveDependencies {
    val container = new SimpleContainer
    container.add(classOf[MyDependency])
    container.add(classOf[ThingWithNoDependencies])

    var myThing = container.resolve(classOf[MyDependency]).asInstanceOf[MyDependency]

    assertThat(myThing.dependency, is(instanceOf(classOf[ThingWithNoDependencies])))
  }

  @Test
  def shouldRecursivelyResolveDependencies {
    val container = new SimpleContainer
    container.add(classOf[MyThing])
    container.add(classOf[MyDependency])
    container.add(classOf[ThingWithNoDependencies])

    var myThing = container.resolve(classOf[MyThing]).asInstanceOf[MyThing]

    assertThat(myThing.dependency, is(instanceOf(classOf[MyDependency])))
    assertThat(myThing.dependency.dependency, is(instanceOf(classOf[ThingWithNoDependencies])))
  }

  @Test
  def shouldResolveWithDependenciesInAnyOrder {
    val container = new SimpleContainer
    container.add(classOf[MyDependency])
    container.add(classOf[MyThing])
    container.add(classOf[ThingWithNoDependencies])

    var myThing = container.resolve(classOf[MyThing]).asInstanceOf[MyThing]

    assertThat("1st level Dependency was not fulfilled", myThing.dependency, is(instanceOf(classOf[MyDependency])))
    assertThat("2nd level Dependency was not fulfiled", myThing.dependency.dependency, is(instanceOf(classOf[ThingWithNoDependencies])))
  }

  @Test
  def shouldResolveUsingConstructorWithMostDependencies {
    val container = new SimpleContainer
    container.add(classOf[MyThingWithReverseConstructor])
    container.add(classOf[ThingWithNoDependencies])

    var myThing: MyThingWithReverseConstructor = container.resolve(classOf[MyThingWithReverseConstructor]).asInstanceOf[MyThingWithReverseConstructor]

    assertThat("Wrong constructor was used", myThing.dependency, is(notNullValue(classOf[Thing])))
    assertThat(myThing.dependency, is(instanceOf(classOf[ThingWithNoDependencies])))
  }
}

object SimpleContainerTest {
  class MyThingWithReverseConstructor(val dependency: ThingWithNoDependencies) extends Thing {
    def this() = this (null)
  }

  class MyThing(val dependency: MyDependency) extends Thing

  class MyDependency(val dependency: ThingWithNoDependencies) extends Thing

  class ThingWithNoDependencies extends Thing {
    val dependency: Thing = null
  }

  class DecoratedThing(val dependency: Thing) extends Thing

  trait Thing {
    val dependency: Thing
  }
}