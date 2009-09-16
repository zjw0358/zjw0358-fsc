package com.googlecode.yadic

import java.lang.Class
import java.util.HashMap

class SimpleContainer(missingHandler: (Class[_]) => Object) extends Container {
  def this() = this ((aClass:Class[_]) => {throw new ContainerException(aClass.getName + " not found in container")})

  val activators = new HashMap[Class[_], Activator]

  def resolve(aClass: Class[_]): Object = {
    activators.get(aClass) match {
      case null => missingHandler(aClass)
      case creator:Activator => creator.activate()
    }
  }

  def add(concrete: Class[_]): Unit = add(concrete, () => createInstance(concrete) )

  def add(interface: Class[_], concrete: Class[_]): Unit = add(interface, () => createInstance(concrete) )

  def add(aClass: Class[_], activator: () => Object): Unit = {
    activators.containsKey(aClass) match {
      case true => throw new ContainerException(aClass.getName + " already added to container")
      case false => activators.put(aClass, new LazyInstanceActivator(activator))
    }
  }

  def decorate(interface: Class[_], concrete: Class[_]): Unit = {
    val existing = activators.get(interface)
    activators.put(interface, new LazyInstanceActivator(() => createInstance(concrete, (aClass: Class[_]) => {
      if(aClass.equals(interface)) existing.activate() else resolve(aClass)
    })))
  }

  def createInstance(aClass: Class[_]): Object = createInstance(aClass, resolve(_))

  def createInstance(aClass: Class[_], resolver: (Class[_]) => Object ): Object = {
    val constructors = aClass.getConstructors.toList.sort(_.getParameterTypes.length > _.getParameterTypes.length)
    constructors.foreach( constructor => {
      try {
        val instances = constructor.getParameterTypes.map( resolver(_) )
        return constructor.newInstance(instances: _*).asInstanceOf[Object]
      } catch {
        case e:ContainerException =>
      }
    })
    throw new ContainerException(aClass.getName + " did not have all it's dependancies satisfied")
  }
}