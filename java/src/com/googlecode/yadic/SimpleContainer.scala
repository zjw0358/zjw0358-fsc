package com.googlecode.yadic

import java.lang.Class
import java.util.HashMap

class SimpleContainer(missingHandler: (Class[_]) => Object) extends Container {
  def this() = this ((aClass:Class[_]) => {throw new ContainerException(aClass.getName + " not found in container")})

  val activators = new HashMap[Class[_], () => Object]

  def resolve(aClass: Class[_]): Object = {
    activators.get(aClass) match {
      case null => missingHandler(aClass)
      case activator: (() => Object) => {
        val instance: Object = activator()
        activators.put(aClass, () => instance)
        instance
      }
    }
  }

  def add(concrete: Class[_]): Unit = add(concrete, () => createInstance(concrete) )

  def add(interface: Class[_], concrete: Class[_]): Unit = add(interface, () => createInstance(concrete) )

  def add(aClass: Class[_], activator: () => Object): Unit = {
    if( activators.containsKey(aClass)){
      throw new ContainerException(aClass.getName + " already added to container")
    }
    activators.put(aClass, activator)
  }

  def decorate(interface: Class[_], concrete: Class[_]): Unit = {
    val existing = resolve(interface)
    activators.put(interface, () => createInstance(concrete, (aClass: Class[_]) => {
      if(aClass.equals(interface)) existing else resolve(aClass)
    }))
  }

  def createInstance(aClass: Class[_]): Object = createInstance(aClass, resolve(_))

  def createInstance(aClass: Class[_], resolver: (Class[_]) => Object ): Object = {
    val constructor = aClass.getConstructors.toList.sort(_.getParameterTypes.length > _.getParameterTypes.length).head
    val instances = constructor.getParameterTypes.map( resolver(_) )
    return constructor.newInstance(instances: _*).asInstanceOf[Object]
  }

}